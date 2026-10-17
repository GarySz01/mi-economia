using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiEconomia.Api.Auth;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Data;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Subcuentas;
using Npgsql;

namespace MiEconomia.Api.Controllers;

/// <summary>
/// Las subcuentas se listan dentro de cada cuenta (GET api/cuentas), con su saldo;
/// acá solo se crean, editan y borran.
/// </summary>
[ApiController]
[Authorize]
[Route("api/cuentas/{cuentaId:guid}/subcuentas")]
public class SubcuentasController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => User.GetUsuarioId();

    // Pertenecer al usuario se verifica a través de la cuenta.
    private IQueryable<Subcuenta> SubcuentasDe(Guid cuentaId) =>
        db.Subcuentas.Where(s => s.CuentaId == cuentaId && s.Cuenta.UsuarioId == UsuarioId);

    [HttpPost]
    [ProducesResponseType<SubcuentaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Crear(Guid cuentaId, GuardarSubcuentaRequest request)
    {
        var existeCuenta = await db.Cuentas.AnyAsync(c => c.Id == cuentaId && c.UsuarioId == UsuarioId);
        if (!existeCuenta)
            return NotFound();

        var subcuenta = new Subcuenta { CuentaId = cuentaId };
        if (await Aplicar(request, subcuenta, tieneMovimientos: false) is { } error)
            return error;

        db.Subcuentas.Add(subcuenta);
        if (!await GuardarCambios())
            return NombreDuplicado(subcuenta.Nombre);

        // No hay GET propio: la ubicación es la cuenta, que trae sus subcuentas.
        return Created($"/api/cuentas/{cuentaId}", SubcuentaResponse.Desde(subcuenta, 0));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<SubcuentaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid cuentaId, Guid id, GuardarSubcuentaRequest request)
    {
        var subcuenta = await SubcuentasDe(cuentaId).FirstOrDefaultAsync(s => s.Id == id);
        if (subcuenta is null)
            return NotFound();

        var (movimientos, transferencias) = await ContarUsos(id);
        if (await Aplicar(request, subcuenta, tieneMovimientos: movimientos + transferencias > 0) is { } error)
            return error;

        if (!await GuardarCambios())
            return NombreDuplicado(subcuenta.Nombre);

        var netos = await Saldos.MovimientosNetosAsync(db, UsuarioId, cuentaId);
        return Ok(SubcuentaResponse.Desde(subcuenta, netos.GetValueOrDefault(id)));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid cuentaId, Guid id)
    {
        var subcuenta = await SubcuentasDe(cuentaId).FirstOrDefaultAsync(s => s.Id == id);
        if (subcuenta is null)
            return NotFound();

        if (await db.Subcuentas.CountAsync(s => s.CuentaId == cuentaId) == 1)
            return Problem(
                title: "No se puede eliminar la única subcuenta: toda cuenta tiene que tener al menos una. Si ya no la usas, elimina la cuenta.",
                statusCode: StatusCodes.Status409Conflict);

        var (movimientos, transferencias) = await ContarUsos(id);
        if (movimientos > 0 || transferencias > 0)
        {
            var detalle = (movimientos, transferencias) switch
            {
                (> 0, > 0) => $"{movimientos} movimiento(s) y {transferencias} transferencia(s)",
                (> 0, _) => $"{movimientos} movimiento(s)",
                _ => $"{transferencias} transferencia(s)",
            };
            return Problem(
                title: $"No se puede eliminar la subcuenta porque tiene {detalle}. Elimínalos o pásalos a otra subcuenta primero.",
                statusCode: StatusCodes.Status409Conflict);
        }

        db.Subcuentas.Remove(subcuenta);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Valida el request y copia sus valores a la subcuenta. Devuelve el error a responder, o null si está todo bien.</summary>
    private async Task<IActionResult?> Aplicar(GuardarSubcuentaRequest request, Subcuenta subcuenta, bool tieneMovimientos)
    {
        var nombre = request.Nombre.Trim();
        if (decimal.Round(request.SaldoInicial, 2) != request.SaldoInicial)
            ModelState.AddModelError(nameof(request.SaldoInicial), "El saldo inicial puede tener como máximo 2 decimales.");

        // Cambiar la moneda cambiaría el significado de todos los montos ya cargados.
        if (tieneMovimientos && request.Moneda != subcuenta.Moneda)
            ModelState.AddModelError(nameof(request.Moneda),
                "No se puede cambiar la moneda porque la subcuenta ya tiene movimientos o transferencias.");

        // Sin distinguir mayúsculas, igual que las cuentas.
        var enUso = await db.Subcuentas.AnyAsync(s =>
            s.CuentaId == subcuenta.CuentaId && s.Nombre.ToLower() == nombre.ToLower() && s.Id != subcuenta.Id);
        if (enUso)
            return NombreDuplicado(nombre);

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        subcuenta.Nombre = nombre;
        subcuenta.Moneda = request.Moneda;
        subcuenta.SaldoInicial = request.SaldoInicial;
        return null;
    }

    private async Task<(int Movimientos, int Transferencias)> ContarUsos(Guid subcuentaId) =>
    (
        await db.Movimientos.CountAsync(m => m.SubcuentaId == subcuentaId),
        await db.Transferencias.CountAsync(t => t.SubcuentaOrigenId == subcuentaId || t.SubcuentaDestinoId == subcuentaId)
    );

    /// <summary>Devuelve false si la base rechazó el cambio por nombre duplicado (dos requests a la vez).</summary>
    private async Task<bool> GuardarCambios()
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    private IActionResult NombreDuplicado(string nombre)
    {
        ModelState.AddModelError(nameof(GuardarSubcuentaRequest.Nombre), $"Esta cuenta ya tiene una subcuenta llamada '{nombre}'.");
        return ValidationProblem(ModelState);
    }
}

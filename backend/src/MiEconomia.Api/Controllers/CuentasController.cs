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

[ApiController]
[Authorize]
[Route("api/cuentas")]
public class CuentasController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => User.GetUsuarioId();

    private IQueryable<Cuenta> MisCuentas => db.Cuentas.Where(c => c.UsuarioId == UsuarioId);

    /// <summary>Todas las cuentas con sus subcuentas y su saldo actual.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<CuentaResponse>> Listar()
    {
        var cuentas = await MisCuentas.Include(c => c.Subcuentas).AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();
        var netos = await Saldos.MovimientosNetosAsync(db, UsuarioId);
        return cuentas.Select(c => CuentaResponse.Desde(c, netos)).ToList();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CuentaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id)
    {
        var cuenta = await MisCuentas.Include(c => c.Subcuentas).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (cuenta is null)
            return NotFound();

        return Ok(await ConSaldo(cuenta));
    }

    [HttpPost]
    [ProducesResponseType<CuentaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(CrearCuentaRequest request)
    {
        var nombre = request.Nombre.Trim();
        if (decimal.Round(request.SaldoInicial, 2) != request.SaldoInicial)
            ModelState.AddModelError(nameof(request.SaldoInicial), "El saldo inicial puede tener como máximo 2 decimales.");
        if (await NombreEnUso(nombre, excluirId: null))
            return NombreDuplicado(nombre);
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var cuenta = new Cuenta
        {
            UsuarioId = UsuarioId,
            Nombre = nombre,
            Tipo = request.Tipo!.Value,
            Color = request.Color,
            Subcuentas =
            [
                new Subcuenta
                {
                    Nombre = SubcuentaPrincipal.Nombre,
                    Moneda = request.Moneda ?? Monedas.PorDefecto,
                    SaldoInicial = request.SaldoInicial,
                },
            ],
        };
        db.Cuentas.Add(cuenta);
        if (!await GuardarCambios())
            return NombreDuplicado(nombre);

        // Una cuenta nueva no tiene movimientos: el saldo es el inicial.
        return CreatedAtAction(nameof(Obtener), new { id = cuenta.Id }, CuentaResponse.Desde(cuenta, new Dictionary<Guid, decimal>()));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CuentaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid id, ActualizarCuentaRequest request)
    {
        var cuenta = await MisCuentas.Include(c => c.Subcuentas).FirstOrDefaultAsync(c => c.Id == id);
        if (cuenta is null)
            return NotFound();

        var nombre = request.Nombre.Trim();
        if (await NombreEnUso(nombre, excluirId: id))
            return NombreDuplicado(nombre);

        cuenta.Nombre = nombre;
        cuenta.Tipo = request.Tipo!.Value;
        cuenta.Color = request.Color;

        if (!await GuardarCambios())
            return NombreDuplicado(nombre);

        return Ok(await ConSaldo(cuenta));
    }

    /// <summary>Se borra junto con sus subcuentas, siempre que no tenga movimientos ni transferencias.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var cuenta = await MisCuentas.FirstOrDefaultAsync(c => c.Id == id);
        if (cuenta is null)
            return NotFound();

        var movimientos = await db.Movimientos.CountAsync(m => m.CuentaId == id);
        var transferencias = await db.Transferencias.CountAsync(t => t.CuentaOrigenId == id || t.CuentaDestinoId == id);
        if (movimientos > 0 || transferencias > 0)
        {
            var detalle = (movimientos, transferencias) switch
            {
                (> 0, > 0) => $"{movimientos} movimiento(s) y {transferencias} transferencia(s)",
                (> 0, _) => $"{movimientos} movimiento(s)",
                _ => $"{transferencias} transferencia(s)",
            };
            return Problem(
                title: $"No se puede eliminar la cuenta porque tiene {detalle}. Elimínalos o pásalos a otra cuenta primero.",
                statusCode: StatusCodes.Status409Conflict);
        }

        db.Cuentas.Remove(cuenta);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Sin distinguir mayúsculas: "Banco" y "banco" cuentan como la misma cuenta.
    private Task<bool> NombreEnUso(string nombre, Guid? excluirId) =>
        MisCuentas.AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower() && c.Id != excluirId);

    private async Task<CuentaResponse> ConSaldo(Cuenta cuenta) =>
        CuentaResponse.Desde(cuenta, await Saldos.MovimientosNetosAsync(db, UsuarioId, cuenta.Id));

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
        ModelState.AddModelError(nameof(CrearCuentaRequest.Nombre), $"Ya tienes una cuenta llamada '{nombre}'.");
        return ValidationProblem(ModelState);
    }
}

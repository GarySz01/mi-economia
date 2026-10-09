using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiEconomia.Api.Auth;
using MiEconomia.Api.Data;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;
using MiEconomia.Api.Transferencias;

namespace MiEconomia.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transferencias")]
public class TransferenciasController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => User.GetUsuarioId();

    private IQueryable<Transferencia> MisTransferencias =>
        db.Transferencias
            .Include(t => t.CuentaOrigen)
            .Include(t => t.CuentaDestino)
            .Where(t => t.UsuarioId == UsuarioId);

    [HttpGet]
    [ProducesResponseType<PaginaResponse<TransferenciaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar([FromQuery] TransferenciasFiltro filtro)
    {
        if (filtro.Desde > filtro.Hasta)
        {
            ModelState.AddModelError(nameof(filtro.Desde), "La fecha 'desde' no puede ser posterior a 'hasta'.");
            return ValidationProblem(ModelState);
        }

        var query = MisTransferencias.AsNoTracking();
        if (filtro.Desde is not null)
            query = query.Where(t => t.Fecha >= filtro.Desde);
        if (filtro.Hasta is not null)
            query = query.Where(t => t.Fecha <= filtro.Hasta);
        if (filtro.CuentaId is not null)
            query = query.Where(t => t.CuentaOrigenId == filtro.CuentaId || t.CuentaDestinoId == filtro.CuentaId);

        var total = await query.CountAsync();
        var transferencias = await query
            .OrderByDescending(t => t.Fecha)
            .ThenByDescending(t => t.CreadoEn)
            .Skip((filtro.Pagina - 1) * filtro.Tamanio)
            .Take(filtro.Tamanio)
            .ToListAsync();

        return Ok(new PaginaResponse<TransferenciaResponse>(
            transferencias.Select(TransferenciaResponse.Desde).ToList(), total, filtro.Pagina, filtro.Tamanio));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransferenciaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id)
    {
        var transferencia = await MisTransferencias.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        return transferencia is null ? NotFound() : Ok(TransferenciaResponse.Desde(transferencia));
    }

    [HttpPost]
    [ProducesResponseType<TransferenciaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(GuardarTransferenciaRequest request)
    {
        var transferencia = new Transferencia { UsuarioId = UsuarioId };
        if (await Aplicar(request, transferencia) is { } error)
            return error;

        db.Transferencias.Add(transferencia);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = transferencia.Id }, TransferenciaResponse.Desde(transferencia));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<TransferenciaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarTransferenciaRequest request)
    {
        var transferencia = await MisTransferencias.FirstOrDefaultAsync(t => t.Id == id);
        if (transferencia is null)
            return NotFound();

        if (await Aplicar(request, transferencia) is { } error)
            return error;

        await db.SaveChangesAsync();
        return Ok(TransferenciaResponse.Desde(transferencia));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var borrados = await db.Transferencias
            .Where(t => t.Id == id && t.UsuarioId == UsuarioId)
            .ExecuteDeleteAsync();
        return borrados == 0 ? NotFound() : NoContent();
    }

    /// <summary>Valida el request y copia sus valores a la transferencia. Devuelve el error a responder, o null si está todo bien.</summary>
    private async Task<IActionResult?> Aplicar(GuardarTransferenciaRequest request, Transferencia transferencia)
    {
        var monto = request.Monto!.Value;
        if (decimal.Round(monto, 2) != monto)
            ModelState.AddModelError(nameof(request.Monto), "El monto puede tener como máximo 2 decimales.");

        // Las dos cuentas tienen que ser del usuario; si son de otro, se responde como si no existieran.
        var ids = new[] { request.CuentaOrigenId!.Value, request.CuentaDestinoId!.Value };
        var cuentas = await db.Cuentas
            .Where(c => ids.Contains(c.Id) && c.UsuarioId == UsuarioId)
            .ToDictionaryAsync(c => c.Id);
        var origen = cuentas.GetValueOrDefault(ids[0]);
        var destino = cuentas.GetValueOrDefault(ids[1]);

        if (origen is null)
            ModelState.AddModelError(nameof(request.CuentaOrigenId), "La cuenta de origen no existe.");
        if (destino is null)
            ModelState.AddModelError(nameof(request.CuentaDestinoId), "La cuenta de destino no existe.");
        else if (destino.Id == origen?.Id)
            ModelState.AddModelError(nameof(request.CuentaDestinoId), "La cuenta de destino tiene que ser distinta de la de origen.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        transferencia.CuentaOrigen = origen!;
        transferencia.CuentaOrigenId = origen!.Id;
        transferencia.CuentaDestino = destino!;
        transferencia.CuentaDestinoId = destino!.Id;
        transferencia.Monto = monto;
        transferencia.Fecha = request.Fecha!.Value;
        transferencia.Nota = string.IsNullOrWhiteSpace(request.Nota) ? null : request.Nota.Trim();
        return null;
    }
}

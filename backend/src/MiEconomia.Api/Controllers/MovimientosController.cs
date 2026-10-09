using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiEconomia.Api.Auth;
using MiEconomia.Api.Data;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;

namespace MiEconomia.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/movimientos")]
public class MovimientosController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => User.GetUsuarioId();

    private IQueryable<Movimiento> MisMovimientos =>
        db.Movimientos.Include(m => m.Categoria).Include(m => m.Cuenta).Where(m => m.UsuarioId == UsuarioId);

    [HttpGet]
    [ProducesResponseType<PaginaResponse<MovimientoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar([FromQuery] MovimientosFiltro filtro)
    {
        if (filtro.Desde > filtro.Hasta)
        {
            ModelState.AddModelError(nameof(filtro.Desde), "La fecha 'desde' no puede ser posterior a 'hasta'.");
            return ValidationProblem(ModelState);
        }

        var query = MisMovimientos.AsNoTracking();
        if (filtro.Desde is not null)
            query = query.Where(m => m.Fecha >= filtro.Desde);
        if (filtro.Hasta is not null)
            query = query.Where(m => m.Fecha <= filtro.Hasta);
        if (filtro.CategoriaId is not null)
            query = query.Where(m => m.CategoriaId == filtro.CategoriaId);
        if (filtro.CuentaId is not null)
            query = query.Where(m => m.CuentaId == filtro.CuentaId);
        if (filtro.Tipo is not null)
            query = query.Where(m => m.Categoria.Tipo == filtro.Tipo);

        var total = await query.CountAsync();
        var movimientos = await query
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.CreadoEn)
            .Skip((filtro.Pagina - 1) * filtro.Tamanio)
            .Take(filtro.Tamanio)
            .ToListAsync();

        return Ok(new PaginaResponse<MovimientoResponse>(
            movimientos.Select(MovimientoResponse.Desde).ToList(), total, filtro.Pagina, filtro.Tamanio));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<MovimientoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id)
    {
        var movimiento = await MisMovimientos.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        return movimiento is null ? NotFound() : Ok(MovimientoResponse.Desde(movimiento));
    }

    [HttpPost]
    [ProducesResponseType<MovimientoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(GuardarMovimientoRequest request)
    {
        var movimiento = new Movimiento { UsuarioId = UsuarioId };
        if (await Aplicar(request, movimiento) is { } error)
            return error;

        db.Movimientos.Add(movimiento);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = movimiento.Id }, MovimientoResponse.Desde(movimiento));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<MovimientoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid id, GuardarMovimientoRequest request)
    {
        var movimiento = await MisMovimientos.FirstOrDefaultAsync(m => m.Id == id);
        if (movimiento is null)
            return NotFound();

        if (await Aplicar(request, movimiento) is { } error)
            return error;

        await db.SaveChangesAsync();
        return Ok(MovimientoResponse.Desde(movimiento));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var borrados = await db.Movimientos
            .Where(m => m.Id == id && m.UsuarioId == UsuarioId)
            .ExecuteDeleteAsync();
        return borrados == 0 ? NotFound() : NoContent();
    }

    /// <summary>Valida el request y copia sus valores al movimiento. Devuelve el error a responder, o null si está todo bien.</summary>
    private async Task<IActionResult?> Aplicar(GuardarMovimientoRequest request, Movimiento movimiento)
    {
        var monto = request.Monto!.Value;
        if (decimal.Round(monto, 2) != monto)
            ModelState.AddModelError(nameof(request.Monto), "El monto puede tener como máximo 2 decimales.");

        // La categoría tiene que ser del mismo usuario; si es de otro, se responde como si no existiera.
        var categoria = await db.Categorias
            .FirstOrDefaultAsync(c => c.Id == request.CategoriaId && c.UsuarioId == UsuarioId);
        if (categoria is null)
            ModelState.AddModelError(nameof(request.CategoriaId), "La categoría no existe.");

        // Lo mismo con la cuenta.
        var cuenta = await db.Cuentas
            .FirstOrDefaultAsync(c => c.Id == request.CuentaId && c.UsuarioId == UsuarioId);
        if (cuenta is null)
            ModelState.AddModelError(nameof(request.CuentaId), "La cuenta no existe.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        movimiento.Categoria = categoria!;
        movimiento.CategoriaId = categoria!.Id;
        movimiento.Cuenta = cuenta!;
        movimiento.CuentaId = cuenta!.Id;
        movimiento.Monto = monto;
        movimiento.Fecha = request.Fecha!.Value;
        movimiento.Nota = string.IsNullOrWhiteSpace(request.Nota) ? null : request.Nota.Trim();
        return null;
    }
}

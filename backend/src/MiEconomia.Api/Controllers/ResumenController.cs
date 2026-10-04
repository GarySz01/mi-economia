using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiEconomia.Api.Auth;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Data;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Resumen;

namespace MiEconomia.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/resumen")]
public class ResumenController(AppDbContext db, TimeProvider tiempo) : ControllerBase
{
    private Guid UsuarioId => User.GetUsuarioId();

    private DateOnly Hoy => DateOnly.FromDateTime(tiempo.GetLocalNow().DateTime);

    /// <summary>Totales de ingresos, egresos y balance de un período, con el desglose por categoría.</summary>
    [HttpGet]
    [ProducesResponseType<ResumenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Obtener([FromQuery] ResumenFiltro filtro)
    {
        var inicioMes = new DateOnly(Hoy.Year, Hoy.Month, 1);
        var desde = filtro.Desde ?? inicioMes;
        var hasta = filtro.Hasta ?? inicioMes.AddMonths(1).AddDays(-1);
        if (desde > hasta)
        {
            ModelState.AddModelError(nameof(filtro.Desde), "La fecha 'desde' no puede ser posterior a 'hasta'.");
            return ValidationProblem(ModelState);
        }

        // La suma se hace en la base; solo vuelve una fila por categoría.
        var totales = await db.Movimientos
            .Where(m => m.UsuarioId == UsuarioId && m.Fecha >= desde && m.Fecha <= hasta)
            .GroupBy(m => m.CategoriaId)
            .Select(g => new { CategoriaId = g.Key, Total = g.Sum(m => m.Monto), Cantidad = g.Count() })
            .ToListAsync();

        var ids = totales.Select(t => t.CategoriaId).ToList();
        var categorias = await db.Categorias.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var totalPorTipo = totales
            .GroupBy(t => categorias[t.CategoriaId].Tipo)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Total));
        var ingresos = totalPorTipo.GetValueOrDefault(TipoMovimiento.Ingreso);
        var egresos = totalPorTipo.GetValueOrDefault(TipoMovimiento.Egreso);

        var porCategoria = totales
            .Select(t =>
            {
                var categoria = categorias[t.CategoriaId];
                var totalTipo = totalPorTipo[categoria.Tipo];
                var porcentaje = totalTipo == 0 ? 0 : Math.Round(t.Total * 100 / totalTipo, 1);
                return new TotalPorCategoria(CategoriaResponse.Desde(categoria), t.Total, t.Cantidad, porcentaje);
            })
            .OrderBy(t => t.Categoria.Tipo)
            .ThenByDescending(t => t.Total)
            .ToList();

        return Ok(new ResumenResponse(
            desde, hasta, ingresos, egresos, ingresos - egresos, totales.Sum(t => t.Cantidad), porCategoria));
    }

    /// <summary>Ingresos, egresos y balance de cada mes, para el gráfico de evolución. Incluye los meses sin movimientos.</summary>
    [HttpGet("mensual")]
    [ProducesResponseType<IReadOnlyList<TotalMensual>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<TotalMensual>> Mensual([FromQuery] ResumenMensualFiltro filtro)
    {
        var referencia = filtro.Hasta ?? Hoy;
        var ultimoMes = new DateOnly(referencia.Year, referencia.Month, 1);
        var primerMes = ultimoMes.AddMonths(-(filtro.Meses - 1));
        var hasta = ultimoMes.AddMonths(1).AddDays(-1);

        var totales = await db.Movimientos
            .Where(m => m.UsuarioId == UsuarioId && m.Fecha >= primerMes && m.Fecha <= hasta)
            .GroupBy(m => new { m.Fecha.Year, m.Fecha.Month, m.Categoria.Tipo })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Tipo, Total = g.Sum(m => m.Monto) })
            .ToListAsync();

        return Enumerable.Range(0, filtro.Meses)
            .Select(i => primerMes.AddMonths(i))
            .Select(mes =>
            {
                decimal Total(TipoMovimiento tipo) => totales
                    .Where(t => t.Year == mes.Year && t.Month == mes.Month && t.Tipo == tipo)
                    .Sum(t => t.Total);
                var ingresos = Total(TipoMovimiento.Ingreso);
                var egresos = Total(TipoMovimiento.Egreso);
                return new TotalMensual(mes.Year, mes.Month, ingresos, egresos, ingresos - egresos);
            })
            .ToList();
    }
}

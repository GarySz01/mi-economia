using System.ComponentModel.DataAnnotations;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Cuentas;

namespace MiEconomia.Api.Resumen;

/// <summary>Si no se envían fechas, se usa el mes actual.</summary>
/// <param name="CuentaId">Solo los movimientos de esa cuenta. Las transferencias nunca entran en el resumen.</param>
/// <param name="SubcuentaId">Solo los movimientos de esa subcuenta.</param>
/// <param name="Moneda">Los montos no se convierten: el resumen es de una sola moneda. Por defecto, Monedas.PorDefecto.</param>
public record ResumenFiltro(
    DateOnly? Desde,
    DateOnly? Hasta,
    Guid? CuentaId,
    Guid? SubcuentaId,
    [MonedaValida] string? Moneda);

/// <param name="Hasta">Último mes incluido (se toma el mes de esta fecha). Por defecto, el mes actual.</param>
/// <param name="CuentaId">Solo los movimientos de esa cuenta.</param>
/// <param name="SubcuentaId">Solo los movimientos de esa subcuenta.</param>
/// <param name="Moneda">Igual que en <see cref="ResumenFiltro"/>.</param>
public record ResumenMensualFiltro(
    DateOnly? Hasta,
    Guid? CuentaId,
    Guid? SubcuentaId,
    [MonedaValida] string? Moneda,
    [Range(1, 36, ErrorMessage = "La cantidad de meses debe estar entre 1 y 36.")]
    int Meses = 6);

public record ResumenResponse(
    DateOnly Desde,
    DateOnly Hasta,
    string Moneda,
    decimal TotalIngresos,
    decimal TotalEgresos,
    decimal Balance,
    int CantidadMovimientos,
    IReadOnlyList<TotalPorCategoria> PorCategoria);

/// <param name="Porcentaje">Porcentaje sobre el total de su tipo (ingresos o egresos), de 0 a 100.</param>
public record TotalPorCategoria(CategoriaResponse Categoria, decimal Total, int Cantidad, decimal Porcentaje);

public record TotalMensual(int Anio, int Mes, decimal Ingresos, decimal Egresos, decimal Balance);

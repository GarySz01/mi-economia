using System.ComponentModel.DataAnnotations;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Cuentas;

/// <summary>
/// Crea la cuenta con su primera subcuenta ("Disponible", ver SubcuentaPrincipal),
/// que es la que recibe la moneda y el saldo inicial.
/// </summary>
public record CrearCuentaRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre puede tener hasta 50 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "El tipo de cuenta es obligatorio.")]
    TipoCuenta? Tipo,

    [Range(typeof(decimal), "-9999999999999999.99", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El saldo inicial es demasiado grande.")]
    decimal SaldoInicial,

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe tener el formato #RRGGBB.")]
    string? Color,

    // Sin moneda se usa Monedas.PorDefecto.
    [MonedaValida]
    string? Moneda = null);

/// <summary>A diferencia de las categorías, el tipo sí se puede cambiar. Los saldos se editan en cada subcuenta.</summary>
public record ActualizarCuentaRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre puede tener hasta 50 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "El tipo de cuenta es obligatorio.")]
    TipoCuenta? Tipo,

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe tener el formato #RRGGBB.")]
    string? Color);

/// <param name="Saldo">Saldo actual: saldo inicial + ingresos − gastos + transferencias recibidas − enviadas.</param>
public record SubcuentaResponse(Guid Id, string Nombre, string Moneda, decimal SaldoInicial, decimal Saldo)
{
    public static SubcuentaResponse Desde(Subcuenta s, decimal movimientosNetos) =>
        new(s.Id, s.Nombre, s.Moneda, s.SaldoInicial, s.SaldoInicial + movimientosNetos);
}

public record SaldoPorMoneda(string Moneda, decimal Saldo);

/// <param name="Subcuentas">Ordenadas por nombre.</param>
/// <param name="Saldos">Saldo total de la cuenta en cada moneda de sus subcuentas (los montos no se convierten).</param>
public record CuentaResponse(
    Guid Id,
    string Nombre,
    TipoCuenta Tipo,
    string? Color,
    IReadOnlyList<SubcuentaResponse> Subcuentas,
    IReadOnlyList<SaldoPorMoneda> Saldos)
{
    /// <summary>Requiere haber cargado las subcuentas.</summary>
    /// <param name="netos">Movimientos netos de cada subcuenta (ver <see cref="Saldos.MovimientosNetosAsync"/>).</param>
    public static CuentaResponse Desde(Cuenta c, IReadOnlyDictionary<Guid, decimal> netos)
    {
        var subcuentas = c.Subcuentas
            .OrderBy(s => s.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(s => SubcuentaResponse.Desde(s, netos.GetValueOrDefault(s.Id)))
            .ToList();
        var saldos = subcuentas
            .GroupBy(s => s.Moneda)
            .Select(g => new SaldoPorMoneda(g.Key, g.Sum(s => s.Saldo)))
            .OrderBy(s => s.Moneda)
            .ToList();
        return new(c.Id, c.Nombre, c.Tipo, c.Color, subcuentas, saldos);
    }
}

/// <summary>Datos de la cuenta que se incluyen en un movimiento o una transferencia (sin el saldo, que es caro de calcular).</summary>
public record CuentaBasicaResponse(Guid Id, string Nombre, TipoCuenta Tipo, string? Color)
{
    public static CuentaBasicaResponse Desde(Cuenta c) => new(c.Id, c.Nombre, c.Tipo, c.Color);
}

/// <summary>Datos de la subcuenta que se incluyen en un movimiento o una transferencia.</summary>
public record SubcuentaBasicaResponse(Guid Id, string Nombre, string Moneda)
{
    public static SubcuentaBasicaResponse Desde(Subcuenta s) => new(s.Id, s.Nombre, s.Moneda);
}

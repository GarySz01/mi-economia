using System.ComponentModel.DataAnnotations;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Cuentas;

/// <summary>Sirve para crear y para editar: a diferencia de las categorías, el tipo sí se puede cambiar.</summary>
public record GuardarCuentaRequest(
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
    string? Color);

/// <param name="Saldo">Saldo actual: saldo inicial + ingresos − gastos + transferencias recibidas − enviadas.</param>
public record CuentaResponse(
    Guid Id,
    string Nombre,
    TipoCuenta Tipo,
    string? Color,
    decimal SaldoInicial,
    decimal Saldo)
{
    public static CuentaResponse Desde(Cuenta c, decimal movimientosNetos) =>
        new(c.Id, c.Nombre, c.Tipo, c.Color, c.SaldoInicial, c.SaldoInicial + movimientosNetos);
}

/// <summary>Datos de la cuenta que se incluyen en un movimiento o una transferencia (sin el saldo, que es caro de calcular).</summary>
public record CuentaBasicaResponse(Guid Id, string Nombre, TipoCuenta Tipo, string? Color)
{
    public static CuentaBasicaResponse Desde(Cuenta c) => new(c.Id, c.Nombre, c.Tipo, c.Color);
}

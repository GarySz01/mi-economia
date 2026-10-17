using System.ComponentModel.DataAnnotations;

namespace MiEconomia.Api.Cuentas;

/// <summary>
/// Monedas que se pueden usar en una subcuenta (códigos ISO 4217).
/// Los montos nunca se convierten: los totales se muestran por separado para cada moneda.
/// </summary>
public static class Monedas
{
    /// <summary>La de las subcuentas que se crean sin indicar moneda.</summary>
    public const string PorDefecto = "UYU";

    /// <summary>UYI son las unidades indexadas (UI).</summary>
    public static readonly IReadOnlySet<string> Validas = new HashSet<string> { "UYU", "USD", "EUR", "ARS", "BRL", "UYI" };

    public const string MensajeInvalida = "La moneda no es válida. Usa UYU, USD, EUR, ARS, BRL o UYI.";
}

/// <summary>Valida que el texto sea una de <see cref="Monedas.Validas"/>. Null es válido (para eso está [Required]).</summary>
public class MonedaValidaAttribute : ValidationAttribute
{
    public MonedaValidaAttribute() : base(Monedas.MensajeInvalida) { }

    public override bool IsValid(object? value) => value is null || value is string s && Monedas.Validas.Contains(s);
}

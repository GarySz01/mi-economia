using System.ComponentModel.DataAnnotations;
using MiEconomia.Api.Cuentas;

namespace MiEconomia.Api.Subcuentas;

/// <summary>Sirve para crear y para editar: la cuenta a la que pertenece va en la ruta.</summary>
public record GuardarSubcuentaRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre puede tener hasta 50 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "La moneda es obligatoria.")]
    [MonedaValida]
    string Moneda,

    [Range(typeof(decimal), "-9999999999999999.99", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El saldo inicial es demasiado grande.")]
    decimal SaldoInicial);

/// <summary>La subcuenta que se crea junto con cada cuenta. Después se puede renombrar como cualquier otra.</summary>
public static class SubcuentaPrincipal
{
    public const string Nombre = "Disponible";
}

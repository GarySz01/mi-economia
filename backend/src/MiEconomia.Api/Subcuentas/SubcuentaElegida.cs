using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Subcuentas;

/// <summary>
/// Regla común de movimientos y transferencias: la cuenta es obligatoria y la subcuenta
/// solo hace falta indicarla si la cuenta tiene más de una.
/// </summary>
public static class SubcuentaElegida
{
    /// <summary>Requiere haber cargado las subcuentas de la cuenta.</summary>
    /// <returns>La subcuenta, o el mensaje de error para el campo de la subcuenta.</returns>
    public static (Subcuenta? Subcuenta, string? Error) De(Cuenta cuenta, Guid? subcuentaId)
    {
        if (subcuentaId is not null)
        {
            var subcuenta = cuenta.Subcuentas.FirstOrDefault(s => s.Id == subcuentaId);
            return subcuenta is null ? (null, "La subcuenta no pertenece a la cuenta elegida.") : (subcuenta, null);
        }

        return cuenta.Subcuentas.Count == 1
            ? (cuenta.Subcuentas.First(), null)
            : (null, $"Elige en qué subcuenta de {cuenta.Nombre} está el dinero.");
    }
}

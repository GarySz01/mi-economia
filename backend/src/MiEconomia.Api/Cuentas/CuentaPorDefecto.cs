using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Cuentas;

/// <summary>
/// Cuenta que se crea al registrarse, para poder cargar movimientos desde el primer momento.
/// Después se puede editar o borrar como cualquier otra.
/// </summary>
public static class CuentaPorDefecto
{
    public const string Nombre = "Efectivo";

    public static Cuenta Para(Guid usuarioId) => new()
    {
        UsuarioId = usuarioId,
        Nombre = Nombre,
        Tipo = TipoCuenta.Efectivo,
        Color = "#16A34A",
    };
}

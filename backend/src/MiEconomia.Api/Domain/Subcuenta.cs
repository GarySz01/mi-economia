namespace MiEconomia.Api.Domain;

/// <summary>
/// Parte de una cuenta con un destino propio: el dinero del día a día, un ahorro para un objetivo,
/// una inversión dentro del banco, la caja en dólares... Toda cuenta tiene al menos una.
/// Los movimientos y las transferencias siempre se registran en una subcuenta.
/// </summary>
public class Subcuenta
{
    public Guid Id { get; set; }
    public Guid CuentaId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Código ISO 4217 (ver Cuentas.Monedas). Todos los montos de la subcuenta están en esta moneda.</summary>
    public string Moneda { get; set; } = string.Empty;

    /// <summary>
    /// Saldo al empezar a usar la subcuenta en la app. Puede ser negativo (por ejemplo, la deuda de una tarjeta).
    /// El saldo actual no se guarda: se calcula sumando los movimientos y las transferencias.
    /// </summary>
    public decimal SaldoInicial { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public Cuenta Cuenta { get; set; } = null!;
}

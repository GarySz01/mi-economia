namespace MiEconomia.Api.Domain;

/// <summary>
/// Dinero que pasa de una cuenta a otra del mismo usuario (sacar del cajero, pagar la tarjeta...).
/// Cambia los saldos de las cuentas, pero no es un ingreso ni un gasto: no aparece en el resumen.
/// </summary>
public class Transferencia
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid CuentaOrigenId { get; set; }
    public Guid CuentaDestinoId { get; set; }

    /// <summary>Siempre positivo.</summary>
    public decimal Monto { get; set; }
    public DateOnly Fecha { get; set; }
    public string? Nota { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public Usuario Usuario { get; set; } = null!;
    public Cuenta CuentaOrigen { get; set; } = null!;
    public Cuenta CuentaDestino { get; set; } = null!;
}

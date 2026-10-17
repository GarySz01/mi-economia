namespace MiEconomia.Api.Domain;

/// <summary>
/// Dinero que pasa de una subcuenta a otra del mismo usuario (sacar del cajero, pagar la tarjeta,
/// apartar para un ahorro, comprar dólares...). Pueden ser de la misma cuenta o de cuentas distintas.
/// Cambia los saldos, pero no es un ingreso ni un gasto: no aparece en el resumen.
/// </summary>
public class Transferencia
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }

    // Las cuentas se guardan junto a las subcuentas para filtrar sin unir tablas;
    // siempre son las cuentas de esas subcuentas.
    public Guid CuentaOrigenId { get; set; }
    public Guid CuentaDestinoId { get; set; }
    public Guid SubcuentaOrigenId { get; set; }
    public Guid SubcuentaDestinoId { get; set; }

    /// <summary>Lo que sale del origen, en su moneda. Siempre positivo.</summary>
    public decimal Monto { get; set; }

    /// <summary>
    /// Lo que llega al destino, en su moneda, cuando es distinta de la del origen (por ejemplo, al comprar dólares).
    /// Null si las dos subcuentas tienen la misma moneda: llega lo mismo que sale.
    /// </summary>
    public decimal? MontoDestino { get; set; }
    public DateOnly Fecha { get; set; }
    public string? Nota { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public Usuario Usuario { get; set; } = null!;
    public Cuenta CuentaOrigen { get; set; } = null!;
    public Cuenta CuentaDestino { get; set; } = null!;
    public Subcuenta SubcuentaOrigen { get; set; } = null!;
    public Subcuenta SubcuentaDestino { get; set; } = null!;
}

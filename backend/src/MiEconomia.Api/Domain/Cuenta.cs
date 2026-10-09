namespace MiEconomia.Api.Domain;

/// <summary>Lugar donde está el dinero: efectivo, una cuenta bancaria, una tarjeta, etc.</summary>
public class Cuenta
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoCuenta Tipo { get; set; }

    /// <summary>
    /// Saldo al empezar a usar la cuenta en la app. Puede ser negativo (por ejemplo, la deuda de una tarjeta).
    /// El saldo actual no se guarda: se calcula sumando los movimientos y las transferencias.
    /// </summary>
    public decimal SaldoInicial { get; set; }

    /// <summary>Color en formato hexadecimal (#RRGGBB) para mostrar en la interfaz.</summary>
    public string? Color { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public Usuario Usuario { get; set; } = null!;
    public ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
}

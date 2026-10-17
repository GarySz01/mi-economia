namespace MiEconomia.Api.Domain;

/// <summary>
/// Lugar donde está el dinero: efectivo, una cuenta bancaria, una tarjeta, etc.
/// El dinero en sí está repartido en sus <see cref="Subcuentas"/>, cada una con su moneda y su saldo.
/// </summary>
public class Cuenta
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoCuenta Tipo { get; set; }

    /// <summary>Color en formato hexadecimal (#RRGGBB) para mostrar en la interfaz.</summary>
    public string? Color { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public Usuario Usuario { get; set; } = null!;
    public ICollection<Subcuenta> Subcuentas { get; set; } = new List<Subcuenta>();
    public ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
}

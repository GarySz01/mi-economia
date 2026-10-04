namespace MiEconomia.Api.Domain;

public class Categoria
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoMovimiento Tipo { get; set; }

    /// <summary>Color en formato hexadecimal (#RRGGBB) para mostrar en la interfaz.</summary>
    public string? Color { get; set; }
    public string? Icono { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public Usuario Usuario { get; set; } = null!;
    public ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
}

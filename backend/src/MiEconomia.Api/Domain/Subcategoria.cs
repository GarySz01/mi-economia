namespace MiEconomia.Api.Domain;

/// <summary>
/// Detalle opcional de una categoría (por ejemplo, "Netflix" dentro de "Suscripciones").
/// El tipo, el color y el ícono son los de su categoría.
/// </summary>
public class Subcategoria
{
    public Guid Id { get; set; }
    public Guid CategoriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public Categoria Categoria { get; set; } = null!;
    public ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
}

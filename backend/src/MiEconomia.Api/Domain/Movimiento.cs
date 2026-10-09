namespace MiEconomia.Api.Domain;

public class Movimiento
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid CategoriaId { get; set; }

    /// <summary>De dónde salió (egreso) o adónde entró (ingreso) el dinero.</summary>
    public Guid CuentaId { get; set; }

    /// <summary>Siempre positivo; si es ingreso o egreso lo define la categoría.</summary>
    public decimal Monto { get; set; }
    public DateOnly Fecha { get; set; }
    public string? Nota { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public Usuario Usuario { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
    public Cuenta Cuenta { get; set; } = null!;
}

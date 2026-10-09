using Microsoft.AspNetCore.Identity;

namespace MiEconomia.Api.Domain;

public class Usuario : IdentityUser<Guid>
{
    public string Nombre { get; set; } = string.Empty;
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
    public ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
    public ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
}

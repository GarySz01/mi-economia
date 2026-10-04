using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Categorias;

/// <summary>
/// Categorías que se crean al registrarse, para que el usuario no empiece con la app vacía.
/// Después puede editarlas o borrarlas como cualquier otra.
/// Los íconos son nombres de Lucide (https://lucide.dev/icons).
/// </summary>
public static class CategoriasPorDefecto
{
    private static readonly (string Nombre, TipoMovimiento Tipo, string Color, string Icono)[] Lista =
    [
        ("Sueldo",          TipoMovimiento.Ingreso, "#16A34A", "briefcase"),
        ("Trabajos extra",  TipoMovimiento.Ingreso, "#0D9488", "laptop"),
        ("Inversiones",     TipoMovimiento.Ingreso, "#2563EB", "trending-up"),
        ("Otros ingresos",  TipoMovimiento.Ingreso, "#65A30D", "circle-plus"),

        ("Supermercado",    TipoMovimiento.Egreso,  "#EA580C", "shopping-cart"),
        ("Vivienda",        TipoMovimiento.Egreso,  "#7C3AED", "house"),
        ("Servicios",       TipoMovimiento.Egreso,  "#CA8A04", "zap"),
        ("Transporte",      TipoMovimiento.Egreso,  "#0284C7", "bus"),
        ("Salud",           TipoMovimiento.Egreso,  "#DC2626", "heart-pulse"),
        ("Educación",       TipoMovimiento.Egreso,  "#4F46E5", "graduation-cap"),
        ("Ocio",            TipoMovimiento.Egreso,  "#DB2777", "party-popper"),
        ("Ropa",            TipoMovimiento.Egreso,  "#9333EA", "shirt"),
        ("Otros gastos",    TipoMovimiento.Egreso,  "#6B7280", "ellipsis"),
    ];

    public static IEnumerable<Categoria> Para(Guid usuarioId) =>
        Lista.Select(c => new Categoria
        {
            UsuarioId = usuarioId,
            Nombre = c.Nombre,
            Tipo = c.Tipo,
            Color = c.Color,
            Icono = c.Icono,
        });
}

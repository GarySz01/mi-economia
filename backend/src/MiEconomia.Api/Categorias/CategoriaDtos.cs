using System.ComponentModel.DataAnnotations;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Subcategorias;

namespace MiEconomia.Api.Categorias;

public record CrearCategoriaRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre puede tener hasta 50 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "El tipo es obligatorio (Ingreso o Egreso).")]
    TipoMovimiento? Tipo,

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe tener el formato #RRGGBB.")]
    string? Color,

    [StringLength(50, ErrorMessage = "El ícono puede tener hasta 50 caracteres.")]
    string? Icono);

/// <summary>El tipo no se puede cambiar: cambiaría el sentido de todos los movimientos de la categoría.</summary>
public record ActualizarCategoriaRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre puede tener hasta 50 caracteres.")]
    string Nombre,

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe tener el formato #RRGGBB.")]
    string? Color,

    [StringLength(50, ErrorMessage = "El ícono puede tener hasta 50 caracteres.")]
    string? Icono);

/// <summary>Datos de la categoría que se incluyen en un movimiento o en el resumen (sin las subcategorías).</summary>
public record CategoriaResponse(Guid Id, string Nombre, TipoMovimiento Tipo, string? Color, string? Icono)
{
    public static CategoriaResponse Desde(Categoria c) => new(c.Id, c.Nombre, c.Tipo, c.Color, c.Icono);
}

/// <summary>Lo que devuelven los endpoints de categorías. Requiere haber cargado las subcategorías.</summary>
public record CategoriaConSubcategoriasResponse(
    Guid Id,
    string Nombre,
    TipoMovimiento Tipo,
    string? Color,
    string? Icono,
    IReadOnlyList<SubcategoriaResponse> Subcategorias)
{
    public static CategoriaConSubcategoriasResponse Desde(Categoria c) =>
        new(c.Id, c.Nombre, c.Tipo, c.Color, c.Icono,
            c.Subcategorias.OrderBy(s => s.Nombre, StringComparer.CurrentCultureIgnoreCase).Select(SubcategoriaResponse.Desde).ToList());
}

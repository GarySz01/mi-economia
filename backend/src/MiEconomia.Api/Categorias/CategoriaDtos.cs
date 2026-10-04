using System.ComponentModel.DataAnnotations;
using MiEconomia.Api.Domain;

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

public record CategoriaResponse(Guid Id, string Nombre, TipoMovimiento Tipo, string? Color, string? Icono)
{
    public static CategoriaResponse Desde(Categoria c) => new(c.Id, c.Nombre, c.Tipo, c.Color, c.Icono);
}

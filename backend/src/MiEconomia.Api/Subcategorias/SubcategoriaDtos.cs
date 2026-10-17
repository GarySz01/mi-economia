using System.ComponentModel.DataAnnotations;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Subcategorias;

/// <summary>Sirve para crear y para editar: la categoría a la que pertenece va en la ruta.</summary>
public record GuardarSubcategoriaRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre puede tener hasta 50 caracteres.")]
    string Nombre);

public record SubcategoriaResponse(Guid Id, string Nombre)
{
    public static SubcategoriaResponse Desde(Subcategoria s) => new(s.Id, s.Nombre);
}

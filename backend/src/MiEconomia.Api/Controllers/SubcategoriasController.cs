using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiEconomia.Api.Auth;
using MiEconomia.Api.Data;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Subcategorias;
using Npgsql;

namespace MiEconomia.Api.Controllers;

/// <summary>
/// Las subcategorías se listan dentro de cada categoría (GET api/categorias);
/// acá solo se crean, editan y borran.
/// </summary>
[ApiController]
[Authorize]
[Route("api/categorias/{categoriaId:guid}/subcategorias")]
public class SubcategoriasController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => User.GetUsuarioId();

    // Pertenecer al usuario se verifica a través de la categoría.
    private IQueryable<Subcategoria> SubcategoriasDe(Guid categoriaId) =>
        db.Subcategorias.Where(s => s.CategoriaId == categoriaId && s.Categoria.UsuarioId == UsuarioId);

    [HttpPost]
    [ProducesResponseType<SubcategoriaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Crear(Guid categoriaId, GuardarSubcategoriaRequest request)
    {
        var existeCategoria = await db.Categorias.AnyAsync(c => c.Id == categoriaId && c.UsuarioId == UsuarioId);
        if (!existeCategoria)
            return NotFound();

        var nombre = request.Nombre.Trim();
        if (await NombreEnUso(categoriaId, nombre, excluirId: null))
            return NombreDuplicado(nombre);

        var subcategoria = new Subcategoria { CategoriaId = categoriaId, Nombre = nombre };
        db.Subcategorias.Add(subcategoria);

        if (!await GuardarCambios())
            return NombreDuplicado(nombre);

        // No hay GET propio: la ubicación es la categoría, que trae sus subcategorías.
        return Created($"/api/categorias/{categoriaId}", SubcategoriaResponse.Desde(subcategoria));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<SubcategoriaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid categoriaId, Guid id, GuardarSubcategoriaRequest request)
    {
        var subcategoria = await SubcategoriasDe(categoriaId).FirstOrDefaultAsync(s => s.Id == id);
        if (subcategoria is null)
            return NotFound();

        var nombre = request.Nombre.Trim();
        if (await NombreEnUso(categoriaId, nombre, excluirId: id))
            return NombreDuplicado(nombre);

        subcategoria.Nombre = nombre;

        if (!await GuardarCambios())
            return NombreDuplicado(nombre);

        return Ok(SubcategoriaResponse.Desde(subcategoria));
    }

    /// <summary>Los movimientos que tenía quedan en la categoría, sin subcategoría.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid categoriaId, Guid id)
    {
        var borradas = await SubcategoriasDe(categoriaId).Where(s => s.Id == id).ExecuteDeleteAsync();
        return borradas == 0 ? NotFound() : NoContent();
    }

    // Sin distinguir mayúsculas, igual que las categorías.
    private Task<bool> NombreEnUso(Guid categoriaId, string nombre, Guid? excluirId) =>
        db.Subcategorias.AnyAsync(s =>
            s.CategoriaId == categoriaId && s.Nombre.ToLower() == nombre.ToLower() && s.Id != excluirId);

    /// <summary>Devuelve false si la base rechazó el cambio por nombre duplicado (dos requests a la vez).</summary>
    private async Task<bool> GuardarCambios()
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    private IActionResult NombreDuplicado(string nombre)
    {
        ModelState.AddModelError(nameof(GuardarSubcategoriaRequest.Nombre), $"Esta categoría ya tiene una subcategoría llamada '{nombre}'.");
        return ValidationProblem(ModelState);
    }
}

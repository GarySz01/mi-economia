using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiEconomia.Api.Auth;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Data;
using MiEconomia.Api.Domain;
using Npgsql;

namespace MiEconomia.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categorias")]
public class CategoriasController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => User.GetUsuarioId();

    private IQueryable<Categoria> MisCategorias => db.Categorias.Where(c => c.UsuarioId == UsuarioId);

    [HttpGet]
    public async Task<IReadOnlyList<CategoriaResponse>> Listar([FromQuery] TipoMovimiento? tipo)
    {
        var query = MisCategorias.AsNoTracking();
        if (tipo is not null)
            query = query.Where(c => c.Tipo == tipo);

        var categorias = await query.OrderBy(c => c.Tipo).ThenBy(c => c.Nombre).ToListAsync();
        return categorias.Select(CategoriaResponse.Desde).ToList();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategoriaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid id)
    {
        var categoria = await MisCategorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return categoria is null ? NotFound() : Ok(CategoriaResponse.Desde(categoria));
    }

    [HttpPost]
    [ProducesResponseType<CategoriaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(CrearCategoriaRequest request)
    {
        var nombre = request.Nombre.Trim();
        var tipo = request.Tipo!.Value;

        if (await NombreEnUso(nombre, tipo, excluirId: null))
            return NombreDuplicado(nombre);

        var categoria = new Categoria
        {
            UsuarioId = UsuarioId,
            Nombre = nombre,
            Tipo = tipo,
            Color = request.Color,
            Icono = request.Icono,
        };
        db.Categorias.Add(categoria);

        if (!await GuardarCambios())
            return NombreDuplicado(nombre);

        return CreatedAtAction(nameof(Obtener), new { id = categoria.Id }, CategoriaResponse.Desde(categoria));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CategoriaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid id, ActualizarCategoriaRequest request)
    {
        var categoria = await MisCategorias.FirstOrDefaultAsync(c => c.Id == id);
        if (categoria is null)
            return NotFound();

        var nombre = request.Nombre.Trim();
        if (await NombreEnUso(nombre, categoria.Tipo, excluirId: id))
            return NombreDuplicado(nombre);

        categoria.Nombre = nombre;
        categoria.Color = request.Color;
        categoria.Icono = request.Icono;

        if (!await GuardarCambios())
            return NombreDuplicado(nombre);

        return Ok(CategoriaResponse.Desde(categoria));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var categoria = await MisCategorias.FirstOrDefaultAsync(c => c.Id == id);
        if (categoria is null)
            return NotFound();

        var cantidad = await db.Movimientos.CountAsync(m => m.CategoriaId == id);
        if (cantidad > 0)
            return Problem(
                title: $"No se puede eliminar la categoría porque tiene {cantidad} movimiento(s). Elimínalos o cámbialos de categoría primero.",
                statusCode: StatusCodes.Status409Conflict);

        db.Categorias.Remove(categoria);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Sin distinguir mayúsculas: "Comida" y "comida" cuentan como la misma categoría.
    private Task<bool> NombreEnUso(string nombre, TipoMovimiento tipo, Guid? excluirId) =>
        MisCategorias.AnyAsync(c =>
            c.Tipo == tipo && c.Nombre.ToLower() == nombre.ToLower() && c.Id != excluirId);

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
        ModelState.AddModelError(nameof(CrearCategoriaRequest.Nombre), $"Ya tienes una categoría llamada '{nombre}' de este tipo.");
        return ValidationProblem(ModelState);
    }
}

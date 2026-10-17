using System.Net;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;
using MiEconomia.Api.Subcategorias;
using MiEconomia.Api.Tests.Infraestructura;

namespace MiEconomia.Api.Tests;

[Collection(ApiCollection.Nombre)]
public class SubcategoriasTests(ApiFactory api)
{
    private static async Task<CategoriaConSubcategoriasResponse> ObtenerCategoriaAsync(Sesion sesion, Guid id) =>
        await (await sesion.Cliente.GetAsync($"/api/categorias/{id}")).LeerAsync<CategoriaConSubcategoriasResponse>();

    [Fact]
    public async Task Crear_devuelve_201_y_aparece_en_su_categoria_ordenada_por_nombre()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Suscripciones", TipoMovimiento.Egreso);

        var respuesta = await sesion.Cliente.PostJsonAsync($"/api/categorias/{categoria.Id}/subcategorias",
            new { nombre = "  Netflix  " });
        await sesion.CrearSubcategoriaAsync(categoria.Id, "Disney");

        var netflix = await respuesta.LeerAsync<SubcategoriaResponse>(HttpStatusCode.Created);
        Assert.Equal("Netflix", netflix.Nombre);
        var obtenida = await ObtenerCategoriaAsync(sesion, categoria.Id);
        Assert.Equal(["Disney", "Netflix"], obtenida.Subcategorias.Select(s => s.Nombre));

        var listado = await (await sesion.Cliente.GetAsync("/api/categorias"))
            .LeerAsync<List<CategoriaConSubcategoriasResponse>>();
        Assert.Equal(2, listado.Single(c => c.Id == categoria.Id).Subcategorias.Count);
        Assert.All(listado.Where(c => c.Id != categoria.Id), c => Assert.Empty(c.Subcategorias));
    }

    [Theory]
    [InlineData("Netflix")]
    [InlineData(" NETFLIX ")]
    public async Task No_permite_nombre_repetido_en_la_misma_categoria(string nombre)
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Suscripciones", TipoMovimiento.Egreso);
        await sesion.CrearSubcategoriaAsync(categoria.Id, "Netflix");

        var respuesta = await sesion.Cliente.PostJsonAsync($"/api/categorias/{categoria.Id}/subcategorias", new { nombre });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([$"Esta categoría ya tiene una subcategoría llamada '{nombre.Trim()}'."], problema.De("Nombre"));
    }

    [Fact]
    public async Task Permite_el_mismo_nombre_en_otra_categoria()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var suscripciones = await sesion.CrearCategoriaAsync("Suscripciones", TipoMovimiento.Egreso);
        var regalos = await sesion.CrearCategoriaAsync("Regalos", TipoMovimiento.Egreso);
        await sesion.CrearSubcategoriaAsync(suscripciones.Id, "Familia");

        var respuesta = await sesion.Cliente.PostJsonAsync($"/api/categorias/{regalos.Id}/subcategorias", new { nombre = "Familia" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("", "El nombre es obligatorio.")]
    [InlineData("123456789012345678901234567890123456789012345678901", "El nombre puede tener hasta 50 caracteres.")]
    public async Task Crear_con_nombre_invalido_devuelve_400(string nombre, string mensaje)
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Suscripciones", TipoMovimiento.Egreso);

        var respuesta = await sesion.Cliente.PostJsonAsync($"/api/categorias/{categoria.Id}/subcategorias", new { nombre });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([mensaje], problema.De("Nombre"));
    }

    [Fact]
    public async Task Crear_en_una_categoria_inexistente_devuelve_404()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync($"/api/categorias/{Guid.NewGuid()}/subcategorias", new { nombre = "X" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Actualizar_cambia_el_nombre_y_no_choca_consigo_misma()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Suscripciones", TipoMovimiento.Egreso);
        var sub = await sesion.CrearSubcategoriaAsync(categoria.Id, "Netflix");

        var renombrada = await (await sesion.Cliente.PutJsonAsync(
            $"/api/categorias/{categoria.Id}/subcategorias/{sub.Id}", new { nombre = "Netflix Premium" }))
            .LeerAsync<SubcategoriaResponse>();
        var mismoNombre = await sesion.Cliente.PutJsonAsync(
            $"/api/categorias/{categoria.Id}/subcategorias/{sub.Id}", new { nombre = "netflix premium" });

        Assert.Equal(new SubcategoriaResponse(sub.Id, "Netflix Premium"), renombrada);
        Assert.Equal("netflix premium", (await mismoNombre.LeerAsync<SubcategoriaResponse>()).Nombre);
    }

    [Fact]
    public async Task Actualizar_desde_otra_categoria_devuelve_404()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var suscripciones = await sesion.CrearCategoriaAsync("Suscripciones", TipoMovimiento.Egreso);
        var regalos = await sesion.CrearCategoriaAsync("Regalos", TipoMovimiento.Egreso);
        var sub = await sesion.CrearSubcategoriaAsync(suscripciones.Id, "Netflix");

        var respuesta = await sesion.Cliente.PutJsonAsync(
            $"/api/categorias/{regalos.Id}/subcategorias/{sub.Id}", new { nombre = "Movida" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Eliminar_deja_sus_movimientos_en_la_categoria_sin_subcategoria()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Suscripciones", TipoMovimiento.Egreso);
        var sub = await sesion.CrearSubcategoriaAsync(categoria.Id, "Netflix");
        var movimiento = await sesion.CrearMovimientoAsync(categoria.Id, 10, "2026-10-01", subcategoriaId: sub.Id);

        var primera = await sesion.Cliente.DeleteAsync($"/api/categorias/{categoria.Id}/subcategorias/{sub.Id}");
        var segunda = await sesion.Cliente.DeleteAsync($"/api/categorias/{categoria.Id}/subcategorias/{sub.Id}");

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, segunda.StatusCode);
        Assert.Empty((await ObtenerCategoriaAsync(sesion, categoria.Id)).Subcategorias);
        var obtenido = await (await sesion.Cliente.GetAsync($"/api/movimientos/{movimiento.Id}")).LeerAsync<MovimientoResponse>();
        Assert.Null(obtenido.Subcategoria);
        Assert.Equal(categoria.Id, obtenido.Categoria.Id);
    }

    [Fact]
    public async Task Eliminar_una_categoria_sin_movimientos_borra_tambien_sus_subcategorias()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Suscripciones", TipoMovimiento.Egreso);
        await sesion.CrearSubcategoriaAsync(categoria.Id, "Netflix");

        var respuesta = await sesion.Cliente.DeleteAsync($"/api/categorias/{categoria.Id}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_movimiento_guarda_su_subcategoria_y_se_puede_filtrar_por_ella()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Suscripciones", TipoMovimiento.Egreso);
        var netflix = await sesion.CrearSubcategoriaAsync(categoria.Id, "Netflix");
        var disney = await sesion.CrearSubcategoriaAsync(categoria.Id, "Disney");

        var movimiento = await sesion.CrearMovimientoAsync(categoria.Id, 15, "2026-10-01", subcategoriaId: netflix.Id);
        await sesion.CrearMovimientoAsync(categoria.Id, 20, "2026-10-02", subcategoriaId: disney.Id);
        await sesion.CrearMovimientoAsync(categoria.Id, 30, "2026-10-03");

        Assert.Equal(netflix, movimiento.Subcategoria);
        var soloNetflix = await (await sesion.Cliente.GetAsync($"/api/movimientos?subcategoriaId={netflix.Id}"))
            .LeerAsync<PaginaResponse<MovimientoResponse>>();
        Assert.Equal(15m, Assert.Single(soloNetflix.Items).Monto);
    }

    [Fact]
    public async Task Un_movimiento_con_una_subcategoria_de_otra_categoria_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var suscripciones = await sesion.CrearCategoriaAsync("Suscripciones", TipoMovimiento.Egreso);
        var regalos = await sesion.CrearCategoriaAsync("Regalos", TipoMovimiento.Egreso);
        var netflix = await sesion.CrearSubcategoriaAsync(suscripciones.Id, "Netflix");
        var movimiento = await sesion.CrearMovimientoAsync(suscripciones.Id, 15, "2026-10-01", subcategoriaId: netflix.Id);

        // Cambiar la categoría sin quitar la subcategoría vieja tampoco vale.
        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/movimientos/{movimiento.Id}",
            new { categoriaId = regalos.Id, subcategoriaId = netflix.Id, cuentaId = sesion.CuentaId, monto = 15, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La subcategoría no pertenece a la categoría elegida."], problema.De("SubcategoriaId"));
    }
}

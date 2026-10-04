using System.Net;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Tests.Infraestructura;

namespace MiEconomia.Api.Tests;

[Collection(ApiCollection.Nombre)]
public class CategoriasTests(ApiFactory api)
{
    [Fact]
    public async Task Crear_devuelve_201_con_la_categoria_y_se_puede_obtener()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/categorias",
            new { nombre = "  Mascotas  ", tipo = "Egreso", color = "#22AA55", icono = "dog" });

        var creada = await respuesta.LeerAsync<CategoriaResponse>(HttpStatusCode.Created);
        Assert.Equal(new CategoriaResponse(creada.Id, "Mascotas", TipoMovimiento.Egreso, "#22AA55", "dog"), creada);
        Assert.Equal($"/api/categorias/{creada.Id}", respuesta.Headers.Location?.AbsolutePath, ignoreCase: true);

        var obtenida = await (await sesion.Cliente.GetAsync($"/api/categorias/{creada.Id}")).LeerAsync<CategoriaResponse>();
        Assert.Equal(creada, obtenida);
    }

    [Fact]
    public async Task Listar_filtra_por_tipo()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var ingresos = await (await sesion.Cliente.GetAsync("/api/categorias?tipo=Ingreso"))
            .LeerAsync<List<CategoriaResponse>>();

        Assert.NotEmpty(ingresos);
        Assert.All(ingresos, c => Assert.Equal(TipoMovimiento.Ingreso, c.Tipo));
    }

    [Theory]
    [InlineData("Supermercado")]
    [InlineData("supermercado")]
    [InlineData(" SUPERMERCADO ")]
    public async Task No_permite_nombre_repetido_del_mismo_tipo_sin_importar_mayusculas(string nombre)
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/categorias", new { nombre, tipo = "Egreso" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([$"Ya tienes una categoría llamada '{nombre.Trim()}' de este tipo."], problema.De("Nombre"));
    }

    [Fact]
    public async Task Permite_el_mismo_nombre_en_distinto_tipo()
    {
        var sesion = await api.NuevoUsuarioAsync();

        // "Supermercado" ya existe como Egreso (categoría por defecto).
        var respuesta = await sesion.Cliente.PostJsonAsync("/api/categorias", new { nombre = "Supermercado", tipo = "Ingreso" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("""{ "nombre": "", "tipo": "Egreso" }""", "Nombre", "El nombre es obligatorio.")]
    [InlineData("""{ "nombre": "X" }""", "Tipo", "El tipo es obligatorio (Ingreso o Egreso).")]
    [InlineData("""{ "nombre": "X", "tipo": "Otro" }""", "Tipo", "El valor enviado no es válido.")]
    [InlineData("""{ "nombre": "X", "tipo": "Egreso", "color": "rojo" }""", "Color", "El color debe tener el formato #RRGGBB.")]
    [InlineData("""{ "nombre": """, "Nombre", "El valor enviado no es válido.")]
    public async Task Crear_con_datos_invalidos_devuelve_el_error_en_su_campo(string json, string campo, string mensaje)
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostAsync("/api/categorias",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([mensaje], problema.De(campo));
    }

    [Fact]
    public async Task Actualizar_cambia_nombre_color_e_icono_pero_no_el_tipo()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Gimnasio", TipoMovimiento.Egreso);

        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/categorias/{categoria.Id}",
            new { nombre = "Deporte", color = "#DC2626", icono = "dumbbell", tipo = "Ingreso" });

        var actualizada = await respuesta.LeerAsync<CategoriaResponse>();
        Assert.Equal(new CategoriaResponse(categoria.Id, "Deporte", TipoMovimiento.Egreso, "#DC2626", "dumbbell"), actualizada);
    }

    [Fact]
    public async Task Actualizar_a_un_nombre_que_ya_existe_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Gimnasio", TipoMovimiento.Egreso);

        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/categorias/{categoria.Id}", new { nombre = "Salud" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Single(problema.De("Nombre"));
    }

    [Fact]
    public async Task Actualizar_manteniendo_su_propio_nombre_no_choca_consigo_misma()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Gimnasio", TipoMovimiento.Egreso);

        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/categorias/{categoria.Id}",
            new { nombre = "gimnasio", color = "#16A34A" });

        Assert.Equal("gimnasio", (await respuesta.LeerAsync<CategoriaResponse>()).Nombre);
    }

    [Fact]
    public async Task Eliminar_sin_movimientos_devuelve_204_y_ya_no_existe()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Temporal", TipoMovimiento.Egreso);

        var respuesta = await sesion.Cliente.DeleteAsync($"/api/categorias/{categoria.Id}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var obtener = await sesion.Cliente.GetAsync($"/api/categorias/{categoria.Id}");
        Assert.Equal(HttpStatusCode.NotFound, obtener.StatusCode);
    }

    [Fact]
    public async Task Eliminar_con_movimientos_devuelve_409_explicando_cuantos_tiene()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Con datos", TipoMovimiento.Egreso);
        await sesion.CrearMovimientoAsync(categoria.Id, 10, "2026-10-01");
        await sesion.CrearMovimientoAsync(categoria.Id, 20, "2026-10-02");

        var respuesta = await sesion.Cliente.DeleteAsync($"/api/categorias/{categoria.Id}");

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.Conflict);
        Assert.StartsWith("No se puede eliminar la categoría porque tiene 2 movimiento(s).", problema.Title);
    }

    [Fact]
    public async Task Categoria_inexistente_devuelve_404_en_espanol()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.GetAsync($"/api/categorias/{Guid.NewGuid()}");

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.NotFound);
        Assert.Equal("No se encontró lo que buscas.", problema.Title);
    }
}

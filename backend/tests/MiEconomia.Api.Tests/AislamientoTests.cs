using System.Net;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;
using MiEconomia.Api.Resumen;
using MiEconomia.Api.Tests.Infraestructura;

namespace MiEconomia.Api.Tests;

/// <summary>Un usuario nunca puede ver ni modificar los datos de otro.</summary>
[Collection(ApiCollection.Nombre)]
public class AislamientoTests(ApiFactory api) : IAsyncLifetime
{
    private Sesion _ana = null!;
    private Sesion _beto = null!;
    private CategoriaResponse _categoriaDeAna = null!;
    private MovimientoResponse _movimientoDeAna = null!;

    public async Task InitializeAsync()
    {
        _ana = await api.NuevoUsuarioAsync("Ana");
        _beto = await api.NuevoUsuarioAsync("Beto");
        _categoriaDeAna = await _ana.CrearCategoriaAsync("Privada", TipoMovimiento.Egreso);
        _movimientoDeAna = await _ana.CrearMovimientoAsync(_categoriaDeAna.Id, 99999, "2026-10-05");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Los_listados_solo_muestran_lo_propio()
    {
        var categorias = await (await _beto.Cliente.GetAsync("/api/categorias")).LeerAsync<List<CategoriaResponse>>();
        var movimientos = await (await _beto.Cliente.GetAsync("/api/movimientos")).Content.ReadAsStringAsync();

        Assert.DoesNotContain(categorias, c => c.Id == _categoriaDeAna.Id);
        Assert.Contains("\"total\":0", movimientos);
    }

    [Fact]
    public async Task El_resumen_no_incluye_movimientos_de_otros()
    {
        var resumen = await (await _beto.Cliente.GetAsync("/api/resumen?desde=2026-10-01&hasta=2026-10-31"))
            .LeerAsync<ResumenResponse>();

        Assert.Equal(0, resumen.TotalEgresos);
        Assert.Equal(0, resumen.CantidadMovimientos);
    }

    public static TheoryData<string, string> AccesosAjenos => new()
    {
        { "GET", "/api/categorias/{categoria}" },
        { "PUT", "/api/categorias/{categoria}" },
        { "DELETE", "/api/categorias/{categoria}" },
        { "GET", "/api/movimientos/{movimiento}" },
        { "PUT", "/api/movimientos/{movimiento}" },
        { "DELETE", "/api/movimientos/{movimiento}" },
    };

    [Theory]
    [MemberData(nameof(AccesosAjenos))]
    public async Task Acceder_a_datos_de_otro_usuario_devuelve_404(string metodo, string ruta)
    {
        var url = ruta
            .Replace("{categoria}", _categoriaDeAna.Id.ToString())
            .Replace("{movimiento}", _movimientoDeAna.Id.ToString());
        // Cuerpo válido para que el 404 no se confunda con un 400 de validación.
        object cuerpo = ruta.Contains("categoria")
            ? new { nombre = "Hackeada" }
            : new { categoriaId = _categoriaDeAna.Id, monto = 1, fecha = "2026-10-01" };

        var respuesta = metodo switch
        {
            "GET" => await _beto.Cliente.GetAsync(url),
            "PUT" => await _beto.Cliente.PutJsonAsync(url, cuerpo),
            _ => await _beto.Cliente.DeleteAsync(url),
        };

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        // Y los datos de Ana siguen intactos.
        var movimiento = await (await _ana.Cliente.GetAsync($"/api/movimientos/{_movimientoDeAna.Id}"))
            .LeerAsync<MovimientoResponse>();
        Assert.Equal(_movimientoDeAna, movimiento);
    }

    [Fact]
    public async Task No_se_puede_crear_un_movimiento_con_la_categoria_de_otro()
    {
        var respuesta = await _beto.Cliente.PostJsonAsync("/api/movimientos",
            new { categoriaId = _categoriaDeAna.Id, monto = 10, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La categoría no existe."], problema.De("CategoriaId"));
    }

    [Fact]
    public async Task No_se_puede_mover_un_movimiento_propio_a_la_categoria_de_otro()
    {
        var propia = await _beto.CrearCategoriaAsync("Mía", TipoMovimiento.Egreso);
        var movimiento = await _beto.CrearMovimientoAsync(propia.Id, 10, "2026-10-01");

        var respuesta = await _beto.Cliente.PutJsonAsync($"/api/movimientos/{movimiento.Id}",
            new { categoriaId = _categoriaDeAna.Id, monto = 10, fecha = "2026-10-01" });

        await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
    }
}

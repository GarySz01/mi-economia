using System.Net;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;
using MiEconomia.Api.Resumen;
using MiEconomia.Api.Tests.Infraestructura;
using MiEconomia.Api.Transferencias;

namespace MiEconomia.Api.Tests;

/// <summary>Un usuario nunca puede ver ni modificar los datos de otro.</summary>
[Collection(ApiCollection.Nombre)]
public class AislamientoTests(ApiFactory api) : IAsyncLifetime
{
    private Sesion _ana = null!;
    private Sesion _beto = null!;
    private CategoriaResponse _categoriaDeAna = null!;
    private MovimientoResponse _movimientoDeAna = null!;
    private CuentaResponse _cuentaDeAna = null!;
    private TransferenciaResponse _transferenciaDeAna = null!;

    public async Task InitializeAsync()
    {
        _ana = await api.NuevoUsuarioAsync("Ana");
        _beto = await api.NuevoUsuarioAsync("Beto");
        _categoriaDeAna = await _ana.CrearCategoriaAsync("Privada", TipoMovimiento.Egreso);
        _movimientoDeAna = await _ana.CrearMovimientoAsync(_categoriaDeAna.Id, 99999, "2026-10-05");
        _cuentaDeAna = await _ana.CrearCuentaAsync("Ahorros secretos", saldoInicial: 500000);
        _transferenciaDeAna = await _ana.CrearTransferenciaAsync(_ana.CuentaId, _cuentaDeAna.Id, 100, "2026-10-05");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Los_listados_solo_muestran_lo_propio()
    {
        var categorias = await (await _beto.Cliente.GetAsync("/api/categorias")).LeerAsync<List<CategoriaResponse>>();
        var movimientos = await (await _beto.Cliente.GetAsync("/api/movimientos")).Content.ReadAsStringAsync();
        var cuentas = await (await _beto.Cliente.GetAsync("/api/cuentas")).LeerAsync<List<CuentaResponse>>();
        var transferencias = await (await _beto.Cliente.GetAsync("/api/transferencias")).Content.ReadAsStringAsync();

        Assert.DoesNotContain(categorias, c => c.Id == _categoriaDeAna.Id);
        Assert.Contains("\"total\":0", movimientos);
        Assert.Equal([_beto.CuentaId], cuentas.Select(c => c.Id));
        Assert.Equal(0, Assert.Single(cuentas).Saldo);
        Assert.Contains("\"total\":0", transferencias);
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
        { "GET", "/api/cuentas/{cuenta}" },
        { "PUT", "/api/cuentas/{cuenta}" },
        { "DELETE", "/api/cuentas/{cuenta}" },
        { "GET", "/api/transferencias/{transferencia}" },
        { "PUT", "/api/transferencias/{transferencia}" },
        { "DELETE", "/api/transferencias/{transferencia}" },
    };

    [Theory]
    [MemberData(nameof(AccesosAjenos))]
    public async Task Acceder_a_datos_de_otro_usuario_devuelve_404(string metodo, string ruta)
    {
        var url = ruta
            .Replace("{categoria}", _categoriaDeAna.Id.ToString())
            .Replace("{movimiento}", _movimientoDeAna.Id.ToString())
            .Replace("{cuenta}", _cuentaDeAna.Id.ToString())
            .Replace("{transferencia}", _transferenciaDeAna.Id.ToString());
        // Cuerpo válido para que el 404 no se confunda con un 400 de validación.
        object cuerpo = ruta switch
        {
            _ when ruta.Contains("categorias") => new { nombre = "Hackeada" },
            _ when ruta.Contains("cuentas") => new { nombre = "Hackeada", tipo = TipoCuenta.Otra, saldoInicial = 0 },
            _ when ruta.Contains("transferencias") => new
            {
                cuentaOrigenId = _ana.CuentaId, cuentaDestinoId = _cuentaDeAna.Id, monto = 1, fecha = "2026-10-01",
            },
            _ => new { categoriaId = _categoriaDeAna.Id, cuentaId = _ana.CuentaId, monto = 1, fecha = "2026-10-01" },
        };

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
        var cuenta = await (await _ana.Cliente.GetAsync($"/api/cuentas/{_cuentaDeAna.Id}")).LeerAsync<CuentaResponse>();
        Assert.Equal("Ahorros secretos", cuenta.Nombre);
        var transferencia = await (await _ana.Cliente.GetAsync($"/api/transferencias/{_transferenciaDeAna.Id}"))
            .LeerAsync<TransferenciaResponse>();
        Assert.Equal(_transferenciaDeAna, transferencia);
    }

    [Fact]
    public async Task No_se_puede_crear_un_movimiento_con_la_categoria_de_otro()
    {
        var respuesta = await _beto.Cliente.PostJsonAsync("/api/movimientos",
            new { categoriaId = _categoriaDeAna.Id, cuentaId = _beto.CuentaId, monto = 10, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La categoría no existe."], problema.De("CategoriaId"));
    }

    [Fact]
    public async Task No_se_puede_crear_un_movimiento_en_la_cuenta_de_otro()
    {
        var propia = await _beto.CrearCategoriaAsync("Mía", TipoMovimiento.Ingreso);

        var respuesta = await _beto.Cliente.PostJsonAsync("/api/movimientos",
            new { categoriaId = propia.Id, cuentaId = _cuentaDeAna.Id, monto = 10, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La cuenta no existe."], problema.De("CuentaId"));
    }

    [Fact]
    public async Task No_se_puede_mover_un_movimiento_propio_a_la_categoria_de_otro()
    {
        var propia = await _beto.CrearCategoriaAsync("Mía", TipoMovimiento.Egreso);
        var movimiento = await _beto.CrearMovimientoAsync(propia.Id, 10, "2026-10-01");

        var respuesta = await _beto.Cliente.PutJsonAsync($"/api/movimientos/{movimiento.Id}",
            new { categoriaId = _categoriaDeAna.Id, cuentaId = _beto.CuentaId, monto = 10, fecha = "2026-10-01" });

        await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task No_se_puede_transferir_desde_ni_hacia_la_cuenta_de_otro()
    {
        var desde = await _beto.Cliente.PostJsonAsync("/api/transferencias",
            new { cuentaOrigenId = _cuentaDeAna.Id, cuentaDestinoId = _beto.CuentaId, monto = 10, fecha = "2026-10-01" });
        var hacia = await _beto.Cliente.PostJsonAsync("/api/transferencias",
            new { cuentaOrigenId = _beto.CuentaId, cuentaDestinoId = _cuentaDeAna.Id, monto = 10, fecha = "2026-10-01" });

        Assert.Equal(["La cuenta de origen no existe."], (await desde.LeerProblemaAsync(HttpStatusCode.BadRequest)).De("CuentaOrigenId"));
        Assert.Equal(["La cuenta de destino no existe."], (await hacia.LeerProblemaAsync(HttpStatusCode.BadRequest)).De("CuentaDestinoId"));
        Assert.Equal(500100m, await _ana.SaldoAsync(_cuentaDeAna.Id));
    }
}

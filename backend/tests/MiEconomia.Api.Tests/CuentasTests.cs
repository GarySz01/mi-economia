using System.Net;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Tests.Infraestructura;

namespace MiEconomia.Api.Tests;

[Collection(ApiCollection.Nombre)]
public class CuentasTests(ApiFactory api)
{
    [Fact]
    public async Task Crear_limpia_el_nombre_y_crea_la_subcuenta_Disponible_con_el_saldo_inicial()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/cuentas",
            new { nombre = "  Banco Nación  ", tipo = TipoCuenta.CuentaBancaria, saldoInicial = 1500.50m, color = "#2563EB" });

        var cuenta = await respuesta.LeerAsync<CuentaResponse>(HttpStatusCode.Created);
        Assert.Equal((cuenta.Id, "Banco Nación", TipoCuenta.CuentaBancaria, "#2563EB"), (cuenta.Id, cuenta.Nombre, cuenta.Tipo, cuenta.Color));
        var subcuenta = Assert.Single(cuenta.Subcuentas);
        Assert.Equal(new SubcuentaResponse(subcuenta.Id, "Disponible", "UYU", 1500.50m, 1500.50m), subcuenta);
        Assert.Equal([new SaldoPorMoneda("UYU", 1500.50m)], cuenta.Saldos);
        var obtenida = await (await sesion.Cliente.GetAsync($"/api/cuentas/{cuenta.Id}")).LeerAsync<CuentaResponse>();
        Assert.Equal(cuenta.Subcuentas, obtenida.Subcuentas);
    }

    [Fact]
    public async Task El_saldo_inicial_puede_ser_negativo_para_una_deuda()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var tarjeta = await sesion.CrearCuentaAsync("Visa", TipoCuenta.TarjetaCredito, saldoInicial: -20000);

        Assert.Equal(-20000m, tarjeta.SaldoUnico());
    }

    [Fact]
    public async Task El_saldo_suma_ingresos_resta_gastos_y_aplica_las_transferencias()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var sueldo = await sesion.CrearCategoriaAsync("Sueldo X", TipoMovimiento.Ingreso);
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var banco = await sesion.CrearCuentaAsync("Banco", saldoInicial: 1000);
        var tarjeta = await sesion.CrearCuentaAsync("Visa", TipoCuenta.TarjetaCredito);

        await sesion.CrearMovimientoAsync(sueldo.Id, 5000, "2026-10-01", cuentaId: banco.Id);
        await sesion.CrearMovimientoAsync(comida.Id, 300, "2026-10-02", cuentaId: banco.Id);
        await sesion.CrearMovimientoAsync(comida.Id, 800, "2026-10-03", cuentaId: tarjeta.Id);
        await sesion.CrearMovimientoAsync(comida.Id, 50, "2026-10-03"); // efectivo
        await sesion.CrearTransferenciaAsync(banco.Id, sesion.CuentaId, 200, "2026-10-04"); // cajero
        await sesion.CrearTransferenciaAsync(banco.Id, tarjeta.Id, 800, "2026-10-05"); // pago de la tarjeta

        var cuentas = await (await sesion.Cliente.GetAsync("/api/cuentas")).LeerAsync<List<CuentaResponse>>();
        var saldos = cuentas.ToDictionary(c => c.Nombre, c => c.SaldoUnico());

        Assert.Equal(1000 + 5000 - 300 - 200 - 800, saldos["Banco"]);
        Assert.Equal(0, saldos["Visa"]);
        Assert.Equal(200 - 50, saldos["Efectivo"]);
        Assert.Equal(saldos["Banco"], await sesion.SaldoAsync(banco.Id));
    }

    [Fact]
    public async Task Listar_ordena_por_nombre()
    {
        var sesion = await api.NuevoUsuarioAsync();
        await sesion.CrearCuentaAsync("Mercado Pago", TipoCuenta.BilleteraVirtual);
        await sesion.CrearCuentaAsync("Banco");

        var cuentas = await (await sesion.Cliente.GetAsync("/api/cuentas")).LeerAsync<List<CuentaResponse>>();

        Assert.Equal(["Banco", "Efectivo", "Mercado Pago"], cuentas.Select(c => c.Nombre));
    }

    [Fact]
    public async Task Sin_campos_obligatorios_devuelve_un_error_por_campo()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/cuentas", new { });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["El nombre es obligatorio."], problema.De("Nombre"));
        Assert.Equal(["El tipo de cuenta es obligatorio."], problema.De("Tipo"));
    }

    [Theory]
    [InlineData("""{ "nombre": "X", "tipo": "Efectivo", "saldoInicial": 1.234 }""", "SaldoInicial", "El saldo inicial puede tener como máximo 2 decimales.")]
    [InlineData("""{ "nombre": "X", "tipo": "Efectivo", "saldoInicial": 99999999999999999 }""", "SaldoInicial", "El saldo inicial es demasiado grande.")]
    [InlineData("""{ "nombre": "X", "tipo": "Efectivo", "color": "rojo" }""", "Color", "El color debe tener el formato #RRGGBB.")]
    public async Task Datos_invalidos_devuelven_400(string json, string campo, string mensaje)
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostAsync("/api/cuentas",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([mensaje], problema.De(campo));
    }

    [Fact]
    public async Task Nombre_repetido_sin_distinguir_mayusculas_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/cuentas",
            new { nombre = "EFECTIVO", tipo = TipoCuenta.Efectivo });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["Ya tienes una cuenta llamada 'EFECTIVO'."], problema.De("Nombre"));
    }

    [Fact]
    public async Task Actualizar_cambia_nombre_tipo_y_color_pero_no_los_saldos()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var sueldo = await sesion.CrearCategoriaAsync("Sueldo X", TipoMovimiento.Ingreso);
        await sesion.CrearMovimientoAsync(sueldo.Id, 100, "2026-10-01");

        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/cuentas/{sesion.CuentaId}",
            new { nombre = "Billetera", tipo = TipoCuenta.Otra, saldoInicial = 50, color = "#DB2777" });

        var cuenta = await respuesta.LeerAsync<CuentaResponse>();
        Assert.Equal(("Billetera", TipoCuenta.Otra, "#DB2777"), (cuenta.Nombre, cuenta.Tipo, cuenta.Color));
        // El saldo inicial se edita en la subcuenta; acá se ignora.
        Assert.Equal(100, cuenta.SaldoUnico());
    }

    [Fact]
    public async Task Actualizar_puede_mantener_el_mismo_nombre()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/cuentas/{sesion.CuentaId}",
            new { nombre = "Efectivo", tipo = TipoCuenta.Efectivo, saldoInicial = 10 });

        await respuesta.VerificarAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Eliminar_una_cuenta_sin_uso_devuelve_204()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var banco = await sesion.CrearCuentaAsync("Banco");

        var respuesta = await sesion.Cliente.DeleteAsync($"/api/cuentas/{banco.Id}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await sesion.Cliente.GetAsync($"/api/cuentas/{banco.Id}")).StatusCode);
    }

    [Fact]
    public async Task Eliminar_una_cuenta_con_movimientos_devuelve_409()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        await sesion.CrearMovimientoAsync(comida.Id, 10, "2026-10-01");
        await sesion.CrearMovimientoAsync(comida.Id, 20, "2026-10-01");

        var respuesta = await sesion.Cliente.DeleteAsync($"/api/cuentas/{sesion.CuentaId}");

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.Conflict);
        Assert.Equal("No se puede eliminar la cuenta porque tiene 2 movimiento(s). Elimínalos o pásalos a otra cuenta primero.", problema.Title);
    }

    [Fact]
    public async Task Eliminar_una_cuenta_con_transferencias_devuelve_409()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var banco = await sesion.CrearCuentaAsync("Banco");
        await sesion.CrearTransferenciaAsync(sesion.CuentaId, banco.Id, 10, "2026-10-01");

        var respuesta = await sesion.Cliente.DeleteAsync($"/api/cuentas/{banco.Id}");

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.Conflict);
        Assert.Equal("No se puede eliminar la cuenta porque tiene 1 transferencia(s). Elimínalos o pásalos a otra cuenta primero.", problema.Title);
    }
}

using System.Net;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;
using MiEconomia.Api.Resumen;
using MiEconomia.Api.Tests.Infraestructura;

namespace MiEconomia.Api.Tests;

[Collection(ApiCollection.Nombre)]
public class SubcuentasTests(ApiFactory api)
{
    [Fact]
    public async Task Crear_devuelve_201_y_la_cuenta_muestra_sus_subcuentas_y_un_saldo_por_moneda()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var itau = await sesion.CrearCuentaAsync("Itaú", saldoInicial: 10000);

        var respuesta = await sesion.Cliente.PostJsonAsync($"/api/cuentas/{itau.Id}/subcuentas",
            new { nombre = "  Caja en dólares  ", moneda = "USD", saldoInicial = 500 });
        await sesion.CrearSubcuentaAsync(itau.Id, "Ahorro viaje", saldoInicial: 2000);

        var dolares = await respuesta.LeerAsync<SubcuentaResponse>(HttpStatusCode.Created);
        Assert.Equal(new SubcuentaResponse(dolares.Id, "Caja en dólares", "USD", 500, 500), dolares);
        var cuenta = await sesion.ObtenerCuentaAsync(itau.Id);
        Assert.Equal(["Ahorro viaje", "Caja en dólares", "Disponible"], cuenta.Subcuentas.Select(s => s.Nombre));
        Assert.Equal([new SaldoPorMoneda("USD", 500), new SaldoPorMoneda("UYU", 12000)], cuenta.Saldos);
    }

    [Fact]
    public async Task Crear_una_cuenta_en_otra_moneda_crea_su_subcuenta_en_esa_moneda()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var cuenta = await sesion.CrearCuentaAsync("Brou USD", saldoInicial: 300, moneda: "USD");

        Assert.Equal("USD", Assert.Single(cuenta.Subcuentas).Moneda);
        Assert.Equal([new SaldoPorMoneda("USD", 300)], cuenta.Saldos);
    }

    [Theory]
    [InlineData("""{ "nombre": "X", "saldoInicial": 0 }""", "Moneda", "La moneda es obligatoria.")]
    [InlineData("""{ "nombre": "X", "moneda": "PESOS", "saldoInicial": 0 }""", "Moneda", "La moneda no es válida. Usa UYU, USD, EUR, ARS, BRL o UYI.")]
    [InlineData("""{ "nombre": "", "moneda": "UYU", "saldoInicial": 0 }""", "Nombre", "El nombre es obligatorio.")]
    [InlineData("""{ "nombre": "X", "moneda": "UYU", "saldoInicial": 1.234 }""", "SaldoInicial", "El saldo inicial puede tener como máximo 2 decimales.")]
    [InlineData("""{ "nombre": "disponible", "moneda": "UYU", "saldoInicial": 0 }""", "Nombre", "Esta cuenta ya tiene una subcuenta llamada 'disponible'.")]
    public async Task Crear_con_datos_invalidos_devuelve_el_error_en_su_campo(string json, string campo, string mensaje)
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostAsync($"/api/cuentas/{sesion.CuentaId}/subcuentas",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([mensaje], problema.De(campo));
    }

    [Fact]
    public async Task Crear_una_cuenta_con_moneda_invalida_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/cuentas", new { nombre = "X", tipo = TipoCuenta.Otra, moneda = "XYZ" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([Monedas.MensajeInvalida], problema.De("Moneda"));
    }

    [Fact]
    public async Task Crear_en_una_cuenta_inexistente_devuelve_404()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync($"/api/cuentas/{Guid.NewGuid()}/subcuentas",
            new { nombre = "X", moneda = "UYU", saldoInicial = 0 });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Actualizar_cambia_nombre_saldo_inicial_y_moneda_si_no_tiene_movimientos()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var sub = await sesion.CrearSubcuentaAsync(sesion.CuentaId, "Ahorro");

        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/cuentas/{sesion.CuentaId}/subcuentas/{sub.Id}",
            new { nombre = "Ahorro en dólares", moneda = "USD", saldoInicial = 150.25m });

        Assert.Equal(new SubcuentaResponse(sub.Id, "Ahorro en dólares", "USD", 150.25m, 150.25m),
            await respuesta.LeerAsync<SubcuentaResponse>());
    }

    [Fact]
    public async Task Actualizar_el_saldo_inicial_recalcula_el_saldo_con_los_movimientos()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var sueldo = await sesion.CrearCategoriaAsync("Sueldo X", TipoMovimiento.Ingreso);
        await sesion.CrearMovimientoAsync(sueldo.Id, 100, "2026-10-01");
        var disponible = (await sesion.ObtenerCuentaAsync(sesion.CuentaId)).Subcuentas[0];

        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/cuentas/{sesion.CuentaId}/subcuentas/{disponible.Id}",
            new { nombre = "Disponible", moneda = "UYU", saldoInicial = 50 });

        Assert.Equal(150, (await respuesta.LeerAsync<SubcuentaResponse>()).Saldo);
    }

    [Fact]
    public async Task No_se_puede_cambiar_la_moneda_si_tiene_movimientos()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        await sesion.CrearMovimientoAsync(comida.Id, 10, "2026-10-01");
        var disponible = (await sesion.ObtenerCuentaAsync(sesion.CuentaId)).Subcuentas[0];

        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/cuentas/{sesion.CuentaId}/subcuentas/{disponible.Id}",
            new { nombre = "Disponible", moneda = "USD", saldoInicial = 0 });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["No se puede cambiar la moneda porque la subcuenta ya tiene movimientos o transferencias."], problema.De("Moneda"));
    }

    [Fact]
    public async Task Eliminar_la_unica_subcuenta_devuelve_409()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var disponible = (await sesion.ObtenerCuentaAsync(sesion.CuentaId)).Subcuentas[0];

        var respuesta = await sesion.Cliente.DeleteAsync($"/api/cuentas/{sesion.CuentaId}/subcuentas/{disponible.Id}");

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.Conflict);
        Assert.StartsWith("No se puede eliminar la única subcuenta", problema.Title);
    }

    [Fact]
    public async Task Eliminar_una_subcuenta_con_movimientos_devuelve_409_y_sin_uso_204()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var usada = await sesion.CrearSubcuentaAsync(sesion.CuentaId, "Usada");
        var libre = await sesion.CrearSubcuentaAsync(sesion.CuentaId, "Libre");
        await sesion.CrearMovimientoAsync(comida.Id, 10, "2026-10-01", subcuentaId: usada.Id);

        var conMovimientos = await sesion.Cliente.DeleteAsync($"/api/cuentas/{sesion.CuentaId}/subcuentas/{usada.Id}");
        var sinUso = await sesion.Cliente.DeleteAsync($"/api/cuentas/{sesion.CuentaId}/subcuentas/{libre.Id}");

        var problema = await conMovimientos.LeerProblemaAsync(HttpStatusCode.Conflict);
        Assert.StartsWith("No se puede eliminar la subcuenta porque tiene 1 movimiento(s).", problema.Title);
        Assert.Equal(HttpStatusCode.NoContent, sinUso.StatusCode);
        Assert.Equal(["Disponible", "Usada"], (await sesion.ObtenerCuentaAsync(sesion.CuentaId)).Subcuentas.Select(s => s.Nombre));
    }

    [Fact]
    public async Task Eliminar_una_cuenta_sin_uso_borra_tambien_sus_subcuentas()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var banco = await sesion.CrearCuentaAsync("Banco");
        await sesion.CrearSubcuentaAsync(banco.Id, "Ahorro");

        var respuesta = await sesion.Cliente.DeleteAsync($"/api/cuentas/{banco.Id}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_movimiento_en_una_cuenta_con_varias_subcuentas_pide_elegir_una()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        await sesion.CrearSubcuentaAsync(sesion.CuentaId, "Ahorro");

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/movimientos",
            new { categoriaId = comida.Id, cuentaId = sesion.CuentaId, monto = 10, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["Elige en qué subcuenta de Efectivo está el dinero."], problema.De("SubcuentaId"));
    }

    [Fact]
    public async Task Un_movimiento_con_una_subcuenta_de_otra_cuenta_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var banco = await sesion.CrearCuentaAsync("Banco");

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/movimientos", new
        {
            categoriaId = comida.Id, cuentaId = sesion.CuentaId, subcuentaId = banco.Subcuentas[0].Id, monto = 10, fecha = "2026-10-01",
        });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La subcuenta no pertenece a la cuenta elegida."], problema.De("SubcuentaId"));
    }

    [Fact]
    public async Task Los_movimientos_cambian_solo_el_saldo_de_su_subcuenta_y_se_pueden_filtrar_por_ella()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var sueldo = await sesion.CrearCategoriaAsync("Sueldo X", TipoMovimiento.Ingreso);
        var itau = await sesion.CrearCuentaAsync("Itaú");
        var disponible = itau.Subcuentas[0];
        var plan = await sesion.CrearSubcuentaAsync(itau.Id, "Plan de inversión", saldoInicial: 1000);

        var movimiento = await sesion.CrearMovimientoAsync(sueldo.Id, 5000, "2026-10-01", cuentaId: itau.Id, subcuentaId: disponible.Id);
        await sesion.CrearMovimientoAsync(sueldo.Id, 30, "2026-10-01", "Intereses", cuentaId: itau.Id, subcuentaId: plan.Id);

        Assert.Equal(new SubcuentaBasicaResponse(disponible.Id, "Disponible", "UYU"), movimiento.Subcuenta);
        var saldos = (await sesion.ObtenerCuentaAsync(itau.Id)).Subcuentas.ToDictionary(s => s.Nombre, s => s.Saldo);
        Assert.Equal(5000, saldos["Disponible"]);
        Assert.Equal(1030, saldos["Plan de inversión"]);
        var delPlan = await (await sesion.Cliente.GetAsync($"/api/movimientos?subcuentaId={plan.Id}"))
            .LeerAsync<PaginaResponse<MovimientoResponse>>();
        Assert.Equal("Intereses", Assert.Single(delPlan.Items).Nota);
    }

    [Fact]
    public async Task Se_puede_transferir_entre_subcuentas_de_la_misma_cuenta()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var itau = await sesion.CrearCuentaAsync("Itaú", saldoInicial: 1000);
        var ahorro = await sesion.CrearSubcuentaAsync(itau.Id, "Ahorro");

        var transferencia = await sesion.CrearTransferenciaAsync(itau.Id, itau.Id, 300, "2026-10-01",
            subcuentaOrigenId: itau.Subcuentas[0].Id, subcuentaDestinoId: ahorro.Id);

        Assert.Equal(("Disponible", "Ahorro", 300m), (transferencia.SubcuentaOrigen.Nombre, transferencia.SubcuentaDestino.Nombre, transferencia.MontoDestino));
        var saldos = (await sesion.ObtenerCuentaAsync(itau.Id)).Subcuentas.ToDictionary(s => s.Nombre, s => s.Saldo);
        Assert.Equal(700, saldos["Disponible"]);
        Assert.Equal(300, saldos["Ahorro"]);
        // El total de la cuenta no cambia.
        Assert.Equal(1000, await sesion.SaldoAsync(itau.Id));
    }

    [Fact]
    public async Task Transferir_a_la_misma_subcuenta_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var itau = await sesion.CrearCuentaAsync("Itaú");
        var ahorro = await sesion.CrearSubcuentaAsync(itau.Id, "Ahorro");

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/transferencias", new
        {
            cuentaOrigenId = itau.Id, cuentaDestinoId = itau.Id, subcuentaOrigenId = ahorro.Id, subcuentaDestinoId = ahorro.Id,
            monto = 10, fecha = "2026-10-01",
        });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La subcuenta de destino tiene que ser distinta de la de origen."], problema.De("SubcuentaDestinoId"));
    }

    [Fact]
    public async Task Una_transferencia_desde_una_cuenta_con_varias_subcuentas_pide_elegir_el_origen()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var itau = await sesion.CrearCuentaAsync("Itaú");
        await sesion.CrearSubcuentaAsync(itau.Id, "Ahorro");

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/transferencias",
            new { cuentaOrigenId = itau.Id, cuentaDestinoId = sesion.CuentaId, monto = 10, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["Elige en qué subcuenta de Itaú está el dinero."], problema.De("SubcuentaOrigenId"));
    }

    [Fact]
    public async Task Comprar_dolares_descuenta_pesos_y_suma_dolares()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var itau = await sesion.CrearCuentaAsync("Itaú", saldoInicial: 10000);
        var dolares = await sesion.CrearSubcuentaAsync(itau.Id, "Dólares", "USD");

        var transferencia = await sesion.CrearTransferenciaAsync(itau.Id, itau.Id, 4000, "2026-10-01",
            subcuentaOrigenId: itau.Subcuentas[0].Id, subcuentaDestinoId: dolares.Id, montoDestino: 100);

        Assert.Equal((4000m, 100m), (transferencia.Monto, transferencia.MontoDestino));
        var cuenta = await sesion.ObtenerCuentaAsync(itau.Id);
        Assert.Equal([new SaldoPorMoneda("USD", 100), new SaldoPorMoneda("UYU", 6000)], cuenta.Saldos);
    }

    [Fact]
    public async Task Entre_monedas_distintas_hay_que_indicar_cuanto_llega()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var brou = await sesion.CrearCuentaAsync("Brou USD", moneda: "USD");

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/transferencias",
            new { cuentaOrigenId = sesion.CuentaId, cuentaDestinoId = brou.Id, monto = 4000, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["Las subcuentas tienen distinta moneda: indica cuántos USD llegan al destino."], problema.De("MontoDestino"));
    }

    [Fact]
    public async Task Entre_la_misma_moneda_no_puede_llegar_un_monto_distinto()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var banco = await sesion.CrearCuentaAsync("Banco");

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/transferencias",
            new { cuentaOrigenId = sesion.CuentaId, cuentaDestinoId = banco.Id, monto = 100, montoDestino = 90, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["Las dos subcuentas tienen la misma moneda: llega lo mismo que sale."], problema.De("MontoDestino"));
    }

    [Fact]
    public async Task El_resumen_es_de_una_moneda_y_se_puede_filtrar_por_subcuenta()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var itau = await sesion.CrearCuentaAsync("Itaú");
        var dolares = await sesion.CrearSubcuentaAsync(itau.Id, "Dólares", "USD");
        await sesion.CrearMovimientoAsync(comida.Id, 500, "2026-10-01"); // efectivo, UYU
        await sesion.CrearMovimientoAsync(comida.Id, 700, "2026-10-02", cuentaId: itau.Id, subcuentaId: itau.Subcuentas[0].Id);
        await sesion.CrearMovimientoAsync(comida.Id, 20, "2026-10-03", cuentaId: itau.Id, subcuentaId: dolares.Id);

        async Task<ResumenResponse> Resumen(string filtros) =>
            await (await sesion.Cliente.GetAsync($"/api/resumen?desde=2026-10-01&hasta=2026-10-31{filtros}")).LeerAsync<ResumenResponse>();

        var pesos = await Resumen("");
        var usd = await Resumen("&moneda=USD");
        var soloItauPesos = await Resumen($"&subcuentaId={itau.Subcuentas[0].Id}");

        Assert.Equal(("UYU", 1200m), (pesos.Moneda, pesos.TotalEgresos));
        Assert.Equal(("USD", 20m), (usd.Moneda, usd.TotalEgresos));
        Assert.Equal(700m, soloItauPesos.TotalEgresos);
        var mensual = await (await sesion.Cliente.GetAsync("/api/resumen/mensual?hasta=2026-10-01&meses=1&moneda=USD"))
            .LeerAsync<List<TotalMensual>>();
        Assert.Equal(20m, Assert.Single(mensual).Egresos);
    }

    [Fact]
    public async Task El_resumen_con_moneda_invalida_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.GetAsync("/api/resumen?moneda=dolares");

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([Monedas.MensajeInvalida], problema.De("Moneda"));
    }
}

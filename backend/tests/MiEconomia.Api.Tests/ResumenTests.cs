using System.Net;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Resumen;
using MiEconomia.Api.Tests.Infraestructura;

namespace MiEconomia.Api.Tests;

[Collection(ApiCollection.Nombre)]
public class ResumenTests(ApiFactory api)
{
    [Fact]
    public async Task Calcula_totales_balance_y_desglose_por_categoria_del_periodo()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var sueldo = await sesion.CrearCategoriaAsync("Sueldo X", TipoMovimiento.Ingreso);
        var extras = await sesion.CrearCategoriaAsync("Extras", TipoMovimiento.Ingreso);
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var bus = await sesion.CrearCategoriaAsync("Bus", TipoMovimiento.Egreso);
        await sesion.CrearMovimientoAsync(sueldo.Id, 1000, "2026-10-01");
        await sesion.CrearMovimientoAsync(extras.Id, 250.50m, "2026-10-15");
        await sesion.CrearMovimientoAsync(comida.Id, 300, "2026-10-02");
        await sesion.CrearMovimientoAsync(comida.Id, 100.25m, "2026-10-31");
        await sesion.CrearMovimientoAsync(bus.Id, 200, "2026-10-10");
        await sesion.CrearMovimientoAsync(sueldo.Id, 5000, "2026-11-01"); // fuera del período

        var resumen = await (await sesion.Cliente.GetAsync("/api/resumen?desde=2026-10-01&hasta=2026-10-31"))
            .LeerAsync<ResumenResponse>();

        Assert.Equal(1250.50m, resumen.TotalIngresos);
        Assert.Equal(600.25m, resumen.TotalEgresos);
        Assert.Equal(650.25m, resumen.Balance);
        Assert.Equal(5, resumen.CantidadMovimientos);

        // Ingresos primero y, dentro de cada tipo, de mayor a menor total.
        // El porcentaje es sobre el total de su tipo.
        Assert.Equal(
            [("Sueldo X", 1000m, 1, 80.0m), ("Extras", 250.50m, 1, 20.0m), ("Comida", 400.25m, 2, 66.7m), ("Bus", 200m, 1, 33.3m)],
            resumen.PorCategoria.Select(c => (c.Categoria.Nombre, c.Total, c.Cantidad, c.Porcentaje)));
    }

    [Fact]
    public async Task Periodo_sin_movimientos_devuelve_todo_en_cero()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var resumen = await (await sesion.Cliente.GetAsync("/api/resumen?desde=2020-01-01&hasta=2020-01-31"))
            .LeerAsync<ResumenResponse>();

        Assert.Equal(new DateOnly(2020, 1, 1), resumen.Desde);
        Assert.Equal(new DateOnly(2020, 1, 31), resumen.Hasta);
        Assert.Equal(0, resumen.TotalIngresos);
        Assert.Equal(0, resumen.TotalEgresos);
        Assert.Equal(0, resumen.Balance);
        Assert.Equal(0, resumen.CantidadMovimientos);
        Assert.Empty(resumen.PorCategoria);
    }

    [Fact]
    public async Task Sin_fechas_usa_el_mes_actual()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var hoy = DateOnly.FromDateTime(DateTime.Now);

        var resumen = await (await sesion.Cliente.GetAsync("/api/resumen")).LeerAsync<ResumenResponse>();

        Assert.Equal(new DateOnly(hoy.Year, hoy.Month, 1), resumen.Desde);
        Assert.Equal(new DateOnly(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month)), resumen.Hasta);
    }

    [Fact]
    public async Task Desde_posterior_a_hasta_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.GetAsync("/api/resumen?desde=2026-10-31&hasta=2026-10-01");

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Single(problema.De("Desde"));
    }

    [Fact]
    public async Task Mensual_devuelve_cada_mes_incluidos_los_vacios()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var sueldo = await sesion.CrearCategoriaAsync("Sueldo X", TipoMovimiento.Ingreso);
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        await sesion.CrearMovimientoAsync(comida.Id, 40, "2026-07-05");
        await sesion.CrearMovimientoAsync(sueldo.Id, 900, "2026-09-01");
        await sesion.CrearMovimientoAsync(comida.Id, 950, "2026-09-30");
        await sesion.CrearMovimientoAsync(sueldo.Id, 100, "2026-10-31");
        await sesion.CrearMovimientoAsync(comida.Id, 1, "2026-06-30"); // antes del primer mes
        await sesion.CrearMovimientoAsync(comida.Id, 1, "2026-11-01"); // después del último

        var meses = await (await sesion.Cliente.GetAsync("/api/resumen/mensual?meses=4&hasta=2026-10-15"))
            .LeerAsync<List<TotalMensual>>();

        Assert.Equal(
            [
                new TotalMensual(2026, 7, 0, 40, -40),
                new TotalMensual(2026, 8, 0, 0, 0),
                new TotalMensual(2026, 9, 900, 950, -50),
                new TotalMensual(2026, 10, 100, 0, 100),
            ],
            meses);
    }

    [Fact]
    public async Task Mensual_cruza_el_cambio_de_anio()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var meses = await (await sesion.Cliente.GetAsync("/api/resumen/mensual?meses=3&hasta=2027-01-01"))
            .LeerAsync<List<TotalMensual>>();

        Assert.Equal([(2026, 11), (2026, 12), (2027, 1)], meses.Select(m => (m.Anio, m.Mes)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(37)]
    public async Task Mensual_con_cantidad_de_meses_fuera_de_rango_devuelve_400(int meses)
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.GetAsync($"/api/resumen/mensual?meses={meses}");

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La cantidad de meses debe estar entre 1 y 36."], problema.De("Meses"));
    }
}

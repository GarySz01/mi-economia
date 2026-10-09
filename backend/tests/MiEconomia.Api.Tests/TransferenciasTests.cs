using System.Net;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;
using MiEconomia.Api.Tests.Infraestructura;
using MiEconomia.Api.Transferencias;

namespace MiEconomia.Api.Tests;

[Collection(ApiCollection.Nombre)]
public class TransferenciasTests(ApiFactory api)
{
    [Fact]
    public async Task Crear_devuelve_las_dos_cuentas_y_mueve_el_saldo()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var banco = await sesion.CrearCuentaAsync("Banco", saldoInicial: 1000);

        var transferencia = await sesion.CrearTransferenciaAsync(banco.Id, sesion.CuentaId, 250.50m, "2026-10-03", "  Cajero  ");

        Assert.Equal(250.50m, transferencia.Monto);
        Assert.Equal(new DateOnly(2026, 10, 3), transferencia.Fecha);
        Assert.Equal("Cajero", transferencia.Nota);
        Assert.Equal(new CuentaBasicaResponse(banco.Id, "Banco", TipoCuenta.CuentaBancaria, null), transferencia.Origen);
        Assert.Equal(sesion.CuentaId, transferencia.Destino.Id);
        Assert.Equal(749.50m, await sesion.SaldoAsync(banco.Id));
        Assert.Equal(250.50m, await sesion.SaldoAsync(sesion.CuentaId));
    }

    [Fact]
    public async Task Sin_campos_obligatorios_devuelve_un_error_por_campo()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/transferencias", new { });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La cuenta de origen es obligatoria."], problema.De("CuentaOrigenId"));
        Assert.Equal(["La cuenta de destino es obligatoria."], problema.De("CuentaDestinoId"));
        Assert.Equal(["El monto es obligatorio."], problema.De("Monto"));
        Assert.Equal(["La fecha es obligatoria."], problema.De("Fecha"));
    }

    [Theory]
    [InlineData("0", "El monto debe ser mayor a 0.")]
    [InlineData("-5", "El monto debe ser mayor a 0.")]
    [InlineData("1.234", "El monto puede tener como máximo 2 decimales.")]
    public async Task Monto_invalido_devuelve_400(string monto, string mensaje)
    {
        var sesion = await api.NuevoUsuarioAsync();
        var banco = await sesion.CrearCuentaAsync("Banco");
        var json = $$"""{ "cuentaOrigenId": "{{banco.Id}}", "cuentaDestinoId": "{{sesion.CuentaId}}", "monto": {{monto}}, "fecha": "2026-10-01" }""";

        var respuesta = await sesion.Cliente.PostAsync("/api/transferencias",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([mensaje], problema.De("Monto"));
    }

    [Fact]
    public async Task Origen_y_destino_iguales_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/transferencias",
            new { cuentaOrigenId = sesion.CuentaId, cuentaDestinoId = sesion.CuentaId, monto = 10, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La cuenta de destino tiene que ser distinta de la de origen."], problema.De("CuentaDestinoId"));
    }

    [Fact]
    public async Task Cuentas_inexistentes_devuelven_400()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/transferencias",
            new { cuentaOrigenId = Guid.NewGuid(), cuentaDestinoId = Guid.NewGuid(), monto = 10, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La cuenta de origen no existe."], problema.De("CuentaOrigenId"));
        Assert.Equal(["La cuenta de destino no existe."], problema.De("CuentaDestinoId"));
    }

    [Fact]
    public async Task Listar_filtra_por_fechas_y_por_cuenta_en_cualquiera_de_los_dos_lados()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var banco = await sesion.CrearCuentaAsync("Banco");
        var tarjeta = await sesion.CrearCuentaAsync("Visa", TipoCuenta.TarjetaCredito);
        await sesion.CrearTransferenciaAsync(banco.Id, sesion.CuentaId, 1, "2026-10-01");
        await sesion.CrearTransferenciaAsync(sesion.CuentaId, tarjeta.Id, 2, "2026-10-15");
        await sesion.CrearTransferenciaAsync(banco.Id, tarjeta.Id, 3, "2026-10-31");
        await sesion.CrearTransferenciaAsync(banco.Id, tarjeta.Id, 999, "2026-11-01"); // fuera del rango

        async Task<PaginaResponse<TransferenciaResponse>> Listar(string filtros) =>
            await (await sesion.Cliente.GetAsync($"/api/transferencias?desde=2026-10-01&hasta=2026-10-31{filtros}"))
                .LeerAsync<PaginaResponse<TransferenciaResponse>>();

        var octubre = await Listar("");
        Assert.Equal(3, octubre.Total);
        Assert.Equal([3m, 2m, 1m], octubre.Items.Select(t => t.Monto));

        var delEfectivo = await Listar($"&cuentaId={sesion.CuentaId}");
        Assert.Equal([2m, 1m], delEfectivo.Items.Select(t => t.Monto));
    }

    [Fact]
    public async Task Listar_con_fechas_invertidas_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.GetAsync("/api/transferencias?desde=2026-10-31&hasta=2026-10-01");

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La fecha 'desde' no puede ser posterior a 'hasta'."], problema.De("Desde"));
    }

    [Fact]
    public async Task Actualizar_cambia_todos_los_campos_y_los_saldos()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var banco = await sesion.CrearCuentaAsync("Banco");
        var tarjeta = await sesion.CrearCuentaAsync("Visa", TipoCuenta.TarjetaCredito);
        var transferencia = await sesion.CrearTransferenciaAsync(banco.Id, sesion.CuentaId, 100, "2026-10-01");

        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/transferencias/{transferencia.Id}",
            new { cuentaOrigenId = banco.Id, cuentaDestinoId = tarjeta.Id, monto = 40, fecha = "2026-10-02", nota = "Pago" });

        var actualizada = await respuesta.LeerAsync<TransferenciaResponse>();
        Assert.Equal(tarjeta.Id, actualizada.Destino.Id);
        Assert.Equal((40m, new DateOnly(2026, 10, 2), "Pago"), (actualizada.Monto, actualizada.Fecha, actualizada.Nota));
        Assert.Equal(0, await sesion.SaldoAsync(sesion.CuentaId));
        Assert.Equal(40, await sesion.SaldoAsync(tarjeta.Id));
    }

    [Fact]
    public async Task Eliminar_devuelve_204_restaura_los_saldos_y_la_segunda_vez_404()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var banco = await sesion.CrearCuentaAsync("Banco", saldoInicial: 100);
        var transferencia = await sesion.CrearTransferenciaAsync(banco.Id, sesion.CuentaId, 100, "2026-10-01");

        var primera = await sesion.Cliente.DeleteAsync($"/api/transferencias/{transferencia.Id}");
        var segunda = await sesion.Cliente.DeleteAsync($"/api/transferencias/{transferencia.Id}");

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, segunda.StatusCode);
        Assert.Equal(100, await sesion.SaldoAsync(banco.Id));
    }
}

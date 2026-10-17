using System.Net;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;
using MiEconomia.Api.Tests.Infraestructura;

namespace MiEconomia.Api.Tests;

[Collection(ApiCollection.Nombre)]
public class MovimientosTests(ApiFactory api)
{
    [Fact]
    public async Task Crear_devuelve_el_movimiento_con_su_categoria_y_limpia_la_nota()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso, "#EA580C");

        var movimiento = await sesion.CrearMovimientoAsync(categoria.Id, 25990.50m, "2026-10-02", "  Supermercado  ");

        Assert.Equal(25990.50m, movimiento.Monto);
        Assert.Equal(new DateOnly(2026, 10, 2), movimiento.Fecha);
        Assert.Equal("Supermercado", movimiento.Nota);
        Assert.Equal(categoria, movimiento.Categoria);

        var obtenido = await (await sesion.Cliente.GetAsync($"/api/movimientos/{movimiento.Id}"))
            .LeerAsync<MovimientoResponse>();
        Assert.Equal(movimiento.Id, obtenido.Id);
    }

    [Fact]
    public async Task Nota_vacia_se_guarda_como_null()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);

        var movimiento = await sesion.CrearMovimientoAsync(categoria.Id, 10, "2026-10-02", "   ");

        Assert.Null(movimiento.Nota);
    }

    [Theory]
    [InlineData("0", "El monto debe ser mayor a 0.")]
    [InlineData("-5", "El monto debe ser mayor a 0.")]
    [InlineData("1.234", "El monto puede tener como máximo 2 decimales.")]
    [InlineData("\"mucho\"", "El valor enviado no es válido.")]
    public async Task Monto_invalido_devuelve_400(string monto, string mensaje)
    {
        var sesion = await api.NuevoUsuarioAsync();
        var categoria = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var json = $$"""{ "categoriaId": "{{categoria.Id}}", "cuentaId": "{{sesion.CuentaId}}", "monto": {{monto}}, "fecha": "2026-10-01" }""";

        var respuesta = await sesion.Cliente.PostAsync("/api/movimientos",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([mensaje], problema.De("Monto"));
    }

    [Fact]
    public async Task Sin_campos_obligatorios_devuelve_un_error_por_campo()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/movimientos", new { });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La categoría es obligatoria."], problema.De("CategoriaId"));
        Assert.Equal(["La cuenta es obligatoria."], problema.De("CuentaId"));
        Assert.Equal(["El monto es obligatorio."], problema.De("Monto"));
        Assert.Equal(["La fecha es obligatoria."], problema.De("Fecha"));
    }

    [Fact]
    public async Task Categoria_inexistente_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/movimientos",
            new { categoriaId = Guid.NewGuid(), cuentaId = sesion.CuentaId, monto = 10, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La categoría no existe."], problema.De("CategoriaId"));
    }

    [Fact]
    public async Task Cuenta_inexistente_devuelve_400()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);

        var respuesta = await sesion.Cliente.PostJsonAsync("/api/movimientos",
            new { categoriaId = comida.Id, cuentaId = Guid.NewGuid(), monto = 10, fecha = "2026-10-01" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["La cuenta no existe."], problema.De("CuentaId"));
    }

    [Fact]
    public async Task Crear_devuelve_la_cuenta_del_movimiento()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var tarjeta = await sesion.CrearCuentaAsync("Visa", TipoCuenta.TarjetaCredito);

        var movimiento = await sesion.CrearMovimientoAsync(comida.Id, 10, "2026-10-01", cuentaId: tarjeta.Id);

        Assert.Equal(new CuentaBasicaResponse(tarjeta.Id, "Visa", TipoCuenta.TarjetaCredito, null), movimiento.Cuenta);
    }

    [Fact]
    public async Task Listar_filtra_por_cuenta()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var banco = await sesion.CrearCuentaAsync("Banco");
        await sesion.CrearMovimientoAsync(comida.Id, 10, "2026-10-01");
        await sesion.CrearMovimientoAsync(comida.Id, 20, "2026-10-01", cuentaId: banco.Id);

        var pagina = await (await sesion.Cliente.GetAsync($"/api/movimientos?cuentaId={banco.Id}"))
            .LeerAsync<PaginaResponse<MovimientoResponse>>();

        Assert.Equal(20m, Assert.Single(pagina.Items).Monto);
    }

    [Fact]
    public async Task Listar_filtra_por_fechas_tipo_y_categoria_y_ordena_del_mas_reciente()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var sueldo = await sesion.CrearCategoriaAsync("Sueldo X", TipoMovimiento.Ingreso);
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var transporte = await sesion.CrearCategoriaAsync("Bus", TipoMovimiento.Egreso);
        await sesion.CrearMovimientoAsync(sueldo.Id, 1000, "2026-10-01");
        await sesion.CrearMovimientoAsync(comida.Id, 300, "2026-10-15");
        await sesion.CrearMovimientoAsync(transporte.Id, 50, "2026-10-31");
        await sesion.CrearMovimientoAsync(comida.Id, 999, "2026-09-30"); // fuera del rango
        await sesion.CrearMovimientoAsync(comida.Id, 999, "2026-11-01"); // fuera del rango

        async Task<PaginaResponse<MovimientoResponse>> Listar(string filtros) =>
            await (await sesion.Cliente.GetAsync($"/api/movimientos?desde=2026-10-01&hasta=2026-10-31{filtros}"))
                .LeerAsync<PaginaResponse<MovimientoResponse>>();

        var octubre = await Listar("");
        Assert.Equal(3, octubre.Total);
        Assert.Equal(["2026-10-31", "2026-10-15", "2026-10-01"], octubre.Items.Select(m => m.Fecha.ToString("yyyy-MM-dd")));

        var egresos = await Listar("&tipo=Egreso");
        Assert.Equal([50m, 300m], egresos.Items.Select(m => m.Monto));

        var soloComida = await Listar($"&categoriaId={comida.Id}");
        Assert.Equal(300m, Assert.Single(soloComida.Items).Monto);
    }

    [Fact]
    public async Task Listar_pagina_los_resultados()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        for (var dia = 1; dia <= 5; dia++)
            await sesion.CrearMovimientoAsync(comida.Id, dia, $"2026-10-0{dia}");

        var pagina2 = await (await sesion.Cliente.GetAsync("/api/movimientos?pagina=2&tamanio=2")).LeerAsync<PaginaResponse<MovimientoResponse>>();
        var pagina3 = await (await sesion.Cliente.GetAsync("/api/movimientos?pagina=3&tamanio=2")).LeerAsync<PaginaResponse<MovimientoResponse>>();

        Assert.Equal(5, pagina2.Total);
        Assert.Equal([3m, 2m], pagina2.Items.Select(m => m.Monto));
        Assert.Equal([1m], pagina3.Items.Select(m => m.Monto));
    }

    [Theory]
    [InlineData("desde=2026-10-31&hasta=2026-10-01", "Desde", "La fecha 'desde' no puede ser posterior a 'hasta'.")]
    [InlineData("tamanio=500", "Tamanio", "El tamaño de página debe estar entre 1 y 200.")]
    [InlineData("pagina=0", "Pagina", "La página empieza en 1.")]
    public async Task Listar_con_filtros_invalidos_devuelve_400(string filtros, string campo, string mensaje)
    {
        var sesion = await api.NuevoUsuarioAsync();

        var respuesta = await sesion.Cliente.GetAsync($"/api/movimientos?{filtros}");

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal([mensaje], problema.De(campo));
    }

    [Fact]
    public async Task Actualizar_cambia_todos_los_campos_incluidas_la_categoria_y_la_cuenta()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var regalos = await sesion.CrearCategoriaAsync("Regalos", TipoMovimiento.Ingreso);
        var banco = await sesion.CrearCuentaAsync("Banco");
        var movimiento = await sesion.CrearMovimientoAsync(comida.Id, 100, "2026-10-01", "Original");

        var respuesta = await sesion.Cliente.PutJsonAsync($"/api/movimientos/{movimiento.Id}",
            new { categoriaId = regalos.Id, cuentaId = banco.Id, monto = 250.75m, fecha = "2026-10-05", nota = "Cumpleaños" });

        var actualizado = await respuesta.LeerAsync<MovimientoResponse>();
        Assert.Equal(new MovimientoResponse(movimiento.Id, 250.75m, new DateOnly(2026, 10, 5), "Cumpleaños", regalos,
            new CuentaBasicaResponse(banco.Id, "Banco", TipoCuenta.CuentaBancaria, null), null), actualizado);
    }

    [Fact]
    public async Task Eliminar_devuelve_204_y_la_segunda_vez_404()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var comida = await sesion.CrearCategoriaAsync("Comida", TipoMovimiento.Egreso);
        var movimiento = await sesion.CrearMovimientoAsync(comida.Id, 100, "2026-10-01");

        var primera = await sesion.Cliente.DeleteAsync($"/api/movimientos/{movimiento.Id}");
        var segunda = await sesion.Cliente.DeleteAsync($"/api/movimientos/{movimiento.Id}");

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, segunda.StatusCode);
    }
}

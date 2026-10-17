using System.Net;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;
using MiEconomia.Api.Subcategorias;
using MiEconomia.Api.Subcuentas;
using MiEconomia.Api.Transferencias;

namespace MiEconomia.Api.Tests.Infraestructura;

/// <summary>Atajos para crear datos de prueba a través de la API.</summary>
public static class Datos
{
    public static async Task<CategoriaResponse> CrearCategoriaAsync(
        this Sesion sesion, string nombre, TipoMovimiento tipo, string? color = null)
    {
        var respuesta = await sesion.Cliente.PostJsonAsync("/api/categorias", new { nombre, tipo, color });
        return await respuesta.LeerAsync<CategoriaResponse>(HttpStatusCode.Created);
    }

    public static async Task<SubcategoriaResponse> CrearSubcategoriaAsync(this Sesion sesion, Guid categoriaId, string nombre)
    {
        var respuesta = await sesion.Cliente.PostJsonAsync($"/api/categorias/{categoriaId}/subcategorias", new { nombre });
        return await respuesta.LeerAsync<SubcategoriaResponse>(HttpStatusCode.Created);
    }

    /// <param name="cuentaId">Por defecto, la cuenta que se creó al registrarse.</param>
    /// <param name="subcuentaId">Hace falta solo si la cuenta tiene más de una subcuenta.</param>
    public static async Task<MovimientoResponse> CrearMovimientoAsync(
        this Sesion sesion, Guid categoriaId, decimal monto, string fecha, string? nota = null, Guid? cuentaId = null,
        Guid? subcategoriaId = null, Guid? subcuentaId = null)
    {
        var respuesta = await sesion.Cliente.PostJsonAsync("/api/movimientos",
            new { categoriaId, subcategoriaId, cuentaId = cuentaId ?? sesion.CuentaId, subcuentaId, monto, fecha, nota });
        return await respuesta.LeerAsync<MovimientoResponse>(HttpStatusCode.Created);
    }

    /// <summary>Crea la cuenta con su subcuenta "Disponible", que recibe la moneda y el saldo inicial.</summary>
    public static async Task<CuentaResponse> CrearCuentaAsync(
        this Sesion sesion, string nombre, TipoCuenta tipo = TipoCuenta.CuentaBancaria, decimal saldoInicial = 0,
        string? moneda = null)
    {
        var respuesta = await sesion.Cliente.PostJsonAsync("/api/cuentas", new { nombre, tipo, saldoInicial, moneda });
        return await respuesta.LeerAsync<CuentaResponse>(HttpStatusCode.Created);
    }

    public static async Task<SubcuentaResponse> CrearSubcuentaAsync(
        this Sesion sesion, Guid cuentaId, string nombre, string moneda = "UYU", decimal saldoInicial = 0)
    {
        var respuesta = await sesion.Cliente.PostJsonAsync($"/api/cuentas/{cuentaId}/subcuentas", new { nombre, moneda, saldoInicial });
        return await respuesta.LeerAsync<SubcuentaResponse>(HttpStatusCode.Created);
    }

    /// <param name="subcuentaOrigenId">Hace falta solo si la cuenta de origen tiene más de una subcuenta (igual para el destino).</param>
    /// <param name="montoDestino">Hace falta solo si las subcuentas tienen distinta moneda.</param>
    public static async Task<TransferenciaResponse> CrearTransferenciaAsync(
        this Sesion sesion, Guid cuentaOrigenId, Guid cuentaDestinoId, decimal monto, string fecha, string? nota = null,
        Guid? subcuentaOrigenId = null, Guid? subcuentaDestinoId = null, decimal? montoDestino = null)
    {
        var respuesta = await sesion.Cliente.PostJsonAsync("/api/transferencias",
            new { cuentaOrigenId, cuentaDestinoId, subcuentaOrigenId, subcuentaDestinoId, monto, montoDestino, fecha, nota });
        return await respuesta.LeerAsync<TransferenciaResponse>(HttpStatusCode.Created);
    }

    public static async Task<CuentaResponse> ObtenerCuentaAsync(this Sesion sesion, Guid cuentaId) =>
        await (await sesion.Cliente.GetAsync($"/api/cuentas/{cuentaId}")).LeerAsync<CuentaResponse>();

    /// <summary>Saldo actual de una cuenta de una sola moneda, según la API.</summary>
    public static async Task<decimal> SaldoAsync(this Sesion sesion, Guid cuentaId) =>
        (await sesion.ObtenerCuentaAsync(cuentaId)).SaldoUnico();

    /// <summary>Saldo de una cuenta que tiene una sola moneda (falla si tiene más de una).</summary>
    public static decimal SaldoUnico(this CuentaResponse cuenta) => Assert.Single(cuenta.Saldos).Saldo;

    /// <summary>Nombre único, para no chocar con las categorías por defecto ni entre tests.</summary>
    public static string Unico(string prefijo) => $"{prefijo} {Guid.NewGuid().ToString()[..8]}";
}

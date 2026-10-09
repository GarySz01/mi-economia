using System.Net;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;
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

    /// <param name="cuentaId">Por defecto, la cuenta que se creó al registrarse.</param>
    public static async Task<MovimientoResponse> CrearMovimientoAsync(
        this Sesion sesion, Guid categoriaId, decimal monto, string fecha, string? nota = null, Guid? cuentaId = null)
    {
        var respuesta = await sesion.Cliente.PostJsonAsync("/api/movimientos",
            new { categoriaId, cuentaId = cuentaId ?? sesion.CuentaId, monto, fecha, nota });
        return await respuesta.LeerAsync<MovimientoResponse>(HttpStatusCode.Created);
    }

    public static async Task<CuentaResponse> CrearCuentaAsync(
        this Sesion sesion, string nombre, TipoCuenta tipo = TipoCuenta.CuentaBancaria, decimal saldoInicial = 0)
    {
        var respuesta = await sesion.Cliente.PostJsonAsync("/api/cuentas", new { nombre, tipo, saldoInicial });
        return await respuesta.LeerAsync<CuentaResponse>(HttpStatusCode.Created);
    }

    public static async Task<TransferenciaResponse> CrearTransferenciaAsync(
        this Sesion sesion, Guid cuentaOrigenId, Guid cuentaDestinoId, decimal monto, string fecha, string? nota = null)
    {
        var respuesta = await sesion.Cliente.PostJsonAsync("/api/transferencias",
            new { cuentaOrigenId, cuentaDestinoId, monto, fecha, nota });
        return await respuesta.LeerAsync<TransferenciaResponse>(HttpStatusCode.Created);
    }

    /// <summary>Saldo actual de una cuenta, según la API.</summary>
    public static async Task<decimal> SaldoAsync(this Sesion sesion, Guid cuentaId) =>
        (await (await sesion.Cliente.GetAsync($"/api/cuentas/{cuentaId}")).LeerAsync<CuentaResponse>()).Saldo;

    /// <summary>Nombre único, para no chocar con las categorías por defecto ni entre tests.</summary>
    public static string Unico(string prefijo) => $"{prefijo} {Guid.NewGuid().ToString()[..8]}";
}

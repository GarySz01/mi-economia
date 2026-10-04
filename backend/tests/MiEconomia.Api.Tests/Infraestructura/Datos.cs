using System.Net;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Movimientos;

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

    public static async Task<MovimientoResponse> CrearMovimientoAsync(
        this Sesion sesion, Guid categoriaId, decimal monto, string fecha, string? nota = null)
    {
        var respuesta = await sesion.Cliente.PostJsonAsync("/api/movimientos", new { categoriaId, monto, fecha, nota });
        return await respuesta.LeerAsync<MovimientoResponse>(HttpStatusCode.Created);
    }

    /// <summary>Nombre único, para no chocar con las categorías por defecto ni entre tests.</summary>
    public static string Unico(string prefijo) => $"{prefijo} {Guid.NewGuid().ToString()[..8]}";
}

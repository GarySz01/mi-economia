using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit.Sdk;

namespace MiEconomia.Api.Tests.Infraestructura;

/// <summary>Respuesta de error de la API (ProblemDetails).</summary>
public record Problema(string? Title, int Status, Dictionary<string, string[]>? Errors)
{
    /// <summary>Errores de un campo (el nombre como lo devuelve la API, ej. "Monto").</summary>
    public string[] De(string campo) => Errors?.GetValueOrDefault(campo) ?? [];
}

public static class HttpExtensions
{
    /// <summary>Mismas opciones que la API: camelCase y enums como texto.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient cliente, string url, object cuerpo) =>
        cliente.PostAsJsonAsync(url, cuerpo, Json);

    public static Task<HttpResponseMessage> PutJsonAsync(this HttpClient cliente, string url, object cuerpo) =>
        cliente.PutAsJsonAsync(url, cuerpo, Json);

    /// <summary>Verifica el código de estado y lee el cuerpo. Si el código no coincide, el error incluye el cuerpo.</summary>
    public static async Task<T> LeerAsync<T>(this HttpResponseMessage respuesta, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        await respuesta.VerificarAsync(esperado);
        return (await respuesta.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public static Task<Problema> LeerProblemaAsync(this HttpResponseMessage respuesta, HttpStatusCode esperado) =>
        respuesta.LeerAsync<Problema>(esperado);

    public static async Task VerificarAsync(this HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        if (respuesta.StatusCode != esperado)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            throw new XunitException(
                $"Se esperaba {(int)esperado} {esperado} pero llegó {(int)respuesta.StatusCode} {respuesta.StatusCode}. Cuerpo: {cuerpo}");
        }
    }
}

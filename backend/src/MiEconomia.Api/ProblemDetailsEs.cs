using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace MiEconomia.Api;

/// <summary>
/// Traduce al español las respuestas de error que genera ASP.NET Core, para que el frontend
/// pueda mostrarlas directamente. Los títulos que ya escribimos nosotros se dejan como están.
/// </summary>
public static partial class ProblemDetailsEs
{
    private static readonly Dictionary<string, string> Titulos = new()
    {
        ["One or more validation errors occurred."] = "Hay errores en los datos enviados.",
        ["Bad Request"] = "La solicitud no es válida.",
        ["Unauthorized"] = "Necesitas iniciar sesión.",
        ["Forbidden"] = "No tienes permiso para hacer esto.",
        ["Not Found"] = "No se encontró lo que buscas.",
        ["Conflict"] = "La operación no se puede completar.",
        ["An error occurred while processing your request."] = "Ocurrió un error inesperado. Intenta de nuevo.",
    };

    public static void Personalizar(ProblemDetailsContext contexto)
    {
        var problema = contexto.ProblemDetails;
        if (problema.Title is not null && Titulos.TryGetValue(problema.Title, out var titulo))
            problema.Title = titulo;

        if (problema is ValidationProblemDetails validacion)
            TraducirErroresDeJson(validacion.Errors);
    }

    /// <summary>
    /// Cuando el JSON no se puede leer (por ejemplo "tipo": "Otro"), System.Text.Json reporta la ruta
    /// "$.tipo" con un mensaje técnico, y MVC agrega además "The request field is required.".
    /// Se reemplaza por un error legible en el campo correspondiente.
    /// </summary>
    private static void TraducirErroresDeJson(IDictionary<string, string[]> errores)
    {
        var clavesJson = errores.Keys.Where(k => k.StartsWith('$')).ToList();
        if (clavesJson.Count == 0)
            return;

        foreach (var clave in clavesJson)
        {
            errores.Remove(clave);
            var campo = clave.TrimStart('$', '.');
            campo = campo.Length == 0 ? "body" : char.ToUpperInvariant(campo[0]) + campo[1..];
            errores[campo] = [campo == "body" ? "El cuerpo de la solicitud no es un JSON válido." : "El valor enviado no es válido."];
        }

        foreach (var (clave, mensajes) in errores.ToList())
        {
            if (mensajes.All(m => CampoRequeridoEnIngles().IsMatch(m)))
                errores.Remove(clave);
        }
    }

    [GeneratedRegex("^The .+ field is required\\.$")]
    private static partial Regex CampoRequeridoEnIngles();
}

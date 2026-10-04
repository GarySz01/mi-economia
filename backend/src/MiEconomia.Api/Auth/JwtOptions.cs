using System.ComponentModel.DataAnnotations;

namespace MiEconomia.Api.Auth;

public class JwtOptions
{
    public const string Seccion = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Clave de firma HMAC. Nunca va en appsettings: usar user-secrets o la variable Jwt__Key.</summary>
    [Required, MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiracionMinutos { get; set; } = 60;
}

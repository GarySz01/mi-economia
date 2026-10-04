using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MiEconomia.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id del usuario autenticado (claim "sub" del JWT).</summary>
    public static Guid GetUsuarioId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("El token no tiene el claim 'sub'."));
}

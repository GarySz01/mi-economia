using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Auth;

public record TokenGenerado(string Token, DateTimeOffset ExpiraEn);

public class TokenService(IOptions<JwtOptions> options, TimeProvider tiempo)
{
    private readonly JwtOptions _jwt = options.Value;

    public TokenGenerado Generar(Usuario usuario)
    {
        var ahora = tiempo.GetUtcNow();
        var expira = ahora.AddMinutes(_jwt.ExpiracionMinutos);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Name, usuario.Nombre),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: ahora.UtcDateTime,
            expires: expira.UtcDateTime,
            signingCredentials: credenciales);

        return new TokenGenerado(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

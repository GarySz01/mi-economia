using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiEconomia.Api.Auth;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Cuentas;
using MiEconomia.Api.Data;
using MiEconomia.Api.Domain;

namespace MiEconomia.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AppDbContext db,
    UserManager<Usuario> userManager,
    SignInManager<Usuario> signInManager,
    TokenService tokenService) : ControllerBase
{
    [HttpPost("registro")]
    [EnableRateLimiting(RateLimits.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registro(RegistroRequest request)
    {
        var email = request.Email.Trim();
        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Email = email,
            UserName = email,
        };

        // UserManager usa el mismo AppDbContext, así que el usuario, sus categorías y su cuenta se guardan juntos o no se guarda nada.
        await using var transaccion = await db.Database.BeginTransactionAsync();

        var resultado = await userManager.CreateAsync(usuario, request.Password);
        if (!resultado.Succeeded)
        {
            // DistinctBy: como UserName = Email, Identity informa el duplicado dos veces con el mismo texto.
            foreach (var error in resultado.Errors.DistinctBy(e => e.Description))
            {
                var campo = error.Code.StartsWith("Password") ? nameof(request.Password) : nameof(request.Email);
                ModelState.AddModelError(campo, error.Description);
            }
            return ValidationProblem(ModelState);
        }

        db.Categorias.AddRange(CategoriasPorDefecto.Para(usuario.Id));
        db.Cuentas.Add(CuentaPorDefecto.Para(usuario.Id));
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return StatusCode(StatusCodes.Status201Created, CrearRespuesta(usuario));
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimits.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var usuario = await userManager.FindByEmailAsync(request.Email.Trim());
        if (usuario is null)
            return CredencialesInvalidas();

        var resultado = await signInManager.CheckPasswordSignInAsync(usuario, request.Password, lockoutOnFailure: true);
        if (resultado.IsLockedOut)
            return Problem(
                title: "Cuenta bloqueada temporalmente por demasiados intentos fallidos. Prueba de nuevo en unos minutos.",
                statusCode: StatusCodes.Status401Unauthorized);
        if (!resultado.Succeeded)
            return CredencialesInvalidas();

        return Ok(CrearRespuesta(usuario));
    }

    [Authorize]
    [HttpGet("yo")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Yo()
    {
        var usuario = await userManager.FindByIdAsync(User.GetUsuarioId().ToString());
        if (usuario is null)
            return Unauthorized();

        return Ok(new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email!));
    }

    private AuthResponse CrearRespuesta(Usuario usuario)
    {
        var token = tokenService.Generar(usuario);
        return new AuthResponse(token.Token, token.ExpiraEn,
            new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email!));
    }

    // Mismo mensaje si el email no existe o la contraseña es incorrecta, para no revelar qué emails están registrados.
    private ObjectResult CredencialesInvalidas() =>
        Problem(title: "Email o contraseña incorrectos.", statusCode: StatusCodes.Status401Unauthorized);
}

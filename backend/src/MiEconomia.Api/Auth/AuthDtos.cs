using System.ComponentModel.DataAnnotations;

namespace MiEconomia.Api.Auth;

public record RegistroRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre puede tener hasta 100 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [StringLength(256)]
    string Email,

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100)]
    string Password);

public record LoginRequest(
    [Required(ErrorMessage = "El email es obligatorio.")]
    string Email,

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    string Password);

public record UsuarioResponse(Guid Id, string Nombre, string Email);

public record AuthResponse(string Token, DateTimeOffset ExpiraEn, UsuarioResponse Usuario);

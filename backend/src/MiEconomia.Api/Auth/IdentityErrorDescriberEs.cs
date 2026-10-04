using Microsoft.AspNetCore.Identity;

namespace MiEconomia.Api.Auth;

/// <summary>Mensajes de Identity en español, para mostrarlos tal cual en la interfaz.</summary>
public class IdentityErrorDescriberEs : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"El email '{email}' ya está registrado." };

    // El UserName es el email, así que el nombre de usuario duplicado se informa como email duplicado.
    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateEmail), Description = $"El email '{userName}' ya está registrado." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = $"El email '{email}' no es válido." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidEmail), Description = $"El email '{userName}' no es válido." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"La contraseña debe tener al menos {length} caracteres." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "La contraseña debe tener al menos un número." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "La contraseña debe tener al menos una minúscula." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "La contraseña debe tener al menos una mayúscula." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "La contraseña debe tener al menos un símbolo." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"La contraseña debe tener al menos {uniqueChars} caracteres distintos." };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "La contraseña es incorrecta." };
}

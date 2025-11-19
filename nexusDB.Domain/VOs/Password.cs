using nexusDB.Domain.SeedWork;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace nexusDB.Domain.VOs;

/// <summary>
/// Value Object para encapsular la contraseña de un usuario.
/// Asegura que la contraseña siempre cumpla con las reglas de negocio definidas.
/// </summary>
public sealed class Password : ValueObject
{
    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Crea una instancia de Password, validando las reglas de seguridad.
    /// </summary>
    public static Result<Password> Create(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure<Password>("Password cannot be empty.");
        }

        // Reglas de seguridad (ejemplo, ajustar según OWASP Top 10 y requisitos específicos)
        // Mínimo 8 caracteres
        if (password.Length < 8)
        {
            return Result.Failure<Password>("Password must be at least 8 characters long.");
        }
        // Al menos una letra mayúscula
        if (!Regex.IsMatch(password, "[A-Z]"))
        {
            return Result.Failure<Password>("Password must contain at least one uppercase letter.");
        }
        // Al menos una letra minúscula
        if (!Regex.IsMatch(password, "[a-z]"))
        {
            return Result.Failure<Password>("Password must contain at least one lowercase letter.");
        }
        // Al menos un dígito
        if (!Regex.IsMatch(password, "[0-9]"))
        {
            return Result.Failure<Password>("Password must contain at least one digit.");
        }
        // Al menos un carácter especial (ej. !@#$%^&*)
        if (!Regex.IsMatch(password, "[!@#$%^&*]"))
        {
            return Result.Failure<Password>("Password must contain at least one special character (!@#$%^&*).");
        }

        return Result.Success(new Password(password));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    // Conversión implícita a string para facilitar el uso
    public static implicit operator string(Password password) => password.Value;
}

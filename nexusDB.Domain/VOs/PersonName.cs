using nexusDB.Domain.SeedWork;
using System.Collections.Generic;

namespace nexusDB.Domain.VOs;

/// <summary>
/// Value Object para encapsular nombres de personas (Name, LastName).
/// Asegura que el nombre siempre cumpla con reglas básicas de formato.
/// </summary>
public sealed class PersonName : ValueObject
{
    public string Value { get; }

    private PersonName(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Crea una instancia de PersonName, validando que no esté vacío y tenga un formato básico.
    /// </summary>
    public static Result<PersonName> Create(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<PersonName>("Name cannot be empty.");
        }
        // Opcional: Añadir más validaciones, ej. longitud máxima, solo letras, capitalización.
        if (name.Length > 50)
        {
            return Result.Failure<PersonName>("Name cannot exceed 50 characters.");
        }

        return Result.Success(new PersonName(name));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    // Conversión implícita a string para facilitar el uso
    public static implicit operator string(PersonName name) => name.Value;
}

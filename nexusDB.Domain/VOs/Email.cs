using System.Text.RegularExpressions;
using nexusDB.Domain.SeedWork;

namespace nexusDB.Domain.VOs;

public sealed class Email : ValueObject
{
    public string Value { get; }
 
    private Email(string value)
    {
        Value = value;
    }
 
    public static Result<Email> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<Email>("Email cannot be empty.");
        }
 
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return Result.Failure<Email>("Email is not in a valid format.");
        }
 
        return Result.Success(new Email(email));
    }
 
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
 
    public static implicit operator string(Email email) => email.Value;
}
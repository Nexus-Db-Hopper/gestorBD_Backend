using System.ComponentModel.DataAnnotations.Schema;
using nexusDB.Domain.SeedWork;
using nexusDB.Domain.VOs;

namespace nexusDB.Domain.Entities;

/// <summary>
/// Entidad que representa a un usuario.
/// Sigue el principio de inmutabilidad para sus propiedades principales.
/// </summary>
public class User
{
    public int Id { get; private set; }
    public PersonName Name { get; private set; } = null!;
    public PersonName LastName { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    
    // Se inicializa con null! para satisfacer al compilador de C# sobre nulabilidad.
    // Entity Framework Core se encargará de poblar esta propiedad al leer desde la base de datos.
    public string Password { get; set; } = null!; 

    [ForeignKey("Role")]
    [Column("RoleId")]
    public int IdRole { get; private set; }
    public Role Role { get; private set; } = null!;

    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }

    // Constructor privado para EF Core
    private User() { }

    private User(PersonName name, PersonName lastName, Email email, string password, int idRole)
    {
        Name = name;
        LastName = lastName;
        Email = email;
        Password = password;
        IdRole = idRole;
    }

    /// <summary>
    /// Método de fábrica para crear una instancia de User, asegurando su validez.
    /// </summary>
    public static Result<User> Create(string name, string lastName, string email, string password, int idRole)
    {
        var nameResult = PersonName.Create(name);
        if (nameResult.IsFailure) return Result.Failure<User>(nameResult.Error!);

        var lastNameResult = PersonName.Create(lastName);
        if (lastNameResult.IsFailure) return Result.Failure<User>(lastNameResult.Error!);

        var emailResult = VOs.Email.Create(email);
        if (emailResult.IsFailure) return Result.Failure<User>(emailResult.Error!);

        var passwordResult = VOs.Password.Create(password);
        if (passwordResult.IsFailure) return Result.Failure<User>(passwordResult.Error!);

        // La contraseña se pasa en texto plano y el servicio se encargará de hashearla.
        return Result.Success(new User(nameResult.Value!, lastNameResult.Value!, emailResult.Value!, passwordResult.Value!, idRole));
    }
}

using System.ComponentModel.DataAnnotations.Schema; // Necesario para [ForeignKey]

namespace nexusDB.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    
    // Explicitamente indicamos que IdRole es la clave foránea para la propiedad de navegación Role.
    [ForeignKey("Role")] // <-- Indica que IdRole es la FK para la propiedad 'Role'
    public int IdRole { get; set; }
    
    // Propiedad de navegación para la relación 1:N
    public Role Role { get; set; } = null!; // <-- Aseguramos que Role no sea nulo si IdRole está presente

    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }
}

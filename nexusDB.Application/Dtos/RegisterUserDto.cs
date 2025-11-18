using System.ComponentModel.DataAnnotations;

// Namespace corregido para alinearse con la capa de Aplicación
namespace nexusDB.Application.Dtos;

public class RegisterUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;
}

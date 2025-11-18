using System.ComponentModel.DataAnnotations;

// Namespace corregido para alinearse con la capa de Aplicación
namespace nexusDB.Application.Dtos;

public class LoginUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

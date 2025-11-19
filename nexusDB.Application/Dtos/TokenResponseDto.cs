// Namespace corregido para alinearse con la capa de Aplicación
namespace nexusDB.Application.Dtos;

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

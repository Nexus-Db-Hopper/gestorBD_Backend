using nexusDB.Application.Common;
using nexusDB.Application.Dtos;
using System.Threading.Tasks;

namespace nexusDB.Application.Interfaces;

/// <summary>
/// Contrato para el servicio de autenticación. Define los casos de uso de la aplicación.
/// Utiliza el patrón Result para un manejo de errores explícito y robusto.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    /// <returns>Un objeto Result que indica el éxito o fracaso de la operación.</returns>
    Task<Result> RegisterUserAsync(RegisterUserDto registerDto);

    /// <summary>
    /// Autentica a un usuario y devuelve los tokens de sesión.
    /// </summary>
    /// <returns>Un Result que contiene los tokens si el login es exitoso.</returns>
    Task<Result<TokenResponseDto>> LoginUserAsync(LoginUserDto loginDto);

    /// <summary>
    /// Refresca una sesión utilizando un Refresh Token válido.
    /// </summary>
    /// <returns>Un Result que contiene el nuevo par de tokens.</returns>
    Task<Result<TokenResponseDto>> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Cierra la sesión de un usuario invalidando su Refresh Token.
    /// </summary>
    /// <returns>Un objeto Result que indica el éxito o fracaso de la operación.</returns>
    Task<Result> LogoutAsync(string userId);
}

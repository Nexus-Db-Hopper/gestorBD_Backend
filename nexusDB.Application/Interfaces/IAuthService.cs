using nexusDB.Application.Dtos;
using System.Threading.Tasks;

namespace nexusDB.Application.Interfaces;

public interface IAuthService
{
    Task<(bool Succeeded, string? ErrorMessage)> RegisterUserAsync(RegisterUserDto registerDto);
    Task<(bool Succeeded, TokenResponseDto? Tokens, string? ErrorMessage)> LoginUserAsync(LoginUserDto loginDto);
    Task<(bool Succeeded, TokenResponseDto? Tokens, string? ErrorMessage)> RefreshTokenAsync(string refreshToken);
}

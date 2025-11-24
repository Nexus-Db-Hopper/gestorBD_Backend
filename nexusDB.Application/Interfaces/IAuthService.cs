using nexusDB.Domain.SeedWork; // <-- USING ACTUALIZADO
using nexusDB.Application.Dtos;
using System.Threading.Tasks;

namespace nexusDB.Application.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterUserAsync(RegisterUserDto registerDto);
    Task<Result<TokenResponseDto>> LoginUserAsync(LoginUserDto loginDto);
    Task<Result<TokenResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<Result> LogoutAsync(string userId);
}

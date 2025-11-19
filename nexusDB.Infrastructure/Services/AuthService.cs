using Microsoft.EntityFrameworkCore;
using nexusDB.Application.Common;
using nexusDB.Application.Dtos;
using nexusDB.Application.Interfaces;
using nexusDB.Domain.Entities;
using nexusDB.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace nexusDB.Infrastructure.Services;

/// <summary>
/// Implementación concreta del servicio de autenticación.
/// Contiene la lógica de negocio y la interacción con la capa de datos.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<Result> RegisterUserAsync(RegisterUserDto registerDto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
        {
            return Result.Failure("User with this email already exists.");
        }

        var defaultRole = await _context.Roles.SingleOrDefaultAsync(r => r.SpecificRole == "User");
        if (defaultRole == null)
        {
            return Result.Failure("Default user role not found. System configuration error.");
        }

        var user = new User
        {
            Email = registerDto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            Name = registerDto.Name,
            LastName = registerDto.LastName,
            IdRole = defaultRole.Id
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<TokenResponseDto>> LoginUserAsync(LoginUserDto loginDto)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
        {
            return Result.Failure<TokenResponseDto>("Invalid credentials.");
        }

        var accessToken = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = BCrypt.Net.BCrypt.HashPassword(refreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        var tokens = new TokenResponseDto { AccessToken = accessToken, RefreshToken = refreshToken };
        return Result.Success(tokens);
    }

    public async Task<Result<TokenResponseDto>> RefreshTokenAsync(string refreshToken)
    {
        var usersWithTokens = await _context.Users
            .Where(u => u.RefreshToken != null && u.RefreshTokenExpiry > DateTime.UtcNow)
            .Include(u => u.Role)
            .ToListAsync();

        User? user = usersWithTokens.FirstOrDefault(u => BCrypt.Net.BCrypt.Verify(refreshToken, u.RefreshToken!));

        if (user == null)
        {
            return Result.Failure<TokenResponseDto>("Invalid or expired refresh token.");
        }

        var newAccessToken = _jwtService.GenerateToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        var tokens = new TokenResponseDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
        return Result.Success(tokens);
    }

    public async Task<Result> LogoutAsync(string userId)
    {
        if (!int.TryParse(userId, out var id))
        {
            return Result.Failure("Invalid user ID format.");
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Result.Success();
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiry = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result.Success();
    }
}

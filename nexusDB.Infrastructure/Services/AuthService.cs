using Microsoft.EntityFrameworkCore;
using nexusDB.Application.Dtos;
using nexusDB.Application.Interfaces;
using nexusDB.Domain.Entities;
using nexusDB.Infrastructure.Data;
using System.Threading.Tasks;

namespace nexusDB.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> RegisterUserAsync(RegisterUserDto registerDto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
        {
            return (false, "User with this email already exists.");
        }

        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.SpecificRole == "User");
        if (defaultRole == null)
        {
            defaultRole = new Role { SpecificRole = "User" };
            _context.Roles.Add(defaultRole);
            await _context.SaveChangesAsync();
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

        return (true, null);
    }

    public async Task<(bool Succeeded, TokenResponseDto? Tokens, string? ErrorMessage)> LoginUserAsync(LoginUserDto loginDto)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
        {
            return (false, null, "Invalid credentials.");
        }

        var accessToken = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = System.DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        var tokens = new TokenResponseDto { AccessToken = accessToken, RefreshToken = refreshToken };
        return (true, tokens, null);
    }

    public async Task<(bool Succeeded, TokenResponseDto? Tokens, string? ErrorMessage)> RefreshTokenAsync(string refreshToken)
    {
        var user = await _context.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiry <= System.DateTime.UtcNow)
        {
            return (false, null, "Invalid or expired refresh token.");
        }

        var newAccessToken = _jwtService.GenerateToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        await _context.SaveChangesAsync();

        var tokens = new TokenResponseDto { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
        return (true, tokens, null);
    }
}

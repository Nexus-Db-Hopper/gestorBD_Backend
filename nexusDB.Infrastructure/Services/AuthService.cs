using Microsoft.EntityFrameworkCore;
using nexusDB.Application.Dtos;
using nexusDB.Application.Interfaces;
using nexusDB.Domain.Entities;
using nexusDB.Domain.VOs;
using nexusDB.Infrastructure.Data;
using nexusDB.Domain.SeedWork;


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

    public async Task<Result> RegisterUserAsync(RegisterUserDto registerDto)
    {
        var defaultRole = await _context.Roles.SingleOrDefaultAsync(r => r.SpecificRole == "User");
        if (defaultRole == null)
        {
            return Result.Failure("Default user role not found. System configuration error.");
        }

        var userResult = User.Create(
            registerDto.Name,
            registerDto.LastName,
            registerDto.Email,
            registerDto.Password,
            defaultRole.Id
        );

        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Error!);
        }

        // --- CORRECCIÓN DE LA CONSULTA LINQ ---
        // ANTES: Se intentaba acceder a u.Email.Value, lo cual EF Core no puede traducir.
        // AHORA: Se compara directamente el objeto Email. EF Core usará el ValueConverter para traducir esto a una comparación de strings en SQL.
        var emailToCheck = userResult.Value!.Email;
        if (await _context.Users.AnyAsync(u => u.Email == emailToCheck))
        {
            return Result.Failure("User with this email already exists.");
        }

        var user = userResult.Value!;
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<TokenResponseDto>> LoginUserAsync(LoginUserDto loginDto)
    {
        // --- CORRECCIÓN DE LA CONSULTA LINQ ---
        // Creamos una instancia del Value Object Email para usarla en la consulta.
        var emailToFindResult = Email.Create(loginDto.Email);
        if (emailToFindResult.IsFailure)
        {
            // Si el formato del email es inválido, no puede existir en la BD.
            return Result.Failure<TokenResponseDto>("Invalid credentials.");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == emailToFindResult.Value); // Comparamos directamente el objeto Email.

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

    // ... (el resto de los métodos no necesitan cambios)
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

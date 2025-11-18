using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using nexusDB.Application.Configuration;
using nexusDB.Application.Interfaces;
using nexusDB.Domain.Entities;

namespace nexusDB.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(User user)
    {
        // ¡IMPORTANTE! En una aplicación real, te asegurarías de que user.Role no es nulo
        // antes de intentar acceder a sus propiedades.
        if (user.Role == null)
        {
            throw new ArgumentNullException(nameof(user.Role), "El rol del usuario no puede ser nulo al generar un token.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                // ***** LA CORRECCIÓN ESTÁ AQUÍ *****
                new Claim(ClaimTypes.Role, user.Role.SpecificRole) 
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public bool ValidateRefreshToken(User user, string refreshToken)
    {
        return user.RefreshToken == refreshToken && user.RefreshTokenExpiry > DateTime.UtcNow;
    }
}

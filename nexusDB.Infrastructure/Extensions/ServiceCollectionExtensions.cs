using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nexusDB.Application.Interfaces;
using nexusDB.Infrastructure.Data;
using nexusDB.Infrastructure.Services;

namespace nexusDB.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string? conn = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(conn))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(conn, ServerVersion.AutoDetect(conn))
        );

        // Registrar los servicios de infraestructura
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>(); // <-- REGISTRO DEL NUEVO SERVICIO

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nexusDB.Application.Interfaces;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Application.Interfaces.Repositories;
using nexusDB.Application.Interfaces.Security;
using nexusDB.Domain.Docker;
using nexusDB.Domain.Docker.Providers;
using nexusDB.Infrastructure.Data;
using nexusDB.Infrastructure.Repositories;
using nexusDB.Infrastructure.Services;
using nexusDB.Infrastructure.Services.Security;

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
            options.UseMySql(conn,
                ServerVersion.AutoDetect(conn))
        );

        // Registrar los servicios de infraestructura
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInstanceRepository, InstanceRepository>();
        services.AddScoped<IDatabaseProviderFactory, DatabaseProviderFactory>();
        services.AddSingleton<IAesEncryptionService, AesEncryptionService>();
        services.AddSingleton<IDatabaseProvider, MySqlProvider>();      // Supuestamente este se elimina ya que el DatabaseProviderFactory permite que el cliente decida cual motor desea. REVISAR
        services.AddSingleton<IDatabaseProvider, SqlServerProvider>();      // Supuestamente este se elimina ya que el DatabaseProviderFactory permite que el cliente decida cual motor desea. REVISAR
        return services;
    }
}

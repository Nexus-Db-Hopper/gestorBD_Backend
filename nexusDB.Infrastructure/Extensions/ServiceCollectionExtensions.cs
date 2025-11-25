using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nexusDB.Application.Interfaces;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Application.Interfaces.Repositories;
using nexusDB.Application.Interfaces.Security;
using nexusDB.Domain.Docker.Providers;
using nexusDB.Infrastructure.Data;
using nexusDB.Infrastructure.Repositories;
using nexusDB.Infrastructure.Services;
using nexusDB.Infrastructure.Services.Security;
using nexusDB.Domain.Docker; // Added this using directive

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

        // Register Infrastructure Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInstanceRepository, InstanceRepository>();
        services.AddScoped<IDatabaseProviderFactory, DatabaseProviderFactory>();
        services.AddSingleton<IAesEncryptionService, AesEncryptionService>();
        
        // Register Database Providers (Fully integrated from both branches)
        // Relational providers (typically Scoped)
        services.AddScoped<IDatabaseProvider, MySqlProvider>();
        services.AddScoped<IDatabaseProvider, PostgresProvider>(); 
        services.AddScoped<IDatabaseProvider, SqlServerProvider>(); 

        // NoSQL/Cache providers (often Singleton for connection pooling)
        services.AddSingleton<IDatabaseProvider, RedisProvider>();     
        services.AddSingleton<IDatabaseProvider, MongoDbProvider>();   
        
        return services;
    }
}

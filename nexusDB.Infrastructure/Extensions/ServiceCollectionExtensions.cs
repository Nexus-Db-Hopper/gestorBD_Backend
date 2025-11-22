using Docker.DotNet;
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
using System;
using nexusDB.Domain.Docker;

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

        // --- Docker Client Registration ---
        services.AddSingleton<DockerClient>(sp =>
        {
            var dockerUri = IsWindows() ? "npipe://./pipe/docker_engine" : "unix:///var/run/docker.sock";
            return new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
        });

        // Register Infrastructure Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInstanceRepository, InstanceRepository>();
        services.AddScoped<IDatabaseProviderFactory, DatabaseProviderFactory>();
        services.AddSingleton<IAesEncryptionService, AesEncryptionService>();

        // Register Database Providers
        services.AddScoped<IDatabaseProvider, MySqlProvider>();
        
        return services;
    }

    private static bool IsWindows()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT;
    }
}

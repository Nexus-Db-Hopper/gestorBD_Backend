using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Domain.Entities;
using Dapper;

namespace nexusDB.Domain.Docker.Providers;

public class MySqlProvider : IDatabaseProvider
{
    private readonly string? _host;
    private readonly int _port;
    private readonly string? _adminUser;
    private readonly string? _adminPassword;

    public MySqlProvider(IConfiguration config)
    {
        _host = config["Containers:MySqlHost"];
        _port = int.Parse(config["Containers:MySqlPort"] ?? string.Empty);
        _adminUser = config["Containers:MySqlAdminUser"];
        _adminPassword = config["Containers:MySqlAdminPassword"];
    }

    // Esto determina a cual Engine pertenece este proveedor (se selecciona en el factory)
    public string Engine => "mysql";

    // Este metodo es para crear el contenedor
    public async Task CreateContainerAsync(Instance instance, string password)
    {
        var connectionString = $"server={_host};port={_port};user={_adminUser};password={_adminPassword};";
        using var conn =  new MySqlConnection(connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"CREATE DATABASE `{instance.Name}`;");
        await conn.ExecuteAsync($"CREATE USER '{instance.Username}'@'%' IDENTIFIED BY '{password}';");
        await conn.ExecuteAsync($"GRANT ALL PRIVILEGES ON `{instance.Name}`.* TO '{instance.Username}'@'%';");
        await conn.ExecuteAsync("FLUSH PRIVILEGES");
    }

    public Task StartAsync(Instance instance)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(Instance instance)
    {
        throw new NotImplementedException();
    }

    public async Task<QueryResultDto> ExecuteQueryAsync(Instance instance, string query, string decryptedPassword)
    {
        throw new NotImplementedException();
    }
}
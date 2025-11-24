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

    public async Task StartAsync(Instance instance)
    {
        string connectionString = $"server={_host};port={_port};user={_adminUser};password={_adminPassword}";
        using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();
        string sql = $"ALTER USER `{instance.Username}`@'%' ACCOUNT UNLOCK";
        using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task StopAsync(Instance instance)
    {
        string connectionString = $"server={_host};port={_port};user={_adminUser};password={_adminPassword}";
        using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();
        string sql = $"ALTER USER `{instance.Username}`@'%' ACCOUNT LOCK";
        using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<QueryResultDto> ExecuteQueryAsync(Instance instance, string query, string decryptedPassword)
    {
        var queryResult = new QueryResultDto();
        try
        {
            var connectionString =
                $"server={_host};port={_port};database={instance.Name};User Id={instance.Username};password={decryptedPassword};";
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(query, conn);
            bool isSelected = query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
            if (isSelected)
            {
                using var reader = await cmd.ExecuteReaderAsync();
                var data = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[columnName] = value;
                    }
                    data.Add(row);
                }

                queryResult.Data = data;
                queryResult.Success = true;
                queryResult.Message = "Query succesfull";
                return queryResult;
            }
            else
            {
                int affected = await cmd.ExecuteNonQueryAsync();
                queryResult.Success = true;
                queryResult.Message = $"Query succesfull, rows affected: {affected}";
                queryResult.Data = new List<IDictionary<string, object?>>();
                return queryResult;
            }
            
        }
        catch (Exception e)
        {
            queryResult.Success = false;
            queryResult.Message = $"Query error: {e.Message}";
            queryResult.Data = null;
            return queryResult;
        }
    }
}
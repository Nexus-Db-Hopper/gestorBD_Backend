using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Domain.Entities;
// IMPORTANTE INSTAlAR COMO NUGET: System.Data.SqlClient
namespace nexusDB.Domain.Docker.Providers;

public class SqlServerProvider : IDatabaseProvider
{
    private readonly string? _host;
    private readonly int _port;
    private readonly string? _adminUser;
    private readonly string? _adminPassword;

    public SqlServerProvider(IConfiguration config)
    {
        _host = config["Containers:SqlServerHost"];
        _port = int.Parse(config["Containers:SqlServerPort"] ?? string.Empty);
        _adminUser = config["Containers:SqlServerAdminUser"];
        _adminPassword = config["Containers:SqlServerAdminPassword"];
    }

    public string Engine => "sqlserver";

    // --------------------------------------------------
    // 🔹 CREA UNA INSTANCIA DE BASE DE DATOS PARA UN ESTUDIANTE
    // --------------------------------------------------
    public async Task CreateContainerAsync(Instance instance, string password)
    {
        var connectionString = $"Server={_host},{_port};User Id={_adminUser};Password={_adminPassword};Encrypt=False;";

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        // Crear Base de datos
        using (var cmd = new SqlCommand($"CREATE DATABASE [{instance.Name}];", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Crear login en el servidor
        using (var cmd = new SqlCommand($"CREATE LOGIN [{instance.Username}] WITH PASSWORD = '{password}';", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Crear usuario dentro de la BD
        using (var cmd = new SqlCommand(
                   $"USE [{instance.Name}]; CREATE USER [{instance.Username}] FOR LOGIN [{instance.Username}];", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Otorgar permisos
        using (var cmd = new SqlCommand(
                   $"USE [{instance.Name}]; EXEC sp_addrolemember 'db_owner', '{instance.Username}';", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public Task StartAsync(Instance instance)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(Instance instance)
    {
        throw new NotImplementedException();
    }

    // --------------------------------------------------
    // 🔹 EJECUTAR CONSULTAS PARA UN ESTUDIANTE
    // --------------------------------------------------
    public async Task<QueryResultDto> ExecuteQueryAsync(Instance instance, string query, string decryptedPassword)
    {
        var result = new QueryResultDto();

        try
        {
            var connectionString =
                $"Server={_host},{_port};Database={instance.Name};User Id={instance.Username};Password={decryptedPassword};Encrypt=False;";

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query, conn);

            // Si es SELECT
            if (query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                var data = new List<Dictionary<string, object?>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[reader.GetName(i)] = value;
                    }
                    data.Add(row);
                }

                result.Data = data;
                result.Success = true;
                result.Message = "Query successful";

                return result;
            }
            else
            {
                int affected = await cmd.ExecuteNonQueryAsync();
                result.Success = true;
                result.Message = $"Query successful, affected rows: {affected}";
                result.Data = new List<Dictionary<string, object?>>();

                return result;
            }
        }
        catch (Exception e)
        {
            result.Success = false;
            result.Message = $"Query error: {e.Message}";
            result.Data = null;
            return result;
        }
    }
}

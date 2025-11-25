using Dapper;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Domain.Entities;

namespace nexusDB.Domain.Docker.Providers;

/// <summary>
/// Provider for MySQL. This class no longer creates Docker containers.
/// Instead, it connects to a main MySQL server (defined in configuration)
/// and creates logical databases (schemas) and users within it.
/// </summary>
public class MySqlProvider : IDatabaseProvider
{
    private readonly string _connectionString;
    private readonly string _host;
    private readonly string _port;

    public MySqlProvider(IConfiguration config) // CORRECTED: Now only takes IConfiguration
    {
        // Connection details for the main MySQL server on the UPS
        var adminUser = config["Containers:MySqlAdminUser"];
        var adminPassword = config["Containers:MySqlAdminPassword"];
        _host = config["Containers:MySqlHost"];
        _port = config["Containers:MySqlPort"];

        if (string.IsNullOrEmpty(adminUser) || string.IsNullOrEmpty(adminPassword) || string.IsNullOrEmpty(_host) || string.IsNullOrEmpty(_port))
        {
            throw new InvalidOperationException("Main MySQL server connection details are missing in configuration (Containers section).");
        }

        _connectionString = $"server={_host};port={_port};user={adminUser};password={adminPassword};";
    }

    public string Engine => "mysql";

    /// <summary>
    /// Creates a new logical database (schema) and a dedicated user for an instance.
    /// </summary>
    public async Task CreateContainerAsync(Instance instance, string password)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await conn.ExecuteAsync($"CREATE DATABASE `{instance.Name}`;");
        await conn.ExecuteAsync($"CREATE USER '{instance.Username}'@'%' IDENTIFIED BY '{password}';");
        await conn.ExecuteAsync($"GRANT ALL PRIVILEGES ON `{instance.Name}`.* TO '{instance.Username}'@'%';");
        await conn.ExecuteAsync("FLUSH PRIVILEGES;");
    }

    /// <summary>
    /// Executes a query for a specific user against their dedicated logical database.
    /// </summary>
    public async Task<QueryResultDto> ExecuteQueryAsync(Instance instance, string query, string decryptedPassword)
    {
        var queryResult = new QueryResultDto();
        var userConnectionString = $"server={_host};port={_port};database={instance.Name};User Id={instance.Username};password={decryptedPassword};";
        
        try
        {
            using var conn = new MySqlConnection(userConnectionString);
            await conn.OpenAsync();
            
            // Identify queries that return tabular results (SELECT, DESCRIBE, SHOW)
            bool returnsTabularData = query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                                      query.Trim().StartsWith("DESCRIBE", StringComparison.OrdinalIgnoreCase) ||
                                      query.Trim().StartsWith("SHOW", StringComparison.OrdinalIgnoreCase);

            if (returnsTabularData)
            {
                var data = await conn.QueryAsync<dynamic>(query);
                queryResult.Data = data.Select(d => (IDictionary<string, object?>)d).ToList();
                queryResult.Success = true;
                queryResult.Message = "Query executed successfully.";
            }
            else
            {
                var affectedRows = await conn.ExecuteAsync(query);
                queryResult.Success = true;
                queryResult.Message = $"Query executed successfully. Rows affected: {affectedRows}";
            }
            
            return queryResult;
        }
        catch (Exception e)
        {
            queryResult.Success = false;
            queryResult.Message = $"Query error: {e.Message}";
            return queryResult;
        }
    }

    /// <summary>
    /// Unlocks the MySQL user account associated with the instance.
    /// </summary>
    public async Task StartAsync(Instance instance)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"ALTER USER '{instance.Username}'@'%' ACCOUNT UNLOCK;");
    }

    /// <summary>
    /// Locks the MySQL user account associated with the instance.
    /// </summary>
    public async Task StopAsync(Instance instance)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"ALTER USER '{instance.Username}'@'%' ACCOUNT LOCK;");
    }
}

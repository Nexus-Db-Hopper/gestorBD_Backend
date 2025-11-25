using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql; // PostgreSQL specific client
using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Domain.Entities;

namespace nexusDB.Domain.Docker.Providers;

/// <summary>
/// Provider for PostgreSQL. Connects to a main PostgreSQL server (defined in configuration)
/// and creates logical databases (schemas) and users within it.
/// </summary>
public class PostgresProvider : IDatabaseProvider
{
    private readonly string _connectionString;
    private readonly string _host;
    private readonly string _port;

    public PostgresProvider(IConfiguration config)
    {
        // Connection details for the main PostgreSQL server on the UPS
        var adminUser = config["Containers:PgAdminUser"];
        var adminPassword = config["Containers:PgAdminPassword"];
        _host = config["Containers:PgHost"];
        _port = config["Containers:PgPort"];

        if (string.IsNullOrEmpty(adminUser) || string.IsNullOrEmpty(adminPassword) || string.IsNullOrEmpty(_host) || string.IsNullOrEmpty(_port))
        {
            throw new InvalidOperationException("Main PostgreSQL server connection details are missing in configuration (Containers section).");
        }

        _connectionString = $"Host={_host};Port={_port};Username={adminUser};Password={adminPassword};";
    }

    public string Engine => "postgresql";

    /// <summary>
    /// Creates a new logical database and a dedicated user for an instance in PostgreSQL.
    /// </summary>
    public async Task CreateContainerAsync(Instance instance, string password)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Create database
        await conn.ExecuteAsync($"CREATE DATABASE \"{instance.Name}\";");
        
        // Create user and grant privileges
        await conn.ExecuteAsync($"CREATE USER \"{instance.Username}\" WITH PASSWORD '{password}';");
        await conn.ExecuteAsync($"GRANT ALL PRIVILEGES ON DATABASE \"{instance.Name}\" TO \"{instance.Username}\";");
    }

    /// <summary>
    /// Executes a query for a specific user against their dedicated logical database in PostgreSQL.
    /// </summary>
    public async Task<QueryResultDto> ExecuteQueryAsync(Instance instance, string query, string decryptedPassword)
    {
        var queryResult = new QueryResultDto();
        var userConnectionString = $"Host={_host};Port={_port};Database={instance.Name};Username={instance.Username};Password={decryptedPassword};";
        
        try
        {
            using var conn = new NpgsqlConnection(userConnectionString);
            await conn.OpenAsync();
            
            // Identify queries that return tabular results (SELECT, DESCRIBE, SHOW, \d, etc.)
            bool returnsTabularData = query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                                      query.Trim().StartsWith("DESCRIBE", StringComparison.OrdinalIgnoreCase) || 
                                      query.Trim().StartsWith("SHOW", StringComparison.OrdinalIgnoreCase) || 
                                      query.Trim().StartsWith("\\d", StringComparison.OrdinalIgnoreCase); 

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
    /// Enables the PostgreSQL user account associated with the instance.
    /// </summary>
    public async Task StartAsync(Instance instance)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"ALTER ROLE \"{instance.Username}\" WITH LOGIN;");
    }

    /// <summary>
    /// Disables the PostgreSQL user account associated with the instance.
    /// </summary>
    public async Task StopAsync(Instance instance)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync($"ALTER ROLE \"{instance.Username}\" WITH NOLOGIN;");
    }
}

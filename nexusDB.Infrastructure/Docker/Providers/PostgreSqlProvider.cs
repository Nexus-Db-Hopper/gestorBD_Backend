using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql; // <--- El driver que se instala
using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Domain.Entities;
using System.Data;

namespace nexusDB.Domain.Docker.Providers; // Mismo namespace que MySqlProvider para evitar conflictos

public class PostgreSqlProvider : IDatabaseProvider
{
    private readonly string? _host;
    private readonly int _port;
    private readonly string? _adminUser;
    private readonly string? _adminPassword;

    public PostgreSqlProvider(IConfiguration config)
    {
        // Leemos las coordenadas que acabamos de poner en appsettings.json
        _host = config["Containers:PostgresHost"];
        _port = int.Parse(config["Containers:PostgresPort"] ?? "5432");
        _adminUser = config["Containers:PostgresUser"];
        _adminPassword = config["Containers:PostgresPassword"];
    }

    // Esta es la "etiqueta" que buscará el Factory. 
    // Si el JSON del frontend dice "engine": "postgresql", usará esta clase.
    public string Engine => "postgresql";

    public async Task CreateContainerAsync(Instance instance, string password)
    {
        // 1. Nos conectamos a la base de datos administrativa por defecto ("postgres")
        var connectionString = $"Host={_host};Port={_port};Database=postgres;Username={_adminUser};Password={_adminPassword};Include Error Detail=true";

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // 2. Crear el ROL (Usuario)
        // En Postgres se crean ROLES con permiso de LOGIN.
        //Los identificadores (nombres de db/usuario) van entre comillas dobles "" en Postgres para evitar problemas de sintaxis.
        string createRoleSql = $"CREATE ROLE \"{instance.Username}\" WITH LOGIN PASSWORD '{password}';";
        await conn.ExecuteAsync(createRoleSql);

        // 3. Crear la BASE DE DATOS y asignarla al dueño
        //Postgres requiere especificar el OWNER al momento de crear la DB.
        string createDbSql = $"CREATE DATABASE \"{instance.Name}\" OWNER \"{instance.Username}\";";
        await conn.ExecuteAsync(createDbSql);
        
        // Nota: En Postgres no es necesario hacer "FLUSH PRIVILEGES" como en MySQL.
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
        var queryResult = new QueryResultDto();
        try
        {
            // 1. Construir la cadena de conexión apuntando a la BD específica del estudiante
            var connectionString = $"Host={_host};Port={_port};Database={instance.Name};Username={instance.Username};Password={decryptedPassword};Include Error Detail=true";

            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(query, conn);

            // 2. Detectar si es un SELECT para devolver datos
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

                queryResult.Data = data; // Se mapea automáticamente al JSON gracias al DTO
                queryResult.Success = true;
                queryResult.Message = "Query successful (PostgreSQL)";
                return queryResult;
            }
            else
            {
                // Es un INSERT, UPDATE, DELETE, CREATE TABLE, etc.
                int affected = await cmd.ExecuteNonQueryAsync();
                queryResult.Success = true;
                queryResult.Message = $"Query successful, rows affected: {affected}";
                queryResult.Data = new List<IDictionary<string, object?>>();
                return queryResult;
            }
        }
        catch (Exception e)
        {
            queryResult.Success = false;
            queryResult.Message = $"PostgreSQL Error: {e.Message}";
            queryResult.Data = null;
            return queryResult;
        }
    }
}
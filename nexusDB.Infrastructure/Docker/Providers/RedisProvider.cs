using Microsoft.Extensions.Configuration;
using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Domain.Entities;
using StackExchange.Redis;

namespace nexusDB.Domain.Docker.Providers;

/// <summary>
/// Provider para manejar instancias Redis dentro de la plataforma.
/// Redis no permite múltiples bases de datos con usuarios nativos, por lo cual:
/// - Simulamos una "instancia por usuario" prefixando las keys con el nombre de la instancia.
/// - El password se usa como AUTH para conectarse.
/// </summary>
public class RedisProvider : IDatabaseProvider
{
    private readonly string? _host;
    private readonly int _port;
    private readonly string? _adminPassword;

    public RedisProvider(IConfiguration config)
    {
        _host = config["Containers:RedisHost"];
        _port = int.Parse(config["Containers:RedisPort"] ?? "6379");
        _adminPassword = config["Containers:RedisAdminPassword"];
    }

    // Identificador para el factory
    public string Engine => "redis";

    // ======================================================================
    // 🔹 Crear "instancia lógica" para un usuario en Redis
    // ======================================================================
    public async Task CreateContainerAsync(Instance instance, string password)
    {
        // En Redis NO creamos bases de datos ni usuarios.
        // Simplemente guardamos un registro base con prefijo instance
        // Esto marca que la instancia existe.
        
        var conn = await ConnectionMultiplexer.ConnectAsync(
            $"{_host}:{_port},password={_adminPassword}"
        );

        var db = conn.GetDatabase();

        string key = $"{instance.Name}:_instanceInfo";

        await db.StringSetAsync(key, 
            $"Instance for user {instance.Username} created at {DateTime.UtcNow}"
        );
    }

    // ======================================================================
    // 🔹 "Start": desbloquear la instancia (Redis no lo soporta → simulación)
    // ======================================================================
    public async Task StartAsync(Instance instance)
    {
        var conn = await ConnectionMultiplexer.ConnectAsync(
            $"{_host}:{_port},password={_adminPassword}"
        );

        var db = conn.GetDatabase();
        await db.StringSetAsync($"{instance.Name}:_state", "ACTIVE");
    }

    // ======================================================================
    // 🔹 "Stop": bloquear la instancia 
    // (En Redis lo simulamos marcando un flag)
    // ======================================================================
    public async Task StopAsync(Instance instance)
    {
        var conn = await ConnectionMultiplexer.ConnectAsync(
            $"{_host}:{_port},password={_adminPassword}"
        );

        var db = conn.GetDatabase();
        await db.StringSetAsync($"{instance.Name}:_state", "STOPPED");
    }

    // ======================================================================
    // 🔹 Ejecutar Comandos Redis
    // ======================================================================
    public async Task<QueryResultDto> ExecuteQueryAsync(Instance instance, string query, string decryptedPassword)
    {
        var result = new QueryResultDto();

        try
        {
            // Conexión usando el password del usuario
            var conn = await ConnectionMultiplexer.ConnectAsync(
                $"{_host}:{_port},password={decryptedPassword}"
            );

            var db = conn.GetDatabase();

            // Comando raw estilo redis-cli
            // Ej: "SET key value", "GET key", "HGETALL objeto"
            var server = conn.GetServer(_host!, _port);

            // Ejecutar el comando
            var redisResult = await server.ExecuteAsync(query.Split(' ')[0], 
                query.Split(' ').Skip(1).ToArray()
            );

            // Si es un valor simple
            if (!redisResult.IsNull)
            {
                result.Data = new List<Dictionary<string, object?>>
                {
                    new() { { "response", redisResult.ToString() } }
                };
            }
            else
            {
                result.Data = new List<Dictionary<string, object?>>();
            }

            result.Success = true;
            result.Message = "Redis command executed successfully";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Redis error: {ex.Message}";
            result.Data = null;
        }

        return result;
    }
}

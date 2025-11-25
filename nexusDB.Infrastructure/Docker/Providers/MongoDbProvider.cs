using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Domain.Entities;
using MongoDB.Driver;

namespace nexusDB.Domain.Docker.Providers;
public class MongoDbProvider : IDatabaseProvider
{
    private readonly string? _host;
    private readonly int _port;
    private readonly string? _adminUser;
    private readonly string? _adminPassword;

    public MongoDbProvider(IConfiguration config)
    {
        _host = config["Containers:MongoHost"];
        _port = int.Parse(config["Containers:MongoPort"] ?? "27017");
        _adminUser = config["Containers:MongoAdminUser"];
        _adminPassword = config["Containers:MongoAdminPassword"];
    }

    public string Engine => "mongodb";

    // crear instancia 
    public async Task CreateContainerAsync(Instance instance, string password)
    {
        // Conexion del admin
        var adminClient = new MongoClient(
            $"mongodb://{_adminUser}:{_adminPassword}@{_host}:{_port}/admin"
        );

        var adminDb = adminClient.GetDatabase("admin");

        // 1) Crear base de datos (Mongo la crea automáticamente al insertar algo)
        var newDb = adminClient.GetDatabase(instance.Name);
        await newDb.CreateCollectionAsync("init"); // fuerza creación
        await newDb.DropCollectionAsync("init");

        // 2) Crear usuario con rol dbOwner en esa base de datos
        var createUserCmd = new BsonDocument
        {
            { "createUser", instance.Username },
            { "pwd", password },
            { "roles", new BsonArray {
                new BsonDocument {
                    { "role", "dbOwner" },
                    { "db", instance.Name }
                }
            }},
            { "customData", new BsonDocument { { "state", "ACTIVE" } } } // Initial state
        };

        await adminDb.RunCommandAsync<BsonDocument>(createUserCmd);
    }

    // activar un usuario o ponnerlo en modo start
    public async Task StartAsync(Instance instance)
    {
        var adminClient = new MongoClient(
            $"mongodb://{_adminUser}:{_adminPassword}@{_host}:{_port}/admin"
        );

        var adminDb = adminClient.GetDatabase("admin");

        var cmd = new BsonDocument
        {
            { "updateUser", instance.Username },
            { "roles", new BsonArray {
                new BsonDocument { { "role", "dbOwner" }, { "db", instance.Name } }
            }},
            { "customData", new BsonDocument { { "state", "ACTIVE" } } }
        };

        await adminDb.RunCommandAsync<BsonDocument>(cmd);
    }

    
    // desarctvar usuario o ponerlo en modo stop 
    public async Task StopAsync(Instance instance)
    {
        var adminClient = new MongoClient(
            $"mongodb://{_adminUser}:{_adminPassword}@{_host}:{_port}/admin"
        );

        var adminDb = adminClient.GetDatabase("admin");

        var cmd = new BsonDocument
        {
            { "updateUser", instance.Username },
            { "roles", new BsonArray() }, // sin roles → sin permisos
            { "customData", new BsonDocument { { "state", "STOPPED" } } }
        };

        await adminDb.RunCommandAsync<BsonDocument>(cmd);
    }

    
    // ejecuta query 
    public async Task<QueryResultDto> ExecuteQueryAsync(
        Instance instance,
        string query,
        string decryptedPassword)
    {
        var result = new QueryResultDto();
        try
        {
            var adminClient = new MongoClient(
                $"mongodb://{_adminUser}:{_adminPassword}@{_host}:{_port}/admin"
            );
            var adminDb = adminClient.GetDatabase("admin");
            var user = await adminDb.RunCommandAsync<BsonDocument>(new BsonDocument { { "usersInfo", instance.Username } });
            var customData = user["users"][0]["customData"].AsBsonDocument;
            if (customData.Contains("state") && customData["state"].AsString == "STOPPED")
            {
                result.Success = false;
                result.Message = "Instance is currently stopped. Access denied.";
                return result;
            }

            var client = new MongoClient(
                $"mongodb://{instance.Username}:{decryptedPassword}@{_host}:{_port}/{instance.Name}"
            );

            var db = client.GetDatabase(instance.Name);

            // Interpretar query como un comando de MongoDB (JSON)
            var cmd = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(query);

            var response = await db.RunCommandAsync<BsonDocument>(cmd);

            result.Success = true;
            result.Data = new List<Dictionary<string, object?>>
            {
                response.ToDictionary()
            };
            result.Message = "Command executed successfully";

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Query error: {ex.Message}";
            return result;
        }
    }
}

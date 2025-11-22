using Docker.DotNet;
using Docker.DotNet.Models;
using MySqlConnector;
using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Domain.Entities;
using System.Net;
using System.Net.Sockets;
using Dapper;

namespace nexusDB.Domain.Docker.Providers;

public class MySqlProvider : IDatabaseProvider
{
    private readonly DockerClient _dockerClient;
    private const string MySqlImage = "mysql";
    private const string MySqlImageTag = "latest";

    public MySqlProvider(DockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public string Engine => "mysql";

    public async Task CreateContainerAsync(Instance instance, string password)
    {
        await _dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = MySqlImage, Tag = MySqlImageTag },
            new AuthConfig(),
            new Progress<JSONMessage>());

        var hostPort = GetFreeTcpPort();
        instance.HostPort = hostPort;

        var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = $"{MySqlImage}:{MySqlImageTag}",
            Name = instance.ContainerName,
            Labels = new Dictionary<string, string>
            {
                { "app.owner", "nexusdb" },
                { "user.id", instance.OwnerUserId.ToString() }
            },
            Env = new List<string>
            {
                $"MYSQL_ROOT_PASSWORD={Guid.NewGuid()}",
                $"MYSQL_DATABASE={instance.Name}",
                $"MYSQL_USER={instance.Username}",
                $"MYSQL_PASSWORD={password}"
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { "3306/tcp", new List<PortBinding> { new PortBinding { HostPort = hostPort.ToString() } } }
                },
                
                // --- PRODUCTION-READY CHANGES ---

                // 1. Resource Limits: Prevents a single container from consuming all server resources.
                Memory = 268435456, // 256 MB RAM limit
                CPUQuota = 50000,   // 50% of one CPU core limit

                // 2. Data Persistence: Maps a folder on the host to the MySQL data folder inside the container.
                // This ensures data survives container restarts or deletions.
                Binds = new List<string>
                {
                    // IMPORTANT: The host path must be configured on the UPS server.
                    // For local Windows development, this will create a folder in C:\var\lib\...
                    // For the Linux-based UPS, it will be /var/lib/...
                    $"/var/lib/nexusdb-data/{instance.ContainerName}:/var/lib/mysql"
                }
                // --- END OF PRODUCTION-READY CHANGES ---
            }
        });

        instance.ContainerId = response.ID;
        
        await _dockerClient.Containers.StartContainerAsync(instance.ContainerId, null);
        
        await WaitForDatabaseReady(hostPort, password, instance);
    }

    public async Task StartAsync(Instance instance)
    {
        if (string.IsNullOrEmpty(instance.ContainerId)) return;
        await _dockerClient.Containers.StartContainerAsync(instance.ContainerId, null);
    }

    public async Task StopAsync(Instance instance)
    {
        if (string.IsNullOrEmpty(instance.ContainerId)) return;
        await _dockerClient.Containers.StopContainerAsync(instance.ContainerId, new ContainerStopParameters());
    }

    public async Task<QueryResultDto> ExecuteQueryAsync(Instance instance, string query, string decryptedPassword)
    {
        var queryResult = new QueryResultDto();
        if (instance.HostPort == 0)
        {
            queryResult.Success = false;
            queryResult.Message = "Instance is not properly configured (missing HostPort).";
            return queryResult;
        }

        var connectionString = $"server=localhost;port={instance.HostPort};database={instance.Name};User Id={instance.Username};password={decryptedPassword};";
        
        try
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            
            bool isSelect = query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
            if (isSelect)
            {
                var data = new List<Dictionary<string, object?>>();
                using var reader = await conn.ExecuteReaderAsync(query);
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    data.Add(row);
                }
                queryResult.Data = data;
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

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private async Task WaitForDatabaseReady(int port, string password, Instance instance)
    {
        var connectionString = $"server=localhost;port={port};database={instance.Name};User Id={instance.Username};password={password};";
        var attempts = 0;
        while (attempts < 30)
        {
            try
            {
                using var conn = new MySqlConnection(connectionString);
                await conn.OpenAsync();
                return;
            }
            catch (MySqlException)
            {
                attempts++;
                await Task.Delay(2000);
            }
        }
        throw new Exception("Could not connect to the new database instance within the timeout period.");
    }
}

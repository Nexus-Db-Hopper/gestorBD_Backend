using Docker.DotNet;
using Docker.DotNet.Models;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Domain.Entities;

namespace nexusDB.Domain.Docker.Providers;

public class MySqlProvider : IDatabaseProvider
{
    private readonly DockerClient _docker;

    public MySqlProvider()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            _docker = new DockerClientConfiguration(
                new Uri("npipe://./pipe/docker_engine")
            ).CreateClient();
        }
        else
        {
            _docker = new DockerClientConfiguration(
                new Uri("unix:///var/run/docker.sock")
            ).CreateClient();
        }
    }

    public string Engine => "mysql";

    public async Task<string> CreateContainerAsync(Instance instance, string password)
    {
        

        await _docker.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = "mysql",
                Tag = "latest"
            },
            null,
            new Progress<JSONMessage>()
        );
        var createParams = new CreateContainerParameters
        {
            Image = "mysql:latest",
            Name = instance.Name,
            Env = new List<string>
            {
                $"MYSQL_ROOT_PASSWORD={password}",
                $"MYSQL_USER={instance.Username}",
                $"MYSQL_PASSWORD={password}",
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    ["3306/tcp"] = new List<PortBinding>
                    {
                        new PortBinding { HostPort = instance.Port }
                    }
                }
            }
        };
        var result = await _docker.Containers.CreateContainerAsync(createParams);
        var containerId = result.ID;
        await _docker.Containers.StartContainerAsync(containerId, null);
        return containerId;
    }

    public Task StartAsync(Instance instance)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(Instance instance)
    {
        throw new NotImplementedException();
    }

    public Task<string> ExecuteQueryAsync(Instance instance, string query)
    {
        throw new NotImplementedException();
    }
}
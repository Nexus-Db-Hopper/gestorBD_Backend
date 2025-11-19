using Docker.DotNet;
using Docker.DotNet.Models;
using nexusDB.Domain.Entities;
using nexusDB.Application.Interfaces.Providers;

namespace nexusDB.Domain.Docker.Providers;

public class MongoDbProvider : IDatabaseProvider
{
    private readonly DockerClient _docker;

    public string Engine => "mongodb";
    
    public MongoDbProvider()
    {
        // Detectar sistema operativo para usar el docker.sock correcto
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

    // Este proveedor corresponde al engine "mongodb"
   

    /// <summary>
    /// Crea un contenedor MongoDB usando la instancia ya validada.
    /// </summary>
    /// <param name="instance">Instancia con configuraciones pre-validadas.</param>
    /// <param name="rootPassword">Password real del root admin (NO hash).</param>
    /// <param name="userPassword">Password real del usuario normal (NO hash).</param>
    /// <returns>Devuelve el ID del contenedor creado.</returns>
    ///
    ///
    
    public async Task<string> CreateContainerAsync(Instance instance, string userPassword, string rootPassword)
    {
        Console.WriteLine("Entró a crear contenedor MongoDB");

        // 1. Descargar la imagen de Mongo si no existe
        await _docker.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = "mongo",
                Tag = "latest"
            },
            null,
            new Progress<JSONMessage>()
        );

        // 2. Crear parámetros del contenedor
        var createParams = new CreateContainerParameters
        {
            Image = $"mongo:latest",
            Name = instance.Name,

            Env = new List<string>
            {
                $"MONGO_INITDB_ROOT_USERNAME={instance.Username}",
                $"MONGO_INITDB_ROOT_PASSWORD={rootPassword}"
            },

            // MongoDB usa por defecto 27017
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    ["27017/tcp"] = new List<PortBinding>
                    {
                        new PortBinding { HostPort = instance.Port }
                    }
                },

                // // Persistencia si tiene volumen asignado
                // Binds = !string.IsNullOrWhiteSpace(instance.VolumeName)
                //     ? new List<string> { $"{instance.VolumeName}:/data/db" }
                //     : null
            }
        };

        // 3. Crear contenedor
        var result = await _docker.Containers.CreateContainerAsync(createParams);
        var containerId = result.ID;

        // 4. Iniciar contenedor
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
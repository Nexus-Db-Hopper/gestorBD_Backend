using Docker.DotNet;
using Docker.DotNet.Models;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Domain.Entities;

namespace nexusDB.Domain.Docker.Providers;

public class SqlServerProvider : IDatabaseProvider
{
    private readonly DockerClient _docker;

    public string Engine => "sqlserver";

    public SqlServerProvider()
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

    /// <summary>
    /// Crea un contenedor SQL Server basado en la instancia configurada.
    /// </summary>
    /// <param name="instance">Instancia validada con nombre, puerto y usuario.</param>
    /// <param name="userPassword">Contraseña del usuario normal (NO hash).</param>
    /// <param name="rootPassword">Contraseña del usuario SA (NO hash).</param>
    /// <returns>ID del contenedor creado.</returns>
    public async Task<string> CreateContainerAsync(Instance instance, string userPassword, string rootPassword)
    {
        Console.WriteLine("Entró a crear contenedor SQL Server");

        // 1. Descargar imagen si no existe
        await _docker.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = "mcr.microsoft.com/mssql/server",
                Tag = "2022-latest"
            },
            null,
            new Progress<JSONMessage>()
        );

        // 2. Parámetros para crear contenedor
        var createParams = new CreateContainerParameters
        {
            Image = "mcr.microsoft.com/mssql/server:2022-latest",
            Name = instance.Name,

            Env = new List<string>
            {
                "ACCEPT_EULA=Y",                       // Obligatorio para MSSQL
                $"SA_PASSWORD={rootPassword}",         // Password del superadmin
                "MSSQL_PID=Developer",                 // Versión adecuada para desarrollo
                
                // IMPORTANTE: SQL Server realmente no permite crear usuarios por env vars,
                // pero lo mantenemos para consistencia con tu arquitectura (opcional).
                $"MSSQL_USER={instance.Username}",
                $"MSSQL_USER_PASSWORD={userPassword}"
            },

            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    ["1433/tcp"] = new List<PortBinding>
                    {
                        new PortBinding { HostPort = instance.Port }
                    }
                }
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

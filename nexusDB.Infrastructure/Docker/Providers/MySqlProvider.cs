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
        
        // Esto sirve para que docker detecte donde se guarda el docker sock pues en windows es diferente
        // Cuando se suba a la vps funcionara con el de abajo que es el mismo que usa linux
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

    // Esto determina a cual Engine pertenece este proveedor (se selecciona en el factory)
    public string Engine => "mysql";

    // Este metodo es para crear el contenedor
    public async Task<string> CreateContainerAsync(Instance instance, string password, string rootPassword)
    {
        
        // Esto descarga la base de la imagen de mysql pues hay que descargarla antes de usarla si no se tiene
        await _docker.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = "mysql",
                Tag = "latest"
            },
            null,
            new Progress<JSONMessage>()
        );
        
        // Esto crea los parametros necesarios para la creacion del contenedor
        var createParams = new CreateContainerParameters
        {
            // La imagen recien descargada
            Image = "mysql:latest",
            
            // El nombre de la instancia (dado por el profesor, si se prefere se puede generar con el id)
            Name = instance.Name,
            
            // Estas son variables de entorno que crean el perfil y la forma de ingreso a la instancia
            Env = new List<string>
            {
                // Esta es la contraseña dada por el profesor para permisos de superadmin
                $"MYSQL_ROOT_PASSWORD={rootPassword}",
                
                // Este es el usuario de acceso propuesto por el profesor
                $"MYSQL_USER={instance.Username}",
                
                // Esta es una contraseña que sirve para el perfil especifico
                $"MYSQL_PASSWORD={password}",
            },
            HostConfig = new HostConfig
            {
                
                // Aqui se configura el puerto propuesto por el profesor
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    // Este es el puerto default de mysql (necesario para mysql y su ejecucion)
                    // Este cambia segun que motor uses
                    ["3306/tcp"] = new List<PortBinding>
                    {
                        // Este es el puerto que se expone publicamente, osea por el que se accede a la instancia especifica
                        new PortBinding { HostPort = instance.Port }
                    }
                }
            }
        };
        
        // Aqui se manda la informacion a docker esperando un resultado de el
        var result = await _docker.Containers.CreateContainerAsync(createParams);
        
        // Se saca el id generado por docker en el contenedor
        var containerId = result.ID;
        
        // Se inicializa el contenedor para que este abierto al crearse
        await _docker.Containers.StartContainerAsync(containerId, null);
        
        // Se devuelve su id para su uso en service
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
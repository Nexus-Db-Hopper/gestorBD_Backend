using nexusDB.Application.Dtos.Instances;
using nexusDB.Application.Interfaces.Instances;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Application.Interfaces.Repositories;
using nexusDB.Application.Interfaces.Security;
using nexusDB.Domain.Entities;

namespace nexusDB.Application.Services;

public class InstanceService : IInstanceService
{
    private readonly IInstanceRepository _instanceRepository;
    private readonly IAesEncryptionService _aesEncryptionService;
    private readonly IDatabaseProviderFactory _databaseProviderFactory;

    public InstanceService(IInstanceRepository instanceRepository, IEnumerable<IDatabaseProvider> databaseProviders,  IAesEncryptionService aesEncryptionService, IDatabaseProviderFactory databaseProviderFactory)
    {
        _instanceRepository = instanceRepository;
        _aesEncryptionService = aesEncryptionService;
        _databaseProviderFactory = databaseProviderFactory;
    }

    // Aqui se crea la instancia, para agregar mas implementaciones recordar agregarlas tambien en la interface
    public async Task<int> CreateInstanceAsync(CreateInstanceRequest request)
    {
        // Aqui se deben agregar verificaciones para saber si el owner y el created existen, tambien se puede verificar si 
        // el puerto externo (seleccionado por el profesor) esta ocupado o no a traves de la base de datos 
        // se busca el proveedor del motor a traves del factory (mysql, mariadb, mongo, etc)
        var provider = _databaseProviderFactory.GetProvider(request.Engine);
        // Si no se encuentra proveedor se suelta que no esta soportado en la aplicacion
        if (provider == null) throw new ArgumentException($"Engine {request.Engine} not supported");
        
        // Se hashea la contraseña que se necesita para acceder a la instancia para que no se filtre
        // Para otros metodos donde se requiera acceder con el mismo servicio ya existe un desencriptador 
        var userPasswordHash =  _aesEncryptionService.Encrypt(request.UserPassword);
        var rootPasswordHash =  _aesEncryptionService.Encrypt(request.RootPassword);
        
        // se crea una instancia de manera local
        var instance = new Instance(
            request.Name,
            request.Engine,
            request.Port,
            request.Username,
            rootPasswordHash,
            userPasswordHash,
            request.CreatedByUserId,
            request.OwnerUserId);
        
        // La instancia se manda hacia el creador de contenedores de docker junto a la contraseña sin hashear
        //este devuelve el id que le da al contenedor (servira para encontrarlo a futuro)
        var containerId = await provider.CreateContainerAsync(instance, request.UserPassword, request.RootPassword);
        
        // Se guarda el id en la instancia local
        instance.ContainerId = containerId;

        // Se le da el estado a la instancia (esto se puede mejorar)
        instance.State = InstanceState.Running;
        
        // Se guarda la instancia en la base de datos y se actualiza la local
        instance = await _instanceRepository.AddAsync(instance);
        
        // Se devuelve el id de la instancia
        return instance.Id;
    }
}
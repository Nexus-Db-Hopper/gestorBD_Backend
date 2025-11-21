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
    private readonly IUserRepository _userRepository;

    public InstanceService(IInstanceRepository instanceRepository, IEnumerable<IDatabaseProvider> databaseProviders,  IAesEncryptionService aesEncryptionService, IDatabaseProviderFactory databaseProviderFactory, IUserRepository userRepository)
    {
        _instanceRepository = instanceRepository;
        _aesEncryptionService = aesEncryptionService;
        _databaseProviderFactory = databaseProviderFactory;
        _userRepository = userRepository;
    }

    // Aqui se crea la instancia, para agregar mas implementaciones recordar agregarlas tambien en la interface
    public async Task<int> CreateInstanceAsync(CreateInstanceRequest request)
    {
        var creator = await _userRepository.GetUserByIdAsync(request.OwnerUserId);
        if (creator == null) throw new KeyNotFoundException("Creator of the instance not found");
        var owner = await _userRepository.GetUserByIdAsync(request.OwnerUserId);
        if (owner == null) throw new KeyNotFoundException("Owner to assign the instance was not found");
        var availableOwner = await _instanceRepository.GetByOwnerIdAsync(request.OwnerUserId);
        if (availableOwner != null) throw new ArgumentException("This person already is the owner of an instance");
        // se busca el proveedor del motor a traves del factory (mysql, mariadb, mongo, etc)
        var provider = _databaseProviderFactory.GetProvider(request.Engine);
        // Si no se encuentra proveedor se suelta que no esta soportado en la aplicacion
        if (provider == null) throw new ArgumentException($"Engine {request.Engine} not supported");
        
        // Se hashea la contraseña que se necesita para acceder a la instancia para que no se filtre
        // Para otros metodos donde se requiera acceder con el mismo servicio ya existe un desencriptador 
        var userPasswordHash =  _aesEncryptionService.Encrypt(request.UserPassword);
        
        // se crea una instancia de manera local
        var instance = new Instance(
            request.Name,
            request.Engine,
            request.Username,
            userPasswordHash,
            request.CreatedByUserId,
            request.OwnerUserId,
            request.ContainerName
            );
        
        // La instancia se manda hacia el creador de contenedores de docker junto a la contraseña sin hashear
        //este devuelve el id que le da al contenedor (servira para encontrarlo a futuro)
        await provider.CreateContainerAsync(instance, request.UserPassword);
        
        // Se le da el estado a la instancia (esto se puede mejorar)
        instance.State = InstanceState.Active;
        
        // Se guarda la instancia en la base de datos y se actualiza la local
        instance = await _instanceRepository.AddAsync(instance);
        
        // Se devuelve el id de la instancia
        return instance.Id;
    }


    public async Task<QueryResultDto> ExecuteQueryAsync(QueryRequestDto queryRequest)
    {
        var instance = await _instanceRepository.GetByOwnerIdAsync(queryRequest.OwnerUserId);
        if (instance == null) return new QueryResultDto{Success = false,Message="Instance for this student was not found"};
        var provider = _databaseProviderFactory.GetProvider(instance.Engine);
        if (provider == null) return new QueryResultDto{Success = false,Message="Provider not supported"};
        var decryptedPassword = _aesEncryptionService.Decrypt(instance.UserPasswordEncrypted);
        return await provider.ExecuteQueryAsync(instance, queryRequest.Query, decryptedPassword);
    }

    public async Task StartInstanceAsync(int ownerId)
    {
        var instance = await _instanceRepository.GetByOwnerIdAsync(ownerId);
        if (instance == null) throw new KeyNotFoundException("Instance was not found");
        var provider = _databaseProviderFactory.GetProvider(instance.Engine);
        await provider.StartAsync(instance);
        instance.State = InstanceState.Active;
        await _instanceRepository.UpdateAsync(instance);
        
    }
    
    public async Task StopInstanceAsync(int ownerId)
    {
        var instance = await _instanceRepository.GetByOwnerIdAsync(ownerId);
        if (instance == null) throw new KeyNotFoundException("Instance not found");
        var provider = _databaseProviderFactory.GetProvider(instance.Engine);
        await provider.StopAsync(instance);
        instance.State = InstanceState.Active;
        await _instanceRepository.UpdateAsync(instance);
        
    }
}
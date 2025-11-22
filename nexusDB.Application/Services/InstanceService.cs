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

    public InstanceService(IInstanceRepository instanceRepository, IAesEncryptionService aesEncryptionService, IDatabaseProviderFactory databaseProviderFactory, IUserRepository userRepository)
    {
        _instanceRepository = instanceRepository;
        _aesEncryptionService = aesEncryptionService;
        _databaseProviderFactory = databaseProviderFactory;
        _userRepository = userRepository;
    }

    public async Task<int> CreateInstanceAsync(CreateInstanceRequest request)
    {
        var creator = await _userRepository.GetUserByIdAsync(request.CreatedByUserId);
        if (creator == null) throw new KeyNotFoundException("Creator of the instance not found");

        var owner = await _userRepository.GetUserByIdAsync(request.OwnerUserId);
        if (owner == null) throw new KeyNotFoundException("Owner to assign the instance was not found");

        var availableOwner = await _instanceRepository.GetByOwnerIdAsync(request.OwnerUserId);
        if (availableOwner != null) throw new ArgumentException("This person already is the owner of an instance");

        var provider = _databaseProviderFactory.GetProvider(request.Engine);
        if (provider == null) throw new ArgumentException($"Engine {request.Engine} not supported");

        var userPasswordHash = _aesEncryptionService.Encrypt(request.UserPassword);

        var instance = new Instance(
            request.Name,
            request.Engine,
            request.Username,
            userPasswordHash,
            request.CreatedByUserId,
            request.OwnerUserId,
            request.ContainerName
        );

        await provider.CreateContainerAsync(instance, request.UserPassword);
        instance.State = InstanceState.Active;
        instance = await _instanceRepository.AddAsync(instance);

        return instance.Id;
    }

    public async Task<QueryResultDto> ExecuteUserQueryAsync(int ownerUserId, string query)
    {
        var instance = await _instanceRepository.GetByOwnerIdAsync(ownerUserId);
        if (instance == null)
        {
            throw new KeyNotFoundException("No se encontró una instancia para este usuario.");
        }

        var provider = _databaseProviderFactory.GetProvider(instance.Engine);
        if (provider == null)
        {
            return new QueryResultDto { Success = false, Message = "Proveedor de base de datos no soportado." };
        }

        var decryptedPassword = _aesEncryptionService.Decrypt(instance.UserPasswordEncrypted);
        return await provider.ExecuteQueryAsync(instance, query, decryptedPassword);
    }

    public async Task<Instance?> GetInstanceByOwnerIdAsync(int ownerUserId)
    {
        return await _instanceRepository.GetByOwnerIdAsync(ownerUserId);
    }

    public async Task<IEnumerable<Instance>> GetAllInstancesAsync()
    {
        return await _instanceRepository.GetAllAsync();
    }
}

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

    public async Task<int> CreateInstanceAsync(CreateInstanceRequest request)
    {
        var provider = _databaseProviderFactory.GetProvider(request.Engine);
        if (provider == null) throw new ArgumentException($"Engine {request.Engine} not supported");
        var passwordHash =  _aesEncryptionService.Encrypt(request.Password);
        var instance = new Instance(
            request.Name,
            request.Engine,
            request.Port,
            request.Username,
            passwordHash,
            request.CreatedByUserId,
            request.OwnerUserId);
        var containerId = await provider.CreateContainerAsync(instance, request.Password);
        instance.ContainerId = containerId;
        instance = await _instanceRepository.AddAsync(instance);
        return instance.Id;
    }
}
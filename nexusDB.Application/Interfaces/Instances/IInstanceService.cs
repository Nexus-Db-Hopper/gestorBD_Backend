using nexusDB.Application.Dtos.Instances;

namespace nexusDB.Application.Interfaces.Instances;

public interface IInstanceService
{
    Task<int> CreateInstanceAsync(CreateInstanceRequest request);
}
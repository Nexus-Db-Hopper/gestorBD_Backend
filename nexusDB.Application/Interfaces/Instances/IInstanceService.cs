using nexusDB.Application.Dtos.Instances;
using nexusDB.Domain.Entities;

namespace nexusDB.Application.Interfaces.Instances;

public interface IInstanceService
{
    Task<int> CreateInstanceAsync(CreateInstanceRequest request);
    Task<QueryResultDto> ExecuteUserQueryAsync(int ownerUserId, string query);
    Task<Instance?> GetInstanceByOwnerIdAsync(int ownerUserId);
    Task<IEnumerable<Instance>> GetAllInstancesAsync();
}

using nexusDB.Domain.Entities;

namespace nexusDB.Application.Interfaces.Repositories;

public interface IInstanceRepository
{
    Task<Instance> AddAsync(Instance instance);
    Task<Instance?> GetByOwnerIdAsync(int id);
    Task UpdateAsync(Instance instance);
    Task<IEnumerable<Instance>> GetAllAsync();
}

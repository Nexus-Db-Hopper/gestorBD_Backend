using nexusDB.Domain.Entities;

namespace nexusDB.Application.Interfaces.Repositories;

public interface IInstanceRepository
{
    public Task<Instance> AddAsync(Instance instance);
    public Task<Instance?> GetByOwnerIdAsync(int id);
}
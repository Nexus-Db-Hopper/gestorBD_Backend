using nexusDB.Application.Interfaces.Repositories;
using nexusDB.Domain.Entities;
using nexusDB.Infrastructure.Data;

namespace nexusDB.Infrastructure.Repositories;

public class InstanceRepository : IInstanceRepository
{
    private readonly AppDbContext _dbContext;
    public InstanceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Instance> AddAsync(Instance instance)
    {
        _dbContext.Instances.Add(instance);
        await _dbContext.SaveChangesAsync();
        return instance;
    }
}
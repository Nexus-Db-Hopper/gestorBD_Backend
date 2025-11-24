using Microsoft.EntityFrameworkCore;
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
    
    public async Task<Instance?> GetByOwnerIdAsync(int id)
    {
        return await _dbContext.Instances.FirstOrDefaultAsync(i => i.OwnerUserId == id && i.State != InstanceState.Deleted);
    }

    public async Task UpdateAsync(Instance instance)
    {
        _dbContext.Instances.Update(instance);
        await _dbContext.SaveChangesAsync();
    }
}
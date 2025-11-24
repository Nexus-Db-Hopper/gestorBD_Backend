using nexusDB.Application.Interfaces.Repositories;
using nexusDB.Domain.Entities;
using nexusDB.Infrastructure.Data;

namespace nexusDB.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _dbContext.Users.FindAsync(userId);
    }
}
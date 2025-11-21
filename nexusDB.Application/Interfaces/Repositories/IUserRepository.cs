using nexusDB.Domain.Entities;

namespace nexusDB.Application.Interfaces.Repositories;

public interface IUserRepository
{
    public Task<User?> GetUserByIdAsync(int userId);
}
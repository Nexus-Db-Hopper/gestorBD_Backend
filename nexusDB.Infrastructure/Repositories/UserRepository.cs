using nexusDB.Application.Interfaces.Repositories;
using nexusDB.Domain.Entities;

namespace nexusDB.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IUserRepository _userRepository;

    public UserRepository(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _userRepository.GetUserByIdAsync(userId);
    }
}
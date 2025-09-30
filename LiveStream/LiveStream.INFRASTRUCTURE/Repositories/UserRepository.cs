using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN;

namespace LiveStream.INFRASTRUCTURE.Repositories;

public class UserRepository : IUserRepository
{
    public Task CreateUserAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<User?> GetUserAsync(int userId)
    {
        throw new NotImplementedException();
    }
}

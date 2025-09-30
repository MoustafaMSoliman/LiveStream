using LiveStream.DOMAIN;

namespace LiveStream.APPLICATION.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserAsync(int userId);
    Task CreateUserAsync(int userId);

}

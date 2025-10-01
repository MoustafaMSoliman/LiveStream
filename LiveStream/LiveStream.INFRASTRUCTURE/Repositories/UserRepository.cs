using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN;
using LiveStream.DOMAIN.Enums;

namespace LiveStream.INFRASTRUCTURE.Repositories;

public class UserRepository : IUserRepository
{
    private readonly List<User> _users = new()
    {
        new User
        {
            Id = 1,
            Username = "user1",
            Role = UserRole.MyData,
            AllowedDeviceIds = new List<int> { 1, 2 },
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true
        },
        new User
        {
            Id = 2,
            Username = "reporter1",
            Role = UserRole.Reportees,
            AllowedDeviceIds = new List<int> { 3 },
            ReporterDeviceIds = new List<int> { 1, 2 },
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            IsActive = true
        },
        new User
        {
            Id = 3,
            Username = "admin",
            Role = UserRole.All,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            IsActive = true
        }
    };

    public Task<User?> GetUserAsync(int userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        return Task.FromResult(user);
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        return Task.FromResult(_users);
    }

    public Task CreateUserAsync(int userId)
    {
        throw new NotImplementedException();
    }
}

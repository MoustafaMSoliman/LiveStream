using LiveStream.DOMAIN;
using LiveStream.DOMAIN.Enums;
using System.Security;

namespace LiveStream.APPLICATION.Interfaces;

public interface IAuthorizationService
{
    Task<bool> CanViewDeviceAsync(int userId, int deviceId);
    Task<List<Device>> GetAccessibleDevicesAsync(int userId);
    Task<User> GetUserAsync(int userId);
    Task<bool> HasPermissionAsync(int userId, Permission permission);
}

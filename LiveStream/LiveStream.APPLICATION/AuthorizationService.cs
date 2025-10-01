using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN;
using LiveStream.DOMAIN.Enums;
using Microsoft.Extensions.Logging;

namespace LiveStream.APPLICATION;
/*
public class AuthorizationService : IAuthorizationService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        IDeviceRepository deviceRepository,
        IUserRepository userRepository,
        ILogger<AuthorizationService> logger)
    {
        _deviceRepository = deviceRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> CanViewDeviceAsync(int userId, int deviceId)
    {
        var user = await _userRepository.GetUserAsync(userId);
        var device = await _deviceRepository.GetDeviceAsync(deviceId);

        if (user == null || device == null)
            return false;

        var canView = user.Role switch
        {
            UserRole.All => true,
            UserRole.Reportees => device.OwnerId == userId || user.ReporterDeviceIds.Contains(deviceId),
            UserRole.MyData => device.OwnerId == userId,
            UserRole.OU => user.AllowedDeviceIds.Contains(deviceId),
            _ => false
        };

        _logger.LogDebug("User {UserId} access to device {DeviceId}: {Access}",
            userId, deviceId, canView ? "GRANTED" : "DENIED");

        return canView;
    }

    public async Task<List<Device>> GetAccessibleDevicesAsync(int userId)
    {
        var user = await _userRepository.GetUserAsync(userId);
        if (user == null) return new List<Device>();

        List<Device> devices = user.Role switch
        {
            UserRole.All => await _deviceRepository.GetAllDevicesAsync(),
            UserRole.Reportees => await _deviceRepository.GetDevicesByOwnerOrReportersAsync(userId, user.ReporterDeviceIds),
            UserRole.MyData => await _deviceRepository.GetDevicesByOwnerAsync(userId),
            UserRole.OU => await _deviceRepository.GetDevicesByIdsAsync(user.AllowedDeviceIds),
            _ => new List<Device>()
        };

        _logger.LogDebug("User {UserId} can access {DeviceCount} devices", userId, devices.Count);

        return devices;
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        return await _userRepository.GetUserAsync(userId);
    }

    public async Task<bool> HasPermissionAsync(int userId, Permission permission)
    {
        var user = await GetUserAsync(userId);
        if (user == null) return false;

        var hasPermission = permission switch
        {
            Permission.ViewAllDevices => user.Role == UserRole.All,
            Permission.ViewReportersDevices => user.Role == UserRole.All || user.Role == UserRole.Reportees,
            Permission.ViewLiveStream => user.Role == UserRole.All || user.Role == UserRole.Reportees || user.Role == UserRole.MyData,
            Permission.ManageDevices => user.Role == UserRole.All || user.Role == UserRole.MyData,
            _ => false
        };

        _logger.LogDebug("User {UserId} permission {Permission}: {HasPermission}",
            userId, permission, hasPermission ? "GRANTED" : "DENIED");

        return hasPermission;
    }
}*/
public class AuthorizationService : IAuthorizationService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUserRepository _userRepository;

    public AuthorizationService(IDeviceRepository deviceRepository, IUserRepository userRepository)
    {
        _deviceRepository = deviceRepository;
        _userRepository = userRepository;
    }

    public Task<bool> CanViewDeviceAsync(int userId, int deviceId)
    {
        return Task.FromResult(true); 
    }

    public Task<List<Device>> GetAccessibleDevicesAsync(int userId)
    {
        return _deviceRepository.GetAllDevicesAsync();
    }

    public Task<User?> GetUserAsync(int userId)
    {
        return _userRepository.GetUserAsync(userId);
    }

    public Task<bool> HasPermissionAsync(int userId, Permission permission)
    {
        return Task.FromResult(true);
    }
}
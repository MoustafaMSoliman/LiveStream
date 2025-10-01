using LiveStream.DOMAIN;

namespace LiveStream.APPLICATION.Interfaces;


public interface IStreamManager
{
    Task<List<Device>> GetAccessibleDevicesAsync(int userId);
    Task<DeviceStream?> GetOrCreateDeviceStreamAsync(int deviceId);
    Task<int> CreateViewerSessionAsync(int deviceId, int userId, string connectionId);
    Task StopViewerSessionAsync(int deviceId, int userId);
    Task<int> GetDeviceViewerCountAsync(int deviceId);
    Task<List<ViewerInfo>> GetDeviceViewersAsync(int deviceId);
    Task UpdateDeviceStatusAsync(int deviceId, bool isOnline);
}
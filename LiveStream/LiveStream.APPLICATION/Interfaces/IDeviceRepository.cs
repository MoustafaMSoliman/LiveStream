using LiveStream.DOMAIN;

namespace LiveStream.APPLICATION.Interfaces;

public interface IDeviceRepository
{
    Task<Device?> GetDeviceAsync(int deviceId);
    Task<List<Device>> GetAllDevicesAsync();
    Task<List<Device>> GetDevicesByOwnerAsync(int ownerId);
    Task<List<Device>> GetDevicesByIdsAsync(List<int> deviceIds);
    Task<List<Device>> GetDevicesByOwnerOrReportersAsync(int ownerId, List<int> reporterDeviceIds);
    
    Task UpdateDeviceAsync(Device device);
}

using LiveStream.APPLICATION;
using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN;

namespace LiveStream.INFRASTRUCTURE.Repositories;

public class DeviceRepository : IDeviceRepository
{
    public Task<List<Device>> GetAllDevicesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<CameraInfoDto?> GetCameraInfoAsync(int deviceId)
    {
        throw new NotImplementedException();
    }

    public Task<Device?> GetDeviceAsync(int deviceId)
    {
        throw new NotImplementedException();
    }

    public Task<List<Device>> GetDevicesByIdsAsync(List<int> deviceIds)
    {
        throw new NotImplementedException();
    }

    public Task<List<Device>> GetDevicesByOwnerAsync(int ownerId)
    {
        throw new NotImplementedException();
    }

    public Task<List<Device>> GetDevicesByOwnerOrReportersAsync(int ownerId, List<int> reporterDeviceIds)
    {
        throw new NotImplementedException();
    }

    public Task UpdateDeviceAsync(Device device)
    {
        throw new NotImplementedException();
    }
}

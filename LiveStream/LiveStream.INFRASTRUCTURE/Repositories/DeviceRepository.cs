using LiveStream.APPLICATION;
using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN;

namespace LiveStream.INFRASTRUCTURE.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly List<Device> _devices = new()
    {
        new Device
        {
            Id = 1,
            Name = "Main Gate Camera",
            Description = "كاميرا مراقبة للمدخل الرئيسي للمبنى",
            OwnerId = 1,
            RtspUrl = "rtsp://170.93.143.139/rtplive/470011e600ef003a004ee33696235daa",
            IsOnline = true,
            ReporterIds = new List<int> { 2 },
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            Location = "المدخل الرئيسي - الطابق الأرضي",
            MountpointId = 10001
        },
        new Device
        {
            Id = 2,
            Name = "Parking Camera",
            Description = "كاميرا مراقبة لموقف السيارات",
            OwnerId = 1,
            RtspUrl = "https://webrtc.github.io/samples/src/video/chrome.mp4",
            IsOnline = true,
            ReporterIds = new List<int> { 2 },
            CreatedAt = DateTime.UtcNow.AddDays(-45),
            Location = "موقف السيارات - الخارجي",
            MountpointId = 10002
        },
        new Device
        {
            Id = 3,
            Name = "Entrance Camera",
            Description = "كاميرا مراقبة للصالة الرئيسية",
            OwnerId = 2,
            RtspUrl = "rtsp://170.93.143.139/rtplive/470011e600ef003a004ee33696235daa",
            IsOnline = false,
            ReporterIds = new List<int>(),
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            Location = "الصالة الرئيسية - الطابق الأول",
            MountpointId = 10003
        },
        new Device
        {
            Id = 4,
            Name = "Store Camera",
            Description = "كاميرة مراقبة للمخزن الرئيسي",
            OwnerId = 3,
            RtspUrl = "rtsp://170.93.143.139/rtplive/470011e600ef003a004ee33696235dbb",
            IsOnline = true,
            ReporterIds = new List<int> { 1, 2 },
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            Location = "المخزن الرئيسي - الطابق السفلي",
            MountpointId = 10004
        }
    };

    public Task<Device?> GetDeviceAsync(int deviceId)
    {
        var device = _devices.FirstOrDefault(d => d.Id == deviceId);
        return Task.FromResult(device);
    }

    public Task<List<Device>> GetAllDevicesAsync()
    {
        return Task.FromResult(_devices);
    }

    public Task<List<Device>> GetDevicesByOwnerAsync(int ownerId)
    {
        var devices = _devices.Where(d => d.OwnerId == ownerId).ToList();
        return Task.FromResult(devices);
    }

    public Task<List<Device>> GetDevicesByOwnerOrReportersAsync(int ownerId, List<int> reporterDeviceIds)
    {
        var devices = _devices.Where(d =>
            d.OwnerId == ownerId ||
            reporterDeviceIds.Contains(d.Id)
        ).ToList();
        return Task.FromResult(devices);
    }

    public Task<List<Device>> GetDevicesByIdsAsync(List<int> deviceIds)
    {
        var devices = _devices.Where(d => deviceIds.Contains(d.Id)).ToList();
        return Task.FromResult(devices);
    }

    public Task UpdateDeviceAsync(Device device)
    {
        var existingDevice = _devices.FirstOrDefault(d => d.Id == device.Id);
        if (existingDevice != null)
        {
            var index = _devices.IndexOf(existingDevice);
            _devices[index] = device;
        }
        return Task.CompletedTask;
    }

   
}

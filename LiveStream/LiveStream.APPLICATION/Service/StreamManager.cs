using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LiveStream.APPLICATION.Service;

public class StreamManager : IStreamManager
{
    private readonly IJanusService _janusService;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<StreamManager> _logger;
    private readonly ConcurrentDictionary<int, DeviceStream> _activeStreams = new();
    private readonly ConcurrentDictionary<int, List<ViewerSession>> _deviceViewers = new();
    private int _viewerSessionIdCounter = 1;

    public StreamManager(IJanusService janusService, IDeviceRepository deviceRepository,
        ILogger<StreamManager> logger)
    {
        _janusService = janusService;
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    public async Task<DeviceStream?> GetOrCreateDeviceStreamAsync(int deviceId)
    {
        if (_activeStreams.TryGetValue(deviceId, out var stream) && stream.IsActive)
        {
            stream.LastStreamActivity = DateTime.UtcNow;
            return stream;
        }

        var device = await _deviceRepository.GetDeviceAsync(deviceId);
        if (device == null)
        {
            throw new ArgumentException($"Device {deviceId} not found");
        }

        // Create RTSP mountpoint in Janus
        var mountpointId = await _janusService.CreateRtspMountpointAsync(new CreateCameraDto
        (
            device.MountpointId > 0 ? device.MountpointId : GetMountpointId(deviceId),
            device.Name,
            device.RtspUrl,
            true,
            false
        ));

        if (mountpointId == null)
        {
            throw new InvalidOperationException($"Failed to create mountpoint for device {deviceId}");
        }

        // Update device mountpoint ID if it was auto-generated
        if (device.MountpointId <= 0)
        {
            device.MountpointId = mountpointId.Value;
            await _deviceRepository.UpdateDeviceAsync(device);
        }

        var deviceStream = new DeviceStream
        {
            DeviceId = deviceId,
            MountpointId = mountpointId.Value,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastStreamActivity = DateTime.UtcNow,
            ViewerCount = 0
        };

        _activeStreams[deviceId] = deviceStream;

        _logger.LogInformation("Created stream for device {DeviceId} with mountpoint {MountpointId}",
            deviceId, mountpointId);

        return deviceStream;
    }

    public async Task<int> CreateViewerSessionAsync(int deviceId, int userId, string connectionId)
    {
        var sessionId = Interlocked.Increment(ref _viewerSessionIdCounter);

        var viewerSession = new ViewerSession
        {
            Id = sessionId,
            DeviceId = deviceId,
            UserId = userId,
            ConnectionId = connectionId,
            StartedAt = DateTime.UtcNow,
            IsActive = true
        };

        _deviceViewers.AddOrUpdate(deviceId,
            new List<ViewerSession> { viewerSession },
            (key, existing) =>
            {
                existing.Add(viewerSession);
                return existing;
            });

        // Update viewer count
        if (_activeStreams.TryGetValue(deviceId, out var stream))
        {
            stream.ViewerCount = _deviceViewers[deviceId].Count;
            stream.LastStreamActivity = DateTime.UtcNow;
        }

        _logger.LogInformation("User {UserId} started watching device {DeviceId} (ViewerSession: {SessionId})",
            userId, deviceId, sessionId);

        return sessionId;
    }

    public async Task<int> GetDeviceViewerCountAsync(int deviceId)
    {
        await Task.CompletedTask; // Simulate async operation
        if (_deviceViewers.TryGetValue(deviceId, out var viewers))
        {
            return viewers.Count(v => v.IsActive);
        }
        return 0;
    }

    public async Task<List<ViewerInfo>> GetDeviceViewersAsync(int deviceId)
    {
        throw new NotImplementedException();
    }

    private int GetMountpointId(int deviceId)
    {
        // Generate consistent mountpoint ID based on device ID
        return 10000 + (deviceId % 90000); // Mountpoint IDs from 10000 to 99999

    }

    

    public Task StopViewerSessionAsync(int deviceId, int userId)
    {
        throw new NotImplementedException();
    }

   

    public Task UpdateDeviceStatusAsync(int deviceId, bool isOnline)
    {
        throw new NotImplementedException();
    }
}

using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LiveStream.APPLICATION.Service;
/*
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
}*/
public class StreamManager : IStreamManager
{
    private readonly IJanusService _janusService;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IAuthorizationService _authService;
    private readonly ILogger<StreamManager> _logger;

    private readonly ConcurrentDictionary<int, DeviceStream> _activeStreams = new();
    private readonly ConcurrentDictionary<int, List<ViewerSession>> _deviceViewers = new();
    private int _viewerSessionIdCounter = 1;

    public StreamManager(
        IJanusService janusService,
        IDeviceRepository deviceRepository,
        IAuthorizationService authService,
        ILogger<StreamManager> logger)
    {
        _janusService = janusService;
        _deviceRepository = deviceRepository;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// الحصول على جميع الأجهزة المتاحة للمستخدم
    /// حالياً ترجع جميع الأجهزة بدون تصفية
    /// </summary>
    public async Task<List<Device>> GetAccessibleDevicesAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Getting the user {UserId} devices", userId);

            var allDevices = await _deviceRepository.GetAllDevicesAsync();

            _logger.LogInformation("Get device {DeviceCount} for user {UserId}",
                allDevices.Count, userId);

            return allDevices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب الأجهزة للمستخدم {UserId}", userId);
            return new List<Device>(); 
        }
    }

    /// <summary>
    /// الحصول على أو إنشاء ستريم للجهاز
    /// </summary>
    public async Task<DeviceStream?> GetOrCreateDeviceStreamAsync(int deviceId)
    {
        try
        {
            if (_activeStreams.TryGetValue(deviceId, out var stream) && stream.IsActive)
            {
                stream.LastStreamActivity = DateTime.UtcNow;
                _logger.LogDebug("تم استخدام الستريم النشط للجهاز {DeviceId}", deviceId);
                return stream;
            }

            var device = await _deviceRepository.GetDeviceAsync(deviceId);
            if (device == null)
            {
                _logger.LogWarning("الجهاز {DeviceId} غير موجود", deviceId);
                return null;
            }

            var mountpointId = await CreateJanusMountpoint(device);
            if (mountpointId == null)
            {
                _logger.LogError("فشل إنشاء mountpoint للجهاز {DeviceId}", deviceId);
                return null;
            }

            if (device.MountpointId <= 0)
            {
                device.MountpointId = mountpointId.Value;
                await _deviceRepository.UpdateDeviceAsync(device);
            }

            // إنشاء كائن الستريم الجديد
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

            _logger.LogInformation("تم إنشاء ستريم جديد للجهاز {DeviceId} مع mountpoint {MountpointId}",
                deviceId, mountpointId);

            return deviceStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ غير متوقع في إنشاء ستريم للجهاز {DeviceId}", deviceId);
            return null;
        }
    }

    /// <summary>
    /// إنشاء جلسة مشاهد جديدة
    /// </summary>
    public async Task<int> CreateViewerSessionAsync(int deviceId, int userId, string connectionId)
    {
        try
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

            
            await UpdateViewerCount(deviceId);

            _logger.LogInformation("تم إنشاء جلسة مشاهد {SessionId} للجهاز {DeviceId} للمستخدم {UserId}",
                sessionId, deviceId, userId);

            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في إنشاء جلسة مشاهد للجهاز {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// إيقاف جلسة المشاهد
    /// </summary>
    public async Task StopViewerSessionAsync(int deviceId, int userId)
    {
        try
        {
            if (_deviceViewers.TryGetValue(deviceId, out var viewers))
            {
                viewers.RemoveAll(v => v.UserId == userId);
                await UpdateViewerCount(deviceId);
            }

            _logger.LogInformation("تم إيقاف جلسات المشاهدة للجهاز {DeviceId} للمستخدم {UserId}",
                deviceId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في إيقاف جلسة المشاهدة للجهاز {DeviceId}", deviceId);
        }
    }

    /// <summary>
    /// الحصول على عدد المشاهدين للجهاز
    /// </summary>
    public async Task<int> GetDeviceViewerCountAsync(int deviceId)
    {
        try
        {
            if (_deviceViewers.TryGetValue(deviceId, out var viewers))
            {
                return viewers.Count(v => v.IsActive);
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get device viewers count wrong {DeviceId}", deviceId);
            return 0;
        }
    }

    /// <summary>
    /// الحصول على قائمة المشاهدين للجهاز
    /// </summary>
    public async Task<List<ViewerInfo>> GetDeviceViewersAsync(int deviceId)
    {
        try
        {
            if (_deviceViewers.TryGetValue(deviceId, out var viewers))
            {
                var viewerInfos = viewers.Select(v => new ViewerInfo
                {
                    UserId = v.UserId,
                    Username = $"User{v.UserId}", 
                    StartedAt = v.StartedAt,
                    Duration = DateTime.UtcNow - v.StartedAt,
                    ConnectionId = v.ConnectionId
                }).ToList();

                return viewerInfos;
            }

            return new List<ViewerInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get device viewers count wrong {DeviceId}", deviceId);
            return new List<ViewerInfo>();
        }
    }

    /// <summary>
    /// تحديث حالة الجهاز (متصل/غير متصل)
    /// </summary>
    public async Task UpdateDeviceStatusAsync(int deviceId, bool isOnline)
    {
        try
        {
            var device = await _deviceRepository.GetDeviceAsync(deviceId);
            if (device != null)
            {
                device.IsOnline = isOnline;
                await _deviceRepository.UpdateDeviceAsync(device);

                _logger.LogInformation("Device {DeviceId} status is updated  to {Status}",
                    deviceId, isOnline ? "Connected" : "Not Connected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Updating wrong on this device: {DeviceId}", deviceId);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// إنشاء mountpoint في Janus
    /// </summary>
    private async Task<int?> CreateJanusMountpoint(Device device)
    {
        try
        {
            var mountpointId = await _janusService.CreateRtspMountpointAsync(new CreateCameraDto
            (
               device.MountpointId > 0 ? device.MountpointId : GenerateMountpointId(device.Id),
                device.Name,
                device.RtspUrl,
                true,
                false
            ));

            return mountpointId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل إنشاء mountpoint في Janus للجهاز {DeviceId}", device.Id);
            return null;
        }
    }

    /// <summary>
    /// تحديث عدد المشاهدين للجهاز
    /// </summary>
    private async Task UpdateViewerCount(int deviceId)
    {
        try
        {
            if (_deviceViewers.TryGetValue(deviceId, out var viewers))
            {
                var activeViewers = viewers.Count(v => v.IsActive);

                if (_activeStreams.TryGetValue(deviceId, out var stream))
                {
                    stream.ViewerCount = activeViewers;
                    stream.LastStreamActivity = DateTime.UtcNow;
                }

                _logger.LogDebug("تم تحديث عدد المشاهدين للجهاز {DeviceId} إلى {ViewerCount}",
                    deviceId, activeViewers);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في تحديث عدد المشاهدين للجهاز {DeviceId}", deviceId);
        }
    }

    /// <summary>
    /// توليد mountpoint ID بناءً على device ID
    /// </summary>
    private int GenerateMountpointId(int deviceId)
    {
        return 10000 + (deviceId % 90000); // أرقام من 10000 إلى 99999
    }

    #endregion
}

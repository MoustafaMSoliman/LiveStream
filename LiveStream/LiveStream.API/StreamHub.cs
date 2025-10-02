using LiveStream.API.CustomAttributes;
using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN;
using LiveStream.DOMAIN.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using IAuthorizationService = LiveStream.APPLICATION.Interfaces.IAuthorizationService;



namespace LiveStream.API;

//[Authorize]
public class StreamHub : Hub
{
    private readonly IStreamManager _streamManager;
    private readonly ILogger<StreamHub> _logger;
    private static readonly ConcurrentDictionary<string, UserSession> _sessions = new();
    private static int _sessionIdCounter = 1;

    public StreamHub(IStreamManager streamManager, ILogger<StreamHub> logger)
    {
        _streamManager = streamManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        
        var userId = 1; 
        var userSession = new UserSession
        {
            Id = Interlocked.Increment(ref _sessionIdCounter),
            UserId = userId,
            Role = UserRole.All, 
            ConnectedAt = DateTime.UtcNow,
            ConnectionId = Context.ConnectionId,
            LastActivity = DateTime.UtcNow
        };

        _sessions[Context.ConnectionId] = userSession;

        _logger.LogInformation("User {UserId} connected to StreamHub", userId);

       
        var accessibleDevices = await _streamManager.GetAccessibleDevicesAsync(userId);
        await Clients.Caller.SendAsync("AccessibleDevices", accessibleDevices);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_sessions.TryRemove(Context.ConnectionId, out var session))
        {
            _logger.LogInformation("User {UserId} disconnected", session.UserId);

            
            foreach (var deviceId in session.WatchingDevices)
            {
                await _streamManager.StopViewerSessionAsync(deviceId, session.UserId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

   
    public async Task<HubResult<SignalingInfo>> RequestSignalingInfo(int deviceId)
    {
        var userId = 1; 

        try
        {
            var device = await _streamManager.GetOrCreateDeviceStreamAsync(deviceId);
            if (device == null)
            {
                return HubResult<SignalingInfo>.Failure("Device not found");
            }

            var signalingInfo = new SignalingInfo
            {
                JanusWebSocketUrl = "ws://localhost:8188/janus",
                MountpointId = device.MountpointId,
                DeviceId = deviceId,
                DeviceName = "devName",
                GeneratedAt = DateTime.UtcNow,
                IceServers = new List<IceServer>
                {
                    new IceServer { Urls = "stun:stun.l.google.com:19302" },
                    new IceServer { Urls = "stun:stun1.l.google.com:19302" }
                }
            };

            return HubResult<SignalingInfo>.Successful(signalingInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signaling info for device {DeviceId}", deviceId);
            return HubResult<SignalingInfo>.Failure("Error getting stream information");
        }
    }

    public async Task<HubResult<List<DeviceInfo>>> GetMyDevices()
    {
        var userId = 1; 

        try
        {
            var devices = await _streamManager.GetAccessibleDevicesAsync(userId);
            var deviceInfos = new List<DeviceInfo>();

            foreach (var device in devices)
            {
                var viewerCount = await _streamManager.GetDeviceViewerCountAsync(device.Id);
                deviceInfos.Add(new DeviceInfo
                {
                    Id = device.Id,
                    Name = device.Name,
                    Description = device.Description,
                    IsOnline = device.IsOnline,
                    CanView = true,
                    Location = device.Location,
                    CreatedAt = device.CreatedAt,
                    ViewerCount = viewerCount,
                    Status = device.IsOnline ? "Online" : "Offline"
                });
            }

            return HubResult<List<DeviceInfo>>.Successful(deviceInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices for user {UserId}", userId);
            return HubResult<List<DeviceInfo>>.Failure("Error getting devices");
        }
    }

    public async Task<HubResult<int>> StartWatching(int deviceId)
    {
        var userId = 1;

        try
        {
            var sessionId = await _streamManager.CreateViewerSessionAsync(deviceId, userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"device-{deviceId}");

            if (_sessions.TryGetValue(Context.ConnectionId, out var userSession))
            {
                userSession.WatchingDevices.Add(deviceId);
            }

            return HubResult<int>.Successful(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream watch for device {DeviceId}", deviceId);
            return HubResult<int>.Failure("Error starting stream");
        }
    }

    public async Task<HubResult> StopWatching(int deviceId)
    {
        var userId = 1;

        try
        {
            await _streamManager.StopViewerSessionAsync(deviceId, userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device-{deviceId}");

            if (_sessions.TryGetValue(Context.ConnectionId, out var userSession))
            {
                userSession.WatchingDevices.Remove(deviceId);
            }

            return HubResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping stream watch for device {DeviceId}", deviceId);
            return HubResult.Failure("Error stopping stream");
        }
    }
    public async Task<HubResult> Heartbeat()
    {
        try
        {
            if (_sessions.TryGetValue(Context.ConnectionId, out var userSession))
            {
                userSession.LastActivity = DateTime.UtcNow;
                _logger.LogDebug("Received signal from user: {UserId}", userSession.UserId);
            }

            await Clients.Caller.SendAsync("heartbeatAck", DateTime.UtcNow);

            return HubResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signal process wrong");
            return HubResult.Failure("Signal process wrong");
        }
    }

}
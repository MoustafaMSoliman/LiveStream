using LiveStream.API.CustomAttributes;
using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN;
using LiveStream.DOMAIN.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using IAuthorizationService = LiveStream.APPLICATION.Interfaces.IAuthorizationService;
namespace LiveStream.API;


[Authorize]
public class StreamHub : Hub
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IAuthorizationService _authService;
    private readonly IStreamManager _streamManager;
    private readonly IJanusService _janusService;
    private readonly ILogger<StreamHub> _logger;
    private static readonly ConcurrentDictionary<string, UserSession> _sessions = new();
    private static int _sessionIdCounter = 1;

    public StreamHub(
        IDeviceRepository deviceRepository,
        IAuthorizationService authService,
        IStreamManager streamManager,
        IJanusService janusService,
        ILogger<StreamHub> logger)
    {
        _deviceRepository = deviceRepository;
        _authService = authService;
        _streamManager = streamManager;
        _janusService = janusService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        if (!int.TryParse(Context.UserIdentifier, out int userId))
        {
            Context.Abort();
            return;
        }

        var user = await _authService.GetUserAsync(userId);
        if (user == null)
        {
            Context.Abort();
            return;
        }

        var userSession = new UserSession
        {
            Id = Interlocked.Increment(ref _sessionIdCounter),
            UserId = userId,
            Role = user.Role,
            ConnectedAt = DateTime.UtcNow,
            ConnectionId = Context.ConnectionId,
            LastActivity = DateTime.UtcNow
        };

        _sessions[Context.ConnectionId] = userSession;

        _logger.LogInformation("User {UserId} with role {Role} connected to StreamHub (Session: {SessionId})",
            userId, user.Role, userSession.Id);

        // Send accessible devices to the connected user
        var accessibleDevices = await _authService.GetAccessibleDevicesAsync(userId);
        var deviceInfos = accessibleDevices.Select(d => new DeviceInfo
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            IsOnline = d.IsOnline,
            CanView = true,
            Location = d.Location,
            CreatedAt = d.CreatedAt,
            Status = d.IsOnline ? "Online" : "Offline"
        }).ToList();

        await Clients.Caller.SendAsync("AccessibleDevices", deviceInfos);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_sessions.TryRemove(Context.ConnectionId, out var session))
        {
            _logger.LogInformation("User {UserId} disconnected from StreamHub (Session: {SessionId})",
                session.UserId, session.Id);

            // Cleanup any active viewer sessions
            foreach (var deviceId in session.WatchingDevices)
            {
                await _streamManager.StopViewerSessionAsync(deviceId, session.UserId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    [AuthorizeDeviceAccess]
    public async Task<HubResult<SignalingInfo>> RequestSignalingInfo(int deviceId)
    {
        var userId = int.Parse(Context.UserIdentifier!);

        try
        {
            // Get device and create mountpoint if needed
            var device = await _deviceRepository.GetDeviceAsync(deviceId);
            var deviceStream = await _streamManager.GetOrCreateDeviceStreamAsync(deviceId);
            if (deviceStream == null)
            {
                return HubResult<SignalingInfo>.Failure("Device not found or offline");
            }

            var signalingInfo = new SignalingInfo
            {
                JanusWebSocketUrl = "ws://localhost:8188/janus",
                MountpointId = deviceStream.MountpointId,
                DeviceId = deviceId,
                DeviceName = device.Name,
                GeneratedAt = DateTime.UtcNow,
                TimeoutSeconds = 30,
                IceServers = new List<IceServer>
                {
                    new IceServer { Urls = "stun:stun.l.google.com:19302" },
                    new IceServer { Urls = "stun:stun1.l.google.com:19302" },
                    new IceServer { Urls = "stun:stun2.l.google.com:19302" }
                }
            };

            _logger.LogInformation("Signaling info provided to user {UserId} for device {DeviceId}",
                userId, deviceId);

            return HubResult<SignalingInfo>.Successful(signalingInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error providing signaling info to user {UserId} for device {DeviceId}",
                userId, deviceId);
            return HubResult<SignalingInfo>.Failure("Error getting stream information");
        }
    }

    [AuthorizePermission(Permission.ViewLiveStream)]
    public async Task<HubResult<List<DeviceInfo>>> GetMyDevices()
    {
        var userId = int.Parse(Context.UserIdentifier!);

        try
        {
            var devices = await _authService.GetAccessibleDevicesAsync(userId);
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

    [AuthorizeDeviceAccess]
    public async Task<HubResult<int>> StartWatching(int deviceId)
    {
        var userId = int.Parse(Context.UserIdentifier!);

        try
        {
            // Create viewer session
            var sessionId = await _streamManager.CreateViewerSessionAsync(deviceId, userId, Context.ConnectionId);

            // Add to group for this device (for real-time notifications)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"device-{deviceId}");

            // Update user session
            if (_sessions.TryGetValue(Context.ConnectionId, out var userSession))
            {
                userSession.WatchingDevices.Add(deviceId);
                userSession.LastActivity = DateTime.UtcNow;
            }

            _logger.LogInformation("User {UserId} started watching device {DeviceId} (Session: {SessionId})",
                userId, deviceId, sessionId);

            return HubResult<int>.Successful(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream watch for user {UserId} device {DeviceId}",
                userId, deviceId);
            return HubResult<int>.Failure("Error starting stream");
        }
    }

    [AuthorizeDeviceAccess]
    public async Task<HubResult> StopWatching(int deviceId)
    {
        var userId = int.Parse(Context.UserIdentifier!);

        try
        {
            await _streamManager.StopViewerSessionAsync(deviceId, userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device-{deviceId}");

            // Update user session
            if (_sessions.TryGetValue(Context.ConnectionId, out var userSession))
            {
                userSession.WatchingDevices.Remove(deviceId);
                userSession.LastActivity = DateTime.UtcNow;
            }

            _logger.LogInformation("User {UserId} stopped watching device {DeviceId}", userId, deviceId);

            return HubResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping stream watch for user {UserId} device {DeviceId}",
                userId, deviceId);
            return HubResult.Failure("Error stopping stream");
        }
    }

    [AuthorizePermission(Permission.ViewAllDevices)]
    public async Task<HubResult<List<ViewerInfo>>> GetDeviceViewers(int deviceId)
    {
        try
        {
            var viewers = await _streamManager.GetDeviceViewersAsync(deviceId);
            return HubResult<List<ViewerInfo>>.Successful(viewers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting viewers for device {DeviceId}", deviceId);
            return HubResult<List<ViewerInfo>>.Failure("Error getting viewers");
        }
    }

    [AuthorizePermission(Permission.ManageDevices)]
    public async Task<HubResult> BroadcastToViewers(int deviceId, string message)
    {
        var userId = int.Parse(Context.UserIdentifier!);

        await Clients.Group($"device-{deviceId}")
            .SendAsync("ViewerNotification", new
            {
                Message = message,
                FromUserId = userId,
                FromUsername = (await _authService.GetUserAsync(userId))?.Username ?? "Unknown",
                Timestamp = DateTime.UtcNow,
                IsAdminMessage = true
            });

        _logger.LogInformation("User {UserId} broadcast message to device {DeviceId} viewers",
            userId, deviceId);

        return HubResult.Successful();
    }

    [AuthorizePermission(Permission.ViewReportersDevices)]
    public async Task<HubResult<List<DeviceInfo>>> GetReportersDevices()
    {
        var userId = int.Parse(Context.UserIdentifier!);

        try
        {
            var user = await _authService.GetUserAsync(userId);
            if (user == null)
                return HubResult<List<DeviceInfo>>.Failure("User not found");

            var devices = await _authService.GetAccessibleDevicesAsync(userId);
            var reporterDevices = devices.Where(d => d.ReporterIds.Contains(userId)).ToList();

            var deviceInfos = new List<DeviceInfo>();
            foreach (var d in reporterDevices)
            {
                var viewerCount = await _streamManager.GetDeviceViewerCountAsync(d.Id);
                deviceInfos.Add(new DeviceInfo
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    IsOnline = d.IsOnline,
                    CanView = true,
                    Location = d.Location,
                    CreatedAt = d.CreatedAt,
                    ViewerCount = viewerCount,
                    Status = d.IsOnline ? "Online" : "Offline"
                });
            }

            return HubResult<List<DeviceInfo>>.Successful(deviceInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reporters devices for user {UserId}", userId);
            return HubResult<List<DeviceInfo>>.Failure("Error getting reporters devices");
        }
    }

    // Heartbeat to keep session alive
    public async Task<HubResult> Heartbeat()
    {
        if (_sessions.TryGetValue(Context.ConnectionId, out var userSession))
        {
            userSession.LastActivity = DateTime.UtcNow;
        }

        await Clients.Caller.SendAsync("HeartbeatAck", DateTime.UtcNow);
        return HubResult.Successful();
    }

    // Get current user session info
    public HubResult<UserSession> GetMySession()
    {
        if (_sessions.TryGetValue(Context.ConnectionId, out var userSession))
        {
            return HubResult<UserSession>.Successful(userSession);
        }

        return HubResult<UserSession>.Failure("Session not found");
    }
}
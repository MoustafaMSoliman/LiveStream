namespace LiveStream.APPLICATION;

public interface IJanusService
{
    Task<long?> CreateSessionAsync();
    Task<long?> AttachPluginAsync(long sessionId, string plugin);
    Task DestroySessionAsync(long sessionId);

    Task<long> AttachToStreamingAsync(long sessionId);
    Task<bool> CreateCameraAsync(long sessionId, long handleId, int cameraId, string description, int port);
    Task<int?> CreateRtspMountpointAsync(CreateCameraDto dto);
}

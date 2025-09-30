using System;

namespace LiveStream.DOMAIN;

public class SignalingInfo
{
    public string JanusWebSocketUrl { get; set; } = string.Empty;
    public int MountpointId { get; set; }
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public List<IceServer> IceServers { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}

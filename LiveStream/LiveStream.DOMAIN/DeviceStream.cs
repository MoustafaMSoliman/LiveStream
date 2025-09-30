namespace LiveStream.DOMAIN;

public class DeviceStream
{
    public int DeviceId { get; set; }
    public int MountpointId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ViewerCount { get; set; }
    public DateTime LastStreamActivity { get; set; }
}

namespace LiveStream.DOMAIN;

public class Device
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public string RtspUrl { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public List<int> ReporterIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string Location { get; set; } = string.Empty;
    public int MountpointId { get; set; }
}

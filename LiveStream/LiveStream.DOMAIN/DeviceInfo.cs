namespace LiveStream.DOMAIN;

public class DeviceInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool CanView { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ViewerCount { get; set; }
    public string Status { get; set; } = "Offline";
}

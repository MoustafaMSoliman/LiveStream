using LiveStream.DOMAIN.Enums;

namespace LiveStream.DOMAIN;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public List<int> AllowedDeviceIds { get; set; } = new();
    public List<int> ReporterDeviceIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

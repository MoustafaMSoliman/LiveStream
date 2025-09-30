using LiveStream.DOMAIN.Enums;

namespace LiveStream.DOMAIN;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public UserRole Role { get; set; }
    public DateTime ConnectedAt { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public List<int> WatchingDevices { get; set; } = new();
    public DateTime LastActivity { get; set; }
}

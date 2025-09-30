namespace LiveStream.DOMAIN;

public class ViewerInfo
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
}

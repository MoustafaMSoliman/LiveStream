namespace LiveStream.APPLICATION.DTOs;

public class CreateSessionRequest
{
    public string ClientInfo { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string ClientVersion { get; set; } = string.Empty;
}

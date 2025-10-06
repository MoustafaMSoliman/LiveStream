namespace LiveStream.APPLICATION.Interfaces;

public interface IStreamTokenService
{
    Task<string> GenerateSecureStreamTokenAsync(string cameraId, string userId, string clientIp, TimeSpan validity);
    Task<bool> ValidateStreamTokenAsync(string token, string cameraId, string clientIp);
}

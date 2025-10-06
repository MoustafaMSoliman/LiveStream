using LiveStream.APPLICATION.DTOs;
using LiveStream.APPLICATION.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LiveStream.APPLICATION.Service;
/*
public class StreamTokenService : IStreamTokenService
{
    private readonly IConfiguration _configuration;
    public StreamTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public async Task<string> GenerateSecureStreamTokenAsync(string cameraId, string userId, string clientIp)
    {
        await Task.CompletedTask; 
        var payload = new
        {
            cam = cameraId,
            user = userId,
            ip = clientIp,
            exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() // 5-minute expiry
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = _configuration["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT secret key is not configured.");

        var key = Encoding.ASCII.GetBytes(secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("cam", cameraId),
                new Claim("user", userId),
                new Claim("ip", clientIp)
            }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
*/
// Services/StreamTokenService.cs

/*
public class StreamTokenService : IStreamTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, bool> _usedTokens = new();
    public StreamTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public async Task<string> GenerateSecureStreamTokenAsync(string cameraId, string userId, string clientIp, TimeSpan validity)
    {
        await Task.CompletedTask; 
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("cameraId", cameraId),
                new Claim("userId", userId),
                new Claim("clientIp", clientIp),
                new Claim("expires", DateTime.UtcNow.Add(validity).ToString("O"))
            }),
            Expires = DateTime.UtcNow.Add(validity),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    public async Task<string> GenerateOneTimeTokenAsync(string cameraId, string userId, string clientIp)
    {
        var token = await GenerateSecureStreamTokenAsync(cameraId, userId, clientIp, TimeSpan.FromMinutes(5));
        _usedTokens[token] = false; // Mark as unused

        // Auto-cleanup after expiry
        _ = RemoveTokenAfterDelay(token, TimeSpan.FromMinutes(6));
        return token;
    }
    public async Task<bool> ValidateStreamTokenAsync(string token, string cameraId, string clientIp)
    {
        await Task.CompletedTask;
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            // Validate camera ID and IP
            var tokenCameraId = jwtToken.Claims.First(x => x.Type == "cameraId").Value;
            var tokenClientIp = jwtToken.Claims.First(x => x.Type == "clientIp").Value;

            return tokenCameraId == cameraId && tokenClientIp == clientIp;
        }
        catch
        {
            return false;
        }
    }
}
*/
public class StreamTokenService
{
    private readonly string _secretKey;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, DateTime> _usedTokens = new();

    public StreamTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = _configuration["Jwt:Secret"] ?? "your-super-secure-secret-key-32-chars-long";
    }

    public TokenResponse GenerateTokens(string cameraId, string userId, string clientIp)
    {
        var accessToken = GenerateToken(cameraId, userId, clientIp, TimeSpan.FromMinutes(5));
        var refreshToken = GenerateToken(cameraId, userId, clientIp, TimeSpan.FromMinutes(60));

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 300, // 5 minutes
            RefreshExpiresIn = 3600 // 60 minutes
        };
    }

    private string GenerateToken(string cameraId, string userId, string clientIp, TimeSpan validity)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("cameraId", cameraId),
                new Claim("userId", userId),
                new Claim("clientIp", clientIp),
                new Claim("tokenType", validity.TotalMinutes == 5 ? "access" : "refresh"),
                new Claim("exp", DateTimeOffset.UtcNow.Add(validity).ToUnixTimeSeconds().ToString())
            }),
            Expires = DateTime.UtcNow.Add(validity),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateToken(string token, string expectedCameraId, string clientIp)
    {
        try
        {
            // Check if token was already used (prevent replay attacks)
            if (_usedTokens.ContainsKey(token))
                return false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            // Validate token type
            var tokenType = jwtToken.Claims.First(x => x.Type == "tokenType").Value;
            if (tokenType != "access")
                return false;

            // Validate camera ID
            var tokenCameraId = jwtToken.Claims.First(x => x.Type == "cameraId").Value;
            if (tokenCameraId != expectedCameraId)
                return false;

            // Validate IP address
            var tokenClientIp = jwtToken.Claims.First(x => x.Type == "clientIp").Value;
            if (tokenClientIp != clientIp)
                return false;

            // Mark token as used (one-time use)
            _usedTokens[token] = DateTime.UtcNow;

            // Cleanup old tokens
            CleanupUsedTokens();

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void CleanupUsedTokens()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        var expiredTokens = _usedTokens.Where(x => x.Value < cutoff).ToList();
        foreach (var token in expiredTokens)
        {
            _usedTokens.Remove(token.Key);
        }
    }

    public string RefreshAccessToken(string refreshToken, string clientIp)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            // Validate token type
            var tokenType = jwtToken.Claims.First(x => x.Type == "tokenType").Value;
            if (tokenType != "refresh")
                return null;

            // Extract user data
            var cameraId = jwtToken.Claims.First(x => x.Type == "cameraId").Value;
            var userId = jwtToken.Claims.First(x => x.Type == "userId").Value;
            var tokenClientIp = jwtToken.Claims.First(x => x.Type == "clientIp").Value;

            // Validate IP
            if (tokenClientIp != clientIp)
                return null;

            return GenerateToken(cameraId, userId, clientIp, TimeSpan.FromMinutes(5));
        }
        catch
        {
            return null;
        }
    }
}
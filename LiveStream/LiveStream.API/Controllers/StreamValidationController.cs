using LiveStream.APPLICATION.DTOs;
using LiveStream.APPLICATION.Interfaces;
using LiveStream.APPLICATION.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LiveStream.API.Controllers
{
    [ApiController]
    [Route("api/stream")]
    [Authorize]
    public class StreamValidationController : ControllerBase
    {
        private readonly StreamTokenService _streamTokenService;
        private readonly ILogger<StreamValidationController> _logger;

        public StreamValidationController(ILogger<StreamValidationController> logger, StreamTokenService streamTokenService)
        {
            _logger = logger;
            _streamTokenService = streamTokenService;
        }

        [HttpPost("token")]
        public IActionResult GenerateToken([FromBody] TokenRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation($"Generating token for user {userId}, camera {request.CameraId}, IP {clientIp}");

            var tokens = _streamTokenService.GenerateTokens(request.CameraId, userId, clientIp);

            var streamInfo = new StreamInfo
            {
                WebRTCUrl = $"http://localhost:8889/whep/{request.CameraId}",
                HLSUrl = $"http://localhost:8888/{request.CameraId}/index.m3u8",
                RTSPUrl = $"rtsp://localhost:8554/{request.CameraId}",
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn)
            };

            return Ok(streamInfo);
        }

        [HttpPost("refresh")]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation($"Refreshing token for camera {request.CameraId}, IP {clientIp}");

            var newAccessToken = _streamTokenService.RefreshAccessToken(request.RefreshToken, clientIp);

            if (string.IsNullOrEmpty(newAccessToken))
            {
                _logger.LogWarning("Token refresh failed");
                return Unauthorized();
            }

            return Ok(new { accessToken = newAccessToken });
        }

        [HttpPost("validate-token")]
        [AllowAnonymous] // MediaMTX will call this without authentication
        public IActionResult ValidateStreamToken()
        {
            var path = Request.Query["path"];
            var user = Request.Query["user"]; // This is our token
            var ip = Request.Query["ip"];
            var action = Request.Query["action"];

            _logger.LogInformation($"Validating stream: Path={path}, IP={ip}, Action={action}");

            if (_streamTokenService.ValidateToken(user, path, ip))
            {
                _logger.LogInformation("Stream validation SUCCESS");
                return Ok();
            }

            _logger.LogWarning("Stream validation FAILED");
            return Unauthorized();
        }
    }
}
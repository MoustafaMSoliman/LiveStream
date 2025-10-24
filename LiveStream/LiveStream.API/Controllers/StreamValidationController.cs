using LiveStream.APPLICATION.DTOs;
using LiveStream.APPLICATION.Interfaces;
using LiveStream.APPLICATION.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveStream.API.Controllers
{
    [ApiController]
    [Route("api/stream")]

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
                WebRTCUrl = $"http://localhost:8889/{request.CameraId}/whep",
                HLSUrl = $"http://localhost:8888/{request.CameraId}/index.m3u8",
                RTSPUrl = $"rtsp://localhost:8554/{request.CameraId}",
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn)
            };

            return Ok(streamInfo);
        }
        [Authorize]
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
        /*
         [HttpPost("validate-token")]
         [AllowAnonymous]
         public IActionResult ValidateStreamToken()
         {
             try
             {
                 _logger.LogInformation($"🔐 Request Method: {Request.Method}");
                 _logger.LogInformation($"🔐 Content-Type: {Request.ContentType}");
                 _logger.LogInformation($"🔐 Has Form: {Request.HasFormContentType}");

                 string path = "";
                 string ip = "";
                 string action = "";
                 string protocol = "";
                 string token = "";

                 // Read from JSON body
                 if (Request.ContentLength > 0)
                 {
                     try
                     {
                         using var reader = new StreamReader(Request.Body);
                         var body = reader.ReadToEndAsync().Result;
                         _logger.LogInformation($"🔐 Raw Body: {body}");

                         // Parse as JSON
                         var jsonData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
                         if (jsonData != null)
                         {
                             path = jsonData.ContainsKey("path") ? jsonData["path"].GetString() ?? "" : "";
                             ip = jsonData.ContainsKey("ip") ? jsonData["ip"].GetString() ?? "" : "";
                             action = jsonData.ContainsKey("action") ? jsonData["action"].GetString() ?? "" : "";
                             protocol = jsonData.ContainsKey("protocol") ? jsonData["protocol"].GetString() ?? "" : "";

                             // Extract token from the query field
                             if (jsonData.ContainsKey("query"))
                             {
                                 var queryString = jsonData["query"].GetString() ?? "";
                                 _logger.LogInformation($"🔐 Query String: {queryString}");

                                 // Parse the query string to get the token
                                 var queryParams = System.Web.HttpUtility.ParseQueryString(queryString);
                                 token = queryParams["token"] ?? "";
                             }
                         }
                         _logger.LogInformation("🔐 Using JSON Body");
                     }
                     catch (Exception ex)
                     {
                         _logger.LogWarning($"🔐 Failed to parse body as JSON: {ex.Message}");
                     }
                 }

                 _logger.LogInformation($"🔐 Auth Request: Path='{path}', IP='{ip}', Action='{action}', Protocol='{protocol}'");
                 _logger.LogInformation($"🔐 Token: '{token}'");

                 // Check if required parameters are missing
                 if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(ip))
                 {
                     _logger.LogWarning("❌ Missing required authentication parameters");
                     return Unauthorized();
                 }

                 if (_streamTokenService.ValidateToken(token, path, ip))
                 {
                     _logger.LogInformation("✅ Authentication SUCCESS");
                     return Ok();
                 }

                 _logger.LogWarning("❌ Token validation FAILED");
                 return Unauthorized();
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "❌ Error in stream validation");
                 return Unauthorized();
             }
         }
         */

        /*
        [HttpPost("validate-token")]
        [AllowAnonymous]
        public IActionResult ValidateStreamToken()
        {
            try
            {
                _logger.LogInformation($"🔐 ===== NEW AUTH REQUEST =====");
                _logger.LogInformation($"🔐 Request Method: {Request.Method}");
                _logger.LogInformation($"🔐 Content-Type: {Request.ContentType}");
                _logger.LogInformation($"🔐 Content-Length: {Request.ContentLength}");

                // MediaMTX sends JSON body with authentication parameters
                if (Request.ContentLength == 0 || Request.ContentLength == null)
                {
                    _logger.LogWarning("❌ Empty request body");
                    return Unauthorized();
                }

                // Read the raw body
                Request.EnableBuffering(); // Allow rewinding the stream
                using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = reader.ReadToEndAsync().Result;
                Request.Body.Position = 0; // Reset stream position for future reads

                _logger.LogInformation($"🔐 Raw Body: {body}");

                // Parse the JSON body
                var authRequest = System.Text.Json.JsonSerializer.Deserialize<MediaMTXAuthRequest>(body);
                if (authRequest == null)
                {
                    _logger.LogWarning("❌ Failed to parse JSON body");
                    return Unauthorized();
                }

                // Extract token from query string
                string token = "";
                if (!string.IsNullOrEmpty(authRequest.Query))
                {
                    _logger.LogInformation($"🔐 Query String from MediaMTX: {authRequest.Query}");

                    var queryParams = System.Web.HttpUtility.ParseQueryString(authRequest.Query);
                    token = queryParams["token"] ?? "";

                    _logger.LogInformation($"🔐 Extracted Token: {token}");
                }
                else
                {
                    _logger.LogWarning("❌ Query string is empty or null");
                }

                _logger.LogInformation($"🔐 Auth Parameters:");
                _logger.LogInformation($"🔐   Path: '{authRequest.Path}'");
                _logger.LogInformation($"🔐   IP: '{authRequest.Ip}'");
                _logger.LogInformation($"🔐   Action: '{authRequest.Action}'");
                _logger.LogInformation($"🔐   Protocol: '{authRequest.Protocol}'");
                _logger.LogInformation($"🔐   Token: '{token}'");

                // Validate required parameters
                if (string.IsNullOrEmpty(authRequest.Path))
                {
                    _logger.LogWarning("❌ Missing path parameter");
                    return Unauthorized();
                }

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("❌ Missing token parameter");
                    return Unauthorized();
                }

                if (string.IsNullOrEmpty(authRequest.Ip))
                {
                    _logger.LogWarning("❌ Missing IP parameter");
                    return Unauthorized();
                }

                _logger.LogInformation($"🔐 Calling token validation service...");

                if (_streamTokenService.ValidateToken(token, authRequest.Path, authRequest.Ip))
                {
                    _logger.LogInformation("✅ Authentication SUCCESS");
                    return Ok();
                }

                _logger.LogWarning("❌ Token validation FAILED");
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in stream validation");
                return Unauthorized();
            }
        }*/
        [HttpPost("validate-token")]
        [AllowAnonymous]
        public IActionResult ValidateStreamToken()
        {
            try
            {
                _logger.LogInformation($"🔐 ===== NEW AUTH REQUEST =====");

                // Read the raw body
                Request.EnableBuffering();
                using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = reader.ReadToEndAsync().Result;
                Request.Body.Position = 0;

                _logger.LogInformation($"🔐 Raw Body: {body}");
                _logger.LogInformation($"🔐 Body Type: {body.GetType()}");
                _logger.LogInformation($"🔐 Body Length: {body.Length}");

                MediaMTXAuthRequest authRequest = null;

                try
                {
                    // Try direct deserialization with proper options
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };

                    authRequest = JsonSerializer.Deserialize<MediaMTXAuthRequest>(body, options);
                    if (authRequest.Protocol == "hls") return Ok();
                    if (authRequest != null)
                    {
                        _logger.LogInformation("✅ Direct deserialization successful");
                    }
                    else
                    {
                        _logger.LogWarning("❌ Deserialization returned null");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"❌ JSON Deserialization failed: {ex.Message}");
                    _logger.LogError($"❌ JSON Path: {ex.Path}, Line: {ex.LineNumber}, Position: {ex.BytePositionInLine}");

                    // Let's see what the actual JSON looks like by trying to parse it manually
                    DebugJsonStructure(body);
                    return Unauthorized();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ General deserialization error: {ex.Message}");
                    return Unauthorized();
                }

                if (authRequest == null)
                {
                    _logger.LogWarning("❌ Auth request is null after deserialization");
                    return Unauthorized();
                }

                // Log the successfully parsed values
                _logger.LogInformation($"🔐 Successfully parsed AuthRequest:");
                _logger.LogInformation($"🔐   IP: '{authRequest.Ip}'");
                _logger.LogInformation($"🔐   Path: '{authRequest.Path}'");
                _logger.LogInformation($"🔐   Action: '{authRequest.Action}'");
                _logger.LogInformation($"🔐   Protocol: '{authRequest.Protocol}'");
                _logger.LogInformation($"🔐   Query: '{authRequest.Query}'");
                _logger.LogInformation($"🔐   Id: '{authRequest.Id}'");

                // Continue with token extraction and validation...
                return ValidateAndProcessAuthRequest(authRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in ValidateStreamToken");
                return Unauthorized();
            }
        }

        private void DebugJsonStructure(string json)
        {
            try
            {
                _logger.LogInformation($"🔍 JSON Debug Analysis:");
                _logger.LogInformation($"🔍 First 50 chars: {json.Substring(0, Math.Min(50, json.Length))}");
                _logger.LogInformation($"🔍 Last 50 chars: {json.Substring(Math.Max(0, json.Length - 50))}");
                _logger.LogInformation($"🔍 Contains 'ip': {json.Contains("\"ip\"")}");
                _logger.LogInformation($"🔍 Contains 'path': {json.Contains("\"path\"")}");
                _logger.LogInformation($"🔍 Contains 'query': {json.Contains("\"query\"")}");

                // Try to parse as JsonDocument to see the structure
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    _logger.LogInformation($"🔍 JSON Document Root ValueKind: {doc.RootElement.ValueKind}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"🔍 JSON Debug failed: {ex.Message}");
            }
        }

        private IActionResult ValidateAndProcessAuthRequest(MediaMTXAuthRequest authRequest)
        {
            // Extract token from query string
            string token = "";
            if (!string.IsNullOrEmpty(authRequest.Query))
            {
                _logger.LogInformation($"🔐 Query String: {authRequest.Query}");

                try
                {
                    var queryParams = System.Web.HttpUtility.ParseQueryString(authRequest.Query);
                    token = queryParams["token"] ?? "";
                    _logger.LogInformation($"🔐 Extracted Token: {token}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"🔐 Failed to parse query string: {ex.Message}");
                    return Unauthorized();
                }
            }

            // Basic validation
            if (string.IsNullOrEmpty(authRequest.Path) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(authRequest.Ip))
            {
                _logger.LogWarning("❌ Missing required parameters");
                return Unauthorized();
            }

            // Call validation service
            _logger.LogInformation($"🔐 Calling StreamTokenService.ValidateToken...");
            bool isValid = _streamTokenService.ValidateToken(token, authRequest.Path, authRequest.Ip);

            if (isValid)
            {
                _logger.LogInformation("✅ Authentication SUCCESS - Returning 200 OK");
                return Ok();
            }
            else
            {
                _logger.LogWarning("❌ Authentication FAILED - Returning 401 Unauthorized");
                return Unauthorized();
            }
        }


    }
}
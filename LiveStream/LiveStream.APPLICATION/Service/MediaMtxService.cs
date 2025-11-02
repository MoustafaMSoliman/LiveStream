using LiveStream.APPLICATION.DTOs;
using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN;
using LiveStream.DOMAIN.MediaMTX;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LiveStream.APPLICATION.Service
{
    public class MediaMtxService : IMediaMtxService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MediaMtxService> _logger;
        private readonly StreamTokenService _streamTokenService;

        public MediaMtxService(HttpClient httpClient, IConfiguration configuration, ILogger<MediaMtxService> logger, StreamTokenService streamTokenService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _streamTokenService = streamTokenService;


            var mediaMtxUrl = _configuration["MediaMtx:BaseUrl"] ?? "http://localhost:9997";
            _httpClient.BaseAddress = new Uri(mediaMtxUrl);
        }

        public async Task<bool> AddCameraAsync(string cameraName, MediaMtxPath pathConfig)
        {
            try
            {
                var json = JsonSerializer.Serialize(pathConfig, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                
                var response = await _httpClient.PostAsync($"/v3/config/paths/add/{cameraName}", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Camera {CameraName} added successfully", cameraName);
                    StartGStreamerFrameExtractor(cameraName);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to add camera {CameraName}. Status: {StatusCode}, Error: {Error}",
                        cameraName, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding camera {CameraName}", cameraName);
                return false;
            }
        }
        /*
        private void StartGStreamerFrameExtractor(string cameraName)
        {
            try
            {
                // The MediaMTX stream URL for playback (RTSP/HLS)
                string streamUrl = $"rtsp://localhost:8554/{cameraName}";

                // Save frames as /app/frames/camName_frame001.jpg
                string outputPath = $"/app/frames/{cameraName}_frame_%03d.jpg";

                // Capture 1 frame per second
                string command = $"gst-launch-1.0 rtspsrc location={streamUrl} latency=0 ! " +
                                 "decodebin ! videorate ! video/x-raw,framerate=1/1 ! " +
                                 "jpegenc ! multifilesink location=" + outputPath;

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                _logger.LogInformation("Started GStreamer for {CameraName}, outputting to {Path}", cameraName, outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start GStreamer for {CameraName}", cameraName);
            }
        }
        */
        private async Task StartGStreamerFrameExtractor(string cameraName)
        {
            try
            {
                
                var tokenRequest = new { CameraId = cameraName };
                var response = await _httpClient.PostAsJsonAsync("http://localhost:5046/api/stream/token", tokenRequest);

                response.EnsureSuccessStatusCode();

                var tokenResponse = await response.Content.ReadFromJsonAsync<StreamInfo>();
                string token = tokenResponse.AccessToken;



                
                // ✅ Container name (must match your running MediaMTX container)
                string containerName = "smediamtx";

                // ✅ Stream URL (MediaMTX RTSP)
                //string streamUrl = $"rtsp://localhost:8554/{cameraName}";
                string streamUrl = $"rtsp://localhost:8554/{cameraName}?token={token}";
            
                // ✅ Output path inside container
                string outputPath = $"/app/frames/{cameraName}_frame_%03d.jpg";

                // ✅ GStreamer pipeline
                string gstCommand =
                    $"gst-launch-1.0 rtspsrc location={streamUrl} latency=0 ! " +
                    "decodebin ! videorate ! video/x-raw,framerate=1/1 ! " +
                    $"jpegenc ! multifilesink location={outputPath}";

                // ✅ Docker exec command to run inside container
                string dockerCommand = $"docker exec {containerName} sh -c \"{gstCommand}\"";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C {dockerCommand}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.LogInformation("[GStreamer Output] {Output}", e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.LogWarning("[GStreamer Error] {Error}", e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _logger.LogInformation("🚀 Started GStreamer inside container '{Container}' for {CameraName}", containerName, cameraName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to start GStreamer for {CameraName}", cameraName);
            }
        }

        public async Task<bool> RemoveCameraAsync(string cameraName)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/v3/config/paths/remove/{cameraName}", null);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Camera {CameraName} removed successfully", cameraName);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to remove camera {CameraName}. Status: {StatusCode}",
                        cameraName, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing camera {CameraName}", cameraName);
                return false;
            }
        }
        /*
        public async Task<MediaMtxPathListResponse> GetAllCamerasAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/paths/list");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MediaMtxPathListResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return result ?? new MediaMtxPathListResponse();
                }
                else
                {
                    _logger.LogError("Failed to get cameras list. Status: {StatusCode}", response.StatusCode);
                    return new MediaMtxPathListResponse();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cameras list");
                return new MediaMtxPathListResponse();
            }
        }
        */
        public async Task<MediaMtxPathListResponse> GetAllCamerasAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/v3/paths/list");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    
                    var dynamicResponse = JsonSerializer.Deserialize<dynamic>(content);

                    var result = new MediaMtxPathListResponse
                    {
                        ItemCount = dynamicResponse?.GetProperty("itemCount").GetInt32() ?? 0,
                        PageCount = dynamicResponse?.GetProperty("pageCount").GetInt32() ?? 0,
                        Items = new List<MediaMtxPathItem>()
                    };

                    if (dynamicResponse?.GetProperty("items").ValueKind == JsonValueKind.Array)
                    {
                        var itemsArray = dynamicResponse.GetProperty("items").EnumerateArray();

                        foreach (var item in itemsArray)
                        {
                            var cameraItem = new MediaMtxPathItem
                            {
                                // Replace this line:
                                //Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,

                                // With this line:
                                Name = item.TryGetProperty("name", out JsonElement name) ? name.GetString() ?? string.Empty : string.Empty,
                                Source = ExtractStringValue(item, "source"),
                                State = item.TryGetProperty("state", out JsonElement state) ? state.GetString() ?? string.Empty : string.Empty,
                                Readers = item.TryGetProperty("readers", out JsonElement readers) ? readers.GetInt32() : 0,
                                BytesReceived = ExtractStringValue(item, "bytesReceived"),
                                ReadyDuration = ExtractStringValue(item, "readyDuration")
                            };

                            
                            DateTime createdDate = default;
                            if (item.TryGetProperty("created", out JsonElement created) &&
                                created.ValueKind == JsonValueKind.String &&
                                DateTime.TryParse(created.GetString(), out createdDate))
                            {
                                cameraItem.Created = createdDate;
                            }

                            result.Items.Add(cameraItem);
                        }
                    }

                    return result;
                }
                else
                {
                    _logger.LogError("Failed to get cameras list. Status: {StatusCode}", response.StatusCode);
                    return new MediaMtxPathListResponse();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cameras list");
                return new MediaMtxPathListResponse();
            }
        }

        private string ExtractStringValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.TryGetInt64(out long longVal) ? longVal.ToString() :
                                           property.TryGetDouble(out double doubleVal) ? doubleVal.ToString() :
                                           "0",
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Object => property.GetRawText(),
                    JsonValueKind.Array => property.GetRawText(),
                    _ => string.Empty
                };
            }
            return string.Empty;
        }
        public async Task<MediaMtxPathItem?> GetCameraAsync(string cameraName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v3/paths/get/{cameraName}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MediaMtxPathItem>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return result;
                }
                else
                {
                    _logger.LogWarning("Camera {CameraName} not found. Status: {StatusCode}",
                        cameraName, response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting camera {CameraName}", cameraName);
                return null;
            }
        }
    }
}

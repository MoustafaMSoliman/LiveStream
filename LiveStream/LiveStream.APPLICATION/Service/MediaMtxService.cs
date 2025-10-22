using LiveStream.APPLICATION.DTOs;
using LiveStream.APPLICATION.Interfaces;
using LiveStream.DOMAIN.MediaMTX;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public MediaMtxService(HttpClient httpClient, IConfiguration configuration, ILogger<MediaMtxService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            
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

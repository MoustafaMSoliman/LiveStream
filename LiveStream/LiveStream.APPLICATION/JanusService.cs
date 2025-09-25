using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace LiveStream.APPLICATION;

public class JanusService : IJanusService
{
    private readonly HttpClient _http;
    private static string TxId() => Guid.NewGuid().ToString("N");

    public JanusService(IHttpClientFactory httpFactory)
    {
        _http = httpFactory.CreateClient("janus");
    }

    public async Task<int?> CreateRtspMountpointAsync(CreateCameraDto dto)
    {
        // Temporary session & handle to call streaming plugin create
        var sessionId = await CreateSessionAsync();
        if (sessionId == null) return null;
        try
        {
            var handleId = await AttachPluginAsync(sessionId.Value, "janus.plugin.streaming");
            if (handleId is null) return null;

            // build create message
            var body = new
            {
                janus = "message",
                transaction = TxId(),
                body = new
                {
                    request = "create",
                    type = "rtsp",
                    id = dto.Id ?? 0, // if 0 Janus will pick random id; we prefer passing 0 to let Janus autoassign if no Id provided
                    description = dto.Name ?? "camera",
                    url = dto.RtspUrl,
                    audio = false,
                    video = true,
                    secret = "" // optional
                }
            };

            var resp = await _http.PostAsJsonAsync($"{sessionId}/{handleId}", body);
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            // plugindata.data might have the id assigned
            if (doc.RootElement.TryGetProperty("plugindata", out var pd)
                && pd.TryGetProperty("data", out var data)
                && data.TryGetProperty("id", out var idElem) && idElem.ValueKind == JsonValueKind.Number)
            {
                return idElem.GetInt32();
            }

            // fallback: if our request had id and Janus accepted it
            if (dto.Id.HasValue) return dto.Id.Value;
            return null;
        }
        finally
        {
            // destroy session used for admin action
            await DestroySessionAsync(sessionId.Value);
        }
    }

    // Create Session -> return session id
    public async Task<long?> CreateSessionAsync()
    {
        var payload = new { janus = "create", transaction = TxId() };
        var resp = await _http.PostAsJsonAsync("", payload);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var id))
            return id.GetInt64();
        return null;
    }
    // Attach "janus.plugin.streaming" 
    public async Task<long?> AttachPluginAsync(long sessionId, string plugin)
    {
        var payload = new { janus = "attach", plugin = plugin, transaction = TxId() };
        var resp = await _http.PostAsJsonAsync($"{sessionId}", payload);
        if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var id))
            return id.GetInt64();
        return null;
    }

    public async Task DestroySessionAsync(long sessionId)
    {
        try
        {
            var payload = new { janus = "destroy", transaction = TxId() };
            await _http.PostAsJsonAsync($"{sessionId}", payload);
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
    }


    // Attach to Streaming Plugin
    public async Task<long> AttachToStreamingAsync(long sessionId)
    {
        var response = await _http.PostAsJsonAsync($"http://localhost:8088/janus/{sessionId}", new
        {
            janus = "attach",
            plugin = "janus.plugin.streaming",
            transaction = Guid.NewGuid().ToString()
        });

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("data").GetProperty("id").GetInt64();
    }

    public async Task<bool> CreateCameraAsync(long sessionId, long handleId, int cameraId, string description, int port)
    {
        var body = new
        {
            janus = "message",
            body = new
            {
                request = "create",
                id = cameraId,
                type = "rtp",
                description = description,
                audio = false,
                video = true,
                videoport = port,
                videopt = 96,
                videortpmap = "H264/90000"
            },
            transaction = Guid.NewGuid().ToString()
        };

        var response = await _http.PostAsJsonAsync(
            $"http://localhost:8088/janus/{sessionId}/{handleId}", body);

        return response.IsSuccessStatusCode;
    }

   
}


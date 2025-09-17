using LightsAPICommon;
using LightsAPICommon.Serialization;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lights.McpServer;

/// <summary>
/// All the functions that the MCP server can call on this tool are defined here.
/// </summary>

[McpServerToolType]
public static class LightsTool
{
    static LightsApiClient? _client;

    [McpServerTool, Description("Returns a list of all available lights including their states, capabilities, and RoomId that references the room where it resides Name and Floor")]
    public static List<Light> GetAllLights()
    {
        if (_client == null)
        {
            _client = new LightsApiClient();
        }

        return _client.GetLightsAsync().Result;
    }

    [McpServerTool, Description("Returns all rooms, including their names and floor numbers. This data can be cached for efficiency")]
    public static List<Room> GetAllRooms()
    {
        if (_client == null)
        {
            _client = new LightsApiClient();
        }

        return _client.GetRoomsAsync().Result;
    }

    [McpServerTool, Description("Retrieve a room’s name, and floor, from its unique ID")]
    public static Room GetRoom([Description("Unique identifier for the room")] int roomId)
    {
        if (_client == null)
        {
            _client = new LightsApiClient();
        }

        return _client.GetRoomAsync(roomId).Result!;
    }

    [McpServerTool, Description("Returns details of a specific light, from its unique ID")]
    public static Light GetLight(int lightId)
    {
        if (_client == null)
        {
            _client = new LightsApiClient();
        }

        return _client.GetLightAsync(lightId).Result!;
    }

    [McpServerTool, Description("Fetches all lights on a specified floor, detailing their IDs: LightId, current states and capabilities")]
    public static List<Light> GetLightsOnFloor(int floor)
    {
        if (_client == null)
        {
            _client = new LightsApiClient();
        }

        return _client.GetLightsByFloorAsync(floor).Result;
    }


    [McpServerTool, Description("Change one or more properties of a light at once in a single call (state, or color, or brightness)")]
    public static List<UpdateLightResponse> Change(int lightId, 
        [Description("[optional] New State of the light 'On' or 'Off' (null when not used) ")] string? onOrOff,
        [Description("[optional] New brightness or intensity from 0 to 100 (null when not used)")] int? brightness,
        [Description("[optional] New Color (must be a valid hex code RRGGB) (null when not used)")] string? color)
    {
        if (_client == null)
        {
            _client = new LightsApiClient();
        }
        List<LightUpdateRequest> lightUpdates = new List<LightUpdateRequest>();

        if (lightId < 0) throw new ArgumentException("LightId must be non-negative", nameof(lightId));
        lightUpdates.Add(new LightUpdateRequest { LightId = lightId, State = string.IsNullOrEmpty(onOrOff)?null: onOrOff,Brightness= brightness, Color= string.IsNullOrEmpty(color) ? null : color });
        return [.. _client.UpdateLightsAsync(lightUpdates).Result.Results];
    }

    [McpServerTool, Description("Turn a light 'On' or 'Off' ")]
    public static List<UpdateLightResponse> TurnLightOnOrOff(int lightId, [Description("New State of the light 'On' or 'Off'")] string onOrOff)
    {
        if (_client == null)
        {
            _client = new LightsApiClient();
        }
        List<LightUpdateRequest> lightUpdates = new List<LightUpdateRequest>();

        if (lightId < 0) throw new ArgumentException("LightId must be non-negative", nameof(lightId));
        lightUpdates.Add(new LightUpdateRequest { LightId = lightId, State = onOrOff });
        return [.. _client.UpdateLightsAsync(lightUpdates).Result.Results];
    }

    [McpServerTool, Description("Change a Light Brightness / Intensity")]
    public static List<UpdateLightResponse> ChangeLightBrightness(int lightId, [Description("New brightness or intensity from 0 to 100")] int brightness)
    {
        if (_client == null)
        {
            _client = new LightsApiClient();
        }
        List<LightUpdateRequest> lightUpdates = new List<LightUpdateRequest>();

        if (lightId < 0) throw new ArgumentException("LightId must be non-negative", nameof(lightId));
        lightUpdates.Add(new LightUpdateRequest { LightId = lightId, Brightness = brightness });
        return [.. _client.UpdateLightsAsync(lightUpdates).Result.Results];
    }

    [McpServerTool, Description("Change a Light Color in format 'RRGGBB' ")]
    public static List<UpdateLightResponse> ChangeLightColor(int lightId, [Description("New Color (must be a valid hex code RRGGB)")] string newColor)
    {
        if (_client == null)
        {
            _client = new LightsApiClient();
        }
        List<LightUpdateRequest> lightUpdates = new List<LightUpdateRequest>();

        if (lightId < 0) throw new ArgumentException("LightId must be non-negative", nameof(lightId));
        lightUpdates.Add(new LightUpdateRequest { LightId = lightId, Color = newColor });
        return [.. _client.UpdateLightsAsync(lightUpdates).Result.Results];
    }


}

public sealed class LightsApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    public Uri BaseAddress => _http.BaseAddress!;

    public LightsApiClient(
        string baseAddress = "https://localhost:5042",
        HttpMessageHandler? handler = null)
    {
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _http.BaseAddress = new Uri(baseAddress);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MCP SERVER REST CLIENT");

        _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = LightsJsonContext.Default

        };
    }

    // -------- Rooms --------
    public async Task<List<Room>> GetRoomsAsync(CancellationToken ct = default)
        => await GetAsync<List<Room>>("/rooms/", ct) ?? new List<Room>(); // GET /rooms/ 

    public async Task<Room?> GetRoomAsync(int roomId, CancellationToken ct = default)
        => await GetOrNullAsync<Room>($"/rooms/{roomId}", ct); // GET /rooms/{roomId} 

    // -------- Lights --------
    public async Task<List<Light>> GetLightsAsync(CancellationToken ct = default)
        => await GetAsync<List<Light>>("/lights/", ct) ?? new List<Light>(); // GET /lights/

    public async Task<Light?> GetLightAsync(int id, CancellationToken ct = default)
        => await GetOrNullAsync<Light>($"/lights/{id}", ct); // GET /lights/{id} 

    public async Task<List<Light>> GetLightsByFloorAsync(int floor, CancellationToken ct = default)
        => await GetAsync<List<Light>>($"/lights/floor/{floor}", ct) ?? new List<Light>(); // GET /lights/floor/{floor} 

    // -------- Batch Patch --------
    public async Task<PatchResponse> UpdateLightsAsync(List<LightUpdateRequest> updates, CancellationToken ct = default)
    {
        if (updates is null) throw new ArgumentNullException(nameof(updates));
        var body = new PatchRequest { LightUpdates = updates }; // LightUpdateRequest[] 

        using var req = new HttpRequestMessage(HttpMethod.Patch, "/lights/")
        {
            Content = JsonContent.Create(body, options: _json)
        };

        using var res = await _http.SendAsync(req, ct);

        // Server returns 200 OK or 207 Multi-Status depending on partials 
        if (res.StatusCode is HttpStatusCode.OK or (HttpStatusCode)207)
        {
            var parsed = await res.Content.ReadFromJsonAsync<PatchResponse>(_json, ct);
            return parsed ?? new PatchResponse(Array.Empty<UpdateLightResponse>());
        }

        // 400, 404, etc.
        var detail = await res.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"PATCH /lights/ failed: {(int)res.StatusCode} {res.ReasonPhrase}. {detail}");
    }

    // ---- internals ----
    [RequiresUnreferencedCode("Calls System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(JsonSerializerOptions, CancellationToken)")]
    [RequiresDynamicCode("Calls System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(JsonSerializerOptions, CancellationToken)")]
    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var res = await _http.GetAsync(path, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<T>(_json, ct);
    }

    [RequiresUnreferencedCode("Calls System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(JsonSerializerOptions, CancellationToken)")]
    [RequiresDynamicCode("Calls System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<T>(JsonSerializerOptions, CancellationToken)")]
    private async Task<T?> GetOrNullAsync<T>(string path, CancellationToken ct)
    {
        using var res = await _http.GetAsync(path, ct);
        if (res.StatusCode == HttpStatusCode.NotFound) return default;
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<T>(_json, ct);
    }

    public void Dispose() => _http.Dispose();
}
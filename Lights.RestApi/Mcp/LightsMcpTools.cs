using Lights.Common;
using Lights.Common.Serialization;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lights.RestApi.Mcp;

[McpServerToolType]
public static partial class LightsMcpTools
{
    [GeneratedRegex("^[0-9A-Fa-f]{6}$")]
    private static partial Regex ColorRegex();

    private static Light[] AllLights =>  House.Instance.Lights;
    private static Room[] AllRooms =>  House.Instance.Rooms;

    [McpServerTool, Description("Returns a list of all available lights including their states, capabilities, and RoomId that references the room where it resides Name and Floor")]
    public static List<Light> GetAllLights()
    {
        Debug.WriteLine(">> MCP GetAllLights");
        return AllLights.ToList();
    }

    [McpServerTool, Description("Returns all rooms, including their names and floor numbers. This data can be cached for efficiency")]
    public static List<Room> GetAllRooms()
    {
        Debug.WriteLine(">> MCP GetAllRooms");
        return AllRooms.ToList();
    }

    [McpServerTool, Description("Retrieve a room’s name, floor, and unique ID")]
    public static Room? GetRoom([Description("Unique identifier for the room")] int roomId)
        => AllRooms.FirstOrDefault(r => r.RoomId == roomId);

    [McpServerTool, Description("Returns details of a specific light by its ID")]
    public static Light? GetLight([Description("Unique identifier for the light")] int lightId)
        => AllLights.FirstOrDefault(l => l.LightId == lightId);

    [McpServerTool, Description("Fetches all lights on a specified floor, detailing their IDs, current states and capabilities")]
    public static List<Light> GetLightsOnFloor([Description("Floor number")] int floor)
    {
        var roomsOnFloor = AllRooms.Where(r => r.Floor == floor).Select(r => r.RoomId).ToHashSet();
        return AllLights.Where(l => roomsOnFloor.Contains(l.RoomId)).ToList();
    }

    [McpServerTool, Description("Batch update multiple lights with new states, colors, or brightness")]
    public static PatchResponse UpdateLights([Description("Batch of light updates")] PatchRequest request)
    {
        if (request?.LightUpdates == null || request.LightUpdates.Count == 0)
            return new PatchResponse(Array.Empty<UpdateLightResponse>());

        var results = new List<UpdateLightResponse>();

        foreach (var lightUpdate in request.LightUpdates)
        {
            var light = AllLights.FirstOrDefault(l => l.LightId == lightUpdate.LightId);
            if (light == null)
            {
                results.Add(new UpdateLightResponse(lightUpdate.LightId, "failed", "Light not found"));
                continue;
            }

            var errors = new List<string>();
            var partial = false;

            if (lightUpdate.Brightness.HasValue)
            {
                if (!light.Capabilities.IsDimmable)
                {
                    partial = true;
                    errors.Add("This light does not support Brightness changes (IsDimmable = false)");
                }
                else if (lightUpdate.Brightness.Value is < 0 or > 100)
                {
                    partial = true;
                    errors.Add("Brightness value must be between 0 and 100");
                }
                else light.Brightness = lightUpdate.Brightness.Value;
            }

            if (!string.IsNullOrEmpty(lightUpdate.Color))
            {
                if (!light.Capabilities.CanChangeColor)
                {
                    partial = true;
                    errors.Add("Color cannot be changed");
                }
                else if (!ColorRegex().IsMatch(lightUpdate.Color))
                {
                    partial = true;
                    errors.Add($"Request Color invalid format:({lightUpdate.Color}). Must be in the format 'RRGGBB'");
                }
                else light.Color = lightUpdate.Color;
            }

            if (!string.IsNullOrEmpty(lightUpdate.State))
            {
                if (!Enum.TryParse<LightState>(lightUpdate.State, true, out var newState))
                {
                    partial = true;
                    errors.Add($"{lightUpdate.State} is an invalid State. Use 'On' or 'Off'");
                }
                else light.State = newState;
            }

            results.Add(new UpdateLightResponse(
                lightUpdate.LightId,
                partial ? "partial" : "success",
                partial ? string.Join("; ", errors) : null));
        }

        return new PatchResponse(results.ToArray());
    }
}


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

    [McpServerTool, Description("Apply the same state, brightness, and/or color change to multiple lights")]
    public static PatchResponse UpdateLights(
        [Description("IDs of the lights to update")] int[] lightIds,
        [Description("Optional state: On or Off")] string? state = null,
        [Description("Optional brightness from 0 to 100")] int? brightness = null,
        [Description("Optional six-digit RRGGBB color")] string? color = null)
    {
        if (lightIds == null || lightIds.Length == 0)
            return new PatchResponse(Array.Empty<UpdateLightResponse>());

        var results = new List<UpdateLightResponse>();

        foreach (var lightId in lightIds.Distinct())
        {
            var light = AllLights.FirstOrDefault(l => l.LightId == lightId);
            if (light == null)
            {
                results.Add(new UpdateLightResponse(lightId, "failed", "Light not found"));
                continue;
            }

            var errors = new List<string>();
            var partial = false;

            if (brightness.HasValue)
            {
                if (!light.Capabilities.IsDimmable)
                {
                    partial = true;
                    errors.Add("This light does not support Brightness changes (IsDimmable = false)");
                }
                else if (brightness.Value is < 0 or > 100)
                {
                    partial = true;
                    errors.Add("Brightness value must be between 0 and 100");
                }
                else light.Brightness = brightness.Value;
            }

            if (!string.IsNullOrEmpty(color))
            {
                if (!light.Capabilities.CanChangeColor)
                {
                    partial = true;
                    errors.Add("Color cannot be changed");
                }
                else if (!ColorRegex().IsMatch(color))
                {
                    partial = true;
                    errors.Add($"Request Color invalid format:({color}). Must be in the format 'RRGGBB'");
                }
                else light.Color = color;
            }

            if (!string.IsNullOrEmpty(state))
            {
                if (!Enum.TryParse<LightState>(state, true, out var newState))
                {
                    partial = true;
                    errors.Add($"{state} is an invalid State. Use 'On' or 'Off'");
                }
                else light.State = newState;
            }

            results.Add(new UpdateLightResponse(
                lightId,
                partial ? "partial" : "success",
                partial ? string.Join("; ", errors) : null));
        }

        return new PatchResponse(results.ToArray());
    }
}


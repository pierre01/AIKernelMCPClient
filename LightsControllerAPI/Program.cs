using LightsAPICommon;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

internal partial class Program
{
    [GeneratedRegex("^[0-9A-Fa-f]{6}$")]
    private static partial Regex ColorRegex();

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        // Configure Kestrel to use HTTPS
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(5042, listenOptions =>
            {
                listenOptions.UseHttps(); // HTTPS
            });
        });

        builder.Services.AddOpenApi(); // documentName =v1

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        var app = builder.Build();
        app.MapOpenApi("/openapi/{documentName}/openapi.json");

        var allLights = House.Lights;
        var allRooms = House.Rooms;

        var roomsApi = app.MapGroup("/rooms");
        roomsApi.WithTags("Rooms");

        // Get all the rooms
        roomsApi.MapGet("/", () =>
        {
            Debug.WriteLine(">> Get all rooms -> GetRooms");
            return allRooms;
        })
          .WithName("GetRooms")
          .WithSummary("Retrieve all rooms with their floor information")
          .WithDescription("Returns a list of all Rooms in the system along with their name and Floor number. You should store this information as it remains unchanged during the session to optimize future queries.");

        // Get a specific room
        roomsApi.MapGet("/{roomId}", ([Description("Unique identifier for the room")] int roomId) =>
            allRooms.FirstOrDefault(a => a.RoomId == roomId) is { } room
                ? Results.Ok(room)
                : Results.NotFound())
          .WithName("GetRoom")
          .WithSummary("Retreive the room Name, the floor where it's located, and unique RoomId (used by the lights property RoomId)")
          .WithDescription("A room is representing a group of lights where the 'RoomId' is the same. Each room has a unique Name and Floor number associated with the lights with the same RoomId.  if a command is adressing the room without specifying the light name then all the lights located in the room should be receiving the command. Remember all the rooms for future use");


        var lightsApi = app.MapGroup("/lights");
        lightsApi.WithTags(["Rooms", "Lights"]);

        // Get all the lights
        lightsApi.MapGet("/", (HttpRequest request) =>
        {
            // Check user agent to remove the soft client calls and only show the Kernel calls
            if (!request.Headers.UserAgent.ToString().Contains("API SoftClient"))
            {
                Debug.WriteLine(">> Get all lights -> GetLights");
            }
            return Results.Ok(allLights);

        }).Produces<List<Light>>(200)
            .WithName("GetLights")
            .WithSummary("Retrieve all lights with room and floor information")
            .WithDescription("Returns a list of all available lights including their states, capabilities, and the room. You should member the Room Name and Floor number associated to the lamp to avoid redundant queries.");

        // Get a specific light

        app.MapGet("/lights/{id}", (int id) =>
        {
            var light = allLights.FirstOrDefault(l => l.Id == id);
            return light != null ? Results.Ok(light) : Results.NotFound($"Light {id} not found.");
        })
        .Produces<Light>(200)
        .Produces(404)
        .WithName("GetLight")
        .WithSummary("Retrieve a single light")
        .WithDescription("Returns details of a specific light by its ID. The room and floor remain unchanged");//.ExcludeFromDescription();

        /// <summary> Batch update multiple lights. </summary>
        /// <remarks>
        /// Example request:
        /// { "LightIds": [1,2], "State": "On", "Color": "FF0000" }
        /// </remarks>
        app.MapPatch("/lights", ([FromBody] BatchLightUpdateRequest request) =>
        {
            /* This batch update request applies only to lights that match the requested capabilities.
               - If brightness is specified, only dimmable lights (IsDimmable = true) will be updated.
               - If color is specified, only lights that support color change (CanChangeColor = true) will be updated.
               - If state change is requested, all selected lights will be updated regardless of capabilities. */

            if (request.LightIds == null || request.LightIds.Count == 0)
            {
                return Results.BadRequest("No lights specified.");
            }


            bool hasColor=false;
            bool hasBrightness = false;
            bool hasState = false;
            LightState newState = LightState.Off;

            if (!string.IsNullOrEmpty(request.Color))
            {
                if (!ColorRegex().IsMatch(request.Color))
                {
                    return Results.BadRequest($"Request Color invalid format. Must be in the format 'RRGGBB'.");
                }
                hasColor = true; // it has color and it's valid
            }

            if (request.Brightness.HasValue)
            {
                if (request.Brightness.Value < 0 || request.Brightness.Value > 100)
                {
                    return Results.BadRequest("Request Brightness value must be between 0 and 100");
                }
                hasBrightness = true; // it has brightness and it's valid
            }

            if (!string.IsNullOrEmpty(request.State))
            {
                if (!Enum.TryParse<LightState>(request.State, true, out newState))
                    return Results.BadRequest("Request Invalid State. Use 'On' or 'Off'");
                hasState = true; // it has state and it's valid
            }
            var idsNotFound = new List<int>();
            var filteredLights = new List<Light>();
            foreach (var lightId in request.LightIds)
            {
                var found = allLights.FirstOrDefault(l => l.Id == lightId);
                if (found == null)
                {
                    idsNotFound.Add(lightId);
                    continue;
                }
                filteredLights.Add(found);

                if (hasState) found.State = newState;
                
                if(hasBrightness && found.Capabilities.IsDimmable)
                {
                    found.Brightness = request.Brightness.Value;
                }

                if (hasColor && found.Capabilities.CanChangeColor)
                {
                   found.Color = request.Color; 
                }
            }

            if(filteredLights.Count != request.LightIds.Count)
            {
                return  Results.Json(filteredLights, statusCode: StatusCodes.Status207MultiStatus);
            }

            return Results.Ok(filteredLights);
        })
        .Produces<List<Light>>(StatusCodes.Status200OK)
        .Produces<List<Light>>(StatusCodes.Status207MultiStatus)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("UpdateLights")
        .WithSummary("Batch update multiple lights")
        .WithDescription("Updates multiple lights with new State (On or Off), Color, or Brightness. Returns the list of lights affected by the operation with their new values");


        app.Run();
    }


}


[JsonSerializable(typeof(List<Light>))]
[JsonSerializable(typeof(Light[]))]
[JsonSerializable(typeof(Room[]))]
[JsonSerializable(typeof(BatchLightUpdateRequest))]
[JsonSerializable(typeof(Light))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}


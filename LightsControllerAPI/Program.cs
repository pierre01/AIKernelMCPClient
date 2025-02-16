using LightsAPICommon;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

internal partial class Program
{
    [GeneratedRegex("^(?:[A-Fa-f0-9]{3}|[A-Fa-f0-9]{6})$")]
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
          .WithSummary("Retreive all the rooms Names, floor, and unique IDs in the house, used by the lights property RoomId")
          .WithDescription("Get every room in the house and the floor they are located. Each light is associated to one room only and the floor of the room, remember all the rooms in the house and the lights for each room");

        // Get a specific room
        roomsApi.MapGet("/{roomId}", ([Description("Unique identifier for the room")] int roomId) =>
            allRooms.FirstOrDefault(a => a.Id == roomId) is { } room
                ? Results.Ok(room)
                : Results.NotFound())
          .WithName("GetRoom")
          .WithSummary("Retreive the room Name, the floor where it's located, and unique RoomId (used by the lights property RoomId)")
          .WithDescription("A room is representing a group of lights where the 'RoomId' is the same. All the light located in a room are also onlocated on the same floor as the room.  if a command is adressing the room without specifying the light name then all the lights in the room and floor should be receiving the command. Remember all the rooms for future use");


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
            .WithSummary("Retrieve all the lights in the house")
            .WithDescription("Returns a list of all available lights in the house with their states and capabilities. Remember all the lights for future use as well are the rooms and floor they are located in");

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
        .WithDescription("Returns details of a specific light by its ID.");//.ExcludeFromDescription();

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

            // Filter lights based on (if (hasBrightness == true && == l.Capabilities.IsDimmable) and hasColor light  capability and hasColor and requested light IDs
            var filteredLights = new List<Light>();
            foreach (var lightId in request.LightIds)
            {
                var found = allLights.FirstOrDefault(l => l.Id == lightId);
                if (found == null)
                {
                    return Results.NotFound($"Light {lightId} not found.");
                }
                if(hasBrightness && !found.Capabilities.IsDimmable)
                {
                    continue;
                    //return Results.BadRequest($"Light {lightId} does not support brightness control.");
                }
                if (hasColor && !found.Capabilities.CanChangeColor)
                {
                    continue;
                    //return Results.BadRequest($"Light {lightId} does not support color control.");
                }
                filteredLights.Add(found);
            }


            if (filteredLights.Count == 0)
                return Results.NotFound("No matching lights with the requested capabilities were found");

            foreach (var light in filteredLights)
            {
                if (hasBrightness) light.Brightness = request.Brightness.Value;
                if (hasColor) light.Color = request.Color; 
                if (hasState) light.State = newState;
            }
            if(filteredLights.Count != request.LightIds.Count)
            {
                return  Results.Json(filteredLights, statusCode: StatusCodes.Status207MultiStatus);
            }
            return Results.Ok(filteredLights);
        })
        .Produces<List<Light>>(StatusCodes.Status200OK)
        .Produces<List<Light>>(StatusCodes.Status207MultiStatus)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("UpdateLights")
        .WithSummary("Batch update multiple lights")
        .WithDescription("Updates multiple lights with new state (On or Off), color, or brightness. Make sure that the lights are the same capabilities. Returns the list of lights affected by the operation");


        //// Switch a light on or off
        //// PUT /lights/{id}/toggle (Toggle light state)
        //app.MapPut("/lights/{id}/toggle", (int id, [FromQuery] string state) =>
        //{
        //    var light = allLights.FirstOrDefault(l => l.Id == id);
        //    if (light == null) return Results.NotFound( $"Light {id} not found." );

        //    if (!Enum.TryParse<LightState>(state, true, out var newState))
        //        return Results.BadRequest( "\"Invalid state. Use 'On' or 'Off'" );

        //    light.State = newState;            

        //    return Results.Ok( $"Light {id} turned {newState}." );
        //})
        //.Produces(200)
        //.Produces(StatusCodes.Status404NotFound)
        //.Produces(StatusCodes.Status400BadRequest)
        //.WithName("ToggleLight")
        //.WithSummary("Toggle a light state")
        //.WithDescription("Turns a light on or off based on the provided state.");


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


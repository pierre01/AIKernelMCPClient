using LightsAPICommon;
using System.ComponentModel;
using System.Diagnostics;
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
            Debug.WriteLine(">> Get all rooms -> get_all_rooms");
            return allRooms;
        })
          .WithName("get_all_rooms")
          .WithSummary("Retreive all the rooms Names, floor, and unique IDs in the house, used by the lights property RoomId")
          .WithDescription("if light id=1 and roomID=3 the room mame for this light will be the Room which RoomId=3");

        // Get a specific room
        roomsApi.MapGet("/{roomId}", ([Description("Unique identifier for the room")] int roomId) =>
            allRooms.FirstOrDefault(a => a.RoomId == roomId) is { } room
                ? Results.Ok(room)
                : Results.NotFound())
          .WithName("get_room_by_id")
          .WithSummary("Retreive the room Name, the floor where it's located, and unique RoomId (used by the lights property RoomId)")
          .WithDescription("A room is representing a group of lights where the 'RoomId' is the same. if a command is adressing the room without specifying the light name then all the lights in the room should be receiving the command ");


        var lightsApi = app.MapGroup("/lights");
        lightsApi.WithTags(["Rooms","Lights"]);

        // Get all the lights
        lightsApi.MapGet("/", (HttpRequest request) =>
        {
            // Check user agent to remove the soft client calls and only show the Kernel calls
            if (!request.Headers["User-Agent"].ToString().Contains("API SoftClient"))
            {
                Debug.WriteLine(">> Get all lights -> get_all_lights");
            }
            return allLights;
        })
          .WithName("get_all_lights")
          .WithSummary("Retreive all the lights location, capabilities, and state inside the house")
          .WithDescription("Returns the list of the lights including their capabilities and current state.");

        // Get a specific light
        lightsApi.MapGet("/{id}", ([Description("Unique Identifier for the light")] int id) =>
            allLights.FirstOrDefault(a => a.Id == id) is { } light
                ? Results.Ok(light)
                : Results.NotFound())
            .Produces<Light>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("get_light_by_id")
            .WithSummary("Retreive the information for one light in the house")
            .WithDescription("Returns the capabilities and current state of a light");
        //.ExcludeFromDescription();


        // Switch a light on or off
        lightsApi.MapPut("/{id}/switch/{ison}", ([Description("Unique Identifier for the light")] int id, [Description("Turns the light on if true, or off if false")] bool ison) =>
        {
            Debug.WriteLine($">> Switch light {id} to {ison} -> switch_light");
            var existingLight = allLights.FirstOrDefault(a => a.Id == id);
            if (existingLight is null)
            {
                return Results.NotFound();
            }
            existingLight.IsOn = ison;

            return Results.Ok(value: existingLight);
        }).Produces<Light>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("switch_light")
            .WithSummary("Switch a light on or off (true for on, or false for off)")
            .WithDescription("Turn the light on or off and returns the light state with its new values")
            .WithTags("Lights");

        // Change the Light Color
        lightsApi.MapPut("/{id}/setColor/{color}", ([Description("Unique Identifier for the light")] int id, [Description("updated color of the light 'Color' in hexadecimal format 'RRGGBB'. 'RR' is the red component, 'GG' is the green component, and 'BB' is the blue component. Each component ranges from '00' to 'FF' (0 to 255 in decimal).")] string color) =>
        {

            var existingLight = allLights.FirstOrDefault(a => a.Id == id);
            if (existingLight is null)
            {
                return Results.NotFound();
            }

            Debug.WriteLine($">> Change light {id} color  from {existingLight.Color} to {color} -> change_light_color");

            if (!existingLight.CanChangeColor)
            {
                return Results.NoContent(); // REturn this instead of a Bad Request, so it does not trigger an exception
            }


            if (!ColorRegex().IsMatch(color))
            {
                return Results.BadRequest("Invalid color format. Must be in the format 'RRGGBB'");
            }

            existingLight.Color = color;
            return Results.Ok(value: existingLight);
        }).Produces<Light>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithName("change_light_color")
            .WithSummary("Updates an individual light color. Only call if this light supports color changes ('CanChangeColor' = true)")
            .WithDescription(@"Changes the color of a light identified by its 'id' to a value in hexadecimal format 'RRGGBB'. 'RR' is the red component, 'GG' is the green component, and 'BB' is the blue component. Each component ranges from '00' to 'FF' (0 to 255 in decimal). Only call if this light 'CanChangeColor' is set to true.");


        lightsApi.MapPut("/{id}/dimTo/{brightness}", ([Description("Unique Identifier for the light")] int id, [Description("Brightness intensity of the light from 0 to 100")] int brightness) =>
        {
            var existingLight = allLights.FirstOrDefault(a => a.Id == id);
            if (existingLight is null)
            {
                return Results.NotFound();
            }
            Debug.WriteLine($">> Change light {id} brightness  from {existingLight.Brightness} to {brightness} -> change_light_brightness");

            if (!existingLight.IsDimable)
            {
                 return Results.NoContent();
            }
            // Validate brightness
            if (brightness < 0 || brightness > 100)
            {
                return Results.BadRequest("Brightness value must be between 0 and 100");
            }

            existingLight.Brightness = brightness;

            return Results.Ok(value: existingLight);
        }).Produces<Light>(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status400BadRequest)
           .Produces(StatusCodes.Status404NotFound)
           .Produces(StatusCodes.Status204NoContent)
           .WithName("change_light_brightness")
           .WithSummary("Changes a light 'brightness', the light's brightness level is a value ranging from 0 to 100. This value can only be adjusted if this light property 'IsDimable' is true.")
           .WithDescription(@"Adjusts the light Brightness intensity between the values 0 to 100, ( this light is identified by the parameter 'Id')  Only call this function if this light supports dimming : 'IsDimable' = true.");


        app.Run();
    }


}


[JsonSerializable(typeof(Light[]))]
[JsonSerializable(typeof(Room[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}


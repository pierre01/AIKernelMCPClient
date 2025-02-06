using LightsAPICommon;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing.Constraints;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

// Configure Kestrel to use HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    // You can configure the specific ports for HTTP/HTTPS here
    //options.ListenAnyIP(5041); // HTTP
    options.ListenAnyIP(5042, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});

//// Protecting the API with JWT
//builder.Services.AddAuthentication().AddJwtBearer();
//builder.Services.AddAuthorization(o =>
//{
//    o.AddPolicy("ApiTesterPolicy", b => b.RequireRole("tester"));
//});

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddOpenApi(); // documentNAme =v1

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();
app.MapOpenApi("/openapi/{documentName}/openapi.json");
//app.UseSwagger();
//app.UseSwaggerUI(options =>
//{
//        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
//        options.RoutePrefix = string.Empty;
//});

var sampleLights = House.Lights;

var SampleRooms = House.Rooms;

var roomsApi = app.MapGroup("/rooms");
roomsApi.WithTags("Rooms");

// Get all the rooms
roomsApi.MapGet("/", () => SampleRooms)
   .WithName("get_all_rooms")
  .WithSummary("Retreive all the rooms Names and unique IDs in the house, used by the lights property RoomId")
  .WithDescription("if light id=1 and roomID=3 the room mame for this light will be the Room which RoomId=3");

// Get a specific room
roomsApi.MapGet("/{roomId}", ([Description("Unique room identifier")] int roomId) =>
    SampleRooms.FirstOrDefault(a => a.RoomId == roomId) is { } room
        ? Results.Ok(room)
        : Results.NotFound())
  .WithName("get_room_by_id")
  .WithSummary("Retreive the room Name and unique RoomId (used by the lights property RoomId)") 
  .WithDescription("A room is representing a group of lights where the RoomId is the same. if a command is adressing the room without specifying the light name then all the lights in the room should be receiving the command ");


var lightsApi = app.MapGroup("/lights");

// Get all the lights
lightsApi.MapGet("/", () => sampleLights)
  .WithName("get_all_lights")
  .WithSummary("Retreive all the lights location, capabilities, and state inside the house")
  .WithDescription("Returns the list of the lights including their capabilities and current state");

// Get a specific light
lightsApi.MapGet("/{id}", ([Description("Unique Identifier for the light")] int id) =>
    sampleLights.FirstOrDefault(a => a.Id == id) is { } light
        ? Results.Ok(light)
        : Results.NotFound())
  .WithName("get_light_by_id")
  .WithSummary("Retreive the information for one light in the house")
  .WithDescription("Returns the capabilities and current state of a light");
  //.ExcludeFromDescription();

//lightsApi.MapPost("/", (Light light) =>
//{
//    return Results.Created($"/lights/{light.Id}", light);
//});

// Switch a light on or off
lightsApi.MapPut("/{id}/switch/{ison}", ([Description("Unique Identifier for the light")] int id, [Description("Turns the light on if true, or off if false")]bool ison) =>
{
    var existingLight = sampleLights.FirstOrDefault(a => a.Id == id);
    if (existingLight is null)
    {
        return Results.NotFound();
    }


    existingLight.IsOn = ison;


    return Results.Ok(value: existingLight);
})
    .WithName("switch_light")
    .WithSummary("Switch a light on or off (true for on, or false for off)")
    .WithDescription("Change the light state. to dark or light")
    .WithTags("Lights");



// Change the Light Color
lightsApi.MapPut("/{id}/setColor/{color}", ([Description("Unique Identifier for the light")] int id,[Description("Color of the light in exadecimal format: RRGGBB")] string color) =>
{
    var existingLight = sampleLights.FirstOrDefault(a => a.Id == id);
    if (existingLight is null)
    {
        return Results.NotFound();
    }

    if (!existingLight.IsRgb)
    {
        return Results.BadRequest("This light can't change color");
    }

    color = color.ToUpper();
    if (!System.Text.RegularExpressions.Regex.IsMatch(color, "^(?:[A-Fa-f0-9]{3}|[A-Fa-f0-9]{6})$"))
    {
        return Results.BadRequest("Invalid color format. Must be in the format RRGGBB");
    }

    existingLight.HexColor = color;
    return Results.Ok(value: existingLight);
}) .WithName("chage_light_color")
   .WithSummary("Change the color of a light that accepts RGB values (i.e. IsRgb=true)")
   .WithDescription(@"Color of the light in exadecimal format: RRGGBB  Each pair of characters (RR, GG, BB) represents the intensity of Red, Green, and Blue from 00 to FF (in decimal: 0 to 255). Red would be FF0000, Blue: 0000FF, Green: 00FF00, and other colors like Yellow: FFFF00 or Purple: 800080 ");

// Dim the light
lightsApi.MapPut("/{id}/dimTo/{brightness}", ([Description("Unique Identifier for the light")] int id, [Description("Brightness intensity of the light from 0 to 100")] int brightness) =>
{
    var existingLight = sampleLights.FirstOrDefault(a => a.Id == id);
    if (existingLight is null)
    {
        return Results.NotFound();
    }

    if (!existingLight.IsDimable)
    {
        return Results.BadRequest("This light is not dimable");
    }
    // Validate brightness
    if (brightness < 0 || brightness > 100)
    {
        return Results.BadRequest("Brightness value must be between 0 and 100");
    }

    existingLight.Brightness = brightness;

    return Results.Ok(value: existingLight);
}).WithName("change_light_brightness")
  .WithSummary("Change the light brightness for lights that can be dimmed (i.e. IsDimable=true) otherwise the value cannot be changed")
  .WithDescription(@"The value 100 is the maximum intensity for the light. It can be addressed as a percentage also, 0 is complete dark, similar to off, 100 is full brightness. Lighten would increase it by 25, darken would decrease it by 25");


app.Run();

[JsonSerializable(typeof(Light[]))]
[JsonSerializable(typeof(Room[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}


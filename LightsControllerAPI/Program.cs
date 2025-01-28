using LightsAPICommon;
using Microsoft.AspNetCore.Builder;
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

builder.Services.AddOpenApi(); // documentNAme =v1

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();
app.MapOpenApi("/openapi/{documentName}/openapi.json");

var sampleLights = House.Lights;

var SampleRooms = House.Rooms;

var roomsApi = app.MapGroup("/rooms");
roomsApi.WithTags("Rooms");

roomsApi.MapGet("/", () => SampleRooms)
  .WithSummary("Retreive all the rooms Names and unique IDs used by the lights RoomId")
  .WithDescription("of light id=1 and roomID=3 the room mame for this light will be the Room which Id=3");

roomsApi.MapGet("/{id}", (int id) =>
    SampleRooms.FirstOrDefault(a => a.Id == id) is { } room
        ? Results.Ok(room)
        : Results.NotFound())
  .WithSummary("Retreive the room Name and unique ID used by the light RoomId")
  .WithDescription("What is the name of the room ");

//// Get all the lights in a room
//roomsApi.MapGet("/{id}/lights",(int id)=>
//{
//    var lights = sampleLights.Where(l => l.RoomId == id).ToArray();
//    return lights.Any() ? Results.Ok(lights) : Results.NotFound();

//});

var lightsApi = app.MapGroup("/lights");

lightsApi.MapGet("/", () => sampleLights)
  .WithSummary("Retreive all the lights information and state")
  .WithDescription("This is a description.");

lightsApi.MapGet("/{id}", (int id) =>
    sampleLights.FirstOrDefault(a => a.Id == id) is { } light
        ? Results.Ok(light)
        : Results.NotFound())  
  .WithSummary("Retreive the information for one light")
  .WithDescription("This is a description.");

//lightsApi.MapPost("/", (Light light) =>
//{
//    return Results.Created($"/lights/{light.Id}", light);
//});

lightsApi.MapPut("/{id}/switch/{onoff}", (int id, string onoff) =>
{
    var existingLight = sampleLights.FirstOrDefault(a => a.Id == id);
    if (existingLight is null)
    {
        return Results.NotFound();
    }

    onoff = onoff.ToLower();
    if (onoff == "on")
    {
        existingLight.IsOn = true;
    }
    else if (onoff == "off")
    {
        existingLight.IsOn = false;
    }
    else
    {
        return Results.BadRequest("Invalid value for onoff. Must be 'on' or 'off'");
    }

    return Results.Ok(value:existingLight);
});



lightsApi.MapPut("/{id}/setColor/{color}", (int id, string color) =>
{
    var existingLight = sampleLights.FirstOrDefault(a => a.Id == id);
    if (existingLight is null)
    {
        return Results.NotFound();
    }

    if (!existingLight.IsRgb)
    {
        return Results.BadRequest("Light can't change colors");
    }
    // TODO: validate color
    color = color.ToUpper();
    if (!System.Text.RegularExpressions.Regex.IsMatch(color, "^(?:[A-Fa-f0-9]{3}|[A-Fa-f0-9]{6})$"))
    {
        return Results.BadRequest("Invalid color format. Must be in the format RRGGBB");
    }

    existingLight.HexColor = color;
    return Results.Ok(value:existingLight);
});

lightsApi.MapPut("/{id}/dimTo/{brightness}", (int id, int brightness) =>
{
    var existingLight = sampleLights.FirstOrDefault(a => a.Id == id);
    if (existingLight is null)
    {
        return Results.NotFound();
    }

    if (!existingLight.IsDimable)
    {
        return Results.BadRequest("Light is not dimable");
    }
    // TODO: validate brightness
    // Validate brightness
    if (brightness < 0 || brightness > 100)
    {
        return Results.BadRequest("Brightness must be between 0 and 100");
    }

    existingLight.Brightness = brightness;

    return Results.Ok(value: existingLight);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1/openapi.json", "v1");
    });

}

app.Run();

[JsonSerializable(typeof(Light[]))]
[JsonSerializable(typeof(Room[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}

//{
//   "isOn": true,
//   "hexColor": "#FF0000",
//   "brightness": 100,
//   "fadeDurationInMilliseconds": 500,
//   "scheduledTime": "2023-07-12T12:00:00Z"
//}
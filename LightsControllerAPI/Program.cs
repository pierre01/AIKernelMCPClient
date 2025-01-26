using Microsoft.AspNetCore.Builder;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

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

var sampleLights = new Light[] {
    new(1, "Ceiling",1),
    new(2, "Ceiling",2),
    new(3, "Stairs Chandelier", 3),
    new(4, "Main Light",4),
    new(4, "Mirror Light",4),
    new(5, "Left Nightstand Light", 5,isOn:true,isDimable:true),
    new(6, "Right Nightstand Light", 5,isOn:true,isDimable:true),
    new(7, "Over Bed Light", 5),
    new(8, "Ceiling Light",6),
    new(9, "Bar Light", 2),
    new(10, "Cabinets Lights", 2),
    new(11, "Ceiling", 2),
    new(12, "Desk Light", 9),
    new(13, "Main Light", 9),
    new(14, "Mirror Light",4),
    new(15, "Mirror Light",7),
    new(16, "Closet",10),
    new(17, "Closet",11),
    new(18, "Wall",1),
};

var SampleRooms = new Room[] {
    new(1, "Living Room"),
    new(2, "Kitchen"),
    new(3, "Guest Bedroom"),
    new(4, "Master Bathroom"),
    new(5, "Master Bedroom"),
    new(6, "Stairs"),
    new(7, "Guest Bathroom"),
    new(8, "Downstairs Bathroom"),
    new(9, "Office"),
    new(10, "Master Closet"),
    new(11, "Guest Closet"),
    new(12, "Laundry Room"),
    };

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

// Get all the lights in a room
roomsApi.MapGet("/lights/{id}",(int id)=>
{
    var lights = sampleLights.Where(l => l.RoomId == id).ToArray();
    return lights.Any() ? Results.Ok(lights) : Results.NotFound();

});

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

lightsApi.MapPut("/turnOn/{id}", (int id) =>
{
    var existingLight = sampleLights.FirstOrDefault(a => a.Id == id);
    if (existingLight is null)
        return Results.NotFound();
    existingLight.IsOn = true;
    return Results.Accepted(value:existingLight);
});

lightsApi.MapPut("/turnOff/{id}", (int id) =>
{
    var existingLight = sampleLights.FirstOrDefault(a => a.Id == id);
    if (existingLight is null)
        return Results.NotFound();
    existingLight.IsOn = false;
    return Results.Accepted(value:existingLight);
});

lightsApi.MapPut("/setColor/{id}/{color}", (int id, string color) =>
{
    var existingLight = sampleLights.FirstOrDefault(a => a.Id == id);
    if (existingLight is null)
        return Results.NotFound();
      if(!existingLight.IsRgb)
        return Results.BadRequest("Light can't change colors");
      existingLight.HexColor = color;
    return Results.Accepted(value:existingLight);
});

lightsApi.MapPut("/dimTo/{id}/{brightness}", (int id, int brightness) =>
{
    var existingLight = sampleLights.FirstOrDefault(a => a.Id == id);
    if (existingLight is null)
        return Results.NotFound();
    if(!existingLight.IsDimable)
        return Results.BadRequest("Light is not dimable");
    // TODO: validate brightness
      // Validate brightness
    if (brightness < 0 || brightness > 100)
        return Results.BadRequest("Brightness must be between 0 and 100");
    
    existingLight.Brightness = brightness;

    return Results.Accepted(value: existingLight);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
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
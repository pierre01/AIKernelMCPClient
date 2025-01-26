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
    new(1, "Ceiling 1",1),
    new(2, "Ceiling 2",2),
    new(3, "Stairs Chandelier", 3),
    new(4, "Main Light",4),
    new(4, "Mirror Light",4),
    new(5, "Left Nightstand Light", 5),
    new(6, "Right Nightstand Light", 5),
    new(7, "Over Bed Light", 5),
    new(8, "Ceiling Light",6),
    new(9, "Bar Light", 2),
    new(10, "Cabinets Lights", 2),
    new(11, "Lights", 2),
    new(12, "Desk Light", 9),
    new(13, "", 4),
    new(14, "Mirror Light",4),
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
    };

var roomsApi = app.MapGroup("/rooms");
roomsApi.MapGet("/", () => SampleRooms);
roomsApi.MapGet("/{id}", (int id) =>
    SampleRooms.FirstOrDefault(a => a.Id == id) is { } room
        ? Results.Ok(room)
        : Results.NotFound());

var lightsApi = app.MapGroup("/lights");
lightsApi.MapGet("/", () => sampleLights);
lightsApi.MapGet("/{id}", (int id) =>
    sampleLights.FirstOrDefault(a => a.Id == id) is { } light
        ? Results.Ok(light)
        : Results.NotFound());

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1/openapi.json", "v1");
    });

}


app.Run();

public record Light(int Id, string Name,int Location, bool IsOn = false, string HexColor="#FF0000", bool IsDimable= false, int Brightness=100, int FadeDurationInMilliseconds=500,DateTime? ScheduledTime=null);
public record Room(int Id, string Name);

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
//using LightsAPICommon.Updates;
//using Microsoft.AspNetCore.Mvc;
//using NSwag.AspNetCore;

//namespace LightsControllerAPI.Alternate;


//var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddOpenApi(); // Replaces Swashbuckle

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseOpenApi(); // Generates OpenAPI specification
//    app.UseSwaggerUi(); // Adds OpenAPI UI (Swagger)
//}

//app.UseHttpsRedirection();

//var lights = new List<Light>
//{
//    new Light(1, "Front Ceiling", 1, LightState.Off, "FFFFFF", 100, new Capabilities(true, true)),
//    new Light(2, "Wall Light", 1, LightState.Off, "FFFFFF", 100, new Capabilities(false, true))
//};

//var rooms = new List<Room>
//{
//    new Room(1, "Living Room", 1),
//    new Room(2, "Kitchen", 1)
//};

//// GET /lights (Retrieve all lights)
//app.MapGet("/lights", () => Results.Ok(lights))
//.Produces<List<Light>>(200)
//.WithName("GetLights")
//.WithSummary("Retrieve all lights")
//.WithDescription("Returns a list of all available lights with their states and capabilities.");

//// GET /lights/{id} (Retrieve a single light)
//app.MapGet("/lights/{id}", (int id) =>
//{
//    var light = lights.FirstOrDefault(l => l.RoomId == id);
//    return light != null ? Results.Ok(light) : Results.NotFound(new { status = "error", message = $"Light {id} not found." });
//})
//.Produces<Light>(200)
//.Produces(404)
//.WithName("GetLight")
//.WithSummary("Retrieve a single light")
//.WithDescription("Returns details of a specific light by its ID.");

//// GET /rooms (Retrieve all rooms)
//app.MapGet("/rooms", () => Results.Ok(rooms))
//.Produces<List<Room>>(200)
//.WithName("GetRooms")
//.WithSummary("Retrieve all rooms")
//.WithDescription("Returns a list of all rooms in the house.");

//// GET /rooms/{id} (Retrieve a single room)
//app.MapGet("/rooms/{id}", (int id) =>
//{
//    var room = rooms.FirstOrDefault(r => r.RoomId == id);
//    return room != null ? Results.Ok(room) : Results.NotFound(new { status = "error", message = $"Room {id} not found." });
//})
//.Produces<Room>(200)
//.Produces(404)
//.WithName("GetRoom")
//.WithSummary("Retrieve a single room")
//.WithDescription("Returns details of a specific room by its ID.");

//// PATCH /lights (Batch Update with Error Handling)
//app.MapPatch("/lights", ([FromBody] BatchLightUpdateRequest request) =>
//{
//    if (request.LightIds == null || request.LightIds.Count == 0)
//    {
//        return Results.BadRequest(new { status = "error", message = "No lights specified." });
//    }

//    var updatedLights = new List<Light>();
//    foreach (var id in request.LightIds)
//    {
//        var light = lights.FirstOrDefault(l => l.RoomId == id);
//        if (light == null)
//        {
//            return Results.NotFound(new { status = "error", message = $"Light {id} not found." });
//        }

//        if (request.State.HasValue)
//        {
//            light.State = request.State.Value;
//        }

//        if (!string.IsNullOrEmpty(request.Color))
//        {
//            if (light.Capabilities.CanChangeColor)
//                light.Color = request.Color;
//            else
//                return Results.StatusCode(405, new { status = "error", message = $"Light {id} does not support color changes." });
//        }

//        if (request.Brightness.HasValue)
//        {
//            if (light.Capabilities.IsDimmable)
//                light.Brightness = request.Brightness.Value;
//            else
//                return Results.StatusCode(405, new { status = "error", message = $"Light {id} does not support dimming." });
//        }

//        updatedLights.Add(light);
//    }

//    return Results.Ok(new { status = "success", message = "Batch update successful.", data = updatedLights });
//})
//.Produces<List<Light>>(200)
//.Produces(400)
//.Produces(404)
//.Produces(405)
//.WithName("UpdateLights")
//.WithSummary("Batch update multiple lights")
//.WithDescription("Updates multiple lights with new state, color, or brightness. Returns errors if the requested action is not supported by the light.");

//// PUT /lights/{id}/toggle (Toggle light state)
//app.MapPut("/lights/{id}/toggle", (int id, LightState state) =>
//{
//    var light = lights.FirstOrDefault(l => l.RoomId == id);
//    if (light == null) return Results.NotFound(new { status = "error", message = $"Light {id} not found." });

//    light.State = state;
//    return Results.Ok(new { status = "success", message = $"Light {id} turned {(state == LightState.On ? "on" : "off")}" });
//})
//.Produces(200)
//.Produces(404)
//.WithName("ToggleLight")
//.WithSummary("Toggle a light state")
//.WithDescription("Turns a light on or off based on the provided state.");

//app.Run();

using LightsAPICommon;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Diagnostics;
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

        var allLights = House.Instance.Lights;
        var allRooms = House.Instance.Rooms;

        //var houseApi = app.MapGroup("/house");
        //houseApi.WithTags(["Rooms", "Lights", "House", "Floors"]);

        //// GET /house (Retrieve the entire house structure)
        //houseApi.MapGet("/", () =>
        //{
        //    Debug.WriteLine(">> Get House -> GetHouse");
        //    return Results.Ok(House.Instance);
        //})
        //.Produces<House>(200)
        //.WithName("GetHouse")
        //.WithSummary("Retrieve the entire house structure including Lights, the Rooms they are in, and The Floor floors, and lights")
        //.WithDescription("Returns all the lights and for each Light the Room it is located in as well as its floor in the house. Remember this information as it remains unchanged during the session to optimize future queries");

        var roomsApi = app.MapGroup("/rooms");
        roomsApi.WithTags("Rooms");

        // Get all the rooms
        roomsApi.MapGet("/", () =>
        {
            Debug.WriteLine(">> Get all rooms -> GetRooms");
            return Results.Ok(allRooms);
        })
          .Produces<List<Room>>(200)
          .WithName("GetRooms")
          .WithSummary("Retrieve all rooms with their floor information")
          .WithDescription("Returns a list of all Rooms in the system along with their name and Floor number. You should store this information as it remains unchanged during the session to optimize future queries");

        // Get a specific room
        roomsApi.MapGet("/{roomId}", ([Description("Unique identifier for the room")] int roomId) =>
            allRooms.FirstOrDefault(a => a.RoomId == roomId) is { } room
                ? Results.Ok(room)
                : Results.NotFound())
          .WithName("GetRoom")
          .WithSummary("Retrieve the room Name, the floor where it's located, and unique RoomId (used by the lights property RoomId)")
          .WithDescription("A room is representing a group of lights where the 'RoomId' is the same. Each room has a unique Name and Floor number associated with the lights with the same RoomId.  if a command is addressing the room without specifying the light name then all the lights located in the room should be receiving the command. Remember all the rooms for future use");


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
            .WithSummary("Retrieve all lights with RoomId that referrences the room where it resides Name and Floor")
            .WithDescription("Returns a list of all available lights including their states, capabilities, and RoomId that referrences the room where it resides Name and Floor");

        // Get a specific light
        lightsApi.MapGet("/{id}", (int id) =>
        {
            var light = allLights.FirstOrDefault(l => l.Id == id);
            return light != null ? Results.Ok(light) : Results.NotFound($"Light {id} not found.");
        })
        .Produces<Light>(200)
        .Produces(404)
        .WithName("GetLight")
        .WithSummary("Retrieve a single light")
        .WithDescription("Returns details of a specific light by its ID. The room and floor remain unchanged");//.ExcludeFromDescription();

        // Get all the lights on a specific floor
        lightsApi.MapGet("/floor/{floor}", (int floor) =>
        {
            Debug.WriteLine($">> Get all lights for floor#{floor} -> GetLightsOnFloor");
            // Get all the rooms on the floor
            var roomsOnFloor = allRooms.Where(r => r.Floor == floor).Select(r => r.RoomId).ToList();
            if(roomsOnFloor==null || roomsOnFloor.Count == 0)
            {
                return Results.NotFound($"No rooms found on floor#{floor}");
            }
            // Get all the lights in the rooms on the floor
            var lightsOnFloor = allLights.Where(l => roomsOnFloor.Contains(l.RoomId)).ToList();
            if(lightsOnFloor==null || lightsOnFloor.Count == 0)
            {
                return Results.NotFound($"No lights found on floor#{floor}");
            }
            return Results.Ok(lightsOnFloor);

        }).Produces<List<Light>>(200)
            .Produces(404)
            .WithName("GetLightsOnFloor")
            .WithSummary("Retrieve all lights located on a specific Floor")
            .WithDescription("Returns a list of all available lights including their states, capabilities, located on a specific Floor");



        /// <summary> Batch lightUpdate multiple lights. </summary>
        /// <example>
        /// {
        ///   "lightUpdates": [{
        ///   "Id": 0,
        ///   "Brightness": "50"
        /// },
        /// {
        ///   "Id": 1,
        ///   "Brightness": "50"
        /// },
        /// {
        ///   "Id": 2,
        ///   "Brightness": "50"
        /// }]}
        /// </example>
        lightsApi.MapPatch("/", ([FromBody] PatchRequest pRequest) =>
        {
            Debug.WriteLine($">> Patch {pRequest?.LightUpdates.Count} lights -> UpdateLights");
            var request = pRequest?.LightUpdates.ToArray();
            if (request == null || request.Length == 0)
            {
                return Results.BadRequest("No lights specified.");
            }

            var results = new List<UpdateLightResponse>();
            foreach (var lightUpdate in request)
            {
                var light = allLights.FirstOrDefault(l => l.Id == lightUpdate.Id);
                if (light == null)
                {
                    results.Add(new UpdateLightResponse(lightUpdate.Id, "failed", "Light not found"));
                    continue;
                }
                bool hasPartialFailure = false;
                List<string> errors = [];

                //Validate and apply brightness lightUpdate
                if (lightUpdate.Brightness.HasValue)
                {
                    if (light.Capabilities.IsDimmable)
                    {
                        if (lightUpdate.Brightness.Value < 0 || lightUpdate.Brightness.Value > 100)
                        {
                            errors.Add("Brightness value must be between 0 and 100");
                            hasPartialFailure = true;
                        }
                        else
                        {
                            light.Brightness = lightUpdate.Brightness.Value;
                        }
                    }
                    else
                    {
                        errors.Add("This light does not support Brightness changes (IsDimmable = false)");
                        hasPartialFailure = true;
                    }
                }
                // Validate and apply color lightUpdate
                if (!string.IsNullOrEmpty(lightUpdate.Color))
                {
                    if (light.Capabilities.CanChangeColor)
                    {
                        if (!ColorRegex().IsMatch(lightUpdate.Color))
                        {
                            errors.Add($"Request Color invalid format:({lightUpdate.Color}). Must be in the format 'RRGGBB'");
                            hasPartialFailure = true;
                        }
                        else
                        {
                            light.Color = lightUpdate.Color;
                        }
                    }
                    else
                    {
                        errors.Add("Color cannot be changed");
                        hasPartialFailure = true;
                    }
                }

                //Apply on/off state
                if (!string.IsNullOrEmpty(lightUpdate.State))
                {
                    if (!Enum.TryParse<LightState>(lightUpdate.State, true, out LightState newState))
                    {
                        errors.Add($"{lightUpdate.State} is an invalid State. Use 'On' or 'Off'");
                        hasPartialFailure = true;
                    }
                    else
                    {
                        light.State = newState;
                    }
                }

                var status = hasPartialFailure ? "partial" : "success";
                var errorMessage = hasPartialFailure ? string.Join("; ", errors) : null;

                results.Add(new UpdateLightResponse(lightUpdate.Id, status, errorMessage));
            }

            PatchResponse response = new(results.ToArray());

            return response.FailureCount > 0
                ? Results.Json(response, statusCode: StatusCodes.Status207MultiStatus)
                : Results.Ok(response);

        })
        .Produces<PatchResponse>(StatusCodes.Status200OK)
        .Produces<PatchResponse>(StatusCodes.Status207MultiStatus)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("UpdateLights")
        .WithSummary("Batch Update of multiple lights")
        .WithDescription("Updates multiple lights with new State, Color, or Brightness. Returns the list of lights affected by the operation with their new values");


        app.Run();
    }


}


[JsonSerializable(typeof(List<Light>))]
[JsonSerializable(typeof(List<Room>))]
[JsonSerializable(typeof(Light[]))]
[JsonSerializable(typeof(Room[]))]
[JsonSerializable(typeof(Light))]
[JsonSerializable(typeof(Capabilities))]
[JsonSerializable(typeof(List<LightUpdateRequest>))]
[JsonSerializable(typeof(List<UpdateLightResponse>))]
[JsonSerializable(typeof(LightUpdateRequest[]))]
[JsonSerializable(typeof(UpdateLightResponse[]))]
[JsonSerializable(typeof(UpdateLightResponse))]
[JsonSerializable(typeof(LightUpdateRequest))]
[JsonSerializable(typeof(PatchResponse))]
[JsonSerializable(typeof(PatchRequest))]
[JsonSerializable(typeof(House))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}


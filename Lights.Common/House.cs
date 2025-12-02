using System.ComponentModel;

namespace Lights.Common;

/// <summary>
/// Made the class a singleton to ensure only one instance of the house is created
/// And to allow it in the API
/// </summary>
public class House
{
    private static readonly Lazy<House> _instance = new Lazy<House>(() => new House());

    public static House Instance => _instance.Value;

    [Description("All lights installed in the house, each associated with a unique room and unique floor where the room is located")]
    public Light[] Lights { get; private set; }

    [Description("All rooms within the house, each associated to a unique floor. Each light is located in a unique room and each room in a hunique floor floor")]
    public Room[] Rooms { get; private set; }

    public static string[] CustomPrompts { get; private set; } =
        [
                "Get all the Rooms, Get all the Lights, get all the floors",
                "Turn all the living room lights on.",
                "Switch all the lights in the Kitchen on, as well as the office lights, then change them to a medium intensity",
                "Change the kitchen lights color to a value similar to a 2000K bulb",
                "Are the lights in the office on or off?",
                "Turn off all the lights",
                "When I tell you 'I'm home' you will switch the front living room lights, then the wall lights as well as the stairs lights, then switch on the master bedroom left nightstand light, all those lights with medium brightness.",
                "I'm home",
                "when I tell you 'I'm leaving' you will turn off all the lights in the house",
                "I'm leaving",
                "How many lights are currently on?",
                "Create a C#  function that execute the calls for \"I'm home\" ",
                // "Switch all the house lights on, don't generate code, just call functions.", // Will avoid context continuation of Code generation 
                "Switch all the house lights on", // Will create code for this
                "How many rooms are in the house?",
                "Change the house lights to match a 70's party theme.",
                "Use different colors for each light.",
                "How about a Christmas party theme?",
                "Reset all the house lights colors to white",
                "Switch the house lights off",
                "Turn all the lights located on the first floor on",
                "Turn all the lights located on the second floor on",
                "Turn all the lights located on the first floor off",
                "Turn all the lights located on the second floor off",

               // "Call GetLights and GetRooms, remember the Lighs names, Ids, and capabilities, as well as their location such as room name, and floor number, for future use"
        ];

    private House()
    {
        var sampleLights = new Light[] {
                // Living room
                new(0, "Front Ceiling",0, capabilities:new( canChangeColor:true,isDimmable:true)),
                new(1, "Back Ceilling",0,capabilities:new(canChangeColor:true,isDimmable:true)),
                new(2, "Living Wall",0,capabilities:new(isDimmable:true)),
                // Kitchen
                new(3, "Bar Light", 1,capabilities:new(canChangeColor:true,isDimmable:true)),
                new(4, "Kitchen Ceiling",1,capabilities:new(canChangeColor:true,isDimmable:true)),
                new(5, "Cabinet lights", 1,capabilities:new(isDimmable:true)),
                // Downstairs Bathroom
                new(6, "first floor toilets", 7),            
                // Laundry room
                new(7, "Ceiling",11),            
                // Office
                new(8, "Desk light", 8,  capabilities:new(isDimmable : true, canChangeColor:true)),
                new(9, "Floor lamp", 8,  capabilities:new(isDimmable : true, canChangeColor:true)),            
                // Stairs
                new(10, "Stairs Chandelier", 5,capabilities:new(isDimmable:true)),
                // Master Bedroom
                new(11, "Left Nightstand", 4,capabilities:new(isDimmable:true)),
                new(12, "Right Nightstand", 4,capabilities:new(isDimmable:true)),
                new(13, "Master Wall light", 4, capabilities:new(canChangeColor:true)),
                // Master Bathroom
                new(14, "Main light",3),
                new(15, "Mirror light",3),

                // Guest Bathroom
                new(16, "Mirror light",6),
                new(17, "Main light",6),
                // Master Bedroom Closet
                new(18, "Closet",9),
                // Guest Bedroom Closet
                new(19, "Closet",10),            
                // Guest Bedroom
                new(20, "Left Nightstand", 2,capabilities:new(isDimmable:true)),
                new(21, "Right Nightstand", 2,capabilities:new(isDimmable:true)),
                new(22, "Guest Wall light", 2, capabilities:new(canChangeColor:true))
            };

        var sampleRooms = new Room[] {
                new(0, "Living Room",1),
                new(1, "Kitchen",1),
                new(2, "Guest Bedroom",2),
                new(3, "Master Bathroom",2),
                new(4, "Master Bedroom",2),
                new(5, "Stairs", 1),
                new(6, "Guest Bathroom",2),
                new(7, "Toilets", 1),
                new(8, "Office", 1),
                new(9, "Master Closet",2),
                new(10, "Guest Closet",2),
                new(11, "Laundry Room", 1),
            };

        Lights = sampleLights;
        Rooms = sampleRooms;
        
    }
}

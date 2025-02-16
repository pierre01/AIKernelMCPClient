using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightsAPICommon;

public static class House
{
    public static Light[] Lights { get; set; }
    public static Room[] Rooms { get; set; }
    public static string[] CustomPrompts { get; set; }

    static House()
    {
        var sampleLights = new Light[] {
            // Living room
            new(1, "Front Ceiling",1, capabilities:new( canChangeColor:true,isDimmable:true)),
            new(2, "Back Ceilling",1,capabilities:new(canChangeColor:true,isDimmable:true)),
            new(3, "Wall",1,capabilities:new(isDimmable:true)),
            // Kitchen
            new(4, "Bar Light", 2,capabilities:new(canChangeColor:true,isDimmable:true)),
            new(5, "Ceiling",2,capabilities:new(canChangeColor:true,isDimmable:true)),
            new(6, "Cabinet", 2,capabilities:new(isDimmable:true)),
            // Downstairs Bathroom
            new(7, "Vanity Light", 8),            
            // Laundry room
            new(8, "Ceiling",12),            
            // Office
            new(9, "Desk", 9,  capabilities:new(isDimmable : true, canChangeColor:true)),
            new(10, "Floor Lamp", 9,  capabilities:new(isDimmable : true, canChangeColor:true)),            
            // Stairs
            new(11, "Stairs Chandelier", 6,capabilities:new(isDimmable:true)),
            // Master Bedroom
            new(12, "Left Nightstand", 5,capabilities:new(isDimmable:true)),
            new(13, "Right Nightstand", 5,capabilities:new(isDimmable:true)),
            new(14, "Wall", 5, capabilities:new(canChangeColor:true)),
            // Master Bathroom
            new(15, "Main",4),
            new(16, "Mirror",4),

            // Guest Bathroom
            new(17, "Mirror",7),
            new(18, "Main",7),
            // Master Bedroom Closet
            new(19, "Closet",10),
            // Guest Bedroom Closet
            new(20, "Closet",11),            
            // Guest Bedroom
            new(21, "Left Nightstand", 3,capabilities:new(isDimmable:true)),
            new(22, "Right Nightstand", 3,capabilities:new(isDimmable:true)),
            new(23, "Wall", 3, capabilities:new(canChangeColor:true))
       };

        var SampleRooms = new Room[] {
            new(1, "Living Room"),
            new(2, "Kitchen"),
            new(3, "Guest Bedroom",2),
            new(4, "Master Bathroom",2),
            new(5, "Master Bedroom",2),
            new(6, "Stairs"),
            new(7, "Guest Bathroom",2),
            new(8, "Downstairs Bathroom"),
            new(9, "Office"),
            new(10, "Master Bedroom Closet",2),
            new(11, "Guest Bedroom Closet",2),
            new(12, "Laundry Room"),
            };
        Lights = sampleLights;
        Rooms = SampleRooms;
        CustomPrompts = [
            "Turn all the living room lights on.",
            "Switch all the lights in the Kitchen on, as well as the office lights, then change them to a medium intensity.",
            "Change the kitchen lights color to a value similar to a 2000K bulb",
            "Are the lights in the office on or off?",
            "Turn off all the lights",
            "When I tell you 'I'm home' you will switch the front living room lights, then the wall lights as well as the stairs lights, then switch on the master bedroom left nightstand light, all those lights with medium brightness.",
            "I'm home",
            "when I tell you 'I'm leaving' you will turn off all the lights in the house",
            "I'm leaving",
            "How many lights are currently on?",
            "Switch all the house lights on", // talk about mispelling
            "Change the house lights to match a 70's party theme.",
            "Use different colors for each light.",
            "How about a Christmas party theme?",
            "Reset all the house lights colors to white",
            "Switch the house lights off",
            "Switch all the first floor lights on"
        ];
    }
}

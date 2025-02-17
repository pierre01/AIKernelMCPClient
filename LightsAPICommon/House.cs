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
            new(0, "Front Ceiling",0, capabilities:new( canChangeColor:true,isDimmable:true)),
            new(1, "Back Ceilling",0,capabilities:new(canChangeColor:true,isDimmable:true)),
            new(2, "Living Wall",0,capabilities:new(isDimmable:true)),
            // Kitchen
            new(3, "Bar Light", 1,capabilities:new(canChangeColor:true,isDimmable:true)),
            new(4, "Kitchen Ceiling",1,capabilities:new(canChangeColor:true,isDimmable:true)),
            new(5, "Cabinet lights", 1,capabilities:new(isDimmable:true)),
            // Downstairs Bathroom
            new(6, "Vanity Light", 7),            
            // Laundry room
            new(7, "Laudry Ceiling",11),            
            // Office
            new(8, "Desk light", 8,  capabilities:new(isDimmable : true, canChangeColor:true)),
            new(9, "Floor lamp", 8,  capabilities:new(isDimmable : true, canChangeColor:true)),            
            // Stairs
            new(10, "Stairs Chandelier", 5,capabilities:new(isDimmable:true)),
            // Master Bedroom
            new(11, "Left Nightstand light", 4,capabilities:new(isDimmable:true)),
            new(12, "Right Nightstand light", 4,capabilities:new(isDimmable:true)),
            new(13, "Master Wall light", 4, capabilities:new(canChangeColor:true)),
            // Master Bathroom
            new(14, "Main light",3),
            new(15, "Mirror light",3),

            // Guest Bathroom
            new(16, "Mirror light",6),
            new(17, "Main light",6),
            // Master Bedroom Closet
            new(18, "Closet light",9),
            // Guest Bedroom Closet
            new(19, "Closet light",10),            
            // Guest Bedroom
            new(20, "Left Nightstand", 2,capabilities:new(isDimmable:true)),
            new(21, "Right Nightstand", 2,capabilities:new(isDimmable:true)),
            new(22, "Guest Wall light", 2, capabilities:new(canChangeColor:true))
       };

        var SampleRooms = new Room[] {
            new(0, "Living Room",1),
            new(1, "Kitchen",1),
            new(2, "Guest Bedroom",2),
            new(3, "Master Bathroom",2),
            new(4, "Master Bedroom",2),
            new(5, "Stairs", 1),
            new(6, "Guest Bathroom",2),
            new(7, "Downstairs Bathroom", 1),
            new(8, "Office", 1),
            new(9, "Master Bedroom Closet",2),
            new(10, "Guest Bedroom Closet",2),
            new(11, "Laundry Room", 1),
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

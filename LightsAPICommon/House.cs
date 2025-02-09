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
            new(1, "Front Ceiling",1, isRgb:true,isDimable:true),
            new(2, "Back Ceilling",1,isRgb:true,isDimable:true),
            new(3, "Wall",1,isDimable:true),
            // Kitchen
            new(4, "Bar Light", 2,isRgb:true,isDimable:true),
            new(5, "Main Ceiling Light",2,isRgb:true,isDimable:true),
            new(6, "Cabinets Lights", 2,isDimable:true),
            // Downstairs Bathroom
            new(7, "Vanity Light", 8),            
            // Laundry room
            new(8, "Ceiling Light",12),            
            // Office
            new(9, "Desk Light", 9,  isDimable : true, isRgb:true,brightness:70),
            new(10, "Floor Lamp Light", 9,  isDimable : true, isRgb:true),            
            // Stairs
            new(11, "Stairs Chandelier", 6,isDimable:true),
            // Master Bedroom
            new(12, "Left Nightstand Light", 5,isDimable:true),
            new(13, "Right Nightstand Light", 5,isDimable:true),
            new(14, "Over Bed Light", 5),
            // Master Bathroom
            new(15, "Main Light",4),
            new(16, "Mirror Light",4),
            // Guest Bathroom
            new(17, "Mirror Light",7),
            new(18, "Main Light",7),
            // Master Bedroom Closet
            new(19, "Closet",10),
            // Guest Bedroom Closet
            new(20, "Closet",11),
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
            new(10, "Master Bedroom Closet"),
            new(11, "Guest Bedroom Closet"),
            new(12, "Laundry Room"),
            };
        Lights = sampleLights;
        Rooms = SampleRooms;
        CustomPrompts = [
            "Turn all the living room lights on.",
            "Switch all the lights in the Kitchen on, as well as the office lights, then change them to a medium intensity.",
            "Switch the kitchen lights color to 2000K",
            "Are the lights in the office on or off?",
            "Turn off all the lights.",
            "When I tell you 'I'm home' you will switch the front living room lights, then the wall lights as well as the stairs lights, all with medium brightness",
            "I'm home",
            "when I tell you 'I'm leaving' you will turn all the lights off",
            "I'm leaving",
            "How many lights are currently on?",
            "Switch all the lights on",
            "Change the colors of the lights to match a 70's party theme",
            "How about a Christmas party theme?",
            "Reset all the house lights colors to white",
            "Switch the house lights off."
        ];
    }
}

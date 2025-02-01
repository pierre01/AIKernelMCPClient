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
    static House()
    {
        var sampleLights = new Light[] {
            new(1, "Front Ceiling",1, isRgb:true,isDimable:true),
            new(2, "Main Ceiling Light",2,isRgb:true,isDimable:true),
            new(3, "Stairs Chandelier", 3,isDimable:true),
            new(4, "Main Light",4),
            new(5, "Left Nightstand Light", 5,isDimable:true),
            new(6, "Right Nightstand Light", 5,isDimable:true),
            new(7, "Over Bed Light", 5),
            new(8, "Ceiling Light",6),
            new(9, "Bar Light", 2,isRgb:true,isDimable:true),
            new(10, "Cabinets Lights", 2,isDimable:true),
            new(11, "Vanity Light", 8),
            new(12, "Desk Light", 9,  isDimable : true, isRgb:true,brightness:70),
            new(13, "Floor Lamp Light", 9,  isDimable : true),
            new(14, "Mirror Light",4),
            new(15, "Mirror Light",7),
            new(16, "Closet",10),
            new(17, "Closet",11),
            new(18, "Wall",1,isDimable:true),
            new(19, "Back Ceilling",1,isRgb:true,isDimable:true),
            new(20, "Mirror Light",4),
            new(21, "Ceiling Light",12),
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
        Lights = sampleLights;
        Rooms = SampleRooms;
    }
}

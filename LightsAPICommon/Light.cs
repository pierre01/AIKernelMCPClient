using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Text.Json.Serialization;
namespace LightsAPICommon;

[JsonSerializable(typeof(Light))]
[Description("A Light inside a Room, can optionaly support brightness control and color")]
public class Light(int id, string name, int roomId, bool isOn = false, bool isRgb = false, string hexColor = "FFFFFF", bool isDimable = false, int brightness = 100)
{
    [Description("Unique Identifier for the light")]
    public int Id { get; set; } = id;

    [Required]
    [Description("The name of the light in the room")]
    public string Name { get; set; } = name;

    [Required]
    [Description("Room unique identifier that indicates where the light is located, correspomnd to the RoomId of a Room")]
    public int RoomId { get; set; } = roomId;

    [DefaultValue(false)]
    [Description("Is the light switched on or off, active or inactive, if  IsOn=true the light is On")]
    public bool IsOn { get; set; } = isOn;

    [DefaultValue(false)]
    [Description("Does this light allow color changing")]
    public bool IsRgb { get; set; } = isRgb;

    [DefaultValue("FFFFFF")]
    [Description(@"Color of the light in exadecimal format: RRGGBB
        Each pair of characters (RR, GG, BB) represents the intensity of Red, Green, and Blue from 00 to FF (in decimal: 0 to 255).
        It is only possible to change if IsRgb=true")]
    public string HexColor { get; set; } = hexColor;

    [DefaultValue(false)]
    [Description("Does the allow to change its brightness. If true the Brightness value can be changed, if false the brightness stays at 100")]
    public bool IsDimable { get; set; } = isDimable;

    [DefaultValue(100)]
    [Range(0, 100)]
    [Description("If the light can be dimmed (i.e. IsDimmable=true) the brightness can be changed from 0 to 100. 100 is the maximum brightness for any light")]
    public int Brightness { get; set; } = brightness;
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
namespace LightsAPICommon;

public class Light(int id, string name, int roomId, bool isOn = false, bool isRgb = false, string hexColor = "#FFFFFF", bool isDimable = false, int brightness = 100)
{
    [Description]
    public int Id { get; set; } = id;

    [Required]
    [Description]
    public string Name { get; set; } = name;

    [Required]
    [Description("Room unique identifier that indicates where the light is located, correspomnd to the Id of the room")]
    public int RoomId { get; set; } = roomId;

    [DefaultValue(false)]
    [Description("Is the light switched on or off, active or inactive if it isOn=true the light is On")]
    public bool IsOn { get; set; } = isOn;

    [DefaultValue(false)]
    [Description("Does this light allows color changing (i.e. can the user modify the HexColor Property) Those lights are also called color lights")]
    public bool IsRgb { get; set; } = isRgb;

    [DefaultValue("FFFFFF")]
    [Description(@"Color of the light in exadecimal format: RRGGBB
        Each pair of characters (RR, GG, BB) represents the intensity of Red, Green, and Blue from 00 to FF (in decimal: 0 to 255).
        It is only possible to change if isRgb=true")]
    public string HexColor { get; set; } = hexColor;

    [DefaultValue(false)]
    [Description("Does the light have a dimmer in order to change its brightness")]
    public bool IsDimable { get; set; } = isDimable;

    [DefaultValue(100)]
    [Range(0, 100)]
    [Description("If the light can be dimmed (i.e. IsDimmable=true) the brightness can be changed from 0 to 100. 100 is the maximum brightness for the specified light")]
    public int Brightness { get; set; } = brightness;
}

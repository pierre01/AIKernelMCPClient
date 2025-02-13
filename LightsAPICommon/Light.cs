using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Text.Json.Serialization;
namespace LightsAPICommon;

[JsonSerializable(typeof(Light))]
[Description("A Light inside a Room or space can optionaly support brightness and color control")]
public class Light(int id, string name, int roomId, bool isOn = false, bool canChangeColor = false, string color = "FFFFFF", bool isDimable = false, int brightness = 100)
{
    [Description("Id: Unique Identifier for this light.")]
    public int Id { get; set; } = id;

    [Required]
    [Description("Name: Specifies the name of this light within a room.")]
    public string Name { get; set; } = name;

    [Required]
    [Description("RoomId: Unique identifier for the room where this light is located. Corresponds to the 'RoomId' of a Room.")]
    public int RoomId { get; set; } = roomId;

    [DefaultValue(false)]
    [Description("IsOn: Indicates whether this light is currently switched on. If 'IsOn' is set to true, the light is active; otherwise, it is inactive.")]
    public bool IsOn { get; set; } = isOn;


    [DefaultValue(false)]
    [Description("CanChangeColor: Specifies whether this light supports color changes. If true, the 'Color' value can be modified. If false, the 'Color' should not be changed.")]
    public bool CanChangeColor { get; set; } = canChangeColor;

    [DefaultValue("FFFFFF")]
    [Description("Color: Specifies the color of this light in hexadecimal format 'RRGGBB'. 'RR' is the red component, 'GG' is the green component, and 'BB' is the blue component. Each component ranges from '00' to 'FF' (0 to 255 in decimal). If 'CanChangeColor' is false, the 'Color' can not be changed.")]
    public string Color { get; set; } = color;

    [DefaultValue(false)]
    [Description("IsDimable: Indicates whether this light's brightness can be adjusted. If true, the 'Brightness' value can be modified. If false, the brightness remains fixed at 100.")]
    public bool IsDimable { get; set; } = isDimable;

    [DefaultValue(100)]
    [Range(0, 100)]
    [Description("Brightness: Controls this light's brightness level, ranging from 0 to 100, where 100 represents maximum brightness. This value can only be adjusted if 'IsDimable' is true.")]
    public int Brightness { get; set; } = brightness;
}

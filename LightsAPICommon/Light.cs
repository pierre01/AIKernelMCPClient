using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
namespace LightsAPICommon;

[Description("A Light linked to a Room can optionally support brightness and color changes")]
public class Light(int id, string name, int roomId, LightState state = LightState.Off, Capabilities capabilities = null, string color = "FFFFFF", int brightness = 100)
{
    [Required]
    [Range(0, int.MaxValue)]
    [Description("Unique identifier for the light")]
    public int Id { get; set; } = id;

    [Required]
    [StringLength(100)]
    [Description("Name of the light")]
    public string Name { get; set; } = name;

    [Required]
    [Range(0, int.MaxValue)]
    [Description("Room ID where the light is located")]
    public int RoomId { get; set; } = roomId;

    [Required]
    [Description("State of the light: On or Off")]
    public LightState State { get; set; } = state;

    [RegularExpression("^[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex code")]
    [Description("Color of the light in hex format")]
    public string Color { get; set; } = color;

    [Range(0, 100)]
    [Description("Brightness of the light (0-100)")]
    public int Brightness { get; set; } = brightness;

    [Description("Light capabilities such as dimming and color-changing support")]
    public Capabilities Capabilities { get; set; } = capabilities ?? new Capabilities(false, false);
}


[JsonConverter(typeof(JsonStringEnumConverter<LightState>))] 
public enum LightState
{
    [Description("Light is turned Off")]
    [EnumMember(Value = "Off")]
    Off = 0,
    [Description("Light is turned On")]
    [EnumMember(Value = "On")]
    On = 1
}



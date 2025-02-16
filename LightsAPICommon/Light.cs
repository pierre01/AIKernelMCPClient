using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
namespace LightsAPICommon;

[JsonSerializable(typeof(Light))]
[Description("A Light inside a Room or space can optionaly support brightness and color control")]
public class Light(int id, string name, int roomId, LightState state = LightState.Off, Capabilities capabilities = null, string color = "FFFFFF", int brightness = 100)
{
    [Required]
    [Range(1, int.MaxValue)]
    [Display(Name = "Light ID", Description = "Unique light identifier")]
    public int Id { get; set; } = id;

    [Required]
    [StringLength(100)]
    [Display(Name = "Light Name", Description = "Light name")]
    public string Name { get; set; } = name;

    [Required]
    [Range(1, int.MaxValue)]
    [Display(Name = "Room ID", Description = "Associated room ID")]
    public int RoomId { get; set; } = roomId;

    [Required]
    [Display(Name = "State", Description = "State of the light: on or off")]
    public LightState State { get; set; } = state;

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex code.")]
    [Display(Name = "Color", Description = "Light color in hex format")]
    public string Color { get; set; } = color;

    [Range(0, 100)]
    [Display(Name = "Brightness", Description = "Light brightness (0-100)")]
    public int Brightness { get; set; } = brightness;

    [Display(Name = "Capabilities", Description = "Light capabilities such as dimming and color-changing support")]
    public Capabilities Capabilities { get; set; } = capabilities ?? new Capabilities(false, false);
}


[JsonConverter(typeof(JsonStringEnumConverter))] 
public enum LightState
{
    [Description("Light is turned off.")]
    [Display(Name = "Off", Description = "The light is currently off.")]
    [EnumMember(Value = "off")]
    Off = 0,
    [Description("Light is turned on.")]
    [Display(Name = "On", Description = "The light is currently on.")]
    [EnumMember(Value = "on")]
    On = 1
}



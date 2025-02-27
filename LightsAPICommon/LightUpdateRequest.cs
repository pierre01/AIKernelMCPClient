using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LightsAPICommon;

[JsonSerializable(typeof(LightUpdateRequest))]
public class LightUpdateRequest
{
    [Required]
    [MinLength(1)]
    [Description("light LightId to update")]
    public int LightId { get; set; }

    [Description("Turns a light (\"On\" or \"Off\")")]
    public string? State { get; set; }

    [RegularExpression("^[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex code RRGGB")]
    [Description("New color in hex format")]
    public string? Color { get; set; }

    [Range(0, 100)]
    [Description("New brightness level (0-100)")]
    public int? Brightness { get; set; }
}

public class PatchRequest
{
    public List<LightUpdateRequest> LightUpdates { get; set; } 
}


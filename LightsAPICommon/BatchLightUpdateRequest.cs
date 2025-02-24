using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LightsAPICommon;

/// <summary>
/// Represents a batch update request for multiple lights.
/// </summary>
public class BatchLightUpdateRequest
{
    [Required]
    [MinLength(1)]
    [Description("List of light IDs to update")]
    public List<int> LightIds { get; set; } = [];

    [Display(Name = "State", Description = "Turns a light on or off based on the provided state")]
    [Description("Turns a light on or off based on the provided state.")]
    public string? State { get; set; }

    [RegularExpression("^[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex code RRGGB")]
    [Display(Name = "Color", Description = "New color in hex format")]
    [Description("New color in hex format")]
    public string? Color { get; set; }

    [Range(0, 100)]
    [Description("New brightness level (0-100)")]
    public int? Brightness { get; set; }
}


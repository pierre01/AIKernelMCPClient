using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LightsAPICommon;

/// <summary>
/// Represents a batch update request for multiple lights.
/// </summary>
[JsonSerializable(typeof(BatchLightUpdateRequest))]
public class BatchLightUpdateRequest
{
    [Required]
    [MinLength(1)]
    [Display(Name = "Light IDs", Description = "List of light IDs to update")]
    public List<int> LightIds { get; set; } = [];

    [Display(Name = "State", Description = "Turns a light on or off based on the provided state.")]
    public string? State { get; set; }

    [RegularExpression("^(?:[A-Fa-f0-9]{3}|[A-Fa-f0-9]{6})$", ErrorMessage = "Color must be a valid hex code.")]
    [Display(Name = "Color", Description = "New color in hex format")]
    public string? Color { get; set; }

    [Range(0, 100)]
    [Display(Name = "Brightness", Description = "New brightness level (0-100)")]
    public int? Brightness { get; set; }
}


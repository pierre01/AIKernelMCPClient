using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Lights.Common;

public class LightUpdateRequest
{
    [Required]
    [MinLength(1)]
    [Description("light LightId to update")]
    [JsonPropertyName("lightId")]
    public int LightId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Turns a light (\"On\" or \"Off\")")]
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [RegularExpression("^[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex code RRGGB")]
    [Description("New color in hex format")]
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Range(0, 100)]
    [Description("New brightness level (0-100)")]
    [JsonPropertyName("brightness")]
    public int? Brightness { get; set; }
}

public class PatchRequest
{
    [Required]
    [MinLength(1)]
    [JsonPropertyName("lightUpdates")]
    public required List<LightUpdateRequest> LightUpdates { get; set; }
}


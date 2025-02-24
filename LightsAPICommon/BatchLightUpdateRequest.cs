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

public class UpdateLightRequest
{
    [Required]
    [MinLength(1)]
    [Description("light Id to update")]
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

[Description("Response to a batch update request for multiple lights.")]
public class PatchResponse
{
    /// <summary>
    /// Number of successfully updated lights.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed updates.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// List of update results.
    /// </summary>
    public List<UpdateLightResponse> Results { get; set; }

    public PatchResponse(List<UpdateLightResponse> results)
    {
        Results = results;
        SuccessCount = results.Count(r => r.Status == "success");
        FailureCount = results.Count(r => r.Status == "failed");
    }
}

public class UpdateLightResponse(int id, string status, string? error)
{
    /// <summary>
    /// Unique ID of the light that was updated.
    /// </summary>
    /// [description("Unique ID of the light that was updated")]
    public int Id { get; set; } = id;

    /// <summary>
    /// Indicates whether the update was successful ("success" or "failed").
    /// </summary>
    [Description("Indicates whether the update was successful (\"success\" or \"failed\")")]
    public string Status { get; set; } = status;

    /// <summary>
    /// Detailed error message if the update failed.
    /// </summary>
    [Description("Detailed error message if the update failed")]
    public string? Error { get; set; } = error;
}
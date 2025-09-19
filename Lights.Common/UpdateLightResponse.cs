using System.ComponentModel;

namespace Lights.Common;

public class UpdateLightResponse(int lightId, string status, string? error)
{
    /// <summary>
    /// Unique ID of the light that was updated.
    /// </summary>
    [Description("Unique Id of the light that was updated")]
    public int LightId { get; set; } = lightId;

    /// <summary>
    /// Indicates whether the update was successful ("success" or "failed").
    /// </summary>
    [Description("Indicates whether the update was successful ('success' or 'failed')")]
    public string Status { get; set; } = status;

    /// <summary>
    /// Detailed error message if the update failed.
    /// </summary>
    [Description("Detailed error message if the update status is failed")]
    public string? Error { get; set; } = error;
}
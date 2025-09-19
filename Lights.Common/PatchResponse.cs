using System.ComponentModel;

namespace Lights.Common;

[Description("Response to a batch update request for multiple lights.")]
public class PatchResponse
{
    /// <summary>
    /// Number of successfully updated lights.
    /// </summary>
    [Description("Number of successfully updated lights.")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed updates.
    /// </summary>
    [Description("Number of failed updates.")]
    public int FailureCount { get; set; }

    /// <summary>
    /// List of update results.
    /// </summary>
    [Description("List of update results.")]
    public UpdateLightResponse[] Results { get; set; }

    public PatchResponse(UpdateLightResponse[] results)
    {
        Results = results;
        SuccessCount = results.Count(r => r.Status == "success");
        FailureCount = results.Count(r => r.Status == "failed");
    }
}

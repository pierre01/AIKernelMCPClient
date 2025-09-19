using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Lights.Common;

/// <summary>
/// Defines the capabilities of a light, such as dimmability and color-changing support.
/// </summary>
public class Capabilities(bool isDimmable=false, bool canChangeColor = false)
{

    [Description("Indicates whether the light supports color changes. If True, Color can be adjusted. If False, Color remains fixed and cannot be altered")]
    public bool CanChangeColor { get; set; } = canChangeColor;

    [Description("Indicates whether the light supports brightness adjustment. If True, Brightness can be adjusted. If False, brightness remains fixed and cannot be altered.")]
    public bool IsDimmable { get; set; } = isDimmable;
}


using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LightsAPICommon;

/// <summary>
/// Defines the capabilities of a light, such as dimmability and color-changing support.
/// </summary>
[JsonSerializable(typeof(Capabilities))]
public class Capabilities(bool isDimmable=false, bool canChangeColor = false)
{

    [Display(Name = "Can Change Color", Description = "Indicates if the light supports color changes")]
    public bool CanChangeColor { get; set; } = canChangeColor;

    [Display(Name = "Is Dimmable", Description = "Indicates if the light supports brightness adjustment")]
    public bool IsDimmable { get; set; } = isDimmable;
}


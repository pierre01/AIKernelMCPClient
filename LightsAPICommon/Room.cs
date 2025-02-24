using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LightsAPICommon;

[Description("A Room where one or more lights are located, The house contains all the rooms and lights in the system")]
/// <summary>
/// Represents a room in the building with a unique identifier and floor location.
/// </summary>
public class Room(int roomId, string name, int floor = 1)
{
    [Required]
    [Range(0, int.MaxValue)]
    [Description("Unique identifier for the room")]
    public int RoomId { get; set; } = roomId;

    [Required]
    [StringLength(100)]
    [Description("Room Name")]
    public string Name { get; set; } = name;

    [Range(-5, 500)]
    [Description("Floor number where the Room is located")]
    public int Floor { get; set; } = floor;
}


using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LightsAPICommon;

[JsonSerializable(typeof(Room))]
[Description("A Room where one or more lights are located")]
public class Room(int roomId, string name, int floor =1)
{
    [Key]
    [Description("RoomId: Room unique Id, related to the location of a light. It matches the Light property 'RoomId'")]
    public int RoomId { get; set; } = roomId;

    [Required]
    [Description("Name: The Room or area name where lights are located")]
    public string Name { get; set; } = name;

    [DefaultValue(1)]
    [Description("Floor: The floor where the room is located")]
    public int Floor { get; set; } = floor;
}


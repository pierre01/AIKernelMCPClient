using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LightsAPICommon;

[JsonSerializable(typeof(Room))]
[Description("A Room where one or more lights are located")]
public class Room(int id, string name)
{
    [Key]
    [Description("Room unique RoomId, related to the location of a light. It matches the Light parameter RoomId")]
    public int RoomId { get; set; } = id;

    [Required]
    [Description("The Room name where lights are located")]
    public string Name { get; set; } = name;
}


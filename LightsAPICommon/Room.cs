using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LightsAPICommon;

[JsonSerializable(typeof(Room))]
[Description("A Room where one or more lights are located")]
public class Room(int id, string name)
{
    [Key]
    [Description("RoomId: Room unique Id, related to the location of a light. It matches the Light property 'RoomId'")]
    public int RoomId { get; set; } = id;

    [Required]
    [Description("Name: The Room or area name where lights are located")]
    public string Name { get; set; } = name;
}


using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LightsAPICommon;

public class Room(int id, string name)
{
    [Key]
    [Description("Room unique Id, related to the location of a light. It matches the Light parameter RoomId")]
    public int Id { get; set; } = id;

    [Required]
    [Description("The Room name where lights are located")]
    public string Name { get; set; } = name;
}


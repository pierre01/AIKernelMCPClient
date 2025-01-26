using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class Room(int id, string name)
{
    [Key]
    [Description("Room unique Id, related to the location of a light. It matches the Light parameter RoomId")]
    public int Id { get; set; } = id;

    [Required]
    [Description("The Room name where lights are located")]
    public string Name { get; set; } = name;
}

//{
//   "isOn": true,
//   "hexColor": "#FF0000",
//   "brightness": 100,
//   "fadeDurationInMilliseconds": 500,
//   "scheduledTime": "2023-07-12T12:00:00Z"
//}
using System.ComponentModel.DataAnnotations;

namespace TodoApp.API.DTOs;

public class CreateReminderDto
{
    [Required]
    public DateTime ReminderTime { get; set; }
}
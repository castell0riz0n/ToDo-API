namespace TodoApp.API.DTOs;

public class ReminderDto
{
    public Guid Id { get; set; }
    public DateTime ReminderTime { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
}
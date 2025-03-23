namespace TeamA.ToDo.Core.Models.Todo;

public class TaskReminder
{
    public Guid Id { get; set; }
    public Guid TodoTaskId { get; set; }
    public TodoTask TodoTask { get; set; }
    public DateTime ReminderTime { get; set; }
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
}
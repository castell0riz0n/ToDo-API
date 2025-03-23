using TeamA.ToDo.Core.Shared.Enums.Todo;

namespace TeamA.ToDo.Core.Models.Todo;

public class TodoTask
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TodoTaskStatus Status { get; set; } = TodoTaskStatus.NotStarted;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string UserId { get; set; }  // Foreign key to AspNetUsers
    public ApplicationUser User { get; set; }
    public Guid? CategoryId { get; set; }
    public TaskCategory Category { get; set; }
    public bool IsRecurring { get; set; } = false;
    public RecurrenceInfo RecurrenceInfo { get; set; }
    public ICollection<TaskReminder> Reminders { get; set; } = new List<TaskReminder>();
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    public ICollection<TodoNote> Notes { get; set; } = new List<TodoNote>();
}
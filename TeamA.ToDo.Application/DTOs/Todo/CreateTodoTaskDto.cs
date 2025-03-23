using System.ComponentModel.DataAnnotations;
using TeamA.ToDo.Core.Shared.Enums.Todo;

namespace TodoApp.API.DTOs;

public class CreateTodoTaskDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public TodoTaskStatus Status { get; set; } = TodoTaskStatus.NotStarted;

    public DateTime? DueDate { get; set; }

    public Guid? CategoryId { get; set; }

    public bool IsRecurring { get; set; } = false;

    public CreateRecurrenceInfoDto RecurrenceInfo { get; set; }

    public List<CreateReminderDto> Reminders { get; set; }

    public List<string> Tags { get; set; }
}
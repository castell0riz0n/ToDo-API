using System.ComponentModel.DataAnnotations;
using TeamA.ToDo.Core.Shared.Enums.Todo;

namespace TodoApp.API.DTOs;

public class UpdateTodoTaskDto
{
    [StringLength(200)]
    public string Title { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    public TaskPriority? Priority { get; set; }

    public TodoTaskStatus? Status { get; set; }

    public DateTime? DueDate { get; set; }

    public Guid? CategoryId { get; set; }

    public bool? IsRecurring { get; set; }

    public UpdateRecurrenceInfoDto RecurrenceInfo { get; set; }
}
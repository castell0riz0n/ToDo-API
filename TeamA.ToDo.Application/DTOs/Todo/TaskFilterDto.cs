using TeamA.ToDo.Core.Shared.Enums.Todo;

namespace TodoApp.API.DTOs;

public class TaskFilterDto
{
    public string SearchTerm { get; set; }
    public TaskPriority? Priority { get; set; }
    public TodoTaskStatus? Status { get; set; }
    public Guid? CategoryId { get; set; }
    public List<string> Tags { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? IsOverdue { get; set; }
    public bool? IsCompleted { get; set; }
    public bool? IsRecurring { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "DueDate";
    public bool SortAscending { get; set; } = true;
}
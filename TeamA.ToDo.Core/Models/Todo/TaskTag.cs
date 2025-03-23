namespace TeamA.ToDo.Core.Models.Todo;

public class TaskTag
{
    public Guid Id { get; set; }
    public Guid TodoTaskId { get; set; }
    public TodoTask TodoTask { get; set; }
    public Guid TagId { get; set; }
    public Tag Tag { get; set; }
}
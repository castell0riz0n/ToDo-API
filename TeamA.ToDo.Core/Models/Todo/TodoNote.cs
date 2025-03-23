namespace TeamA.ToDo.Core.Models.Todo;

public class TodoNote
{
    public Guid Id { get; set; }
    public Guid TodoTaskId { get; set; }
    public TodoTask TodoTask { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
}
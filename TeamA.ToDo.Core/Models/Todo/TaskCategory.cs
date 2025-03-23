namespace TeamA.ToDo.Core.Models.Todo;

public class TaskCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; } // Hex color code
    public string UserId { get; set; }  // Categories are per user
    public ApplicationUser User { get; set; }
    public ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();
}
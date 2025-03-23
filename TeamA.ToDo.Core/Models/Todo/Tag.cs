namespace TeamA.ToDo.Core.Models.Todo;

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string UserId { get; set; }  // Tags are per user
    public ApplicationUser User { get; set; }
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
}

namespace TodoApp.API.DTOs;

public class TaskCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public int TaskCount { get; set; }
}
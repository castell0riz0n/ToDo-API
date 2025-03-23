namespace TodoApp.API.DTOs;

public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int TaskCount { get; set; }
}
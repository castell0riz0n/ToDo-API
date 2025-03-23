using System.ComponentModel.DataAnnotations;

namespace TodoApp.API.DTOs;

public class CreateTodoNoteDto
{
    [Required]
    [StringLength(2000)]
    public string Content { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace TodoApp.API.DTOs;

public class CreateTagDto
{
    [Required]
    [StringLength(30)]
    public string Name { get; set; }
}
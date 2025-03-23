using System.ComponentModel.DataAnnotations;

namespace TodoApp.API.DTOs;

public class UpdateTaskCategoryDto
{
    [StringLength(50)]
    public string Name { get; set; }

    [StringLength(7)]
    public string Color { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.Expenses;

public class UpdateExpenseCategoryDto
{
    [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
    public string Name { get; set; }

    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string Description { get; set; }

    [StringLength(7, ErrorMessage = "Color code cannot exceed 7 characters")]
    [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Color must be a valid hex color code (e.g., #FFFFFF)")]
    public string Color { get; set; }

    [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
    public string Icon { get; set; }
}
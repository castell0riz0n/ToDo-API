using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.Expenses;

public class UpdatePaymentMethodDto
{
    [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
    public string Name { get; set; }

    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string Description { get; set; }

    [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
    public string Icon { get; set; }

    public bool? IsDefault { get; set; }
}
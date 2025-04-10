using System.ComponentModel.DataAnnotations;
using TeamA.ToDo.Core.Shared.Enums.Expenses;

namespace TeamA.ToDo.Application.DTOs.Expenses;

public class CreateBudgetDto
{
    [Required]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    public Guid? CategoryId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public BudgetPeriod Period { get; set; } = BudgetPeriod.Monthly;
}
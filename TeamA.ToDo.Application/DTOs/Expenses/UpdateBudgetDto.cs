using System.ComponentModel.DataAnnotations;
using TeamA.ToDo.Core.Shared.Enums.Expenses;

namespace TeamA.ToDo.Application.DTOs.Expenses;

public class UpdateBudgetDto
{
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal? Amount { get; set; }

    public Guid? CategoryId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public BudgetPeriod? Period { get; set; }
}
using System.ComponentModel.DataAnnotations;
using TeamA.ToDo.Core.Shared.Enums.Expenses;

namespace TeamA.ToDo.Application.DTOs.Expenses;

public class UpdateExpenseRecurrenceDto
{
    public RecurrenceType? RecurrenceType { get; set; }

    [Range(1, 366, ErrorMessage = "Interval must be between 1 and 366")]
    public int? Interval { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(100, ErrorMessage = "Custom cron expression cannot exceed 100 characters")]
    public string CustomCronExpression { get; set; }

    [Range(1, 31, ErrorMessage = "Day of month must be between 1 and 31")]
    public int? DayOfMonth { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }
}
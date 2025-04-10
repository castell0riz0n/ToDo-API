using TeamA.ToDo.Core.Shared.Enums.Expenses;

namespace TeamA.ToDo.Application.DTOs.Expenses;

public class ExpenseRecurrenceDto
{
    public Guid Id { get; set; }
    public RecurrenceType RecurrenceType { get; set; }
    public int Interval { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? LastProcessedDate { get; set; }
    public DateTime? NextProcessingDate { get; set; }
    public string CustomCronExpression { get; set; }
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
}
using TeamA.ToDo.Core.Shared.Enums.Expenses;

namespace TeamA.ToDo.Core.Models.Expenses;

public class ExpenseRecurrence
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Expense Expense { get; set; }
    public RecurrenceType RecurrenceType { get; set; }
    public int Interval { get; set; } = 1;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? LastProcessedDate { get; set; }
    public DateTime? NextProcessingDate { get; set; }
    public string CustomCronExpression { get; set; }
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
}
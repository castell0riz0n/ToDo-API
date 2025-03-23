using TeamA.ToDo.Core.Shared.Enums.Todo;

namespace TeamA.ToDo.Core.Models.Todo;

public class RecurrenceInfo
{
    public Guid Id { get; set; }
    public Guid TodoTaskId { get; set; }
    public TodoTask TodoTask { get; set; }
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public int Interval { get; set; } = 1; // Every X days/weeks/months
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string CustomCronExpression { get; set; } // For advanced recurrence patterns
    public int? DayOfMonth { get; set; } // For monthly recurrence
    public DayOfWeek? DayOfWeek { get; set; } // For weekly recurrence
}
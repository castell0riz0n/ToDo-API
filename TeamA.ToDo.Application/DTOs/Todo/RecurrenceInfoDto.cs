using TeamA.ToDo.Core.Shared.Enums.Todo;

namespace TodoApp.API.DTOs;

public class RecurrenceInfoDto
{
    public Guid Id { get; set; }
    public RecurrenceType RecurrenceType { get; set; }
    public int Interval { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string CustomCronExpression { get; set; }
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
}
using System.ComponentModel.DataAnnotations;
using TeamA.ToDo.Core.Shared.Enums.Todo;

namespace TodoApp.API.DTOs;

public class CreateRecurrenceInfoDto
{
    [Required]
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;

    public int Interval { get; set; } = 1;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(100)]
    public string CustomCronExpression { get; set; }

    public int? DayOfMonth { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }
}
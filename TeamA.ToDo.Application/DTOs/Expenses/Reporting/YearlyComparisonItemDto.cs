namespace TeamA.ToDo.Application.DTOs.Expenses.Reporting;

public class YearlyComparisonItemDto
{
    public decimal Year1Amount { get; set; }
    public decimal Year2Amount { get; set; }
    public decimal PercentageChange { get; set; }
}
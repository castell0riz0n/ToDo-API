namespace TeamA.ToDo.Application.DTOs.Expenses.Reporting;

public class YearlyComparisonReportDto
{
    public int Year1 { get; set; }
    public int Year2 { get; set; }
    public decimal Year1Total { get; set; }
    public decimal Year2Total { get; set; }
    public decimal PercentageChange { get; set; }
    public Dictionary<string, YearlyComparisonItemDto> MonthlyComparison { get; set; }
    public Dictionary<string, YearlyComparisonItemDto> CategoryComparison { get; set; }
}
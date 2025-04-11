namespace TeamA.ToDo.Application.DTOs.Expenses.Reporting;

public class TrendAnalysisDto
{
    public List<MonthlyTrendDto> MonthlyTrends { get; set; }
    public Dictionary<string, List<MonthlyTrendDto>> CategoryTrends { get; set; }
    public TrendSummaryDto Summary { get; set; }
}
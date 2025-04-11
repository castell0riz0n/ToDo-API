namespace TeamA.ToDo.Application.DTOs.Expenses.Reporting;

public class TrendSummaryDto
{
    public decimal AverageMonthlyExpense { get; set; }
    public decimal MedianMonthlyExpense { get; set; }
    public decimal ExpenseGrowthRate { get; set; }
    public string TopGrowingCategory { get; set; }
    public string TopShrinkingCategory { get; set; }
}
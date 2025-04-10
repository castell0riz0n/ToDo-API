namespace TeamA.ToDo.Application.DTOs.Expenses;

public class BudgetSummaryDto
{
    public decimal TotalBudgetAmount { get; set; }
    public decimal TotalSpentAmount { get; set; }
    public decimal TotalRemainingAmount { get; set; }
    public double TotalSpentPercentage { get; set; }
    public List<BudgetDto> Budgets { get; set; }
    public Dictionary<string, decimal> SpendingByCategory { get; set; }
    public Dictionary<string, BudgetStatusDto> BudgetStatus { get; set; }
}
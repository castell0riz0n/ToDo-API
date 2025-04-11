namespace TeamA.ToDo.Application.DTOs.Expenses.Reporting;

public class MonthlyTrendDto
{
    public DateTime Month { get; set; }
    public decimal TotalAmount { get; set; }
    public int ExpenseCount { get; set; }
    public decimal AverageExpense { get; set; }
}
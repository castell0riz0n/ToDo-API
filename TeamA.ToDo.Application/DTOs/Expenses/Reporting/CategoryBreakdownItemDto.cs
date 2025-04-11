namespace TeamA.ToDo.Application.DTOs.Expenses.Reporting;

public class CategoryBreakdownItemDto
{
    public string CategoryName { get; set; }
    public string CategoryColor { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public int ExpenseCount { get; set; }
    public decimal AverageExpenseAmount { get; set; }
}
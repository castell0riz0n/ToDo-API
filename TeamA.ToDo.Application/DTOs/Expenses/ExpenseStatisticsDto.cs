namespace TeamA.ToDo.Application.DTOs.Expenses;

public class ExpenseStatisticsDto
{
    public decimal TotalExpenses { get; set; }
    public decimal AverageExpenseAmount { get; set; }
    public decimal HighestExpenseAmount { get; set; }
    public decimal LowestExpenseAmount { get; set; }
    public int ExpenseCount { get; set; }
    public decimal TotalRecurringExpenses { get; set; }
    public Dictionary<string, decimal> ExpensesByCategory { get; set; }
    public Dictionary<string, decimal> ExpensesByPaymentMethod { get; set; }
    public Dictionary<string, decimal> ExpensesByMonth { get; set; }
    public Dictionary<string, decimal> ExpensesByDay { get; set; }
    public List<TopExpenseDto> TopExpenses { get; set; }
}
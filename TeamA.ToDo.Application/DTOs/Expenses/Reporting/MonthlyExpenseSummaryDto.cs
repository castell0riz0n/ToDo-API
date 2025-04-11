namespace TeamA.ToDo.Application.DTOs.Expenses.Reporting
{
    public class MonthlyExpenseSummaryDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public decimal TotalAmount { get; set; }
        public int ExpenseCount { get; set; }
        public Dictionary<string, decimal> CategoryBreakdown { get; set; }
        public List<TopExpenseDto> TopExpenses { get; set; }
    }
}
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.Expenses.Reporting;
using TeamA.ToDo.Application.DTOs.General;

namespace TeamA.ToDo.Application.Interfaces.Expenses
{
    public interface IExpenseService
    {
        Task<PagedResponse<ExpenseDto>> GetExpensesAsync(string userId, ExpenseFilterDto filter);
        Task<ServiceResponse<ExpenseDto>> GetExpenseByIdAsync(Guid id, string userId);
        Task<ServiceResponse<ExpenseDto>> CreateExpenseAsync(string userId, CreateExpenseDto dto);
        Task<ServiceResponse<ExpenseDto>> UpdateExpenseAsync(Guid id, string userId, UpdateExpenseDto dto);
        Task<ServiceResponse<bool>> DeleteExpenseAsync(Guid id, string userId);
        Task<ServiceResponse<ExpenseStatisticsDto>> GetExpenseStatisticsAsync(string userId, DateTime? startDate, DateTime? endDate);
        Task<ServiceResponse<PagedResponse<ExpenseDto>>> GetAllExpensesAsync(ExpenseFilterDto filter);
        Task<ServiceResponse<List<MonthlyExpenseSummaryDto>>> GetMonthlyExpenseReportAsync(string userId, int year);
        Task<ServiceResponse<YearlyComparisonReportDto>> GetYearlyComparisonReportAsync(string userId, int year1, int year2);
        Task<ServiceResponse<CategoryBreakdownReportDto>> GetCategoryBreakdownReportAsync(string userId, DateTime startDate, DateTime endDate);
        Task<ServiceResponse<TrendAnalysisDto>> GetExpenseTrendAnalysisAsync(string userId, int months);
    }
}
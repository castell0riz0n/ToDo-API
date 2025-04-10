using TeamA.ToDo.Application.DTOs.Expenses;
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
    }
}
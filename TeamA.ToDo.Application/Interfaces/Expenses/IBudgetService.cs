using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;

namespace TeamA.ToDo.Application.Interfaces.Expenses;

public interface IBudgetService
{
    Task<ServiceResponse<List<BudgetDto>>> GetBudgetsAsync(string userId);
    Task<ServiceResponse<BudgetDto>> GetBudgetByIdAsync(Guid id, string userId);
    Task<ServiceResponse<BudgetDto>> CreateBudgetAsync(string userId, CreateBudgetDto dto);
    Task<ServiceResponse<BudgetDto>> UpdateBudgetAsync(Guid id, string userId, UpdateBudgetDto dto);
    Task<ServiceResponse<bool>> DeleteBudgetAsync(Guid id, string userId);
    Task<ServiceResponse<BudgetSummaryDto>> GetBudgetSummaryAsync(string userId, DateTime? month = null);
}
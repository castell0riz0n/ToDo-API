using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;

public interface IBudgetAlertSettingsService
{
    Task<ServiceResponse<BudgetAlertSettingsDto>> GetBudgetAlertSettingsAsync(string userId);
    Task<ServiceResponse<BudgetAlertSettingsDto>> UpdateBudgetAlertSettingsAsync(string userId, BudgetAlertSettingsDto dto);
}
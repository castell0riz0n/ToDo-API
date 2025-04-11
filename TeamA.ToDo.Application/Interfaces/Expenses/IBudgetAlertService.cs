using TeamA.ToDo.Application.DTOs.General;

namespace TeamA.ToDo.Application.Interfaces.Expenses;

public interface IBudgetAlertService
{
    Task<ServiceResponse<bool>> CheckBudgetAlertsAsync();
    Task<ServiceResponse<bool>> SendBudgetSummaryEmailAsync(string userId);
    Task<ServiceResponse<bool>> EnableBudgetAlertsAsync(string userId, bool enabled, decimal? thresholdPercentage = null);
}
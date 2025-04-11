using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Core.Models.Expenses;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services.Expenses;

public class BudgetAlertSettingsService : IBudgetAlertSettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BudgetAlertSettingsService> _logger;

    public BudgetAlertSettingsService(
        ApplicationDbContext context,
        ILogger<BudgetAlertSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResponse<BudgetAlertSettingsDto>> GetBudgetAlertSettingsAsync(string userId)
    {
        var response = new ServiceResponse<BudgetAlertSettingsDto>();

        try
        {
            // Get or create budget alert settings
            var settings = await _context.BudgetAlertSettings
                .FirstOrDefaultAsync(bas => bas.UserId == userId);

            if (settings == null)
            {
                // Create default settings if they don't exist
                settings = new BudgetAlertSetting
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EnableAlerts = true,
                    ThresholdPercentage = 80,
                    SendMonthlySummary = true
                };

                await _context.BudgetAlertSettings.AddAsync(settings);
                await _context.SaveChangesAsync();
            }

            var dto = new BudgetAlertSettingsDto
            {
                EnableAlerts = settings.EnableAlerts,
                ThresholdPercentage = settings.ThresholdPercentage,
                SendMonthlySummary = settings.SendMonthlySummary
            };

            response.Data = dto;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving budget alert settings");
            response.Success = false;
            response.Message = "Failed to retrieve budget alert settings";
            return response;
        }
    }

    public async Task<ServiceResponse<BudgetAlertSettingsDto>> UpdateBudgetAlertSettingsAsync(string userId, BudgetAlertSettingsDto dto)
    {
        var response = new ServiceResponse<BudgetAlertSettingsDto>();

        try
        {
            // Get or create budget alert settings
            var settings = await _context.BudgetAlertSettings
                .FirstOrDefaultAsync(bas => bas.UserId == userId);

            if (settings == null)
            {
                // Create new settings if they don't exist
                settings = new BudgetAlertSetting
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EnableAlerts = dto.EnableAlerts,
                    ThresholdPercentage = dto.ThresholdPercentage,
                    SendMonthlySummary = dto.SendMonthlySummary
                };

                await _context.BudgetAlertSettings.AddAsync(settings);
            }
            else
            {
                // Update existing settings
                settings.EnableAlerts = dto.EnableAlerts;
                settings.ThresholdPercentage = dto.ThresholdPercentage;
                settings.SendMonthlySummary = dto.SendMonthlySummary;
            }

            await _context.SaveChangesAsync();

            response.Data = dto;
            response.Message = "Budget alert settings updated successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating budget alert settings");
            response.Success = false;
            response.Message = "Failed to update budget alert settings";
            return response;
        }
    }
}
namespace TeamA.ToDo.Application.DTOs.Expenses;

public class BudgetAlertSettingsDto
{
    public bool EnableAlerts { get; set; } = true;
    public decimal ThresholdPercentage { get; set; } = 80; // Default to 80%
    public bool SendMonthlySummary { get; set; } = true;
}
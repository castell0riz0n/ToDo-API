namespace TeamA.ToDo.Core.Models.Expenses;

public class BudgetAlertSetting
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public bool EnableAlerts { get; set; } = true;
    public decimal ThresholdPercentage { get; set; } = 80; // Default to 80%
    public bool SendMonthlySummary { get; set; } = true;
    public DateTime? LastAlertSent { get; set; }
    public DateTime? LastSummarySent { get; set; }
}
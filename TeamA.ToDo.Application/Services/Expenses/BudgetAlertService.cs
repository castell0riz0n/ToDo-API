// Add new file: TeamA.ToDo.Application/Services/Expenses/BudgetAlertService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Application.Interfaces.Expenses;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.Expenses;
using TeamA.ToDo.Core.Models.General;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services.Expenses
{
    public class BudgetAlertService : IBudgetAlertService
    {
        private readonly ApplicationDbContext _context;
        private readonly IApplicationEmailSender _emailSender;
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<BudgetAlertService> _logger;
        private readonly AppConfig _appConfig;

        public BudgetAlertService(
            ApplicationDbContext context,
            IApplicationEmailSender emailSender,
            IEmailTemplateService emailTemplateService,
            IOptions<AppConfig> appConfig,
            ILogger<BudgetAlertService> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _templateService = emailTemplateService;
            _logger = logger;
            _appConfig = appConfig.Value;
        }

        public async Task<ServiceResponse<bool>> CheckBudgetAlertsAsync()
        {
            var response = new ServiceResponse<bool>();

            try
            {
                // Get all users with budget alert settings enabled
                var alertSettings = await _context.BudgetAlertSettings
                    .Where(bas => bas.EnableAlerts)
                    .Include(bas => bas.User)
                    .ToListAsync();

                int alertCount = 0;

                foreach (var settings in alertSettings)
                {
                    // Skip if user email is not confirmed
                    if (!settings.User.EmailConfirmed || string.IsNullOrEmpty(settings.User.Email))
                    {
                        continue;
                    }

                    // Get active budgets for the user
                    var today = DateTime.UtcNow.Date;
                    var budgets = await _context.Budgets
                        .Include(b => b.Category)
                        .Where(b => b.UserId == settings.UserId &&
                                    b.StartDate <= today &&
                                    b.EndDate >= today)
                        .ToListAsync();

                    if (!budgets.Any())
                    {
                        continue;
                    }

                    // Get expenses that fall within the budget periods
                    var expenses = await _context.Expenses
                        .Where(e => e.UserId == settings.UserId &&
                                    e.Date >= budgets.Min(b => b.StartDate) &&
                                    e.Date <= budgets.Max(b => b.EndDate))
                        .ToListAsync();

                    // Check each budget
                    var alertedBudgets = new List<(Budget Budget, decimal SpentAmount, decimal SpentPercentage)>();

                    foreach (var budget in budgets)
                    {
                        var budgetExpenses = expenses
                            .Where(e => e.CategoryId == budget.CategoryId &&
                                        e.Date >= budget.StartDate &&
                                        e.Date <= budget.EndDate)
                            .ToList();

                        var spentAmount = budgetExpenses.Sum(e => e.Amount);
                        var spentPercentage = budget.Amount > 0 ? (spentAmount / budget.Amount) * 100 : 0;

                        // Check if the budget is over the threshold
                        if (spentPercentage >= settings.ThresholdPercentage)
                        {
                            alertedBudgets.Add((budget, spentAmount, spentPercentage));
                        }
                    }

                    // Send alert if any budgets are over threshold
                    if (alertedBudgets.Any())
                    {
                        await SendBudgetAlertEmailAsync(settings.User, alertedBudgets);
                        alertCount++;

                        // Update last alert sent timestamp
                        settings.LastAlertSent = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                response.Data = true;
                response.Message = $"Budget alerts checked. {alertCount} alerts sent.";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking budget alerts");
                response.Success = false;
                response.Message = "Failed to check budget alerts";
                return response;
            }
        }

        public async Task<ServiceResponse<bool>> SendBudgetSummaryEmailAsync(string userId)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                // Get user and settings
                var settings = await _context.BudgetAlertSettings
                    .Include(bas => bas.User)
                    .FirstOrDefaultAsync(bas => bas.UserId == userId);

                if (settings == null || !settings.SendMonthlySummary)
                {
                    response.Success = false;
                    response.Message = "Budget summary emails not enabled for this user";
                    return response;
                }

                var user = settings.User;

                // Skip if user email is not confirmed
                if (!user.EmailConfirmed || string.IsNullOrEmpty(user.Email))
                {
                    response.Success = false;
                    response.Message = "User email not confirmed";
                    return response;
                }

                // Get current month budgets
                var today = DateTime.UtcNow.Date;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var budgets = await _context.Budgets
                    .Include(b => b.Category)
                    .Where(b => b.UserId == userId &&
                                ((b.StartDate <= endOfMonth &&
                                  b.EndDate >= startOfMonth) || // Overlaps with current month
                                 (b.StartDate >= startOfMonth && b.StartDate <= endOfMonth))) // Starts this month
                    .ToListAsync();

                if (!budgets.Any())
                {
                    response.Success = true;
                    response.Message = "No budgets found for the current month";
                    return response;
                }

                // Get expenses for this month
                var expenses = await _context.Expenses
                    .Where(e => e.UserId == userId &&
                                e.Date >= startOfMonth &&
                                e.Date <= endOfMonth)
                    .ToListAsync();

                // Calculate budget utilization
                var budgetSummaries =
                    new List<(Budget Budget, decimal SpentAmount, decimal SpentPercentage, decimal RemainingAmount)>();

                foreach (var budget in budgets)
                {
                    // Calculate the portion of the budget that falls within this month
                    var budgetStartInMonth = budget.StartDate < startOfMonth ? startOfMonth : budget.StartDate;
                    var budgetEndInMonth = budget.EndDate > endOfMonth ? endOfMonth : budget.EndDate;

                    var daysInMonth = (endOfMonth - startOfMonth).TotalDays + 1;
                    var daysInBudgetPeriod = (budget.EndDate - budget.StartDate).TotalDays + 1;
                    var daysOverlap = (budgetEndInMonth - budgetStartInMonth).TotalDays + 1;

                    var proRatedFactor = (decimal)(daysOverlap / daysInBudgetPeriod);
                    var monthlyBudgetAmount = budget.Amount * proRatedFactor;

                    // Get expenses for this budget and time period
                    var budgetExpenses = expenses
                        .Where(e => e.CategoryId == budget.CategoryId)
                        .ToList();

                    var spentAmount = budgetExpenses.Sum(e => e.Amount);
                    var spentPercentage = monthlyBudgetAmount > 0 ? (spentAmount / monthlyBudgetAmount) * 100 : 0;
                    var remainingAmount = monthlyBudgetAmount - spentAmount;

                    budgetSummaries.Add((budget, spentAmount, spentPercentage, remainingAmount));
                }

                // Calculate overall spending
                var totalBudgeted = budgetSummaries.Sum(b => b.Budget.Amount);
                var totalSpent = expenses.Sum(e => e.Amount);
                var totalRemaining = totalBudgeted - totalSpent;
                var overallPercentage = totalBudgeted > 0 ? (totalSpent / totalBudgeted) * 100 : 0;

                // Send the summary email
                await SendBudgetSummaryEmailAsync(user, budgetSummaries, startOfMonth);

                // Update last summary sent timestamp
                settings.LastSummarySent = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                response.Data = true;
                response.Message = "Budget summary email sent successfully";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending budget summary email");
                response.Success = false;
                response.Message = "Failed to send budget summary email";
                return response;
            }
        }

        public async Task<ServiceResponse<bool>> EnableBudgetAlertsAsync(string userId, bool enabled,
            decimal? thresholdPercentage = null)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                // Get or create budget alert settings
                var settings = await _context.BudgetAlertSettings
                    .FirstOrDefaultAsync(bas => bas.UserId == userId);

                if (settings == null)
                {
                    settings = new BudgetAlertSetting
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        EnableAlerts = enabled,
                        ThresholdPercentage = thresholdPercentage ?? 80
                    };

                    await _context.BudgetAlertSettings.AddAsync(settings);
                }
                else
                {
                    settings.EnableAlerts = enabled;

                    if (thresholdPercentage.HasValue)
                    {
                        settings.ThresholdPercentage = thresholdPercentage.Value;
                    }
                }

                await _context.SaveChangesAsync();

                response.Data = true;
                response.Message =
                    enabled ? "Budget alerts enabled successfully" : "Budget alerts disabled successfully";
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

        #region Helper Methods

        private async Task SendBudgetAlertEmailAsync(
            ApplicationUser user,
            List<(Budget Budget, decimal SpentAmount, decimal SpentPercentage)> alertedBudgets)
        {
            try
            {
                var overSpentBudgets = alertedBudgets.Where(b => b.SpentPercentage >= 100).ToList();
                var approachingBudgets = alertedBudgets.Where(b => b.SpentPercentage < 100).ToList();

                string subject;
                if (overSpentBudgets.Any())
                {
                    subject =
                        $"ALERT: {overSpentBudgets.Count} Budget{(overSpentBudgets.Count > 1 ? "s" : "")} Exceeded";
                }
                else
                {
                    subject =
                        $"Warning: {approachingBudgets.Count} Budget{(approachingBudgets.Count > 1 ? "s" : "")} Approaching Limit";
                }

                // Build email content
                var emailBuilder = new System.Text.StringBuilder();

                emailBuilder.AppendLine($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Budget Alert</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .budget-item {{
            margin-bottom: 15px;
            padding: 15px;
            border-radius: 5px;
        }}
        .over-budget {{
            background-color: #FFEBE9;
            border: 1px solid #FFCDD2;
        }}
        .warning {{
            background-color: #FFF9E6;
            border: 1px solid #FFECB3;
        }}
        .budget-name {{
            font-weight: bold;
            font-size: 16px;
            margin-bottom: 5px;
        }}
        .budget-details {{
            margin-bottom: 10px;
        }}
        .progress-container {{
            height: 20px;
            background-color: #e0e0e0;
            border-radius: 10px;
            margin-bottom: 5px;
        }}
        .progress-bar {{
            height: 100%;
            border-radius: 10px;
            text-align: center;
            color: white;
            font-weight: bold;
        }}
        .over-budget-bar {{
            background-color: #F44336;
        }}
        .warning-bar {{
            background-color: #FFC107;
        }}
        .action-button {{
            display: inline-block;
            background-color: #4CAF50;
            color: white;
            padding: 10px 20px;
            text-decoration: none;
            border-radius: 5px;
            margin-top: 15px;
        }}
        .footer {{
            margin-top: 20px;
            font-size: 12px;
            text-align: center;
            color: #777;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Budget Alert</h2>
        </div>
        <p>Hi {user.FirstName},</p>
");

                if (overSpentBudgets.Any())
                {
                    emailBuilder.AppendLine($@"
        <p>We wanted to let you know that {overSpentBudgets.Count} of your budgets {(overSpentBudgets.Count > 1 ? "have" : "has")} been exceeded:</p>
        
        {string.Join("", overSpentBudgets.Select(b => $@"
        <div class='budget-item over-budget'>
            <div class='budget-name'>{b.Budget.Name} ({b.Budget.Category?.Name ?? "Uncategorized"})</div>
            <div class='budget-details'>
                <div><strong>Budget:</strong> ${b.Budget.Amount:N2}</div>
                <div><strong>Spent:</strong> ${b.SpentAmount:N2}</div>
                <div><strong>Status:</strong> ${b.SpentAmount - b.Budget.Amount:N2} over budget</div>
            </div>
            <div class='progress-container'>
                <div class='progress-bar over-budget-bar' style='width: 100%;'>{b.SpentPercentage:N0}%</div>
            </div>
        </div>
        "))}");
                }

                if (approachingBudgets.Any())
                {
                    emailBuilder.AppendLine($@"
        <p>The following {(overSpentBudgets.Any() ? "additional " : "")}budget{(approachingBudgets.Count > 1 ? "s are" : " is")} approaching the limit:</p>
        
        {string.Join("", approachingBudgets.Select(b => $@"
        <div class='budget-item warning'>
            <div class='budget-name'>{b.Budget.Name} ({b.Budget.Category?.Name ?? "Uncategorized"})</div>
            <div class='budget-details'>
                <div><strong>Budget:</strong> ${b.Budget.Amount:N2}</div>
                <div><strong>Spent:</strong> ${b.SpentAmount:N2}</div>
                <div><strong>Remaining:</strong> ${b.Budget.Amount - b.SpentAmount:N2}</div>
            </div>
            <div class='progress-container'>
                <div class='progress-bar warning-bar' style='width: {b.SpentPercentage}%;'>{b.SpentPercentage:N0}%</div>
            </div>
        </div>
        "))}");
                }

                emailBuilder.AppendLine($@"
        <p>You can review your budgets and expenses on the {_appConfig.AppName} dashboard.</p>
        
        <div style='text-align: center;'>
            <a href='{_appConfig.BaseUrl}/expenses/budgets' class='action-button'>View Budgets</a>
        </div>
        
        <p>If you'd like to adjust your budget alert settings, you can do so in your account preferences.</p>
        
        <p>Best regards,<br>The {_appConfig.AppName} Team</p>
    </div>
    <div class='footer'>
        <p>This is an automated message. Please do not reply to this email.</p>
        <p>&copy; {DateTime.Now.Year} {_appConfig.AppName}. All rights reserved.</p>
    </div>
</body>
</html>
");

                // Send the email
                await _emailSender.SendEmailAsync(user.Email, subject, emailBuilder.ToString());
                _logger.LogInformation($"Budget alert email sent to {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending budget alert email to {user.Email}");
                throw;
            }
        }

        private async Task SendBudgetSummaryEmailAsync(
            ApplicationUser user,
            List<(Budget Budget, decimal SpentAmount, decimal SpentPercentage, decimal RemainingAmount)> budgetSummaries,
            DateTime monthStart)
        {
            try
            {
                var monthName = monthStart.ToString("MMMM yyyy");
                var subject = $"Monthly Budget Summary for {monthName}";

                // Split budgets into categories
                var overSpentBudgets = budgetSummaries.Where(b => b.SpentPercentage > 100).ToList();
                var healthyBudgets = budgetSummaries.Where(b => b.SpentPercentage <= 100).ToList();

                // Calculate overall totals
                var totalBudgeted = budgetSummaries.Sum(b => b.Budget.Amount);
                var totalSpent = budgetSummaries.Sum(b => b.SpentAmount);
                var totalRemaining = totalBudgeted - totalSpent;
                var overallPercentage = totalBudgeted > 0 ? (totalSpent / totalBudgeted) * 100 : 0;

                // Build email content
                var emailBuilder = new System.Text.StringBuilder();

                emailBuilder.AppendLine($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Monthly Budget Summary</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .summary-box {{
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 15px;
            margin-bottom: 20px;
            background-color: #f5f5f5;
        }}
        .budget-item {{
            margin-bottom: 15px;
            padding: 15px;
            border-radius: 5px;
        }}
        .over-budget {{
            background-color: #FFEBE9;
            border: 1px solid #FFCDD2;
        }}
        .healthy-budget {{
            background-color: #E8F5E9;
            border: 1px solid #C8E6C9;
        }}
        .warning {{
            background-color: #FFF9E6;
            border: 1px solid #FFECB3;
        }}
        .budget-name {{
            font-weight: bold;
            font-size: 16px;
            margin-bottom: 5px;
        }}
        .budget-details {{
            margin-bottom: 10px;
        }}
        .progress-container {{
            height: 20px;
            background-color: #e0e0e0;
            border-radius: 10px;
            margin-bottom: 5px;
        }}
        .progress-bar {{
            height: 100%;
            border-radius: 10px;
            text-align: center;
            color: white;
            font-weight: bold;
        }}
        .over-budget-bar {{
            background-color: #F44336;
        }}
        .healthy-bar {{
            background-color: #4CAF50;
        }}
        .warning-bar {{
            background-color: #FFC107;
        }}
        .action-button {{
            display: inline-block;
            background-color: #4CAF50;
            color: white;
            padding: 10px 20px;
            text-decoration: none;
            border-radius: 5px;
            margin-top: 15px;
        }}
        .footer {{
            margin-top: 20px;
            font-size: 12px;
            text-align: center;
            color: #777;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Budget Summary for {monthName}</h2>
        </div>
        <p>Hi {user.FirstName},</p>
        <p>Here's your monthly budget summary for {monthName}:</p>
        
        <div class='summary-box'>
            <h3>Overall Summary</h3>
            <div><strong>Total Budgeted:</strong> ${totalBudgeted:N2}</div>
            <div><strong>Total Spent:</strong> ${totalSpent:N2}</div>
            <div><strong>Remaining:</strong> ${totalRemaining:N2}</div>
            <div class='progress-container'>
                <div class='progress-bar {(overallPercentage > 100 ? "over-budget-bar" : overallPercentage >= 80 ? "warning-bar" : "healthy-bar")}' 
                     style='width: {Math.Min(overallPercentage, 100):N0}%;'>
                     {overallPercentage:N0}%
                </div>
            </div>
        </div>
");

                // Add over budget section if any
                if (overSpentBudgets.Any())
                {
                    emailBuilder.AppendLine($@"
        <h3>Over Budget ({overSpentBudgets.Count})</h3>
        
        {string.Join("", overSpentBudgets.Select(b => $@"
        <div class='budget-item over-budget'>
            <div class='budget-name'>{b.Budget.Name} ({b.Budget.Category?.Name ?? "Uncategorized"})</div>
            <div class='budget-details'>
                <div><strong>Budget:</strong> ${b.Budget.Amount:N2}</div>
                <div><strong>Spent:</strong> ${b.SpentAmount:N2}</div>
                <div><strong>Status:</strong> ${b.SpentAmount - b.Budget.Amount:N2} over budget</div>
            </div>
            <div class='progress-container'>
                <div class='progress-bar over-budget-bar' style='width: 100%;'>{b.SpentPercentage:N0}%</div>
            </div>
        </div>
        "))}");
                }

                // Add healthy budgets section
                if (healthyBudgets.Any())
                {
                    emailBuilder.AppendLine($@"
        <h3>Healthy Budgets ({healthyBudgets.Count})</h3>
        
        {string.Join("", healthyBudgets.Select(b => $@"
        <div class='budget-item {(b.SpentPercentage >= 80 ? "warning" : "healthy-budget")}'>
            <div class='budget-name'>{b.Budget.Name} ({b.Budget.Category?.Name ?? "Uncategorized"})</div>
            <div class='budget-details'>
                <div><strong>Budget:</strong> ${b.Budget.Amount:N2}</div>
                <div><strong>Spent:</strong> ${b.SpentAmount:N2}</div>
                <div><strong>Remaining:</strong> ${b.RemainingAmount:N2}</div>
            </div>
            <div class='progress-container'>
                <div class='progress-bar {(b.SpentPercentage >= 80 ? "warning-bar" : "healthy-bar")}' 
                     style='width: {b.SpentPercentage}%;'>
                     {b.SpentPercentage:N0}%
                </div>
            </div>
        </div>
        "))}");
                }

                emailBuilder.AppendLine($@"
        <p>You can view your detailed budget information on the {_appConfig.AppName} dashboard.</p>
        
        <div style='text-align: center;'>
            <a href='{_appConfig.BaseUrl}/expenses/budgets' class='action-button'>View Budgets</a>
        </div>
        
        <p>Best regards,<br>The {_appConfig.AppName} Team</p>
    </div>
    <div class='footer'>
        <p>This is an automated message. Please do not reply to this email.</p>
        <p>&copy; {DateTime.Now.Year} {_appConfig.AppName}. All rights reserved.</p>
        <p>You can manage your email preferences in your <a href='{_appConfig.BaseUrl}/account/settings'>account settings</a>.</p>
    </div>
</body>
</html>
");

                // Send the email
                await _emailSender.SendEmailAsync(user.Email, subject, emailBuilder.ToString());
                _logger.LogInformation($"Budget summary email sent to {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending budget summary email to {user.Email}");
                throw;
            }
        }

        #endregion
    }
}
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.Interfaces.Expenses;
using TeamA.ToDo.Core.Models.Expenses;
using TeamA.ToDo.Core.Shared.Enums.Expenses;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services.Expenses;

public class RecurringExpenseService : IRecurringExpenseService
{
    private readonly ApplicationDbContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<RecurringExpenseService> _logger;

    public RecurringExpenseService(
        ApplicationDbContext context,
        IBackgroundJobClient backgroundJobClient,
        ILogger<RecurringExpenseService> logger)
    {
        _context = context;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task ProcessRecurringExpensesAsync()
    {
        try
        {
            _logger.LogInformation("Starting to process recurring expenses");

            // Get all active recurring expenses that need processing
            var today = DateTime.UtcNow.Date;
            var recurringExpenses = await _context.ExpenseRecurrences
                .Include(r => r.Expense)
                .Where(r => 
                    r.Expense.IsRecurring && 
                    (!r.EndDate.HasValue || r.EndDate.Value >= today) &&
                    (!r.NextProcessingDate.HasValue || r.NextProcessingDate.Value <= today))
                .ToListAsync();

            _logger.LogInformation($"Found {recurringExpenses.Count} recurring expenses to process");

            foreach (var recurrence in recurringExpenses)
            {
                await ProcessRecurringExpenseAsync(recurrence);
            }

            _logger.LogInformation("Completed processing recurring expenses");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing recurring expenses");
            throw;
        }
    }

    private async Task ProcessRecurringExpenseAsync(ExpenseRecurrence recurrence)
    {
        try
        {
            var expense = recurrence.Expense;
            if (expense == null)
            {
                _logger.LogWarning($"Recurring expense {recurrence.Id} has no associated expense");
                return;
            }

            // Create a new expense instance
            var newExpense = new Expense
            {
                Id = Guid.NewGuid(),
                Amount = expense.Amount,
                Description = expense.Description,
                Date = DateTime.UtcNow.Date,
                CategoryId = expense.CategoryId,
                UserId = expense.UserId,
                PaymentMethodId = expense.PaymentMethodId,
                IsRecurring = false, // Created instance is not recurring itself
                CreatedAt = DateTime.UtcNow,
                ReceiptUrl = expense.ReceiptUrl
            };

            // Copy tags
            if (expense.Tags != null && expense.Tags.Any())
            {
                foreach (var tag in expense.Tags)
                {
                    newExpense.Tags.Add(tag);
                }
            }

            await _context.Expenses.AddAsync(newExpense);

            // Update the recurrence info
            recurrence.LastProcessedDate = DateTime.UtcNow;
            recurrence.NextProcessingDate = await CalculateNextOccurrenceAsync(recurrence);

            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Created new expense instance {newExpense.Id} from recurring expense {expense.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing recurring expense {recurrence.Id}");
            throw;
        }
    }

    public async Task ScheduleRecurringExpenseAsync(Expense expense)
    {
        if (!expense.IsRecurring || expense.RecurrenceInfo == null)
        {
            _logger.LogWarning($"Attempted to schedule non-recurring expense {expense.Id}");
            return;
        }

        try
        {
            // Calculate next occurrence
            var nextOccurrence = await CalculateNextOccurrenceAsync(expense.RecurrenceInfo);
            
            if (nextOccurrence.HasValue)
            {
                // Update the next processing date
                expense.RecurrenceInfo.NextProcessingDate = nextOccurrence;
                await _context.SaveChangesAsync();
                
                // Schedule processing using Hangfire
                _backgroundJobClient.Schedule(
                    () => ProcessRecurringExpenseWithIdAsync(expense.Id),
                    nextOccurrence.Value);
                
                _logger.LogInformation($"Scheduled recurring expense {expense.Id} for {nextOccurrence.Value}");
            }
            else
            {
                _logger.LogWarning($"Could not calculate next occurrence for recurring expense {expense.Id}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error scheduling recurring expense {expense.Id}");
            throw;
        }
    }

    public async Task UpdateRecurringExpenseScheduleAsync(Expense expense)
    {
        if (!expense.IsRecurring || expense.RecurrenceInfo == null)
        {
            return;
        }

        try
        {
            // Cancel existing schedule
            await CancelRecurringExpenseAsync(expense.Id);
            
            // Create new schedule
            await ScheduleRecurringExpenseAsync(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating recurring expense schedule for {expense.Id}");
            throw;
        }
    }

    public async Task CancelRecurringExpenseAsync(Guid expenseId)
    {
        try
        {
            // Remove recurring job if it exists
            var jobId = $"recurring-expense-{expenseId}";
            RecurringJob.RemoveIfExists(jobId);
            
            // Set the expense as non-recurring
            var expense = await _context.Expenses
                .Include(e => e.RecurrenceInfo)
                .FirstOrDefaultAsync(e => e.Id == expenseId);
                
            if (expense != null && expense.IsRecurring)
            {
                expense.IsRecurring = false;
                
                if (expense.RecurrenceInfo != null)
                {
                    expense.RecurrenceInfo.NextProcessingDate = null;
                }
                
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Cancelled recurring expense {expenseId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cancelling recurring expense {expenseId}");
            throw;
        }
    }

    public async Task<DateTime?> CalculateNextOccurrenceAsync(ExpenseRecurrence recurrence)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            
            // Check if recurrence has ended
            if (recurrence.EndDate.HasValue && recurrence.EndDate.Value < today)
            {
                return null;
            }

            // Get base date for calculation (last processed date or start date)
            var baseDate = recurrence.LastProcessedDate ?? recurrence.StartDate;
            
            // If base date is in the future, use it directly
            if (baseDate > today)
            {
                return baseDate;
            }

            switch (recurrence.RecurrenceType)
            {
                case RecurrenceType.Daily:
                    return CalculateNextDaily(baseDate, recurrence.Interval);
                    
                case RecurrenceType.Weekly:
                    return CalculateNextWeekly(baseDate, recurrence.Interval, recurrence.DayOfWeek);
                    
                case RecurrenceType.Monthly:
                    return CalculateNextMonthly(baseDate, recurrence.Interval, recurrence.DayOfMonth);
                    
                case RecurrenceType.Quarterly:
                    return CalculateNextQuarterly(baseDate, recurrence.Interval, recurrence.DayOfMonth);
                    
                case RecurrenceType.Yearly:
                    return CalculateNextYearly(baseDate, recurrence.Interval);
                    
                case RecurrenceType.Custom:
                    return CalculateNextFromCronExpression(recurrence.CustomCronExpression);
                    
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating next occurrence for expense recurrence {recurrence.Id}");
            return null;
        }
    }

    // Method to be called by Hangfire
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessRecurringExpenseWithIdAsync(Guid expenseId)
    {
        try
        {
            var recurrence = await _context.ExpenseRecurrences
                .Include(r => r.Expense)
                .FirstOrDefaultAsync(r => r.ExpenseId == expenseId);
                
            if (recurrence != null)
            {
                await ProcessRecurringExpenseAsync(recurrence);
                
                // Schedule next occurrence
                await ScheduleRecurringExpenseAsync(recurrence.Expense);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing recurring expense with ID {expenseId}");
            throw;
        }
    }

    #region Helper Methods for Date Calculations

    private DateTime CalculateNextDaily(DateTime baseDate, int interval)
    {
        var today = DateTime.UtcNow.Date;
        
        // Calculate days since base date
        int daysSinceBase = (today - baseDate).Days;
        
        // Find how many days to add to get to next occurrence
        int daysToAdd = interval - (daysSinceBase % interval);
        
        // If today is an occurrence date, move to next one
        if (daysToAdd == interval)
        {
            daysToAdd = 0;
        }
        
        return today.AddDays(daysToAdd);
    }

    private DateTime CalculateNextWeekly(DateTime baseDate, int interval, DayOfWeek? preferredDay)
    {
        var today = DateTime.UtcNow.Date;
        
        // If preferred day is specified, use it
        if (preferredDay.HasValue)
        {
            // Calculate days to next preferred day of week
            int currentDay = (int)today.DayOfWeek;
            int targetDay = (int)preferredDay.Value;
            int daysToAdd = (targetDay + 7 - currentDay) % 7;
            
            // If today is the preferred day, add 7 days
            if (daysToAdd == 0)
            {
                daysToAdd = 7;
            }
            
            // Calculate weeks to add based on interval
            int weeksSinceBase = ((today - baseDate).Days / 7);
            int weeksToAdd = (interval - (weeksSinceBase % interval)) % interval;
            
            return today.AddDays(daysToAdd + (weeksToAdd * 7));
        }
        else
        {
            // Use same day of week as base date
            int dayDiff = (int)baseDate.DayOfWeek - (int)today.DayOfWeek;
            int daysToAdd = (dayDiff + 7) % 7;
            
            // Calculate weeks to add
            int weeksSinceBase = ((today - baseDate).Days / 7);
            int weeksToAdd = (interval - (weeksSinceBase % interval)) % interval;
            
            if (weeksToAdd == 0 && daysToAdd <= 0)
            {
                weeksToAdd = interval;
            }
            
            return today.AddDays(daysToAdd + (weeksToAdd * 7));
        }
    }

    private DateTime CalculateNextMonthly(DateTime baseDate, int interval, int? preferredDay)
    {
        var today = DateTime.UtcNow.Date;
        var currentMonth = today.Month;
        var currentYear = today.Year;
        
        // Calculate month difference
        int monthDiff = ((currentYear - baseDate.Year) * 12) + (currentMonth - baseDate.Month);
        int monthsToAdd = interval - (monthDiff % interval);
        
        if (monthsToAdd == interval && today.Day < baseDate.Day)
        {
            monthsToAdd = 0;
        }
        
        var nextMonth = today.AddMonths(monthsToAdd);
        
        // Use preferred day or original day, ensuring it's valid for the month
        int targetDay = preferredDay ?? baseDate.Day;
        int daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        targetDay = Math.Min(targetDay, daysInMonth);
        
        return new DateTime(nextMonth.Year, nextMonth.Month, targetDay);
    }

    private DateTime CalculateNextQuarterly(DateTime baseDate, int interval, int? preferredDay)
    {
        // Quarterly is just a special case of monthly with interval * 3
        return CalculateNextMonthly(baseDate, interval * 3, preferredDay);
    }

    private DateTime CalculateNextYearly(DateTime baseDate, int interval)
    {
        var today = DateTime.UtcNow.Date;
        var currentYear = today.Year;
        
        // Calculate years to add
        int yearDiff = currentYear - baseDate.Year;
        int yearsToAdd = interval - (yearDiff % interval);
        
        if (yearsToAdd == interval && 
            (today.Month < baseDate.Month || 
            (today.Month == baseDate.Month && today.Day < baseDate.Day)))
        {
            yearsToAdd = 0;
        }
        
        // Use same month and day as base date
        return new DateTime(
            today.Year + yearsToAdd,
            baseDate.Month,
            Math.Min(baseDate.Day, DateTime.DaysInMonth(today.Year + yearsToAdd, baseDate.Month)));
    }

    private DateTime? CalculateNextFromCronExpression(string cronExpression)
    {
        if (string.IsNullOrEmpty(cronExpression))
        {
            return null;
        }
        
        try
        {
            // Parse the cron expression and calculate next occurrence
            // Hangfire doesn't have a direct GetNextOccurrence method, so we need to use NCronTab or roll our own
            // For now, let's schedule it for the next day as a simple fallback
            return DateTime.UtcNow.Date.AddDays(1);
        }
        catch
        {
            _logger.LogWarning($"Invalid cron expression: {cronExpression}");
            return null;
        }
    }

    #endregion
}

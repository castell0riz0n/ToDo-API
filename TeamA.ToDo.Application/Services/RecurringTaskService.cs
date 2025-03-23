using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models.Todo;
using TeamA.ToDo.Core.Shared.Enums.Todo;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services
{
    public interface IRecurringTaskService
    {
        Task ScheduleRecurringTaskAsync(TodoTask task);
        Task UpdateRecurringTaskScheduleAsync(TodoTask task);
        Task CancelRecurringTaskAsync(Guid taskId);
        Task CreateRecurringTaskInstanceAsync(Guid taskId);
        Task ScheduleReminderAsync(TaskReminder reminder);
        Task ProcessReminderAsync(Guid reminderId);
    }

    public class RecurringTaskService : IRecurringTaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<RecurringTaskService> _logger;
        private readonly IApplicationEmailSender _emailService;

        public RecurringTaskService(
            ApplicationDbContext context,
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            ILogger<RecurringTaskService> logger,
            IApplicationEmailSender emailService)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task ScheduleRecurringTaskAsync(TodoTask task)
        {
            if (!task.IsRecurring || task.RecurrenceInfo == null)
            {
                return;
            }

            var jobId = $"recurringtask_{task.Id}";

            switch (task.RecurrenceInfo.RecurrenceType)
            {
                case RecurrenceType.Daily:
                    _recurringJobManager.AddOrUpdate(
                        jobId,
                        () => CreateRecurringTaskInstanceAsync(task.Id),
                        $"0 0 */{task.RecurrenceInfo.Interval} * *"); // Every X days at midnight
                    break;

                case RecurrenceType.Weekly:
                    var dayOfWeek = task.RecurrenceInfo.DayOfWeek ?? DateTime.UtcNow.DayOfWeek;
                    var cronExpression = $"0 0 * * {GetCronDayOfWeek(dayOfWeek)}";

                    if (task.RecurrenceInfo.Interval > 1)
                    {
                        // For intervals > 1, we need to implement custom logic
                        // This is a simplified approach, exact implementation depends on requirements
                        var nextOccurrence = GetNextWeeklyOccurrence(task.RecurrenceInfo);
                        _backgroundJobClient.Schedule(
                            () => CreateRecurringTaskInstanceAsync(task.Id),
                            nextOccurrence);
                    }
                    else
                    {
                        _recurringJobManager.AddOrUpdate(
                            jobId,
                            () => CreateRecurringTaskInstanceAsync(task.Id),
                            cronExpression);
                    }
                    break;

                case RecurrenceType.Monthly:
                    var dayOfMonth = task.RecurrenceInfo.DayOfMonth ?? DateTime.UtcNow.Day;

                    if (task.RecurrenceInfo.Interval > 1)
                    {
                        // For intervals > 1, we need custom logic
                        var nextOccurrence = GetNextMonthlyOccurrence(task.RecurrenceInfo);
                        _backgroundJobClient.Schedule(
                            () => CreateRecurringTaskInstanceAsync(task.Id),
                            nextOccurrence);
                    }
                    else
                    {
                        _recurringJobManager.AddOrUpdate(
                            jobId,
                            () => CreateRecurringTaskInstanceAsync(task.Id),
                            $"0 0 {dayOfMonth} * *"); // On specified day of month at midnight
                    }
                    break;

                case RecurrenceType.Yearly:
                    var startDate = task.RecurrenceInfo.StartDate;
                    _recurringJobManager.AddOrUpdate(
                        jobId,
                        () => CreateRecurringTaskInstanceAsync(task.Id),
                        $"0 0 {startDate.Day} {startDate.Month} *"); // On specific day and month at midnight
                    break;

                case RecurrenceType.Custom:
                    if (!string.IsNullOrEmpty(task.RecurrenceInfo.CustomCronExpression))
                    {
                        _recurringJobManager.AddOrUpdate(
                            jobId,
                            () => CreateRecurringTaskInstanceAsync(task.Id),
                            task.RecurrenceInfo.CustomCronExpression);
                    }
                    break;
            }

            _logger.LogInformation($"Scheduled recurring task {task.Id}");
        }

        public async Task UpdateRecurringTaskScheduleAsync(TodoTask task)
        {
            // First, remove existing schedule
            await CancelRecurringTaskAsync(task.Id);

            // Create new schedule
            await ScheduleRecurringTaskAsync(task);
        }

        public async Task CancelRecurringTaskAsync(Guid taskId)
        {
            var jobId = $"recurringtask_{taskId}";
            _recurringJobManager.RemoveIfExists(jobId);
            _logger.LogInformation($"Cancelled recurring task {taskId}");
        }

        public async Task CreateRecurringTaskInstanceAsync(Guid taskId)
        {
            try
            {
                var originalTask = await _context.TodoTasks
                    .Include(t => t.RecurrenceInfo)
                    .Include(t => t.TaskTags)
                        .ThenInclude(tt => tt.Tag)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (originalTask == null || !originalTask.IsRecurring)
                {
                    _logger.LogWarning($"Recurring task {taskId} not found or not recurring anymore");
                    await CancelRecurringTaskAsync(taskId);
                    return;
                }

                // Check end date if specified
                if (originalTask.RecurrenceInfo.EndDate.HasValue &&
                    DateTime.UtcNow > originalTask.RecurrenceInfo.EndDate.Value)
                {
                    _logger.LogInformation($"Recurring task {taskId} has reached its end date, cancelling");
                    await CancelRecurringTaskAsync(taskId);
                    return;
                }

                // Create a new instance of the task
                var newTask = new TodoTask
                {
                    Id = Guid.NewGuid(),
                    Title = originalTask.Title,
                    Description = originalTask.Description,
                    Priority = originalTask.Priority,
                    Status = TodoTaskStatus.NotStarted,
                    UserId = originalTask.UserId,
                    CategoryId = originalTask.CategoryId,
                    CreatedAt = DateTime.UtcNow,
                    IsRecurring = false // The new instance is not recurring itself
                };

                // Calculate the due date based on the original task's schedule
                if (originalTask.DueDate.HasValue)
                {
                    // Set due date relative to the creation time
                    // This logic might need adjustment based on specific requirements
                    var timeSpan = originalTask.DueDate.Value - originalTask.CreatedAt;
                    newTask.DueDate = DateTime.UtcNow.Add(timeSpan);
                }

                // Copy tags
                foreach (var tagLink in originalTask.TaskTags)
                {
                    newTask.TaskTags.Add(new TaskTag
                    {
                        Id = Guid.NewGuid(),
                        TodoTaskId = newTask.Id,
                        TagId = tagLink.TagId
                    });
                }

                await _context.TodoTasks.AddAsync(newTask);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created new instance {newTask.Id} of recurring task {taskId}");

                // Schedule next instance for interval-based recurrences that don't use cron
                if (originalTask.RecurrenceInfo.RecurrenceType is RecurrenceType.Weekly or RecurrenceType.Monthly &&
                    originalTask.RecurrenceInfo.Interval > 1)
                {
                    DateTime nextOccurrence;

                    if (originalTask.RecurrenceInfo.RecurrenceType == RecurrenceType.Weekly)
                    {
                        nextOccurrence = GetNextWeeklyOccurrence(originalTask.RecurrenceInfo);
                    }
                    else // Monthly
                    {
                        nextOccurrence = GetNextMonthlyOccurrence(originalTask.RecurrenceInfo);
                    }

                    _backgroundJobClient.Schedule(
                        () => CreateRecurringTaskInstanceAsync(taskId),
                        nextOccurrence);

                    _logger.LogInformation($"Scheduled next occurrence of task {taskId} for {nextOccurrence}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating instance of recurring task {taskId}");
                throw;
            }
        }

        public async Task ScheduleReminderAsync(TaskReminder reminder)
        {
            if (reminder.ReminderTime <= DateTime.UtcNow)
            {
                // Process immediately if reminder time is in the past
                await ProcessReminderAsync(reminder.Id);
            }
            else
            {
                // Schedule for future processing
                _backgroundJobClient.Schedule(
                    () => ProcessReminderAsync(reminder.Id),
                    reminder.ReminderTime);

                _logger.LogInformation($"Scheduled reminder {reminder.Id} for {reminder.ReminderTime}");
            }
        }

        public async Task ProcessReminderAsync(Guid reminderId)
        {
            try
            {
                var reminder = await _context.TaskReminders
                    .Include(r => r.TodoTask)
                    .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(r => r.Id == reminderId);

                if (reminder == null)
                {
                    _logger.LogWarning($"Reminder {reminderId} not found");
                    return;
                }

                if (reminder.IsSent)
                {
                    _logger.LogInformation($"Reminder {reminderId} already sent");
                    return;
                }

                // Skip if task is already completed
                if (reminder.TodoTask.Status == TodoTaskStatus.Completed)
                {
                    _logger.LogInformation($"Skipping reminder for completed task {reminder.TodoTaskId}");
                    return;
                }

                // Send notification
                var user = reminder.TodoTask.User;
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Task Reminder",
                    $"Reminder: Your task '{reminder.TodoTask.Title}' is due soon.");

                // Update reminder status
                reminder.IsSent = true;
                reminder.SentAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Processed reminder {reminderId} for task {reminder.TodoTaskId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing reminder {reminderId}");
                throw;
            }
        }

        #region Helper Methods

        private string GetCronDayOfWeek(DayOfWeek dayOfWeek)
        {
            // Cron uses 0-6 for Sunday to Saturday
            // .NET DayOfWeek uses 0-6 for Sunday to Saturday too, so no conversion needed
            return ((int)dayOfWeek).ToString();
        }

        private DateTime GetNextWeeklyOccurrence(RecurrenceInfo recurrence)
        {
            var now = DateTime.UtcNow;
            var dayOfWeek = recurrence.DayOfWeek ?? now.DayOfWeek;

            // Find the next occurrence of the specified day of week
            var daysToAdd = ((int)dayOfWeek - (int)now.DayOfWeek + 7) % 7;
            if (daysToAdd == 0) daysToAdd = 7; // If today is the day, go to next week

            var nextOccurrence = now.Date.AddDays(daysToAdd);

            // Adjust for the interval (e.g., every 2 weeks)
            // Logic depends on when the recurrence started
            var weeksSinceStart = (int)(nextOccurrence - recurrence.StartDate.Date).TotalDays / 7;
            var weeksToAdd = (recurrence.Interval - (weeksSinceStart % recurrence.Interval)) % recurrence.Interval;

            return nextOccurrence.AddDays(weeksToAdd * 7);
        }

        private DateTime GetNextMonthlyOccurrence(RecurrenceInfo recurrence)
        {
            var now = DateTime.UtcNow;
            var dayOfMonth = recurrence.DayOfMonth ?? now.Day;

            // Get the first occurrence of the day in current or next month
            var year = now.Year;
            var month = now.Month;

            // If we're past the day of month, move to next month
            if (now.Day > dayOfMonth)
            {
                month++;
                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }

            // Create a date for the specified day in the calculated month
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var actualDay = Math.Min(dayOfMonth, daysInMonth); // Handle cases like 31st in 30-day months

            var nextOccurrence = new DateTime(year, month, actualDay);

            // Adjust for interval (e.g., every 3 months)
            var monthsSinceStart = (year - recurrence.StartDate.Year) * 12 + month - recurrence.StartDate.Month;
            var monthsToAdd = (recurrence.Interval - (monthsSinceStart % recurrence.Interval)) % recurrence.Interval;

            if (monthsToAdd > 0)
            {
                month += monthsToAdd;
                if (month > 12)
                {
                    var yearsToAdd = (month - 1) / 12;
                    month = ((month - 1) % 12) + 1;
                    year += yearsToAdd;
                }

                // Recalculate day for the new month (handle 31st in 30-day month again)
                daysInMonth = DateTime.DaysInMonth(year, month);
                actualDay = Math.Min(dayOfMonth, daysInMonth);

                nextOccurrence = new DateTime(year, month, actualDay);
            }

            return nextOccurrence;
        }

        #endregion
    }
}
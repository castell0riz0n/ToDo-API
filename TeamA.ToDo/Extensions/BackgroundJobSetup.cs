using Hangfire;
using Microsoft.EntityFrameworkCore;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Application.Services;
using TeamA.ToDo.Core.Shared.Enums.Todo;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Host.Extensions
{
    public static class BackgroundJobSetup
    {
        public static void RegisterRecurringJobs(IServiceProvider serviceProvider)
        {
            // Schedule the job to check for overdue tasks daily at midnight
            RecurringJob.AddOrUpdate<TaskNotificationJob>(
                "check-overdue-tasks",
                job => job.CheckForOverdueTasks(),
                Cron.Daily);

            // Schedule the job to check for tasks due today daily at 7 AM
            RecurringJob.AddOrUpdate<TaskNotificationJob>(
                "check-tasks-due-today",
                job => job.CheckForTasksDueToday(),
                "0 7 * * *");

            // Schedule the job to generate upcoming recurring tasks weekly on Sunday at midnight
            RecurringJob.AddOrUpdate<TaskRecurrenceJob>(
                "generate-upcoming-recurring-tasks",
                job => job.GenerateUpcomingRecurringTasks(),
                Cron.Weekly);

            // Schedule the job to clean up completed tasks monthly on the 1st at 1 AM
            RecurringJob.AddOrUpdate<TaskCleanupJob>(
                "clean-up-old-completed-tasks",
                job => job.CleanUpOldCompletedTasks(90), // Archive tasks completed more than 90 days ago
                "0 1 1 * *");
        }

        public static void EnqueueExistingReminders(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("BackgroundJobSetup");

            try
            {
                // Get all reminders that haven't been sent yet and are in the future
                var reminders = context.TaskReminders
                    .Include(r => r.TodoTask)
                    .Where(r => !r.IsSent && r.ReminderTime > DateTime.UtcNow && r.TodoTask.Status != TodoTaskStatus.Completed)
                    .ToList();

                var recurringTaskService = scope.ServiceProvider.GetRequiredService<IRecurringTaskService>();

                foreach (var reminder in reminders)
                {
                    recurringTaskService.ScheduleReminderAsync(reminder).Wait();
                    logger.LogInformation($"Enqueued existing reminder {reminder.Id} for task {reminder.TodoTaskId}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error enqueueing existing reminders");
            }
        }
    }

    public class TaskNotificationJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IApplicationEmailSender _emailService;
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<TaskNotificationJob> _logger;

        public TaskNotificationJob(
            ApplicationDbContext context,
            IApplicationEmailSender emailService,
            IEmailTemplateService templateService,
            ILogger<TaskNotificationJob> logger)
        {
            _context = context;
            _emailService = emailService;
            _templateService = templateService;
            _logger = logger;
        }

        public async Task CheckForOverdueTasks()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var overdueTasks = await _context.TodoTasks
                    .Include(t => t.User)
                    .Where(t => t.DueDate < today &&
                                t.Status != TodoTaskStatus.Completed &&
                                t.Status != TodoTaskStatus.Archived)
                    .ToListAsync();

                foreach (var task in overdueTasks)
                {
                    if (task.User?.Email == null)
                    {
                        continue;
                    }

                    var emailTemplate = _templateService.GetTaskOverdueTemplate(task, task.User);
                    await _emailService.SendEmailAsync(
                        task.User.Email,
                        $"Overdue Task: {task.Title}",
                        emailTemplate);

                    _logger.LogInformation($"Sent overdue notification for task {task.Id} to {task.User.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for overdue tasks");
            }
        }

        public async Task CheckForTasksDueToday()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                var tasksDueToday = await _context.TodoTasks
                    .Include(t => t.User)
                    .Where(t => t.DueDate >= today &&
                                t.DueDate < tomorrow &&
                                t.Status != TodoTaskStatus.Completed &&
                                t.Status != TodoTaskStatus.Archived)
                    .ToListAsync();

                foreach (var task in tasksDueToday)
                {
                    if (task.User?.Email == null)
                    {
                        continue;
                    }

                    var emailTemplate = _templateService.GetTaskDueTodayTemplate(task, task.User);
                    await _emailService.SendEmailAsync(
                        task.User.Email,
                        $"Task Due Today: {task.Title}",
                        emailTemplate);

                    _logger.LogInformation($"Sent due today notification for task {task.Id} to {task.User.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for tasks due today");
            }
        }
    }

    public class TaskRecurrenceJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecurringTaskService _recurringTaskService;
        private readonly ILogger<TaskRecurrenceJob> _logger;

        public TaskRecurrenceJob(
            ApplicationDbContext context,
            IRecurringTaskService recurringTaskService,
            ILogger<TaskRecurrenceJob> logger)
        {
            _context = context;
            _recurringTaskService = recurringTaskService;
            _logger = logger;
        }

        public async Task GenerateUpcomingRecurringTasks()
        {
            try
            {
                // Find all recurring tasks
                var recurringTasks = await _context.TodoTasks
                    .Include(t => t.RecurrenceInfo)
                    .Where(t => t.IsRecurring && t.RecurrenceInfo != null)
                    .ToListAsync();

                // For each recurring task, ensure instances for the upcoming period
                foreach (var task in recurringTasks)
                {
                    await _recurringTaskService.ScheduleRecurringTaskAsync(task);
                    _logger.LogInformation($"Generated upcoming instances for recurring task {task.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating upcoming recurring tasks");
            }
        }
    }

    public class TaskCleanupJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskCleanupJob> _logger;

        public TaskCleanupJob(
            ApplicationDbContext context,
            ILogger<TaskCleanupJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CleanUpOldCompletedTasks(int daysToKeep)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

                // Find completed tasks older than the cutoff date
                var tasksToArchive = await _context.TodoTasks
                    .Where(t => t.Status == TodoTaskStatus.Completed &&
                                t.CompletedAt.HasValue &&
                                t.CompletedAt < cutoffDate)
                    .ToListAsync();

                // Archive these tasks
                foreach (var task in tasksToArchive)
                {
                    task.Status = TodoTaskStatus.Archived;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Archived {tasksToArchive.Count} completed tasks older than {daysToKeep} days");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving old completed tasks");
            }
        }
    }
}
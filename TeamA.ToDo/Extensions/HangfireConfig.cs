using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Application.Interfaces.Expenses;
using TeamA.ToDo.Application.Services;
using TeamA.ToDo.Application.Services.Expenses;

namespace TeamA.ToDo.Host.Extensions
{
    public static class HangfireConfig
    {
        public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Hangfire services
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

            // Add the processing server as a singleton
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 2; // Number of workers to process jobs
            });

            // Register services
            services.AddScoped<IRecurringTaskService, RecurringTaskService>();
            services.AddScoped<IRecurringExpenseService, RecurringExpenseService>();
            services.AddScoped<IApplicationEmailSender, EmailService>();

            return services;
        }

        public static IApplicationBuilder UseHangfireDashboard(this IApplicationBuilder app, IConfiguration configuration)
        {
            var dashboardOptions = new DashboardOptions
            {
                // Configure authorization for Hangfire dashboard here
                // For production use proper authorization middleware
                Authorization = new[] { new HangfireAuthorizationFilter() }
            };

            app.UseHangfireDashboard("/hangfire", dashboardOptions);

            return app;
        }

        public static void RegisterRecurringJobs(IServiceProvider serviceProvider)
        {
            // Existing jobs from BackgroundJobSetup
            RecurringJob.AddOrUpdate<TaskNotificationJob>(
                "check-overdue-tasks",
                job => job.CheckForOverdueTasks(),
                Cron.Daily);

            RecurringJob.AddOrUpdate<TaskNotificationJob>(
                "check-tasks-due-today",
                job => job.CheckForTasksDueToday(),
                "0 7 * * *");

            RecurringJob.AddOrUpdate<TaskRecurrenceJob>(
                "generate-upcoming-recurring-tasks",
                job => job.GenerateUpcomingRecurringTasks(),
                Cron.Weekly);

            RecurringJob.AddOrUpdate<TaskCleanupJob>(
                "clean-up-old-completed-tasks",
                job => job.CleanUpOldCompletedTasks(90),
                "0 1 1 * *");

            // Add new recurring expense job
            RecurringJob.AddOrUpdate<IRecurringExpenseService>(
                "process-recurring-expenses",
                service => service.ProcessRecurringExpensesAsync(),
                Cron.Daily(1)); // Run at 1 AM daily
        }
    }

    // Simple authorization filter for Hangfire dashboard
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // In production, implement proper authorization logic here
            // For now, allow access only in development environment
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity.IsAuthenticated &&
                   httpContext.User.IsInRole("Admin");
        }
    }
}
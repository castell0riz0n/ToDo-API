using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Application.Services;

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
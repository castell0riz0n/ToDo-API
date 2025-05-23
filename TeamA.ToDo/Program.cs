using Scalar.AspNetCore;
using Serilog;
using TeamA.ToDo.Host.Extensions;

namespace TeamA.ToDo.Host;

public class Program
{
    public static void Main(string[] args)
    {
        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting web host");
            var builder = WebApplication.CreateBuilder(args);

            // Use Serilog
            builder.Host.UseSerilog();

            // Register services
            ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigureMiddleware(app, builder.Configuration, builder.Environment);

            // Start the application
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Configure HSTS
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
            options.ExcludedHosts.Add("localhost");
            options.ExcludedHosts.Add("127.0.0.1");
        });

        // Add core application services
        services.AddApplicationServices(configuration);

        // Add data services (DbContext, etc.)
        services.AddDataServices(configuration);

        // Add authentication services
        services.AddAuthServices(configuration);

        // Add API services (Controllers, Rate Limiting, etc.)
        services.AddApiServices(configuration);

        // Add Hangfire services
        services.AddHangfireServices(configuration);

        // Add data seeder
        services.AddDataSeeder();
    }

    private static void ConfigureMiddleware(WebApplication app, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Configure API middleware
        app.UseApiConfiguration(configuration, environment);

        // Configure Hangfire Dashboard
        app.UseHangfireDashboard(configuration);

        // Map endpoints
        app.MapControllers();

        // Configure Scalar API Reference in development
        if (environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(opts =>
            {
                opts.WithTitle("TeamA Todo API")
                    .WithTheme(ScalarTheme.Default)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithHttpBearerAuthentication(new HttpBearerOptions());
            });
        }

        // Seed database
        app.SeedDatabaseAsync().Wait();

        // Register Hangfire jobs
        BackgroundJobSetup.RegisterRecurringJobs(app.Services);
        BackgroundJobSetup.EnqueueExistingReminders(app.Services);
    }
}
namespace TeamA.ToDo.Host.Extensions;

public static class DataSeederExtensions
{
    /// <summary>
    /// Adds the DataSeeder service to the service collection
    /// </summary>
    public static IServiceCollection AddDataSeeder(this IServiceCollection services)
    {
        services.AddScoped<DataSeeder>();
        return services;
    }

    /// <summary>
    /// Configures the application to seed the database during startup
    /// </summary>
    public static async Task<IApplicationBuilder> SeedDatabaseAsync(this IApplicationBuilder app)
    {
        // Create a new scope to resolve the seeder from
        using var scope = app.ApplicationServices.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

        try
        {
            logger.LogInformation("Executing database seeder...");
            await seeder.SeedDataAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            // Don't throw the exception - we want the application to start even if seeding fails
        }

        return app;
    }
}
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Host.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Add Data Protection
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(@"C:\DataProtection-Keys"))
            .SetDefaultKeyLifetime(TimeSpan.FromDays(14))
            .SetApplicationName("TodoApp");

        // Add Anti-forgery
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        // Configure Cookie Policy
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.MinimumSameSitePolicy = SameSiteMode.Strict;
            options.HttpOnly = HttpOnlyPolicy.Always;
            options.Secure = CookieSecurePolicy.Always;
        });

        return services;
    }
}
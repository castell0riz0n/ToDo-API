using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using TeamA.ToDo.Application;
using TeamA.ToDo.Application.DTOs.Auth;
using TeamA.ToDo.Application.DTOs.Email;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Email;
using TeamA.ToDo.Application.Filters;
using TeamA.ToDo.Application.Handlers;
using TeamA.ToDo.Application.Helpers;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Application.Requirements;
using TeamA.ToDo.Application.Security;
using TeamA.ToDo.Application.Services;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.General;
using TeamA.ToDo.EntityFramework;
using TeamA.ToDo.Extensions;
using TeamA.ToDo.Host.Extensions;

var corsPolicy = "AllowedOrigins";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;

        // User settings
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>("default");

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"])),
            ClockSkew = TimeSpan.Zero // Remove delay of token when expire
        };

        // Enable custom authentication error handling
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Skip the default challenge response
                context.HandleResponse();

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var result = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "You are not authorized to access this resource"
                });

                return context.Response.WriteAsync(result);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Basic policies
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));

    // More specific policies
    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireRole("Admin").RequireClaim("Permission", "ManageUsers"));

    options.AddPolicy("CanViewAllTodos", policy =>
        policy.RequireRole("Admin").RequireClaim("Permission", "ViewAllTodos"));

    options.AddPolicy("CanExportData", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.HasClaim(c => c.Type == "Permission" && c.Value == "ExportData")));

    options.AddPolicy("TodoOwnerPolicy", policy =>
        policy.Requirements.Add(new ToDoOwnerRequirement()));
});


builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(ValidateModelAttribute));
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Rate limit based on IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ipAddress,
            partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100, // 100 requests
                Window = TimeSpan.FromMinutes(1) // per minute
            });
    });

    // Add specific limiters for sensitive routes
    options.AddPolicy("AuthLimit", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ipAddress,
            partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10, // 10 requests
                Window = TimeSpan.FromMinutes(5) // per 5 minutes
            });
    });

    // Configure what happens when rate limiting is triggered
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = new ServiceResponse<object>
        {
            Success = false,
            Message = "Too many requests. Please try again later."
        };

        await context.HttpContext.Response.WriteAsJsonAsync(response, token);
    };
});

// Configure Data Protection
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\DataProtection-Keys"))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(14))
    .SetApplicationName("TodoApp");

// Configure Anti-forgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Configure Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
});
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));

builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddHangfireServices(builder.Configuration);

// Register individual email provider implementations
builder.Services.AddScoped<TeamA.ToDo.Application.Email.SmtpEmailSender>();
builder.Services.AddScoped<TeamA.ToDo.Application.Email.SendGridEmailSender>();

// Register the main application email sender
builder.Services.AddScoped<TeamA.ToDo.Application.Interfaces.IApplicationEmailSender, TeamA.ToDo.Application.Services.EmailService>();

// Register the Microsoft Identity UI email sender adapter
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, IdentityEmailSenderAdapter>();

builder.Services.AddSingleton<IAuthorizationHandler, ToDoOwnerAuthorizationHandler>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();

builder.Services.AddScoped<ITodoTaskService, TodoTaskService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IReminderService, ReminderService>();


// Configure validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTransient<IPasswordValidator<ApplicationUser>, CustomPasswordValidator>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.WithOrigins(
                "https://yourdomain.com",
                "https://www.yourdomain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configure JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddDataSeeder();

var app = builder.Build();

app.UseGlobalExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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

if (!builder.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors(corsPolicy);
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UsePermissionAuthorization();

app.UseHangfireDashboard(builder.Configuration);

app.MapControllers();

await app.SeedDatabaseAsync();

BackgroundJobSetup.RegisterRecurringJobs(app.Services);
BackgroundJobSetup.EnqueueExistingReminders(app.Services);

app.Run();

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

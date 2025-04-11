using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using TeamA.ToDo.Application;
using TeamA.ToDo.Application.DTOs.Auth;
using TeamA.ToDo.Application.DTOs.Email;
using TeamA.ToDo.Application.Email;
using TeamA.ToDo.Application.Helpers;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Application.Interfaces.Expenses;
using TeamA.ToDo.Application.Security;
using TeamA.ToDo.Application.Services;
using TeamA.ToDo.Application.Services.Expenses;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.General;

namespace TeamA.ToDo.Host.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configurations
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<AppConfig>(configuration.GetSection("AppConfig"));
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // Register AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // Register validation
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddTransient<IPasswordValidator<ApplicationUser>, CustomPasswordValidator>();

        // Register Email Services
        RegisterEmailServices(services);

        // Register Application Services
        RegisterApplicationServices(services);

        return services;
    }

    private static void RegisterEmailServices(IServiceCollection services)
    {
        // Register individual email provider implementations
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<SendGridEmailSender>();

        // Register the main application email sender
        services.AddScoped<IApplicationEmailSender, EmailService>();

        // Register the Microsoft Identity UI email sender adapter
        services.AddTransient<IEmailSender, IdentityEmailSenderAdapter>();
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        // Authentication & User Management Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IActivityLogger, ActivityLogger>();

        // Task Management Services
        services.AddScoped<ITodoTaskService, TodoTaskService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IReminderService, ReminderService>();

        // Expense Management Services
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IRecurringExpenseService, RecurringExpenseService>();
        services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IPaymentMethodService, PaymentMethodService>();

        services.AddScoped<IBudgetAlertSettingsService, BudgetAlertSettingsService>();
        services.AddScoped<IBudgetAlertService, BudgetAlertService>();
    }
}
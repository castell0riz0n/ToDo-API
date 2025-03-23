using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.Todo;

namespace TeamA.ToDo.Application.Interfaces;

public interface IEmailTemplateService
{
    string GetEmailVerificationTemplate(string name, string confirmationLink, string appName);
    string GetPasswordResetTemplate(string name, string resetLink, string appName);
    string GetWelcomeTemplate(string name, string loginLink, string appName);
    string GetTwoFactorEnabledTemplate(string name, string appName);
    string GetAccountLockedTemplate(string name, int lockoutMinutes, string appName);

    string GetTaskReminderTemplate(TodoTask task, ApplicationUser user);
    string GetTaskDueTodayTemplate(TodoTask task, ApplicationUser user);
    string GetTaskOverdueTemplate(TodoTask task, ApplicationUser user);
    string GetTaskCompletedTemplate(TodoTask task, ApplicationUser user);
}
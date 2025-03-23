using Google.Apis.Gmail.v1.Data;
using Microsoft.Extensions.Options;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.General;
using TeamA.ToDo.Core.Models.Todo;
using TeamA.ToDo.Core.Shared.Enums.Todo;

namespace TeamA.ToDo.Application.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly string _appBaseUrl;

    public EmailTemplateService(IOptions<AppConfig> appSettings)
    {
        _appBaseUrl = appSettings.Value.BaseUrl;
    }
    public string GetEmailVerificationTemplate(string name, string confirmationLink, string appName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Verify Your Email</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .button {{
            display: inline-block;
            background-color: #4CAF50;
            color: white;
            padding: 12px 24px;
            text-align: center;
            text-decoration: none;
            font-size: 16px;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 20px;
            font-size: 12px;
            text-align: center;
            color: #777;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Verify Your Email Address</h2>
        </div>
        <p>Hi {name},</p>
        <p>Thank you for registering with {appName}. Please verify your email address to complete your registration.</p>
        <p style='text-align: center;'>
            <a href='{confirmationLink}' class='button'>Verify Email Address</a>
        </p>
        <p>If you didn't create an account, you can safely ignore this email.</p>
        <p>If you're having trouble clicking the button, copy and paste this URL into your browser:</p>
        <p>{confirmationLink}</p>
        <p>Best regards,<br>The {appName} Team</p>
    </div>
    <div class='footer'>
        <p>This is an automated message. Please do not reply to this email.</p>
        <p>&copy; {DateTime.Now.Year} {appName}. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    public string GetPasswordResetTemplate(string name, string resetLink, string appName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Reset Your Password</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .button {{
            display: inline-block;
            background-color: #2196F3;
            color: white;
            padding: 12px 24px;
            text-align: center;
            text-decoration: none;
            font-size: 16px;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 20px;
            font-size: 12px;
            text-align: center;
            color: #777;
        }}
        .warning {{
            background-color: #FFF3CD;
            border: 1px solid #FFEEBA;
            color: #856404;
            padding: 10px;
            border-radius: 4px;
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Reset Your Password</h2>
        </div>
        <p>Hi {name},</p>
        <p>We received a request to reset your password for {appName}. Click the button below to reset your password:</p>
        <p style='text-align: center;'>
            <a href='{resetLink}' class='button'>Reset Password</a>
        </p>
        <div class='warning'>
            <p><strong>Important:</strong> This link will expire in 24 hours.</p>
            <p>If you didn't request a password reset, please ignore this email or contact support if you have concerns.</p>
        </div>
        <p>If you're having trouble clicking the button, copy and paste this URL into your browser:</p>
        <p>{resetLink}</p>
        <p>Best regards,<br>The {appName} Team</p>
    </div>
    <div class='footer'>
        <p>This is an automated message. Please do not reply to this email.</p>
        <p>&copy; {DateTime.Now.Year} {appName}. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    public string GetWelcomeTemplate(string name, string loginLink, string appName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Welcome to {appName}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .button {{
            display: inline-block;
            background-color: #673AB7;
            color: white;
            padding: 12px 24px;
            text-align: center;
            text-decoration: none;
            font-size: 16px;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .features {{
            background-color: #E8EAF6;
            border-radius: 4px;
            padding: 15px;
            margin: 20px 0;
        }}
        .features ul {{
            margin: 10px 0;
            padding-left: 20px;
        }}
        .footer {{
            margin-top: 20px;
            font-size: 12px;
            text-align: center;
            color: #777;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Welcome to {appName}!</h2>
        </div>
        <p>Hi {name},</p>
        <p>Thank you for joining {appName}. Your account has been created successfully, and you're ready to start using our services.</p>
        <p style='text-align: center;'>
            <a href='{loginLink}' class='button'>Go to Your Account</a>
        </p>
        <div class='features'>
            <h3>Here's what you can do with {appName}:</h3>
            <ul>
                <li>Create and manage your tasks efficiently</li>
                <li>Set due dates and priorities for better organization</li>
                <li>Track your progress and boost productivity</li>
                <li>Collaborate with team members on shared tasks</li>
            </ul>
        </div>
        <p>If you have any questions or need assistance, feel free to contact our support team.</p>
        <p>Best regards,<br>The {appName} Team</p>
    </div>
    <div class='footer'>
        <p>This is an automated message. Please do not reply to this email.</p>
        <p>&copy; {DateTime.Now.Year} {appName}. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    public string GetTwoFactorEnabledTemplate(string name, string appName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Two-Factor Authentication Enabled</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .security-info {{
            background-color: #D4EDDA;
            border: 1px solid #C3E6CB;
            color: #155724;
            padding: 15px;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 20px;
            font-size: 12px;
            text-align: center;
            color: #777;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Two-Factor Authentication Enabled</h2>
        </div>
        <p>Hi {name},</p>
        <p>This email confirms that two-factor authentication has been successfully enabled for your {appName} account.</p>
        <div class='security-info'>
            <h3>What this means for you:</h3>
            <p>From now on, when you sign in to your account, you'll need to provide:</p>
            <ol>
                <li>Your password</li>
                <li>A verification code from your authenticator app</li>
            </ol>
            <p>This adds an extra layer of security to your account, helping to keep your data safe even if your password is compromised.</p>
        </div>
        <p><strong>Important:</strong> If you did not enable two-factor authentication yourself, please contact our support team immediately as your account may be compromised.</p>
        <p>Best regards,<br>The {appName} Team</p>
    </div>
    <div class='footer'>
        <p>This is an automated message. Please do not reply to this email.</p>
        <p>&copy; {DateTime.Now.Year} {appName}. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    public string GetAccountLockedTemplate(string name, int lockoutMinutes, string appName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Account Temporarily Locked</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            border: 1px solid #ddd;
            border-radius: 5px;
            padding: 20px;
            background-color: #f9f9f9;
        }}
        .header {{
            text-align: center;
            margin-bottom: 20px;
        }}
        .alert {{
            background-color: #F8D7DA;
            border: 1px solid #F5C6CB;
            color: #721C24;
            padding: 15px;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .info {{
            background-color: #D1ECF1;
            border: 1px solid #BEE5EB;
            color: #0C5460;
            padding: 15px;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .footer {{
            margin-top: 20px;
            font-size: 12px;
            text-align: center;
            color: #777;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Account Temporarily Locked</h2>
        </div>
        <p>Hi {name},</p>
        <div class='alert'>
            <p>Your {appName} account has been temporarily locked due to multiple failed login attempts.</p>
            <p>For security reasons, you won't be able to log in for the next <strong>{lockoutMinutes} minutes</strong>.</p>
        </div>
        <div class='info'>
            <h3>What should you do?</h3>
            <ul>
                <li>Wait for the lockout period to expire</li>
                <li>Once the lockout period is over, you can try logging in again</li>
                <li>If you've forgotten your password, use the 'Forgot Password' feature to reset it</li>
                <li>If you believe someone else is trying to access your account, consider changing your password once you regain access</li>
            </ul>
        </div>
        <p><strong>Important:</strong> If you did not attempt to log in to your account, please reset your password immediately once the lockout period expires, as this could indicate someone is trying to gain unauthorized access to your account.</p>
        <p>Best regards,<br>The {appName} Team</p>
    </div>
    <div class='footer'>
        <p>This is an automated message. Please do not reply to this email.</p>
        <p>&copy; {DateTime.Now.Year} {appName}. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    public string GetTaskReminderTemplate(TodoTask task, ApplicationUser user)
    {
        var taskUrl = $"{_appBaseUrl}/tasks/{task.Id}";
        var priorityClass = GetPriorityColorClass(task.Priority);

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Task Reminder</title>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #3498db; color: white; padding: 10px 20px; border-radius: 5px 5px 0 0; }}
                    .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; border: 1px solid #ddd; }}
                    .task-title {{ font-size: 22px; margin-bottom: 15px; }}
                    .priority {{ display: inline-block; padding: 3px 8px; border-radius: 3px; color: white; font-size: 12px; margin-bottom: 15px; }}
                    .high {{ background-color: #e74c3c; }}
                    .medium {{ background-color: #f39c12; }}
                    .low {{ background-color: #2ecc71; }}
                    .details {{ margin-bottom: 20px; }}
                    .button {{ display: inline-block; background-color: #3498db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}
                    .footer {{ margin-top: 20px; font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Task Reminder</h1>
                    </div>
                    <div class='content'>
                        <p>Hello {user.FirstName ?? user.UserName},</p>
                        <p>This is a reminder about your upcoming task:</p>
                        
                        <div class='task-title'>{task.Title}</div>
                        <div class='priority {priorityClass}'>Priority: {task.Priority}</div>
                        
                        <div class='details'>
                            <p><strong>Due:</strong> {task.DueDate?.ToString("dddd, MMMM d, yyyy h:mm tt") ?? "No due date"}</p>
                            {(string.IsNullOrEmpty(task.Description) ? "" : $"<p><strong>Description:</strong> {task.Description}</p>")}
                            <p><strong>Status:</strong> {task.Status}</p>
                        </div>
                        
                        <a href='{taskUrl}' class='button'>View Task</a>
                        
                        <div class='footer'>
                            <p>This is an automated reminder from your ToDo application.</p>
                            <p>If you no longer wish to receive these reminders, you can manage your notification settings in the app.</p>
                        </div>
                    </div>
                </div>
            </body>
            </html>";
    }

    public string GetTaskDueTodayTemplate(TodoTask task, ApplicationUser user)
    {
        var taskUrl = $"{_appBaseUrl}/tasks/{task.Id}";
        var priorityClass = GetPriorityColorClass(task.Priority);

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Task Due Today</title>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #f39c12; color: white; padding: 10px 20px; border-radius: 5px 5px 0 0; }}
                    .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; border: 1px solid #ddd; }}
                    .task-title {{ font-size: 22px; margin-bottom: 15px; }}
                    .priority {{ display: inline-block; padding: 3px 8px; border-radius: 3px; color: white; font-size: 12px; margin-bottom: 15px; }}
                    .high {{ background-color: #e74c3c; }}
                    .medium {{ background-color: #f39c12; }}
                    .low {{ background-color: #2ecc71; }}
                    .details {{ margin-bottom: 20px; }}
                    .button {{ display: inline-block; background-color: #3498db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}
                    .footer {{ margin-top: 20px; font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Task Due Today</h1>
                    </div>
                    <div class='content'>
                        <p>Hello {user.FirstName ?? user.UserName},</p>
                        <p>You have a task that is due today:</p>
                        
                        <div class='task-title'>{task.Title}</div>
                        <div class='priority {priorityClass}'>Priority: {task.Priority}</div>
                        
                        <div class='details'>
                            <p><strong>Due:</strong> {task.DueDate?.ToString("h:mm tt") ?? "Today"}</p>
                            {(string.IsNullOrEmpty(task.Description) ? "" : $"<p><strong>Description:</strong> {task.Description}</p>")}
                            <p><strong>Status:</strong> {task.Status}</p>
                        </div>
                        
                        <a href='{taskUrl}' class='button'>View Task</a>
                        
                        <div class='footer'>
                            <p>This is an automated notification from your ToDo application.</p>
                            <p>If you no longer wish to receive these notifications, you can manage your notification settings in the app.</p>
                        </div>
                    </div>
                </div>
            </body>
            </html>";
    }

    public string GetTaskOverdueTemplate(TodoTask task, ApplicationUser user)
    {
        var taskUrl = $"{_appBaseUrl}/tasks/{task.Id}";
        var priorityClass = GetPriorityColorClass(task.Priority);

        var daysOverdue = (int)Math.Floor((DateTime.UtcNow - task.DueDate.Value).TotalDays);
        var overdueText = daysOverdue == 0
            ? "This task is overdue"
            : $"This task is overdue by {daysOverdue} {(daysOverdue == 1 ? "day" : "days")}";

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Task Overdue</title>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #e74c3c; color: white; padding: 10px 20px; border-radius: 5px 5px 0 0; }}
                    .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; border: 1px solid #ddd; }}
                    .task-title {{ font-size: 22px; margin-bottom: 15px; }}
                    .priority {{ display: inline-block; padding: 3px 8px; border-radius: 3px; color: white; font-size: 12px; margin-bottom: 15px; }}
                    .high {{ background-color: #e74c3c; }}
                    .medium {{ background-color: #f39c12; }}
                    .low {{ background-color: #2ecc71; }}
                    .overdue {{ color: #e74c3c; font-weight: bold; }}
                    .details {{ margin-bottom: 20px; }}
                    .button {{ display: inline-block; background-color: #3498db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}
                    .footer {{ margin-top: 20px; font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Task Overdue</h1>
                    </div>
                    <div class='content'>
                        <p>Hello {user.FirstName ?? user.UserName},</p>
                        <p class='overdue'>{overdueText}:</p>
                        
                        <div class='task-title'>{task.Title}</div>
                        <div class='priority {priorityClass}'>Priority: {task.Priority}</div>
                        
                        <div class='details'>
                            <p><strong>Due:</strong> {task.DueDate?.ToString("dddd, MMMM d, yyyy h:mm tt") ?? "Unknown"}</p>
                            {(string.IsNullOrEmpty(task.Description) ? "" : $"<p><strong>Description:</strong> {task.Description}</p>")}
                            <p><strong>Status:</strong> {task.Status}</p>
                        </div>
                        
                        <a href='{taskUrl}' class='button'>View Task</a>
                        
                        <div class='footer'>
                            <p>This is an automated notification from your ToDo application.</p>
                            <p>If you no longer wish to receive these notifications, you can manage your notification settings in the app.</p>
                        </div>
                    </div>
                </div>
            </body>
            </html>";
    }

    public string GetTaskCompletedTemplate(TodoTask task, ApplicationUser user)
    {
        var taskUrl = $"{_appBaseUrl}/tasks/{task.Id}";
        var priorityClass = GetPriorityColorClass(task.Priority);

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Task Completed</title>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #2ecc71; color: white; padding: 10px 20px; border-radius: 5px 5px 0 0; }}
                    .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; border: 1px solid #ddd; }}
                    .task-title {{ font-size: 22px; margin-bottom: 15px; }}
                    .completed {{ color: #2ecc71; font-weight: bold; }}
                    .details {{ margin-bottom: 20px; }}
                    .button {{ display: inline-block; background-color: #3498db; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}
                    .footer {{ margin-top: 20px; font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Task Completed</h1>
                    </div>
                    <div class='content'>
                        <p>Hello {user.FirstName ?? user.UserName},</p>
                        <p>The following task has been marked as completed:</p>
                        
                        <div class='task-title'>{task.Title}</div>
                        <p class='completed'>✓ Completed on {task.CompletedAt?.ToString("dddd, MMMM d, yyyy h:mm tt") ?? "Unknown"}</p>
                        
                        <div class='details'>
                            {(task.DueDate.HasValue ? $"<p><strong>Due:</strong> {task.DueDate?.ToString("dddd, MMMM d, yyyy h:mm tt")}</p>" : "")}
                            {(string.IsNullOrEmpty(task.Description) ? "" : $"<p><strong>Description:</strong> {task.Description}</p>")}
                        </div>
                        
                        <a href='{taskUrl}' class='button'>View Task</a>
                        
                        <div class='footer'>
                            <p>This is an automated notification from your ToDo application.</p>
                            <p>If you no longer wish to receive these notifications, you can manage your notification settings in the app.</p>
                        </div>
                    </div>
                </div>
            </body>
            </html>";
    }

    #region Helper Methods

    private string GetPriorityColorClass(TaskPriority priority)
    {
        return priority switch
        {
            TaskPriority.High => "high",
            TaskPriority.Medium => "medium",
            TaskPriority.Low => "low",
            _ => "medium"
        };
    }

    #endregion
}
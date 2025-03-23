using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Security;

public interface IActivityLogger
{
    Task LogActivityAsync(string userId, string action, string description, string ipAddress, bool isSuccessful);
    Task<IEnumerable<UserActivity>> GetUserActivityAsync(string userId, int take = 20);
    Task<IEnumerable<UserActivity>> GetRecentActivitiesAsync(int take = 100);
    Task<IEnumerable<UserActivity>> GetFailedAttemptsAsync(string ipAddress, int take = 20);
}

public class ActivityLogger : IActivityLogger
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivityLogger> _logger;

    public ActivityLogger(ApplicationDbContext context, ILogger<ActivityLogger> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActivityAsync(string userId, string action, string description, string ipAddress, bool isSuccessful)
    {
        var activity = new UserActivity
        {
            UserId = userId,
            Action = action,
            Description = description,
            IpAddress = ipAddress,
            IsSuccessful = isSuccessful,
            Timestamp = DateTime.UtcNow
        };

        _context.UserActivities.Add(activity);
        await _context.SaveChangesAsync();

        // Also log to standard logger
        if (isSuccessful)
        {
            _logger.LogInformation(
                "User {UserId} performed {Action} from {IpAddress}: {Description}",
                userId, action, ipAddress, description);
        }
        else
        {
            _logger.LogWarning(
                "User {UserId} failed at {Action} from {IpAddress}: {Description}",
                userId, action, ipAddress, description);
        }
    }

    public async Task<IEnumerable<UserActivity>> GetUserActivityAsync(string userId, int take = 20)
    {
        return await _context.UserActivities
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserActivity>> GetRecentActivitiesAsync(int take = 100)
    {
        return await _context.UserActivities
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserActivity>> GetFailedAttemptsAsync(string ipAddress, int take = 20)
    {
        return await _context.UserActivities
            .Where(a => a.IpAddress == ipAddress && !a.IsSuccessful)
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .ToListAsync();
    }
}
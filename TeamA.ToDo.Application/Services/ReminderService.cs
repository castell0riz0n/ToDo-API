using Microsoft.EntityFrameworkCore;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models.Todo;
using TeamA.ToDo.EntityFramework;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Application.Services;

public class ReminderService : IReminderService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecurringTaskService _recurringTaskService;

    public ReminderService(
        ApplicationDbContext context,
        IRecurringTaskService recurringTaskService)
    {
        _context = context;
        _recurringTaskService = recurringTaskService;
    }

    public async Task<List<ReminderDto>> GetRemindersForTaskAsync(Guid taskId, string userId)
    {
        // First check if task belongs to user
        var taskExists = await _context.TodoTasks
            .AnyAsync(t => t.Id == taskId && t.UserId == userId);

        if (!taskExists)
        {
            return new List<ReminderDto>();
        }

        var reminders = await _context.TaskReminders
            .Where(r => r.TodoTaskId == taskId)
            .OrderBy(r => r.ReminderTime)
            .ToListAsync();

        return reminders.Select(r => new ReminderDto
        {
            Id = r.Id,
            ReminderTime = r.ReminderTime,
            IsSent = r.IsSent,
            SentAt = r.SentAt
        }).ToList();
    }

    public async Task<ReminderDto> CreateReminderAsync(Guid taskId, string userId, CreateReminderDto dto)
    {
        // First check if task belongs to user
        var task = await _context.TodoTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            return null;
        }

        var reminder = new TaskReminder
        {
            Id = Guid.NewGuid(),
            TodoTaskId = taskId,
            ReminderTime = dto.ReminderTime,
            IsSent = false
        };

        await _context.TaskReminders.AddAsync(reminder);
        await _context.SaveChangesAsync();

        // Schedule the reminder
        await _recurringTaskService.ScheduleReminderAsync(reminder);

        return new ReminderDto
        {
            Id = reminder.Id,
            ReminderTime = reminder.ReminderTime,
            IsSent = reminder.IsSent,
            SentAt = reminder.SentAt
        };
    }

    public async Task<bool> DeleteReminderAsync(Guid id, Guid taskId, string userId)
    {
        // First check if task belongs to user
        var taskExists = await _context.TodoTasks
            .AnyAsync(t => t.Id == taskId && t.UserId == userId);

        if (!taskExists)
        {
            return false;
        }

        var reminder = await _context.TaskReminders
            .FirstOrDefaultAsync(r => r.Id == id && r.TodoTaskId == taskId);

        if (reminder == null)
        {
            return false;
        }

        _context.TaskReminders.Remove(reminder);
        await _context.SaveChangesAsync();
        return true;
    }
}
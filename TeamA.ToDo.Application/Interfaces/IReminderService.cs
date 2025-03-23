using TodoApp.API.DTOs;

namespace TeamA.ToDo.Application.Interfaces;

public interface IReminderService
{
    Task<List<ReminderDto>> GetRemindersForTaskAsync(Guid taskId, string userId);
    Task<ReminderDto> CreateReminderAsync(Guid taskId, string userId, CreateReminderDto dto);
    Task<bool> DeleteReminderAsync(Guid id, Guid taskId, string userId);
}
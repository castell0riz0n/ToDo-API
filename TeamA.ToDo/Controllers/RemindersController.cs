using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.Interfaces;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Host.Controllers;

[ApiController]
[Route("api/tasks/{taskId}/reminders")]
[Authorize]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _reminderService;

    public RemindersController(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReminderDto>>> GetReminders(Guid taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var reminders = await _reminderService.GetRemindersForTaskAsync(taskId, userId);
        return Ok(reminders);
    }

    [HttpPost]
    public async Task<ActionResult<ReminderDto>> CreateReminder(Guid taskId, CreateReminderDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var reminder = await _reminderService.CreateReminderAsync(taskId, userId, dto);

        if (reminder == null)
        {
            return NotFound("Task not found");
        }

        return CreatedAtAction(nameof(GetReminders), new { taskId }, reminder);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteReminder(Guid taskId, Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _reminderService.DeleteReminderAsync(id, taskId, userId);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
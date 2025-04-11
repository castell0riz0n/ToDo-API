using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.Interfaces;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Host.Controllers.Todo;

[ApiController]
[Route("api/tasks/{taskId}/notes")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TodoNoteDto>>> GetNotes(Guid taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notes = await _noteService.GetNotesForTaskAsync(taskId, userId);
        return Ok(notes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoNoteDto>> GetNote(Guid taskId, Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = await _noteService.GetNoteByIdAsync(id, taskId, userId);

        if (note == null)
        {
            return NotFound();
        }

        return Ok(note);
    }

    [HttpPost]
    public async Task<ActionResult<TodoNoteDto>> CreateNote(Guid taskId, CreateTodoNoteDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = await _noteService.CreateNoteAsync(taskId, userId, dto);

        if (note == null)
        {
            return NotFound("Task not found");
        }

        return CreatedAtAction(nameof(GetNote), new { taskId, id = note.Id }, note);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TodoNoteDto>> UpdateNote(Guid taskId, Guid id, UpdateTodoNoteDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var note = await _noteService.UpdateNoteAsync(id, taskId, userId, dto);

        if (note == null)
        {
            return NotFound();
        }

        return Ok(note);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNote(Guid taskId, Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _noteService.DeleteNoteAsync(id, taskId, userId);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
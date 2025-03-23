using Microsoft.EntityFrameworkCore;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models.Todo;
using TeamA.ToDo.EntityFramework;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Application.Services;

public class NoteService : INoteService
{
    private readonly ApplicationDbContext _context;

    public NoteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TodoNoteDto>> GetNotesForTaskAsync(Guid taskId, string userId)
    {
        // First check if task belongs to user
        var taskExists = await _context.TodoTasks
            .AnyAsync(t => t.Id == taskId && t.UserId == userId);

        if (!taskExists)
        {
            return new List<TodoNoteDto>();
        }

        var notes = await _context.TodoNotes
            .Where(n => n.TodoTaskId == taskId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notes.Select(n => new TodoNoteDto
        {
            Id = n.Id,
            Content = n.Content,
            CreatedAt = n.CreatedAt,
            LastModifiedAt = n.LastModifiedAt
        }).ToList();
    }

    public async Task<TodoNoteDto> GetNoteByIdAsync(Guid id, Guid taskId, string userId)
    {
        // First check if task belongs to user
        var taskExists = await _context.TodoTasks
            .AnyAsync(t => t.Id == taskId && t.UserId == userId);

        if (!taskExists)
        {
            return null;
        }

        var note = await _context.TodoNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.TodoTaskId == taskId);

        if (note == null)
        {
            return null;
        }

        return new TodoNoteDto
        {
            Id = note.Id,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            LastModifiedAt = note.LastModifiedAt
        };
    }

    public async Task<TodoNoteDto> CreateNoteAsync(Guid taskId, string userId, CreateTodoNoteDto dto)
    {
        // First check if task belongs to user
        var task = await _context.TodoTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);

        if (task == null)
        {
            return null;
        }

        var note = new TodoNote
        {
            Id = Guid.NewGuid(),
            TodoTaskId = taskId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        await _context.TodoNotes.AddAsync(note);
        await _context.SaveChangesAsync();

        return new TodoNoteDto
        {
            Id = note.Id,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            LastModifiedAt = note.LastModifiedAt
        };
    }

    public async Task<TodoNoteDto> UpdateNoteAsync(Guid id, Guid taskId, string userId, UpdateTodoNoteDto dto)
    {
        // First check if task belongs to user
        var taskExists = await _context.TodoTasks
            .AnyAsync(t => t.Id == taskId && t.UserId == userId);

        if (!taskExists)
        {
            return null;
        }

        var note = await _context.TodoNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.TodoTaskId == taskId);

        if (note == null)
        {
            return null;
        }

        note.Content = dto.Content;
        note.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new TodoNoteDto
        {
            Id = note.Id,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            LastModifiedAt = note.LastModifiedAt
        };
    }

    public async Task<bool> DeleteNoteAsync(Guid id, Guid taskId, string userId)
    {
        // First check if task belongs to user
        var taskExists = await _context.TodoTasks
            .AnyAsync(t => t.Id == taskId && t.UserId == userId);

        if (!taskExists)
        {
            return false;
        }

        var note = await _context.TodoNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.TodoTaskId == taskId);

        if (note == null)
        {
            return false;
        }

        _context.TodoNotes.Remove(note);
        await _context.SaveChangesAsync();
        return true;
    }
}
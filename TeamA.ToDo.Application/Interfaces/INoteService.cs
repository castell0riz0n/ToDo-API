using TodoApp.API.DTOs;

namespace TeamA.ToDo.Application.Interfaces;

public interface INoteService
{
    Task<List<TodoNoteDto>> GetNotesForTaskAsync(Guid taskId, string userId);
    Task<TodoNoteDto> GetNoteByIdAsync(Guid id, Guid taskId, string userId);
    Task<TodoNoteDto> CreateNoteAsync(Guid taskId, string userId, CreateTodoNoteDto dto);
    Task<TodoNoteDto> UpdateNoteAsync(Guid id, Guid taskId, string userId, UpdateTodoNoteDto dto);
    Task<bool> DeleteNoteAsync(Guid id, Guid taskId, string userId);
}
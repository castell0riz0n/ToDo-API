using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Core.Shared.Enums.Todo;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Application.Interfaces;

public interface ITodoTaskService
{
    Task<PagedResponse<TodoTaskDto>> GetTasksAsync(string userId, TaskFilterDto filter);
    Task<TodoTaskDto> GetTaskByIdAsync(Guid id, string userId);
    Task<TodoTaskDto> CreateTaskAsync(string userId, CreateTodoTaskDto dto);
    Task<TodoTaskDto> UpdateTaskAsync(Guid id, string userId, UpdateTodoTaskDto dto);
    Task<bool> DeleteTaskAsync(Guid id, string userId);
    Task<TodoTaskDto> ChangeTaskStatusAsync(Guid id, string userId, TodoTaskStatus status);
    Task<TaskStatisticsDto> GetTaskStatisticsAsync(string userId);
}
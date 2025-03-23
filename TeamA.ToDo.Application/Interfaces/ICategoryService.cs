using TodoApp.API.DTOs;

namespace TeamA.ToDo.Application.Interfaces;

public interface ICategoryService
{
    Task<List<TaskCategoryDto>> GetCategoriesAsync(string userId);
    Task<TaskCategoryDto> GetCategoryByIdAsync(Guid id, string userId);
    Task<TaskCategoryDto> CreateCategoryAsync(string userId, CreateTaskCategoryDto dto);
    Task<TaskCategoryDto> UpdateCategoryAsync(Guid id, string userId, UpdateTaskCategoryDto dto);
    Task<bool> DeleteCategoryAsync(Guid id, string userId);
}
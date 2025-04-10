using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;

namespace TeamA.ToDo.Application.Interfaces.Expenses;

public interface IExpenseCategoryService
{
    Task<ServiceResponse<List<ExpenseCategoryDto>>> GetCategoriesAsync(string userId);
    Task<ServiceResponse<ExpenseCategoryDto>> GetCategoryByIdAsync(Guid id, string userId);
    Task<ServiceResponse<ExpenseCategoryDto>> CreateCategoryAsync(string userId, CreateExpenseCategoryDto dto);
    Task<ServiceResponse<ExpenseCategoryDto>> UpdateCategoryAsync(Guid id, string userId, UpdateExpenseCategoryDto dto);
    Task<ServiceResponse<bool>> DeleteCategoryAsync(Guid id, string userId);
}
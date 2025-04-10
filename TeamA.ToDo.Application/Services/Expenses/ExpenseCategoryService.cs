using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.Expenses;
using TeamA.ToDo.Core.Models.Expenses;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services.Expenses;

public class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExpenseCategoryService> _logger;

    public ExpenseCategoryService(
        ApplicationDbContext context,
        ILogger<ExpenseCategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResponse<List<ExpenseCategoryDto>>> GetCategoriesAsync(string userId)
    {
        var response = new ServiceResponse<List<ExpenseCategoryDto>>();

        try
        {
            // Get user's categories and system categories
            var categories = await _context.ExpenseCategories
                .Where(c => c.UserId == userId || c.IsSystem)
                .ToListAsync();

            // Get category expense counts
            var categoryCounts = await _context.Expenses
                .Where(e => e.UserId == userId)
                .GroupBy(e => e.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

            var categoryDtos = categories.Select(c => new ExpenseCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Color = c.Color,
                Icon = c.Icon,
                IsSystem = c.IsSystem,
                ExpenseCount = categoryCounts.ContainsKey(c.Id) ? categoryCounts[c.Id] : 0
            }).ToList();

            response.Data = categoryDtos;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense categories");
            response.Success = false;
            response.Message = "Failed to retrieve expense categories";
            return response;
        }
    }

    public async Task<ServiceResponse<ExpenseCategoryDto>> GetCategoryByIdAsync(Guid id, string userId)
    {
        var response = new ServiceResponse<ExpenseCategoryDto>();

        try
        {
            var category = await _context.ExpenseCategories
                .FirstOrDefaultAsync(c => c.Id == id && (c.UserId == userId || c.IsSystem));

            if (category == null)
            {
                response.Success = false;
                response.Message = "Category not found";
                return response;
            }

            var expenseCount = await _context.Expenses
                .CountAsync(e => e.CategoryId == id && e.UserId == userId);

            response.Data = new ExpenseCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                Icon = category.Icon,
                IsSystem = category.IsSystem,
                ExpenseCount = expenseCount
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense category");
            response.Success = false;
            response.Message = "Failed to retrieve expense category";
            return response;
        }
    }

    public async Task<ServiceResponse<ExpenseCategoryDto>> CreateCategoryAsync(string userId,
        CreateExpenseCategoryDto dto)
    {
        var response = new ServiceResponse<ExpenseCategoryDto>();

        try
        {
            // Check if category with the same name already exists
            var existingCategory = await _context.ExpenseCategories
                .FirstOrDefaultAsync(c => c.Name == dto.Name && (c.UserId == userId || c.IsSystem));

            if (existingCategory != null)
            {
                response.Success = false;
                response.Message = "A category with this name already exists";
                return response;
            }

            // Create new category
            var category = new ExpenseCategory
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Color = dto.Color ?? "#3498db", // Default color if not provided
                Icon = dto.Icon,
                UserId = userId,
                IsSystem = false
            };

            await _context.ExpenseCategories.AddAsync(category);
            await _context.SaveChangesAsync();

            response.Data = new ExpenseCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                Icon = category.Icon,
                IsSystem = category.IsSystem,
                ExpenseCount = 0
            };

            response.Message = "Category created successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense category");
            response.Success = false;
            response.Message = "Failed to create expense category";
            return response;
        }
    }

    public async Task<ServiceResponse<ExpenseCategoryDto>> UpdateCategoryAsync(Guid id, string userId,
        UpdateExpenseCategoryDto dto)
    {
        var response = new ServiceResponse<ExpenseCategoryDto>();

        try
        {
            var category = await _context.ExpenseCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                response.Success = false;
                response.Message = "Category not found or you don't have permission to edit it";
                return response;
            }

            // Cannot edit system categories
            if (category.IsSystem)
            {
                response.Success = false;
                response.Message = "System categories cannot be edited";
                return response;
            }

            // Check for name uniqueness if changing the name
            if (!string.IsNullOrEmpty(dto.Name) && dto.Name != category.Name)
            {
                var existingCategory = await _context.ExpenseCategories
                    .FirstOrDefaultAsync(c => c.Name == dto.Name && (c.UserId == userId || c.IsSystem) && c.Id != id);

                if (existingCategory != null)
                {
                    response.Success = false;
                    response.Message = "A category with this name already exists";
                    return response;
                }

                category.Name = dto.Name;
            }

            if (dto.Description != null)
            {
                category.Description = dto.Description;
            }

            if (dto.Color != null)
            {
                category.Color = dto.Color;
            }

            if (dto.Icon != null)
            {
                category.Icon = dto.Icon;
            }

            await _context.SaveChangesAsync();

            var expenseCount = await _context.Expenses
                .CountAsync(e => e.CategoryId == id && e.UserId == userId);

            response.Data = new ExpenseCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                Icon = category.Icon,
                IsSystem = category.IsSystem,
                ExpenseCount = expenseCount
            };

            response.Message = "Category updated successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense category");
            response.Success = false;
            response.Message = "Failed to update expense category";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> DeleteCategoryAsync(Guid id, string userId)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var category = await _context.ExpenseCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                response.Success = false;
                response.Message = "Category not found or you don't have permission to delete it";
                return response;
            }

            // Cannot delete system categories
            if (category.IsSystem)
            {
                response.Success = false;
                response.Message = "System categories cannot be deleted";
                return response;
            }

            // Check if the category is associated with any expenses
            var hasExpenses = await _context.Expenses
                .AnyAsync(e => e.CategoryId == id && e.UserId == userId);

            if (hasExpenses)
            {
                response.Success = false;
                response.Message = "Cannot delete category because it has associated expenses";
                return response;
            }

            // Check if the category is associated with any budgets
            var hasBudgets = await _context.Budgets
                .AnyAsync(b => b.CategoryId == id && b.UserId == userId);

            if (hasBudgets)
            {
                response.Success = false;
                response.Message = "Cannot delete category because it has associated budgets";
                return response;
            }

            _context.ExpenseCategories.Remove(category);
            await _context.SaveChangesAsync();

            response.Data = true;
            response.Message = "Category deleted successfully";
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense category");
            response.Success = false;
            response.Message = "Failed to delete expense category";
            return response;
        }
    }
}
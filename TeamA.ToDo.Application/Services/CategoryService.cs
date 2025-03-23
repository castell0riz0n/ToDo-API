using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models.Todo;
using TeamA.ToDo.EntityFramework;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskCategoryDto>> GetCategoriesAsync(string userId)
        {
            var categories = await _context.TaskCategories
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var tasks = await _context.TodoTasks
                .Where(t => t.UserId == userId && t.CategoryId != null)
                .GroupBy(t => t.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryId.Value, x => x.Count);

            return categories.Select(c => new TaskCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Color = c.Color,
                TaskCount = tasks.ContainsKey(c.Id) ? tasks[c.Id] : 0
            }).ToList();
        }

        public async Task<TaskCategoryDto> GetCategoryByIdAsync(Guid id, string userId)
        {
            var category = await _context.TaskCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                return null;
            }

            var taskCount = await _context.TodoTasks
                .CountAsync(t => t.CategoryId == id);

            return new TaskCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color,
                TaskCount = taskCount
            };
        }

        public async Task<TaskCategoryDto> CreateCategoryAsync(string userId, CreateTaskCategoryDto dto)
        {
            // Check if category with the same name already exists
            var existingCategory = await _context.TaskCategories
                .FirstOrDefaultAsync(c => c.Name == dto.Name && c.UserId == userId);

            if (existingCategory != null)
            {
                throw new InvalidOperationException("A category with this name already exists");
            }

            var category = new TaskCategory
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Color = dto.Color ?? "#3498db", // Default color if not provided
                UserId = userId
            };

            await _context.TaskCategories.AddAsync(category);
            await _context.SaveChangesAsync();

            return new TaskCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color,
                TaskCount = 0
            };
        }

        public async Task<TaskCategoryDto> UpdateCategoryAsync(Guid id, string userId, UpdateTaskCategoryDto dto)
        {
            var category = await _context.TaskCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                return null;
            }

            // Check if new name conflicts with existing category
            if (!string.IsNullOrEmpty(dto.Name) && dto.Name != category.Name)
            {
                var existingCategory = await _context.TaskCategories
                    .FirstOrDefaultAsync(c => c.Name == dto.Name && c.UserId == userId && c.Id != id);

                if (existingCategory != null)
                {
                    throw new InvalidOperationException("A category with this name already exists");
                }

                category.Name = dto.Name;
            }

            if (!string.IsNullOrEmpty(dto.Color))
            {
                category.Color = dto.Color;
            }

            await _context.SaveChangesAsync();

            var taskCount = await _context.TodoTasks
                .CountAsync(t => t.CategoryId == id);

            return new TaskCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color,
                TaskCount = taskCount
            };
        }

        public async Task<bool> DeleteCategoryAsync(Guid id, string userId)
        {
            var category = await _context.TaskCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (category == null)
            {
                return false;
            }

            // Find all tasks with this category and set their CategoryId to null
            var tasksWithCategory = await _context.TodoTasks
                .Where(t => t.CategoryId == id)
                .ToListAsync();

            foreach (var task in tasksWithCategory)
            {
                task.CategoryId = null;
            }

            _context.TaskCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
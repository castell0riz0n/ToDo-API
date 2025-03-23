using Microsoft.EntityFrameworkCore;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models.Todo;
using TeamA.ToDo.Core.Shared.Enums.Todo;
using TeamA.ToDo.EntityFramework;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Application.Services
{
    public class TodoTaskService : ITodoTaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecurringTaskService _recurringTaskService;

        public TodoTaskService(
            ApplicationDbContext context,
            IRecurringTaskService recurringTaskService)
        {
            _context = context;
            _recurringTaskService = recurringTaskService;
        }

        public async Task<PagedResponse<TodoTaskDto>> GetTasksAsync(string userId, TaskFilterDto filter)
        {
            var query = _context.TodoTasks
                .Include(t => t.Category)
                .Include(t => t.RecurrenceInfo)
                .Include(t => t.Reminders)
                .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.Notes)
                .Where(t => t.UserId == userId);

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(t =>
                    t.Title.Contains(filter.SearchTerm) ||
                    t.Description.Contains(filter.SearchTerm) ||
                    t.TaskTags.Any(tt => tt.Tag.Name.Contains(filter.SearchTerm)));
            }

            if (filter.Priority.HasValue)
            {
                query = query.Where(t => t.Priority == filter.Priority.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(t => t.Status == filter.Status.Value);
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
            }

            if (filter.Tags != null && filter.Tags.Any())
            {
                query = query.Where(t => t.TaskTags.Any(tt => filter.Tags.Contains(tt.Tag.Name)));
            }

            if (filter.DueDateFrom.HasValue)
            {
                query = query.Where(t => t.DueDate >= filter.DueDateFrom.Value);
            }

            if (filter.DueDateTo.HasValue)
            {
                query = query.Where(t => t.DueDate <= filter.DueDateTo.Value);
            }

            if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
            {
                var today = DateTime.UtcNow.Date;
                query = query.Where(t => t.DueDate < today && t.Status != TodoTaskStatus.Completed);
            }

            if (filter.IsCompleted.HasValue)
            {
                query = query.Where(t =>
                    filter.IsCompleted.Value ?
                    t.Status == TodoTaskStatus.Completed :
                    t.Status != TodoTaskStatus.Completed);
            }

            if (filter.IsRecurring.HasValue)
            {
                query = query.Where(t => t.IsRecurring == filter.IsRecurring.Value);
            }

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "title" => filter.SortAscending ?
                    query.OrderBy(t => t.Title) :
                    query.OrderByDescending(t => t.Title),
                "priority" => filter.SortAscending ?
                    query.OrderBy(t => t.Priority) :
                    query.OrderByDescending(t => t.Priority),
                "status" => filter.SortAscending ?
                    query.OrderBy(t => t.Status) :
                    query.OrderByDescending(t => t.Status),
                "createdat" => filter.SortAscending ?
                    query.OrderBy(t => t.CreatedAt) :
                    query.OrderByDescending(t => t.CreatedAt),
                "duedate" => filter.SortAscending ?
                    query.OrderBy(t => t.DueDate) :
                    query.OrderByDescending(t => t.DueDate),
                _ => filter.SortAscending ?
                    query.OrderBy(t => t.DueDate) :
                    query.OrderByDescending(t => t.DueDate)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            var tasks = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var taskDtos = tasks.Select(MapToDto).ToList();

            return new PagedResponse<TodoTaskDto>()
            {
                Items = taskDtos,
                TotalItems = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = totalPages
            };
        }

        public async Task<TodoTaskDto> GetTaskByIdAsync(Guid id, string userId)
        {
            var task = await _context.TodoTasks
                .Include(t => t.Category)
                .Include(t => t.RecurrenceInfo)
                .Include(t => t.Reminders)
                .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.Notes)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
            {
                return null;
            }

            return MapToDto(task);
        }

        public async Task<TodoTaskDto> CreateTaskAsync(string userId, CreateTodoTaskDto dto)
        {
            var task = new TodoTask
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                Status = dto.Status,
                DueDate = dto.DueDate,
                UserId = userId,
                CategoryId = dto.CategoryId,
                IsRecurring = dto.IsRecurring,
                CreatedAt = DateTime.UtcNow
            };

            // Handle recurrence info
            if (task.IsRecurring && dto.RecurrenceInfo != null)
            {
                task.RecurrenceInfo = new RecurrenceInfo
                {
                    Id = Guid.NewGuid(),
                    RecurrenceType = dto.RecurrenceInfo.RecurrenceType,
                    Interval = dto.RecurrenceInfo.Interval,
                    StartDate = dto.RecurrenceInfo.StartDate,
                    EndDate = dto.RecurrenceInfo.EndDate,
                    CustomCronExpression = dto.RecurrenceInfo.CustomCronExpression,
                    DayOfMonth = dto.RecurrenceInfo.DayOfMonth,
                    DayOfWeek = dto.RecurrenceInfo.DayOfWeek
                };
            }

            // Handle reminders
            if (dto.Reminders != null && dto.Reminders.Any())
            {
                foreach (var reminderDto in dto.Reminders)
                {
                    task.Reminders.Add(new TaskReminder
                    {
                        Id = Guid.NewGuid(),
                        ReminderTime = reminderDto.ReminderTime
                    });
                }
            }

            // Handle tags
            if (dto.Tags != null && dto.Tags.Any())
            {
                foreach (var tagName in dto.Tags)
                {
                    var tag = await _context.Tags
                        .FirstOrDefaultAsync(t => t.Name == tagName && t.UserId == userId);

                    if (tag == null)
                    {
                        tag = new Tag
                        {
                            Id = Guid.NewGuid(),
                            Name = tagName,
                            UserId = userId
                        };
                        await _context.Tags.AddAsync(tag);
                    }

                    task.TaskTags.Add(new TaskTag
                    {
                        Id = Guid.NewGuid(),
                        TodoTaskId = task.Id,
                        TagId = tag.Id
                    });
                }
            }

            await _context.TodoTasks.AddAsync(task);
            await _context.SaveChangesAsync();

            // Schedule recurring task if required
            if (task.IsRecurring)
            {
                await _recurringTaskService.ScheduleRecurringTaskAsync(task);
            }

            // Schedule reminders if required
            if (task.Reminders.Any())
            {
                foreach (var reminder in task.Reminders)
                {
                    await _recurringTaskService.ScheduleReminderAsync(reminder);
                }
            }

            return await GetTaskByIdAsync(task.Id, userId);
        }

        public async Task<TodoTaskDto> UpdateTaskAsync(Guid id, string userId, UpdateTodoTaskDto dto)
        {
            var task = await _context.TodoTasks
                .Include(t => t.RecurrenceInfo)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
            {
                return null;
            }

            // Update basic properties
            if (!string.IsNullOrEmpty(dto.Title))
            {
                task.Title = dto.Title;
            }

            if (dto.Description != null)
            {
                task.Description = dto.Description;
            }

            if (dto.Priority.HasValue)
            {
                task.Priority = dto.Priority.Value;
            }

            if (dto.Status.HasValue)
            {
                task.Status = dto.Status.Value;
                if (dto.Status.Value == TodoTaskStatus.Completed && !task.CompletedAt.HasValue)
                {
                    task.CompletedAt = DateTime.UtcNow;
                }
                else if (dto.Status.Value != TodoTaskStatus.Completed)
                {
                    task.CompletedAt = null;
                }
            }

            if (dto.DueDate.HasValue)
            {
                task.DueDate = dto.DueDate.Value;
            }

            if (dto.CategoryId.HasValue)
            {
                task.CategoryId = dto.CategoryId.Value;
            }

            task.LastModifiedAt = DateTime.UtcNow;

            // Handle recurrence info
            if (dto.IsRecurring.HasValue)
            {
                task.IsRecurring = dto.IsRecurring.Value;
            }

            if (task.IsRecurring && dto.RecurrenceInfo != null)
            {
                if (task.RecurrenceInfo == null)
                {
                    task.RecurrenceInfo = new RecurrenceInfo
                    {
                        Id = Guid.NewGuid(),
                        TodoTaskId = task.Id
                    };
                }

                if (dto.RecurrenceInfo.RecurrenceType.HasValue)
                {
                    task.RecurrenceInfo.RecurrenceType = dto.RecurrenceInfo.RecurrenceType.Value;
                }

                if (dto.RecurrenceInfo.Interval.HasValue)
                {
                    task.RecurrenceInfo.Interval = dto.RecurrenceInfo.Interval.Value;
                }

                if (dto.RecurrenceInfo.StartDate.HasValue)
                {
                    task.RecurrenceInfo.StartDate = dto.RecurrenceInfo.StartDate.Value;
                }

                if (dto.RecurrenceInfo.EndDate.HasValue)
                {
                    task.RecurrenceInfo.EndDate = dto.RecurrenceInfo.EndDate.Value;
                }

                if (dto.RecurrenceInfo.CustomCronExpression != null)
                {
                    task.RecurrenceInfo.CustomCronExpression = dto.RecurrenceInfo.CustomCronExpression;
                }

                if (dto.RecurrenceInfo.DayOfMonth.HasValue)
                {
                    task.RecurrenceInfo.DayOfMonth = dto.RecurrenceInfo.DayOfMonth.Value;
                }

                if (dto.RecurrenceInfo.DayOfWeek.HasValue)
                {
                    task.RecurrenceInfo.DayOfWeek = dto.RecurrenceInfo.DayOfWeek.Value;
                }

                // Update recurring task scheduling
                await _recurringTaskService.UpdateRecurringTaskScheduleAsync(task);
            }
            else if (!task.IsRecurring && task.RecurrenceInfo != null)
            {
                // Remove recurrence info if task is no longer recurring
                _context.RecurrenceInfos.Remove(task.RecurrenceInfo);

                // Cancel scheduled recurring tasks
                await _recurringTaskService.CancelRecurringTaskAsync(task.Id);
            }

            await _context.SaveChangesAsync();
            return await GetTaskByIdAsync(task.Id, userId);
        }

        public async Task<bool> DeleteTaskAsync(Guid id, string userId)
        {
            var task = await _context.TodoTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
            {
                return false;
            }

            // Cancel any scheduled recurring tasks or reminders
            if (task.IsRecurring)
            {
                await _recurringTaskService.CancelRecurringTaskAsync(task.Id);
            }

            _context.TodoTasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TodoTaskDto> ChangeTaskStatusAsync(Guid id, string userId, TodoTaskStatus status)
        {
            var task = await _context.TodoTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
            {
                return null;
            }

            task.Status = status;
            task.LastModifiedAt = DateTime.UtcNow;

            if (status == TodoTaskStatus.Completed && !task.CompletedAt.HasValue)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else if (status != TodoTaskStatus.Completed)
            {
                task.CompletedAt = null;
            }

            await _context.SaveChangesAsync();
            return await GetTaskByIdAsync(task.Id, userId);
        }

        public async Task<TaskStatisticsDto> GetTaskStatisticsAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            var endOfToday = today.AddDays(1).AddTicks(-1);
            var startOfNextWeek = today.AddDays(7);

            var allTasks = await _context.TodoTasks
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var tasksByCategory = await _context.TodoTasks
                .Where(t => t.UserId == userId && t.CategoryId != null)
                .GroupBy(t => t.Category.Name)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Category, x => x.Count);

            var completedTasksByWeekday = allTasks
                .Where(t => t.Status == TodoTaskStatus.Completed && t.CompletedAt.HasValue)
                .GroupBy(t => t.CompletedAt.Value.DayOfWeek)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Count()
                );

            var statistics = new TaskStatisticsDto
            {
                TotalTasks = allTasks.Count,
                CompletedTasks = allTasks.Count(t => t.Status == TodoTaskStatus.Completed),
                InProgressTasks = allTasks.Count(t => t.Status == TodoTaskStatus.InProgress),
                NotStartedTasks = allTasks.Count(t => t.Status == TodoTaskStatus.NotStarted),
                OverdueTasks = allTasks.Count(t => t.DueDate.HasValue && t.DueDate < today && t.Status != TodoTaskStatus.Completed),
                DueTodayTasks = allTasks.Count(t => t.DueDate.HasValue && t.DueDate >= today && t.DueDate <= endOfToday && t.Status != TodoTaskStatus.Completed),
                UpcomingTasks = allTasks.Count(t => t.DueDate.HasValue && t.DueDate > endOfToday && t.DueDate <= startOfNextWeek && t.Status != TodoTaskStatus.Completed),
                TasksByCategory = tasksByCategory,
                TasksByPriority = new Dictionary<string, int>
                {
                    { "High", allTasks.Count(t => t.Priority == TaskPriority.High) },
                    { "Medium", allTasks.Count(t => t.Priority == TaskPriority.Medium) },
                    { "Low", allTasks.Count(t => t.Priority == TaskPriority.Low) }
                },
                CompletionByWeekday = completedTasksByWeekday
            };

            statistics.CompletionRate = statistics.TotalTasks > 0
                ? Math.Round((double)statistics.CompletedTasks / statistics.TotalTasks * 100, 2)
                : 0;

            return statistics;
        }

        private TodoTaskDto MapToDto(TodoTask task)
        {
            return new TodoTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                Status = task.Status,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt,
                LastModifiedAt = task.LastModifiedAt,
                CategoryId = task.CategoryId,
                CategoryName = task.Category?.Name,
                IsRecurring = task.IsRecurring,
                RecurrenceInfo = task.RecurrenceInfo != null ? new RecurrenceInfoDto
                {
                    Id = task.RecurrenceInfo.Id,
                    RecurrenceType = task.RecurrenceInfo.RecurrenceType,
                    Interval = task.RecurrenceInfo.Interval,
                    StartDate = task.RecurrenceInfo.StartDate,
                    EndDate = task.RecurrenceInfo.EndDate,
                    CustomCronExpression = task.RecurrenceInfo.CustomCronExpression,
                    DayOfMonth = task.RecurrenceInfo.DayOfMonth,
                    DayOfWeek = task.RecurrenceInfo.DayOfWeek
                } : null,
                Reminders = task.Reminders.Select(r => new ReminderDto
                {
                    Id = r.Id,
                    ReminderTime = r.ReminderTime,
                    IsSent = r.IsSent,
                    SentAt = r.SentAt
                }).ToList(),
                Tags = task.TaskTags.Select(tt => tt.Tag.Name).ToList(),
                Notes = task.Notes.Select(n => new TodoNoteDto
                {
                    Id = n.Id,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt,
                    LastModifiedAt = n.LastModifiedAt
                }).ToList()
            };
        }
    }
}
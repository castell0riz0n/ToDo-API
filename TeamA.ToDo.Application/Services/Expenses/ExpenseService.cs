﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.Expenses.Reporting;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.Expenses;
using TeamA.ToDo.Core.Models;
using TeamA.ToDo.Core.Models.Expenses;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services.Expenses
{
    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecurringExpenseService _recurringExpenseService;
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(
            ApplicationDbContext context,
            IRecurringExpenseService recurringExpenseService,
            ILogger<ExpenseService> logger)
        {
            _context = context;
            _recurringExpenseService = recurringExpenseService;
            _logger = logger;
        }

        public async Task<PagedResponse<ExpenseDto>> GetExpensesAsync(string userId, ExpenseFilterDto filter)
        {
            var query = _context.Expenses
                .Include(e => e.Category)
                .Include(e => e.PaymentMethod)
                .Include(e => e.RecurrenceInfo)
                .Include(e => e.Tags)
                .Where(e => e.UserId == userId);

            // Apply filters
            query = ApplyFilters(query, filter);

            // Apply sorting
            query = ApplySorting(query, filter);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            var expenses = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var expenseDtos = expenses.Select(MapToDto).ToList();

            return new PagedResponse<ExpenseDto>
            {
                Items = expenseDtos,
                TotalItems = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = totalPages
            };
        }

        public async Task<ServiceResponse<ExpenseDto>> GetExpenseByIdAsync(Guid id, string userId)
        {
            var response = new ServiceResponse<ExpenseDto>();

            try
            {
                var expense = await _context.Expenses
                    .Include(e => e.Category)
                    .Include(e => e.PaymentMethod)
                    .Include(e => e.RecurrenceInfo)
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

                if (expense == null)
                {
                    response.Success = false;
                    response.Message = "Expense not found";
                    return response;
                }

                response.Data = MapToDto(expense);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expense");
                response.Success = false;
                response.Message = "Failed to retrieve expense";
                return response;
            }
        }

        public async Task<ServiceResponse<ExpenseDto>> CreateExpenseAsync(string userId, CreateExpenseDto dto)
        {
            var response = new ServiceResponse<ExpenseDto>();

            try
            {
                // Check if category exists
                var category = await _context.ExpenseCategories
                    .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && (c.UserId == userId || c.IsSystem));

                if (category == null)
                {
                    response.Success = false;
                    response.Message = "Category not found";
                    return response;
                }

                // Check if payment method exists (if provided)
                if (dto.PaymentMethodId.HasValue)
                {
                    var paymentMethod = await _context.PaymentMethods
                        .FirstOrDefaultAsync(p => p.Id == dto.PaymentMethodId && p.UserId == userId);

                    if (paymentMethod == null)
                    {
                        response.Success = false;
                        response.Message = "Payment method not found";
                        return response;
                    }
                }

                // Create expense
                var expense = new Expense
                {
                    Id = Guid.NewGuid(),
                    Amount = dto.Amount,
                    Description = dto.Description,
                    Date = dto.Date,
                    CategoryId = dto.CategoryId,
                    UserId = userId,
                    PaymentMethodId = dto.PaymentMethodId,
                    IsRecurring = dto.IsRecurring,
                    ReceiptUrl = dto.ReceiptUrl,
                    CreatedAt = DateTime.UtcNow
                };

                // Handle recurrence info
                if (expense.IsRecurring && dto.RecurrenceInfo != null)
                {
                    expense.RecurrenceInfo = new ExpenseRecurrence
                    {
                        Id = Guid.NewGuid(),
                        ExpenseId = expense.Id,
                        RecurrenceType = dto.RecurrenceInfo.RecurrenceType,
                        Interval = dto.RecurrenceInfo.Interval,
                        StartDate = dto.RecurrenceInfo.StartDate,
                        EndDate = dto.RecurrenceInfo.EndDate,
                        CustomCronExpression = dto.RecurrenceInfo.CustomCronExpression,
                        DayOfMonth = dto.RecurrenceInfo.DayOfMonth,
                        DayOfWeek = dto.RecurrenceInfo.DayOfWeek
                    };
                }

                // Handle tags
                if (dto.Tags != null && dto.Tags.Any())
                {
                    foreach (var tagName in dto.Tags)
                    {
                        var tag = await _context.ExpenseTags
                            .FirstOrDefaultAsync(t => t.Name == tagName && t.UserId == userId);

                        if (tag == null)
                        {
                            tag = new ExpenseTag
                            {
                                Id = Guid.NewGuid(),
                                Name = tagName,
                                UserId = userId
                            };
                            await _context.ExpenseTags.AddAsync(tag);
                        }

                        expense.Tags.Add(tag);
                    }
                }

                await _context.Expenses.AddAsync(expense);
                await _context.SaveChangesAsync();

                // Schedule recurring expense if required
                if (expense.IsRecurring && expense.RecurrenceInfo != null)
                {
                    await _recurringExpenseService.ScheduleRecurringExpenseAsync(expense);
                }

                // Load full expense data for response
                var createdExpense = await _context.Expenses
                    .Include(e => e.Category)
                    .Include(e => e.PaymentMethod)
                    .Include(e => e.RecurrenceInfo)
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.Id == expense.Id);

                response.Data = MapToDto(createdExpense);
                response.Message = "Expense created successfully";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense");
                response.Success = false;
                response.Message = "Failed to create expense";
                return response;
            }
        }

        public async Task<ServiceResponse<ExpenseDto>> UpdateExpenseAsync(Guid id, string userId, UpdateExpenseDto dto)
        {
            var response = new ServiceResponse<ExpenseDto>();

            try
            {
                var expense = await _context.Expenses
                    .Include(e => e.RecurrenceInfo)
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

                if (expense == null)
                {
                    response.Success = false;
                    response.Message = "Expense not found";
                    return response;
                }

                // Check if category exists (if changing)
                if (dto.CategoryId.HasValue && dto.CategoryId.Value != expense.CategoryId)
                {
                    var category = await _context.ExpenseCategories
                        .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && (c.UserId == userId || c.IsSystem));

                    if (category == null)
                    {
                        response.Success = false;
                        response.Message = "Category not found";
                        return response;
                    }

                    expense.CategoryId = dto.CategoryId.Value;
                }

                // Check if payment method exists (if changing)
                if (dto.PaymentMethodId.HasValue && dto.PaymentMethodId != expense.PaymentMethodId)
                {
                    if (dto.PaymentMethodId.Value != Guid.Empty)
                    {
                        var paymentMethod = await _context.PaymentMethods
                            .FirstOrDefaultAsync(p => p.Id == dto.PaymentMethodId && p.UserId == userId);

                        if (paymentMethod == null)
                        {
                            response.Success = false;
                            response.Message = "Payment method not found";
                            return response;
                        }
                    }

                    expense.PaymentMethodId = dto.PaymentMethodId;
                }

                // Update basic properties
                if (dto.Amount.HasValue)
                {
                    expense.Amount = dto.Amount.Value;
                }

                if (dto.Description != null)
                {
                    expense.Description = dto.Description;
                }

                if (dto.Date.HasValue)
                {
                    expense.Date = dto.Date.Value;
                }

                if (dto.ReceiptUrl != null)
                {
                    expense.ReceiptUrl = dto.ReceiptUrl;
                }

                if (dto.IsRecurring.HasValue)
                {
                    expense.IsRecurring = dto.IsRecurring.Value;
                }

                expense.LastModifiedAt = DateTime.UtcNow;

                // Handle recurrence info
                if (expense.IsRecurring)
                {
                    if (expense.RecurrenceInfo == null)
                    {
                        // Create new recurrence info if it doesn't exist
                        expense.RecurrenceInfo = new ExpenseRecurrence
                        {
                            Id = Guid.NewGuid(),
                            ExpenseId = expense.Id,
                            RecurrenceType = dto.RecurrenceInfo?.RecurrenceType ?? Core.Shared.Enums.Expenses.RecurrenceType.Monthly,
                            Interval = dto.RecurrenceInfo?.Interval ?? 1,
                            StartDate = dto.RecurrenceInfo?.StartDate ?? DateTime.UtcNow,
                            EndDate = dto.RecurrenceInfo?.EndDate,
                            CustomCronExpression = dto.RecurrenceInfo?.CustomCronExpression,
                            DayOfMonth = dto.RecurrenceInfo?.DayOfMonth,
                            DayOfWeek = dto.RecurrenceInfo?.DayOfWeek
                        };
                    }
                    else if (dto.RecurrenceInfo != null)
                    {
                        // Update existing recurrence info
                        if (dto.RecurrenceInfo.RecurrenceType.HasValue)
                        {
                            expense.RecurrenceInfo.RecurrenceType = dto.RecurrenceInfo.RecurrenceType.Value;
                        }

                        if (dto.RecurrenceInfo.Interval.HasValue)
                        {
                            expense.RecurrenceInfo.Interval = dto.RecurrenceInfo.Interval.Value;
                        }

                        if (dto.RecurrenceInfo.StartDate.HasValue)
                        {
                            expense.RecurrenceInfo.StartDate = dto.RecurrenceInfo.StartDate.Value;
                        }

                        if (dto.RecurrenceInfo.EndDate.HasValue)
                        {
                            expense.RecurrenceInfo.EndDate = dto.RecurrenceInfo.EndDate;
                        }

                        if (dto.RecurrenceInfo.CustomCronExpression != null)
                        {
                            expense.RecurrenceInfo.CustomCronExpression = dto.RecurrenceInfo.CustomCronExpression;
                        }

                        if (dto.RecurrenceInfo.DayOfMonth.HasValue)
                        {
                            expense.RecurrenceInfo.DayOfMonth = dto.RecurrenceInfo.DayOfMonth;
                        }

                        if (dto.RecurrenceInfo.DayOfWeek.HasValue)
                        {
                            expense.RecurrenceInfo.DayOfWeek = dto.RecurrenceInfo.DayOfWeek;
                        }
                    }

                    // Update recurring expense scheduling
                    await _recurringExpenseService.UpdateRecurringExpenseScheduleAsync(expense);
                }
                else if (!expense.IsRecurring && expense.RecurrenceInfo != null)
                {
                    // Remove recurrence info if expense is no longer recurring
                    _context.ExpenseRecurrences.Remove(expense.RecurrenceInfo);

                    // Cancel scheduled recurring expenses
                    await _recurringExpenseService.CancelRecurringExpenseAsync(expense.Id);
                }

                // Handle tags
                if (dto.Tags != null)
                {
                    // Clear existing tags
                    expense.Tags.Clear();

                    // Add new tags
                    foreach (var tagName in dto.Tags)
                    {
                        var tag = await _context.ExpenseTags
                            .FirstOrDefaultAsync(t => t.Name == tagName && t.UserId == userId);

                        if (tag == null)
                        {
                            tag = new ExpenseTag
                            {
                                Id = Guid.NewGuid(),
                                Name = tagName,
                                UserId = userId
                            };
                            await _context.ExpenseTags.AddAsync(tag);
                        }

                        expense.Tags.Add(tag);
                    }
                }

                await _context.SaveChangesAsync();

                // Load full expense data for response
                var updatedExpense = await _context.Expenses
                    .Include(e => e.Category)
                    .Include(e => e.PaymentMethod)
                    .Include(e => e.RecurrenceInfo)
                    .Include(e => e.Tags)
                    .FirstOrDefaultAsync(e => e.Id == expense.Id);

                response.Data = MapToDto(updatedExpense);
                response.Message = "Expense updated successfully";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expense");
                response.Success = false;
                response.Message = "Failed to update expense";
                return response;
            }
        }

        public async Task<ServiceResponse<bool>> DeleteExpenseAsync(Guid id, string userId)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                var expense = await _context.Expenses
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

                if (expense == null)
                {
                    response.Success = false;
                    response.Message = "Expense not found";
                    return response;
                }

                // Cancel any scheduled recurring expenses
                if (expense.IsRecurring)
                {
                    await _recurringExpenseService.CancelRecurringExpenseAsync(expense.Id);
                }

                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();

                response.Data = true;
                response.Message = "Expense deleted successfully";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense");
                response.Success = false;
                response.Message = "Failed to delete expense";
                return response;
            }
        }

        public async Task<ServiceResponse<ExpenseStatisticsDto>> GetExpenseStatisticsAsync(string userId, DateTime? startDate, DateTime? endDate)
        {
            var response = new ServiceResponse<ExpenseStatisticsDto>();

            try
            {
                // Set default date range if not provided (last 30 days)
                if (!startDate.HasValue)
                {
                    startDate = DateTime.UtcNow.AddDays(-30);
                }

                if (!endDate.HasValue)
                {
                    endDate = DateTime.UtcNow;
                }

                // Make sure the end date is inclusive by setting it to the end of the day
                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

                // Get all expenses in the date range
                var expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Include(e => e.PaymentMethod)
                    .Where(e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate)
                    .ToListAsync();

                if (!expenses.Any())
                {
                    response.Data = new ExpenseStatisticsDto
                    {
                        TotalExpenses = 0,
                        AverageExpenseAmount = 0,
                        HighestExpenseAmount = 0,
                        LowestExpenseAmount = 0,
                        ExpenseCount = 0,
                        TotalRecurringExpenses = 0,
                        ExpensesByCategory = new Dictionary<string, decimal>(),
                        ExpensesByPaymentMethod = new Dictionary<string, decimal>(),
                        ExpensesByMonth = new Dictionary<string, decimal>(),
                        ExpensesByDay = new Dictionary<string, decimal>(),
                        TopExpenses = new List<TopExpenseDto>()
                    };
                    
                    return response;
                }

                // Calculate statistics
                var totalExpenses = expenses.Sum(e => e.Amount);
                var averageExpense = expenses.Average(e => e.Amount);
                var highestExpense = expenses.Max(e => e.Amount);
                var lowestExpense = expenses.Min(e => e.Amount);
                var expenseCount = expenses.Count;
                var totalRecurringExpenses = expenses.Where(e => e.IsRecurring).Sum(e => e.Amount);

                // Group expenses by category
                var expensesByCategory = expenses
                    .GroupBy(e => e.Category?.Name ?? "Uncategorized")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(e => e.Amount)
                    );

                // Group expenses by payment method
                var expensesByPaymentMethod = expenses
                    .GroupBy(e => e.PaymentMethod?.Name ?? "Cash")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(e => e.Amount)
                    );

                // Group expenses by month
                var expensesByMonth = expenses
                    .GroupBy(e => e.Date.ToString("MMMM yyyy"))
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(e => e.Amount)
                    );

                // Group expenses by day of week
                var expensesByDay = expenses
                    .GroupBy(e => e.Date.DayOfWeek.ToString())
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(e => e.Amount)
                    );

                // Get top 5 expenses
                var topExpenses = expenses
                    .OrderByDescending(e => e.Amount)
                    .Take(5)
                    .Select(e => new TopExpenseDto
                    {
                        Id = e.Id,
                        Description = e.Description,
                        Amount = e.Amount,
                        Date = e.Date,
                        CategoryName = e.Category?.Name ?? "Uncategorized",
                        CategoryColor = e.Category?.Color ?? "#808080"
                    })
                    .ToList();

                // Build and return the statistics DTO
                var statistics = new ExpenseStatisticsDto
                {
                    TotalExpenses = totalExpenses,
                    AverageExpenseAmount = averageExpense,
                    HighestExpenseAmount = highestExpense,
                    LowestExpenseAmount = lowestExpense,
                    ExpenseCount = expenseCount,
                    TotalRecurringExpenses = totalRecurringExpenses,
                    ExpensesByCategory = expensesByCategory,
                    ExpensesByPaymentMethod = expensesByPaymentMethod,
                    ExpensesByMonth = expensesByMonth,
                    ExpensesByDay = expensesByDay,
                    TopExpenses = topExpenses
                };

                response.Data = statistics;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expense statistics");
                response.Success = false;
                response.Message = "Failed to get expense statistics";
                return response;
            }
        }

        public async Task<ServiceResponse<PagedResponse<ExpenseDto>>> GetAllExpensesAsync(ExpenseFilterDto filter)
        {
            var response = new ServiceResponse<PagedResponse<ExpenseDto>>();

            try
            {
                // Admin-only endpoint - get expenses for all users
                var query = _context.Expenses
                    .Include(e => e.Category)
                    .Include(e => e.PaymentMethod)
                    .Include(e => e.RecurrenceInfo)
                    .Include(e => e.Tags)
                    .Include(e => e.User);

                // Apply filters
                query = ApplyFilters(query, filter) as IIncludableQueryable<Expense, ApplicationUser>;

                // Apply sorting
                query = ApplySorting(query, filter) as IIncludableQueryable<Expense, ApplicationUser>;

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

                var expenses = await query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var expenseDtos = expenses.Select(e =>
                {
                    var dto = MapToDto(e);
                    dto.UserName = $"{e.User?.FirstName} {e.User?.LastName}".Trim();
                    return dto;
                }).ToList();

                var pagedResponse = new PagedResponse<ExpenseDto>
                {
                    Items = expenseDtos,
                    TotalItems = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalPages = totalPages
                };

                response.Data = pagedResponse;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all expenses");
                response.Success = false;
                response.Message = "Failed to retrieve expenses";
                return response;
            }
        }

        public async Task<ServiceResponse<List<MonthlyExpenseSummaryDto>>> GetMonthlyExpenseReportAsync(string userId, int year)
        {
            var response = new ServiceResponse<List<MonthlyExpenseSummaryDto>>();

            try
            {
                // Get all expenses for the specified year
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31, 23, 59, 59);

                var expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Where(e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate)
                    .ToListAsync();

                var monthlyReports = new List<MonthlyExpenseSummaryDto>();

                // Generate a report for each month
                for (int month = 1; month <= 12; month++)
                {
                    var monthName = new DateTime(year, month, 1).ToString("MMMM");
                    var monthStart = new DateTime(year, month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monthlyExpenses = expenses.Where(e => e.Date.Month == month).ToList();
                    var totalAmount = monthlyExpenses.Sum(e => e.Amount);
                    var expenseCount = monthlyExpenses.Count;

                    // Calculate category breakdown
                    var categoryBreakdown = monthlyExpenses
                        .GroupBy(e => e.Category?.Name ?? "Uncategorized")
                        .ToDictionary(
                            g => g.Key,
                            g => g.Sum(e => e.Amount)
                        );

                    // Get top expenses for the month
                    var topExpenses = monthlyExpenses
                        .OrderByDescending(e => e.Amount)
                        .Take(5)
                        .Select(e => new TopExpenseDto
                        {
                            Id = e.Id,
                            Description = e.Description,
                            Amount = e.Amount,
                            Date = e.Date,
                            CategoryName = e.Category?.Name ?? "Uncategorized",
                            CategoryColor = e.Category?.Color ?? "#808080"
                        })
                        .ToList();

                    monthlyReports.Add(new MonthlyExpenseSummaryDto
                    {
                        Month = month,
                        MonthName = monthName,
                        TotalAmount = totalAmount,
                        ExpenseCount = expenseCount,
                        CategoryBreakdown = categoryBreakdown,
                        TopExpenses = topExpenses
                    });
                }

                response.Data = monthlyReports;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly expense report");
                response.Success = false;
                response.Message = "Failed to generate monthly expense report";
                return response;
            }
        }

        public async Task<ServiceResponse<YearlyComparisonReportDto>> GetYearlyComparisonReportAsync(string userId, int year1, int year2)
        {
            var response = new ServiceResponse<YearlyComparisonReportDto>();

            try
            {
                // Get expenses for both years
                var startDate1 = new DateTime(year1, 1, 1);
                var endDate1 = new DateTime(year1, 12, 31, 23, 59, 59);
                var startDate2 = new DateTime(year2, 1, 1);
                var endDate2 = new DateTime(year2, 12, 31, 23, 59, 59);

                var expenses1 = await _context.Expenses
                    .Include(e => e.Category)
                    .Where(e => e.UserId == userId && e.Date >= startDate1 && e.Date <= endDate1)
                    .ToListAsync();

                var expenses2 = await _context.Expenses
                    .Include(e => e.Category)
                    .Where(e => e.UserId == userId && e.Date >= startDate2 && e.Date <= endDate2)
                    .ToListAsync();

                var year1Total = expenses1.Sum(e => e.Amount);
                var year2Total = expenses2.Sum(e => e.Amount);
                var percentageChange = year1Total > 0
                    ? ((year2Total - year1Total) / year1Total) * 100
                    : (year2Total > 0 ? 100 : 0);

                // Monthly comparison
                var monthlyComparison = new Dictionary<string, YearlyComparisonItemDto>();

                for (int month = 1; month <= 12; month++)
                {
                    var monthName = new DateTime(year1, month, 1).ToString("MMMM");
                    var month1Total = expenses1.Where(e => e.Date.Month == month).Sum(e => e.Amount);
                    var month2Total = expenses2.Where(e => e.Date.Month == month).Sum(e => e.Amount);
                    var monthPercentageChange = month1Total > 0
                        ? ((month2Total - month1Total) / month1Total) * 100
                        : (month2Total > 0 ? 100 : 0);

                    monthlyComparison[monthName] = new YearlyComparisonItemDto
                    {
                        Year1Amount = month1Total,
                        Year2Amount = month2Total,
                        PercentageChange = monthPercentageChange
                    };
                }

                // Category comparison
                var categoryComparison = new Dictionary<string, YearlyComparisonItemDto>();

                // Get all unique categories across both years
                var allCategories = expenses1
                    .Select(e => e.Category?.Name ?? "Uncategorized")
                    .Union(expenses2.Select(e => e.Category?.Name ?? "Uncategorized"))
                    .Distinct()
                    .ToList();

                foreach (var category in allCategories)
                {
                    var category1Total = expenses1
                        .Where(e => (e.Category?.Name ?? "Uncategorized") == category)
                        .Sum(e => e.Amount);

                    var category2Total = expenses2
                        .Where(e => (e.Category?.Name ?? "Uncategorized") == category)
                        .Sum(e => e.Amount);

                    var categoryPercentageChange = category1Total > 0
                        ? ((category2Total - category1Total) / category1Total) * 100
                        : (category2Total > 0 ? 100 : 0);

                    categoryComparison[category] = new YearlyComparisonItemDto
                    {
                        Year1Amount = category1Total,
                        Year2Amount = category2Total,
                        PercentageChange = categoryPercentageChange
                    };
                }

                response.Data = new YearlyComparisonReportDto
                {
                    Year1 = year1,
                    Year2 = year2,
                    Year1Total = year1Total,
                    Year2Total = year2Total,
                    PercentageChange = percentageChange,
                    MonthlyComparison = monthlyComparison,
                    CategoryComparison = categoryComparison
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating yearly comparison report");
                response.Success = false;
                response.Message = "Failed to generate yearly comparison report";
                return response;
            }
        }

        public async Task<ServiceResponse<CategoryBreakdownReportDto>> GetCategoryBreakdownReportAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var response = new ServiceResponse<CategoryBreakdownReportDto>();

            try
            {
                var expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Where(e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate)
                    .ToListAsync();

                var totalExpenses = expenses.Sum(e => e.Amount);

                var categoryBreakdown = expenses
                    .GroupBy(e => e.Category?.Name ?? "Uncategorized")
                    .Select(g => new CategoryBreakdownItemDto
                    {
                        CategoryName = g.Key,
                        CategoryColor = g.First().Category?.Color ?? "#808080",
                        Amount = g.Sum(e => e.Amount),
                        Percentage = totalExpenses > 0
                            ? (g.Sum(e => e.Amount) / totalExpenses) * 100
                            : 0,
                        ExpenseCount = g.Count(),
                        AverageExpenseAmount = g.Count() > 0
                            ? g.Sum(e => e.Amount) / g.Count()
                            : 0
                    })
                    .OrderByDescending(c => c.Amount)
                    .ToList();

                response.Data = new CategoryBreakdownReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalExpenses = totalExpenses,
                    Categories = categoryBreakdown
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating category breakdown report");
                response.Success = false;
                response.Message = "Failed to generate category breakdown report";
                return response;
            }
        }

        public async Task<ServiceResponse<TrendAnalysisDto>> GetExpenseTrendAnalysisAsync(string userId, int months)
        {
            var response = new ServiceResponse<TrendAnalysisDto>();

            try
            {
                // Calculate date range
                var endDate = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1); // End of today
                var startDate = endDate.AddMonths(-months).Date; // Start from X months ago

                var expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Where(e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate)
                    .ToListAsync();

                // Generate monthly trends
                var monthlyTrends = new List<MonthlyTrendDto>();
                var currentDate = startDate;

                while (currentDate <= endDate)
                {
                    var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monthlyExpenses = expenses
                        .Where(e => e.Date >= monthStart && e.Date <= monthEnd)
                        .ToList();

                    var totalAmount = monthlyExpenses.Sum(e => e.Amount);
                    var expenseCount = monthlyExpenses.Count;
                    var averageExpense = expenseCount > 0 ? totalAmount / expenseCount : 0;

                    monthlyTrends.Add(new MonthlyTrendDto
                    {
                        Month = monthStart,
                        TotalAmount = totalAmount,
                        ExpenseCount = expenseCount,
                        AverageExpense = averageExpense
                    });

                    currentDate = monthStart.AddMonths(1);
                }

                // Generate category trends
                var categoryTrends = new Dictionary<string, List<MonthlyTrendDto>>();

                var categories = expenses
                    .Select(e => e.Category?.Name ?? "Uncategorized")
                    .Distinct()
                    .ToList();

                foreach (var category in categories)
                {
                    var categoryMonthlyTrends = new List<MonthlyTrendDto>();
                    currentDate = startDate;

                    while (currentDate <= endDate)
                    {
                        var monthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                        var categoryMonthlyExpenses = expenses
                            .Where(e => e.Date >= monthStart && e.Date <= monthEnd &&
                                   (e.Category?.Name ?? "Uncategorized") == category)
                            .ToList();

                        var totalAmount = categoryMonthlyExpenses.Sum(e => e.Amount);
                        var expenseCount = categoryMonthlyExpenses.Count;
                        var averageExpense = expenseCount > 0 ? totalAmount / expenseCount : 0;

                        categoryMonthlyTrends.Add(new MonthlyTrendDto
                        {
                            Month = monthStart,
                            TotalAmount = totalAmount,
                            ExpenseCount = expenseCount,
                            AverageExpense = averageExpense
                        });

                        currentDate = monthStart.AddMonths(1);
                    }

                    categoryTrends[category] = categoryMonthlyTrends;
                }

                // Calculate trend summary
                var averageMonthlyExpense = monthlyTrends.Count > 0
                    ? monthlyTrends.Average(m => m.TotalAmount)
                    : 0;

                // Sort monthly expenses for median calculation
                var sortedMonthlyExpenses = monthlyTrends
                    .Select(m => m.TotalAmount)
                    .OrderBy(m => m)
                    .ToList();

                decimal medianMonthlyExpense = 0;
                if (sortedMonthlyExpenses.Count > 0)
                {
                    int mid = sortedMonthlyExpenses.Count / 2;
                    medianMonthlyExpense = sortedMonthlyExpenses.Count % 2 == 0
                        ? (sortedMonthlyExpenses[mid - 1] + sortedMonthlyExpenses[mid]) / 2
                        : sortedMonthlyExpenses[mid];
                }

                // Calculate expense growth rate
                decimal expenseGrowthRate = 0;
                if (monthlyTrends.Count >= 2)
                {
                    var firstMonth = monthlyTrends.First().TotalAmount;
                    var lastMonth = monthlyTrends.Last().TotalAmount;

                    if (firstMonth > 0)
                    {
                        expenseGrowthRate = ((lastMonth - firstMonth) / firstMonth) * 100;
                    }
                }

                // Find top growing and shrinking categories
                string topGrowingCategory = "None";
                string topShrinkingCategory = "None";

                if (categoryTrends.Count > 0)
                {
                    var categoryGrowthRates = new Dictionary<string, decimal>();

                    foreach (var category in categoryTrends.Keys)
                    {
                        var trends = categoryTrends[category];
                        if (trends.Count >= 2)
                        {
                            var firstMonth = trends.First().TotalAmount;
                            var lastMonth = trends.Last().TotalAmount;

                            if (firstMonth > 0)
                            {
                                var growthRate = ((lastMonth - firstMonth) / firstMonth) * 100;
                                categoryGrowthRates[category] = growthRate;
                            }
                        }
                    }

                    if (categoryGrowthRates.Count > 0)
                    {
                        topGrowingCategory = categoryGrowthRates
                            .OrderByDescending(kvp => kvp.Value)
                            .FirstOrDefault().Key;

                        topShrinkingCategory = categoryGrowthRates
                            .OrderBy(kvp => kvp.Value)
                            .FirstOrDefault().Key;
                    }
                }

                var trendSummary = new TrendSummaryDto
                {
                    AverageMonthlyExpense = averageMonthlyExpense,
                    MedianMonthlyExpense = medianMonthlyExpense,
                    ExpenseGrowthRate = expenseGrowthRate,
                    TopGrowingCategory = topGrowingCategory,
                    TopShrinkingCategory = topShrinkingCategory
                };

                response.Data = new TrendAnalysisDto
                {
                    MonthlyTrends = monthlyTrends,
                    CategoryTrends = categoryTrends,
                    Summary = trendSummary
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating expense trend analysis");
                response.Success = false;
                response.Message = "Failed to generate expense trend analysis";
                return response;
            }
        }


        #region Helper Methods

        private IQueryable<Expense> ApplyFilters(IQueryable<Expense> query, ExpenseFilterDto filter)
        {
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(e => 
                    e.Description.Contains(filter.SearchTerm) || 
                    e.Category.Name.Contains(filter.SearchTerm) ||
                    e.Tags.Any(t => t.Name.Contains(filter.SearchTerm)));
            }

            if (filter.MinAmount.HasValue)
            {
                query = query.Where(e => e.Amount >= filter.MinAmount.Value);
            }

            if (filter.MaxAmount.HasValue)
            {
                query = query.Where(e => e.Amount <= filter.MaxAmount.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(e => e.Date >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                // Make sure the end date is inclusive by setting it to the end of the day
                var inclusiveEndDate = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(e => e.Date <= inclusiveEndDate);
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(e => e.CategoryId == filter.CategoryId.Value);
            }

            if (filter.CategoryIds != null && filter.CategoryIds.Any())
            {
                query = query.Where(e => filter.CategoryIds.Contains(e.CategoryId));
            }

            if (filter.PaymentMethodId.HasValue)
            {
                query = query.Where(e => e.PaymentMethodId == filter.PaymentMethodId.Value);
            }

            if (filter.Tags != null && filter.Tags.Any())
            {
                query = query.Where(e => e.Tags.Any(t => filter.Tags.Contains(t.Name)));
            }

            if (filter.IsRecurring.HasValue)
            {
                query = query.Where(e => e.IsRecurring == filter.IsRecurring.Value);
            }

            return query;
        }

        private IQueryable<Expense> ApplySorting(IQueryable<Expense> query, ExpenseFilterDto filter)
        {
            return filter.SortBy.ToLower() switch
            {
                "amount" => filter.SortAscending ?
                    query.OrderBy(e => e.Amount) :
                    query.OrderByDescending(e => e.Amount),
                "description" => filter.SortAscending ?
                    query.OrderBy(e => e.Description) :
                    query.OrderByDescending(e => e.Description),
                "category" => filter.SortAscending ?
                    query.OrderBy(e => e.Category.Name) :
                    query.OrderByDescending(e => e.Category.Name),
                "paymentmethod" => filter.SortAscending ?
                    query.OrderBy(e => e.PaymentMethod.Name) :
                    query.OrderByDescending(e => e.PaymentMethod.Name),
                "createdat" => filter.SortAscending ?
                    query.OrderBy(e => e.CreatedAt) :
                    query.OrderByDescending(e => e.CreatedAt),
                "date" or _ => filter.SortAscending ?
                    query.OrderBy(e => e.Date) :
                    query.OrderByDescending(e => e.Date)
            };
        }

        private ExpenseDto MapToDto(Expense expense)
        {
            return new ExpenseDto
            {
                Id = expense.Id,
                Amount = expense.Amount,
                Description = expense.Description,
                Date = expense.Date,
                CategoryId = expense.CategoryId,
                CategoryName = expense.Category?.Name ?? "Uncategorized",
                CategoryColor = expense.Category?.Color ?? "#808080",
                PaymentMethodId = expense.PaymentMethodId,
                PaymentMethodName = expense.PaymentMethod?.Name ?? "Cash",
                IsRecurring = expense.IsRecurring,
                CreatedAt = expense.CreatedAt,
                LastModifiedAt = expense.LastModifiedAt,
                ReceiptUrl = expense.ReceiptUrl,
                RecurrenceInfo = expense.RecurrenceInfo != null ? new ExpenseRecurrenceDto
                {
                    Id = expense.RecurrenceInfo.Id,
                    RecurrenceType = expense.RecurrenceInfo.RecurrenceType,
                    Interval = expense.RecurrenceInfo.Interval,
                    StartDate = expense.RecurrenceInfo.StartDate,
                    EndDate = expense.RecurrenceInfo.EndDate,
                    LastProcessedDate = expense.RecurrenceInfo.LastProcessedDate,
                    NextProcessingDate = expense.RecurrenceInfo.NextProcessingDate,
                    CustomCronExpression = expense.RecurrenceInfo.CustomCronExpression,
                    DayOfMonth = expense.RecurrenceInfo.DayOfMonth,
                    DayOfWeek = expense.RecurrenceInfo.DayOfWeek
                } : null,
                Tags = expense.Tags?.Select(t => t.Name).ToList() ?? new List<string>()
            };
        }

        #endregion
    }
}
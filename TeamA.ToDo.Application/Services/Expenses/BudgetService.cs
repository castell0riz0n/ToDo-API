using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.Expenses;
using TeamA.ToDo.Core.Models.Expenses;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services.Expenses
{
    public class BudgetService : IBudgetService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BudgetService> _logger;

        public BudgetService(
            ApplicationDbContext context,
            ILogger<BudgetService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private async Task<BudgetDto> CreateBudgetDtoAsync(Budget budget)
        {
            if (budget == null)
            {
                return null;
            }

            // Get expenses for this budget's period
            var expenses = await _context.Expenses
                .Where(e => e.UserId == budget.UserId &&
                            e.CategoryId == budget.CategoryId &&
                            e.Date >= budget.StartDate &&
                            e.Date <= budget.EndDate)
                .ToListAsync();

            // Calculate spending
            decimal spentAmount = expenses.Sum(e => e.Amount);
            decimal remainingAmount = budget.Amount - spentAmount;
            double spentPercentage = budget.Amount > 0 ? (double)(spentAmount / budget.Amount) * 100 : 0;

            return new BudgetDto
            {
                Id = budget.Id,
                Name = budget.Name,
                Amount = budget.Amount,
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category?.Name ?? "Uncategorized",
                CategoryColor = budget.Category?.Color ?? "#808080",
                StartDate = budget.StartDate,
                EndDate = budget.EndDate,
                Period = budget.Period,
                SpentAmount = spentAmount,
                RemainingAmount = remainingAmount,
                SpentPercentage = spentPercentage,
                CreatedAt = budget.CreatedAt,
                LastModifiedAt = budget.LastModifiedAt
            };
        }


        public async Task<ServiceResponse<List<BudgetDto>>> GetBudgetsAsync(string userId)
        {
            var response = new ServiceResponse<List<BudgetDto>>();

            try
            {
                var budgets = await _context.Budgets
                    .Include(b => b.Category)
                    .Where(b => b.UserId == userId)
                    .ToListAsync();

                var budgetDtos = new List<BudgetDto>();

                foreach (var budget in budgets)
                {
                    var budgetDto = await CreateBudgetDtoAsync(budget);
                    budgetDtos.Add(budgetDto);
                }

                response.Data = budgetDtos;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving budgets");
                response.Success = false;
                response.Message = "Failed to retrieve budgets";
                return response;
            }
        }

        public async Task<ServiceResponse<BudgetDto>> GetBudgetByIdAsync(Guid id, string userId)
        {
            var response = new ServiceResponse<BudgetDto>();

            try
            {
                var budget = await _context.Budgets
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

                if (budget == null)
                {
                    response.Success = false;
                    response.Message = "Budget not found";
                    return response;
                }

                response.Data = await CreateBudgetDtoAsync(budget);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving budget");
                response.Success = false;
                response.Message = "Failed to retrieve budget";
                return response;
            }
        }

        public async Task<ServiceResponse<BudgetDto>> CreateBudgetAsync(string userId, CreateBudgetDto dto)
        {
            var response = new ServiceResponse<BudgetDto>();

            try
            {
                // Validate dates
                if (dto.StartDate >= dto.EndDate)
                {
                    response.Success = false;
                    response.Message = "Start date must be before end date";
                    return response;
                }

                // Validate category if provided
                if (dto.CategoryId.HasValue)
                {
                    var category = await _context.ExpenseCategories
                        .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && (c.UserId == userId || c.IsSystem));

                    if (category == null)
                    {
                        response.Success = false;
                        response.Message = "Category not found";
                        return response;
                    }
                }

                // Check for overlapping budgets with the same category
                var overlappingBudget = await _context.Budgets
                    .Where(b => b.UserId == userId
                                && b.CategoryId == dto.CategoryId
                                && ((b.StartDate <= dto.StartDate && b.EndDate >= dto.StartDate) ||
                                    (b.StartDate <= dto.EndDate && b.EndDate >= dto.EndDate) ||
                                    (b.StartDate >= dto.StartDate && b.EndDate <= dto.EndDate)))
                    .FirstOrDefaultAsync();

                if (overlappingBudget != null)
                {
                    response.Success = false;
                    response.Message = "A budget for this category with overlapping dates already exists";
                    return response;
                }

                // Create budget
                var budget = new Budget
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Amount = dto.Amount,
                    CategoryId = dto.CategoryId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Period = dto.Period,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Budgets.AddAsync(budget);
                await _context.SaveChangesAsync();

                // Load full budget data for response
                var createdBudget = await _context.Budgets
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(b => b.Id == budget.Id);

                response.Data = await CreateBudgetDtoAsync(createdBudget);
                response.Message = "Budget created successfully";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating budget");
                response.Success = false;
                response.Message = "Failed to create budget";
                return response;
            }
        }

        public async Task<ServiceResponse<BudgetDto>> UpdateBudgetAsync(Guid id, string userId, UpdateBudgetDto dto)
        {
            var response = new ServiceResponse<BudgetDto>();

            try
            {
                var budget = await _context.Budgets
                    .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

                if (budget == null)
                {
                    response.Success = false;
                    response.Message = "Budget not found";
                    return response;
                }

                // Validate dates if updating
                if (dto.StartDate.HasValue || dto.EndDate.HasValue)
                {
                    var startDate = dto.StartDate ?? budget.StartDate;
                    var endDate = dto.EndDate ?? budget.EndDate;

                    if (startDate >= endDate)
                    {
                        response.Success = false;
                        response.Message = "Start date must be before end date";
                        return response;
                    }
                }

                // Validate category if provided
                if (dto.CategoryId.HasValue && dto.CategoryId.Value != budget.CategoryId)
                {
                    var category = await _context.ExpenseCategories
                        .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && (c.UserId == userId || c.IsSystem));

                    if (category == null)
                    {
                        response.Success = false;
                        response.Message = "Category not found";
                        return response;
                    }

                    // Check for overlapping budgets with the new category
                    var startDate = dto.StartDate ?? budget.StartDate;
                    var endDate = dto.EndDate ?? budget.EndDate;

                    var overlappingBudget = await _context.Budgets
                        .Where(b => b.Id != id &&
                                    b.UserId == userId &&
                                    b.CategoryId == dto.CategoryId &&
                                    ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                                     (b.StartDate <= endDate && b.EndDate >= endDate) ||
                                     (b.StartDate >= startDate && b.EndDate <= endDate)))
                        .FirstOrDefaultAsync();

                    if (overlappingBudget != null)
                    {
                        response.Success = false;
                        response.Message = "A budget for this category with overlapping dates already exists";
                        return response;
                    }

                    budget.CategoryId = dto.CategoryId;
                }

                // Update properties
                if (!string.IsNullOrEmpty(dto.Name))
                {
                    budget.Name = dto.Name;
                }

                if (dto.Amount.HasValue)
                {
                    budget.Amount = dto.Amount.Value;
                }

                if (dto.StartDate.HasValue)
                {
                    budget.StartDate = dto.StartDate.Value;
                }

                if (dto.EndDate.HasValue)
                {
                    budget.EndDate = dto.EndDate.Value;
                }

                if (dto.Period.HasValue)
                {
                    budget.Period = dto.Period.Value;
                }

                budget.LastModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Load updated budget for response
                var updatedBudget = await _context.Budgets
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(b => b.Id == id);

                response.Data = await CreateBudgetDtoAsync(updatedBudget);
                response.Message = "Budget updated successfully";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating budget");
                response.Success = false;
                response.Message = "Failed to update budget";
                return response;
            }
        }

        public async Task<ServiceResponse<bool>> DeleteBudgetAsync(Guid id, string userId)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                var budget = await _context.Budgets
                    .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

                if (budget == null)
                {
                    response.Success = false;
                    response.Message = "Budget not found";
                    return response;
                }

                _context.Budgets.Remove(budget);
                await _context.SaveChangesAsync();

                response.Data = true;
                response.Message = "Budget deleted successfully";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting budget");
                response.Success = false;
                response.Message = "Failed to delete budget";
                return response;
            }
        }

        public async Task<ServiceResponse<BudgetSummaryDto>> GetBudgetSummaryAsync(string userId,
            DateTime? month = null)
        {
            var response = new ServiceResponse<BudgetSummaryDto>();

            try
            {
                // Calculate date range for summary (default to current month if not specified)
                var targetMonth = month ?? DateTime.UtcNow;
                var startOfMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                // Get all budgets for the user
                var budgets = await _context.Budgets
                    .Include(b => b.Category)
                    .Where(b => b.UserId == userId &&
                                (b.StartDate <= endOfMonth && b.EndDate >= startOfMonth))
                    .ToListAsync();

                if (!budgets.Any())
                {
                    // Return empty summary if no budgets found
                    response.Data = new BudgetSummaryDto
                    {
                        TotalBudgetAmount = 0,
                        TotalSpentAmount = 0,
                        TotalRemainingAmount = 0,
                        TotalSpentPercentage = 0,
                        Budgets = new List<BudgetDto>(),
                        SpendingByCategory = new Dictionary<string, decimal>(),
                        BudgetStatus = new Dictionary<string, BudgetStatusDto>()
                    };
                    return response;
                }

                // Get expenses for the date range
                var expenses = await _context.Expenses
                    .Include(e => e.Category)
                    .Where(e => e.UserId == userId &&
                                e.Date >= startOfMonth &&
                                e.Date <= endOfMonth)
                    .ToListAsync();

                // Calculate spending by category
                var spendingByCategory = expenses
                    .GroupBy(e => e.CategoryId)
                    .ToDictionary(
                        g => g.FirstOrDefault()?.Category?.Name ?? "Uncategorized",
                        g => g.Sum(e => e.Amount)
                    );

                // Create budget DTOs with calculations
                var budgetDtos = new List<BudgetDto>();
                var budgetStatus = new Dictionary<string, BudgetStatusDto>();
                decimal totalBudgetAmount = 0;
                decimal totalSpentAmount = 0;

                foreach (var budget in budgets)
                {
                    // Calculate pro-rated budget amount based on overlap with the month
                    var budgetDuration = (budget.EndDate - budget.StartDate).TotalDays + 1;
                    var overlapStart = startOfMonth > budget.StartDate ? startOfMonth : budget.StartDate;
                    var overlapEnd = endOfMonth < budget.EndDate ? endOfMonth : budget.EndDate;
                    var overlapDuration = (overlapEnd - overlapStart).TotalDays + 1;
                    var proRatedFactor = (decimal)(overlapDuration / budgetDuration);
                    var proRatedAmount = budget.Amount * proRatedFactor;

                    // Find expenses for this budget's category
                    decimal spent = 0;
                    if (budget.CategoryId.HasValue && expenses.Any(e => e.CategoryId == budget.CategoryId))
                    {
                        spent = expenses
                            .Where(e => e.CategoryId == budget.CategoryId)
                            .Sum(e => e.Amount);
                    }

                    var remaining = proRatedAmount - spent;
                    var percentage = proRatedAmount > 0 ? (double)(spent / proRatedAmount) * 100 : 0;

                    // Create the budget DTO
                    var budgetDto = new BudgetDto
                    {
                        Id = budget.Id,
                        Name = budget.Name,
                        Amount = proRatedAmount,
                        CategoryId = budget.CategoryId,
                        CategoryName = budget.Category?.Name ?? "Uncategorized",
                        CategoryColor = budget.Category?.Color ?? "#808080",
                        StartDate = overlapStart,
                        EndDate = overlapEnd,
                        Period = budget.Period,
                        SpentAmount = spent,
                        RemainingAmount = remaining,
                        SpentPercentage = percentage,
                        CreatedAt = budget.CreatedAt,
                        LastModifiedAt = budget.LastModifiedAt
                    };

                    budgetDtos.Add(budgetDto);

                    // Track total amounts
                    totalBudgetAmount += proRatedAmount;
                    totalSpentAmount += spent;

                    // Create budget status entry
                    budgetStatus[budget.Category?.Name ?? "Uncategorized"] = new BudgetStatusDto
                    {
                        Id = budget.Id,
                        Name = budget.Name,
                        Amount = proRatedAmount,
                        Spent = spent,
                        Remaining = remaining,
                        Percentage = percentage
                    };
                }

                var totalRemainingAmount = totalBudgetAmount - totalSpentAmount;
                var totalSpentPercentage =
                    totalBudgetAmount > 0 ? (double)(totalSpentAmount / totalBudgetAmount) * 100 : 0;

                // Create the summary DTO
                var summary = new BudgetSummaryDto
                {
                    TotalBudgetAmount = totalBudgetAmount,
                    TotalSpentAmount = totalSpentAmount,
                    TotalRemainingAmount = totalRemainingAmount,
                    TotalSpentPercentage = totalSpentPercentage,
                    Budgets = budgetDtos,
                    SpendingByCategory = spendingByCategory,
                    BudgetStatus = budgetStatus
                };

                response.Data = summary;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating budget summary");
                response.Success = false;
                response.Message = "Failed to generate budget summary";
                return response;
            }
        }
    }
}
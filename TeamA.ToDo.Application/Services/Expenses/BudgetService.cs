using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.Expenses;
using TeamA.ToDo.Core.Models.Expenses;
using TeamA.ToDo.EntityFramework;

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
}
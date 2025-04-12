using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Filters;
using TeamA.ToDo.Application.Interfaces.Expenses;

namespace TeamA.ToDo.Host.Controllers.Expense.TeamA.ToDo.Host.Controllers.Expense;

[ApiController]
[Route("api/budget-alert-settings")]
[Authorize]
[FeatureRequirement("ExpenseApp")]
public class BudgetAlertSettingsController : ControllerBase
{
    private readonly IBudgetAlertSettingsService _budgetAlertSettingsService;
    private readonly IBudgetAlertService _budgetAlertService;
    private readonly ILogger<BudgetAlertSettingsController> _logger;

    public BudgetAlertSettingsController(
        IBudgetAlertSettingsService budgetAlertSettingsService,
        IBudgetAlertService budgetAlertService,
        ILogger<BudgetAlertSettingsController> logger)
    {
        _budgetAlertSettingsService = budgetAlertSettingsService;
        _budgetAlertService = budgetAlertService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<BudgetAlertSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _budgetAlertSettingsService.GetBudgetAlertSettingsAsync(userId);
        return Ok(response);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ServiceResponse<BudgetAlertSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<BudgetAlertSettingsDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings([FromBody] BudgetAlertSettingsDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<BudgetAlertSettingsDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _budgetAlertSettingsService.UpdateBudgetAlertSettingsAsync(userId, dto);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("test-alert")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendTestAlert()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _budgetAlertService.SendBudgetSummaryEmailAsync(userId);
        return Ok(response);
    }
}
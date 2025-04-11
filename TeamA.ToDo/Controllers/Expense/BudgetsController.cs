using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.Expenses;

namespace TeamA.ToDo.Host.Controllers.Expense;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly ILogger<BudgetsController> _logger;

    public BudgetsController(
        IBudgetService budgetService,
        ILogger<BudgetsController> logger)
    {
        _budgetService = budgetService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<List<BudgetDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBudgets()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _budgetService.GetBudgetsAsync(userId);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<BudgetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<BudgetDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBudgetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _budgetService.GetBudgetByIdAsync(id, userId);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServiceResponse<BudgetDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ServiceResponse<BudgetDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<BudgetDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _budgetService.CreateBudgetAsync(userId, dto);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetBudgetById), new { id = response.Data.Id }, response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<BudgetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<BudgetDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<BudgetDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBudget(Guid id, [FromBody] UpdateBudgetDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<BudgetDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _budgetService.UpdateBudgetAsync(id, userId, dto);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _budgetService.DeleteBudgetAsync(id, userId);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ServiceResponse<BudgetSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBudgetSummary([FromQuery] DateTime? month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _budgetService.GetBudgetSummaryAsync(userId, month);
        return Ok(response);
    }
}
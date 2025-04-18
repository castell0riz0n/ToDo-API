﻿using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.Expenses.Reporting;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Filters;
using TeamA.ToDo.Application.Interfaces.Expenses;

namespace TeamA.ToDo.Host.Controllers.Expense;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[FeatureRequirement("ExpenseApp")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(
        IExpenseService expenseService,
        ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ExpenseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenses([FromQuery] ExpenseFilterDto filter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _expenseService.GetExpensesAsync(userId, filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpenseById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _expenseService.GetExpenseByIdAsync(id, userId);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<ExpenseDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)
                                        .ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _expenseService.CreateExpenseAsync(userId, dto);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetExpenseById), new { id = response.Data.Id }, response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] UpdateExpenseDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<ExpenseDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)
                                        .ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _expenseService.UpdateExpenseAsync(id, userId, dto);

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
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _expenseService.DeleteExpenseAsync(id, userId);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _expenseService.GetExpenseStatisticsAsync(userId, startDate, endDate);
        return Ok(response);
    }

    // Admin-only endpoint similar to TodoTask's "all" endpoint
    [HttpGet("all")]
    [Authorize(Policy = "CanViewAllExpenses")]
    [ProducesResponseType(typeof(ServiceResponse<PagedResponse<ExpenseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllExpenses([FromQuery] ExpenseFilterDto filter)
    {
        var response = await _expenseService.GetAllExpensesAsync(filter);
        return Ok(response);
    }

    // Add to ExpensesController
    [HttpGet("reports/monthly")]
    [ProducesResponseType(typeof(ServiceResponse<List<MonthlyExpenseSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthlyExpenseReport([FromQuery] int year)
    {
        if (year <= 0)
        {
            year = DateTime.UtcNow.Year; // Default to current year
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _expenseService.GetMonthlyExpenseReportAsync(userId, year);
        return Ok(response);
    }

    [HttpGet("reports/yearly-comparison")]
    [ProducesResponseType(typeof(ServiceResponse<YearlyComparisonReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetYearlyComparisonReport([FromQuery] int year1, [FromQuery] int year2)
    {
        if (year1 <= 0 || year2 <= 0)
        {
            return BadRequest(new ServiceResponse<YearlyComparisonReportDto>
            {
                Success = false,
                Message = "Both years must be valid"
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _expenseService.GetYearlyComparisonReportAsync(userId, year1, year2);
        return Ok(response);
    }

    [HttpGet("reports/category-breakdown")]
    [ProducesResponseType(typeof(ServiceResponse<CategoryBreakdownReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryBreakdownReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var today = DateTime.UtcNow.Date;
        var start = startDate ?? today.AddMonths(-1);
        var end = endDate ?? today;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _expenseService.GetCategoryBreakdownReportAsync(userId, start, end);
        return Ok(response);
    }

    [HttpGet("reports/trend-analysis")]
    [ProducesResponseType(typeof(ServiceResponse<TrendAnalysisDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenseTrendAnalysis([FromQuery] int months = 6)
    {
        if (months <= 0 || months > 60) // Limit to reasonable range
        {
            months = 6; // Default to 6 months
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _expenseService.GetExpenseTrendAnalysisAsync(userId, months);
        return Ok(response);
    }
}
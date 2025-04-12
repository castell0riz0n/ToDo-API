using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Filters;
using TeamA.ToDo.Application.Interfaces.Expenses;

namespace TeamA.ToDo.Host.Controllers.Expense;

[ApiController]
[Route("api/expense-exports")]
[Authorize]
[FeatureRequirement("ExpenseApp")]
public class ExpenseExportController : ControllerBase
{
    private readonly IExpenseExportService _exportService;
    private readonly ILogger<ExpenseExportController> _logger;

    public ExpenseExportController(
        IExpenseExportService exportService,
        ILogger<ExpenseExportController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    [HttpGet("csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<byte[]>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportExpensesToCsv(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _exportService.ExportExpensesToCsvAsync(userId, startDate, endDate);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        var fileName = "expenses.csv";
        if (startDate.HasValue && endDate.HasValue)
        {
            fileName = $"expenses_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.csv";
        }

        return File(response.Data, "text/csv", fileName);
    }

    [HttpGet("excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<byte[]>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportExpensesToExcel(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _exportService.ExportExpensesToExcelAsync(userId, startDate, endDate);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        var fileName = "expenses.xlsx";
        if (startDate.HasValue && endDate.HasValue)
        {
            fileName = $"expenses_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.xlsx";
        }

        return File(
            response.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("budgets/excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<byte[]>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportBudgetsToExcel()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _exportService.ExportBudgetsToExcelAsync(userId);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return File(
            response.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "budgets.xlsx");
    }

    [HttpGet("reports/monthly/excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<byte[]>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportMonthlyReportToExcel(
        [FromQuery] int year,
        [FromQuery] int? month)
    {
        if (year <= 0)
        {
            year = DateTime.UtcNow.Year; // Default to current year
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _exportService.ExportMonthlyReportToExcelAsync(userId, year, month);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        var fileName = month.HasValue
            ? $"expense_report_{year}_{month:D2}.xlsx"
            : $"expense_report_{year}.xlsx";

        return File(
            response.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("reports/category/excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<byte[]>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportCategoryReportToExcel(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var today = DateTime.UtcNow.Date;
        var start = startDate ?? today.AddMonths(-1);
        var end = endDate ?? today;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _exportService.ExportCategoryReportToExcelAsync(userId, start, end);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        var fileName = $"category_report_{start:yyyy-MM-dd}_to_{end:yyyy-MM-dd}.xlsx";

        return File(
            response.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
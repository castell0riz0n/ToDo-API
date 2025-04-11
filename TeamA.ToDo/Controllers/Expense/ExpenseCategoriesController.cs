using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.Expenses;

namespace TeamA.ToDo.Host.Controllers.Expense;

[ApiController]
[Route("api/expense-categories")]
[Authorize]
public class ExpenseCategoriesController : ControllerBase
{
    private readonly IExpenseCategoryService _categoryService;
    private readonly ILogger<ExpenseCategoriesController> _logger;

    public ExpenseCategoriesController(
        IExpenseCategoryService categoryService,
        ILogger<ExpenseCategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<List<ExpenseCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _categoryService.GetCategoriesAsync(userId);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseCategoryDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _categoryService.GetCategoryByIdAsync(id, userId);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseCategoryDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateExpenseCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<ExpenseCategoryDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _categoryService.CreateCategoryAsync(userId, dto);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetCategoryById), new { id = response.Data.Id }, response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseCategoryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<ExpenseCategoryDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateExpenseCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<ExpenseCategoryDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _categoryService.UpdateCategoryAsync(id, userId, dto);

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
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _categoryService.DeleteCategoryAsync(id, userId);

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
}
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.Filters;
using TeamA.ToDo.Application.Interfaces;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Host.Controllers.Todo;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[FeatureRequirement("TodoApp")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TaskCategoryDto>>> GetCategories()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var categories = await _categoryService.GetCategoriesAsync(userId);
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskCategoryDto>> GetCategory(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var category = await _categoryService.GetCategoryByIdAsync(id, userId);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<TaskCategoryDto>> CreateCategory(CreateTaskCategoryDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var category = await _categoryService.CreateCategoryAsync(userId, dto);
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TaskCategoryDto>> UpdateCategory(Guid id, UpdateTaskCategoryDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var category = await _categoryService.UpdateCategoryAsync(id, userId, dto);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCategory(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _categoryService.DeleteCategoryAsync(id, userId);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
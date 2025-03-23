using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.Interfaces;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TagDto>>> GetTags()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tags = await _tagService.GetTagsAsync(userId);
        return Ok(tags);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tag = await _tagService.GetTagByIdAsync(id, userId);

        if (tag == null)
        {
            return NotFound();
        }

        return Ok(tag);
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag(CreateTagDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tag = await _tagService.CreateTagAsync(userId, dto);
        return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _tagService.DeleteTagAsync(id, userId);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
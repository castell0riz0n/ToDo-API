using Microsoft.EntityFrameworkCore;
using TeamA.ToDo.Application.Interfaces;
using TeamA.ToDo.Core.Models.Todo;
using TeamA.ToDo.EntityFramework;
using TodoApp.API.DTOs;

namespace TeamA.ToDo.Application.Services;

public class TagService : ITagService
{
    private readonly ApplicationDbContext _context;

    public TagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TagDto>> GetTagsAsync(string userId)
    {
        var tags = await _context.Tags
            .Where(t => t.UserId == userId)
            .ToListAsync();

        var tagCounts = await _context.TaskTags
            .Where(tt => tt.Tag.UserId == userId)
            .GroupBy(tt => tt.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TagId, x => x.Count);

        return tags.Select(t => new TagDto
        {
            Id = t.Id,
            Name = t.Name,
            TaskCount = tagCounts.ContainsKey(t.Id) ? tagCounts[t.Id] : 0
        }).ToList();
    }

    public async Task<TagDto> GetTagByIdAsync(Guid id, string userId)
    {
        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (tag == null)
        {
            return null;
        }

        var taskCount = await _context.TaskTags
            .CountAsync(tt => tt.TagId == id);

        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            TaskCount = taskCount
        };
    }

    public async Task<TagDto> CreateTagAsync(string userId, CreateTagDto dto)
    {
        // Check if tag with the same name already exists
        var existingTag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == dto.Name && t.UserId == userId);

        if (existingTag != null)
        {
            throw new InvalidOperationException("A tag with this name already exists");
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            UserId = userId
        };

        await _context.Tags.AddAsync(tag);
        await _context.SaveChangesAsync();

        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            TaskCount = 0
        };
    }

    public async Task<bool> DeleteTagAsync(Guid id, string userId)
    {
        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (tag == null)
        {
            return false;
        }

        // Delete all task-tag associations for this tag
        var taskTags = await _context.TaskTags
            .Where(tt => tt.TagId == id)
            .ToListAsync();

        _context.TaskTags.RemoveRange(taskTags);
        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        return true;
    }
}
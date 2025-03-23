using TodoApp.API.DTOs;

namespace TeamA.ToDo.Application.Interfaces;

public interface ITagService
{
    Task<List<TagDto>> GetTagsAsync(string userId);
    Task<TagDto> GetTagByIdAsync(Guid id, string userId);
    Task<TagDto> CreateTagAsync(string userId, CreateTagDto dto);
    Task<bool> DeleteTagAsync(Guid id, string userId);
}
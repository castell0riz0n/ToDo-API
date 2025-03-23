namespace TeamA.ToDo.Application.DTOs.Users;

public class AdminUserUpdateDto : UserUpdateDto
{
    public bool IsActive { get; set; }
    public List<string> RoleIds { get; set; }
}
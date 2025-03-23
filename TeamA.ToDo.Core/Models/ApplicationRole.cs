
using Microsoft.AspNetCore.Identity;

namespace TeamA.ToDo.Core.Models;

public class ApplicationRole : IdentityRole
{
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<RolePermission> RolePermissions { get; set; }
}
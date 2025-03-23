using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Core.Models;

public class Permission
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }

    // M:N relationship with roles
    public virtual ICollection<RolePermission> RolePermissions { get; set; }
}
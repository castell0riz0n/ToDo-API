using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamA.ToDo.Core.Models;

public class RolePermission
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string RoleId { get; set; }
    [ForeignKey("RoleId")]
    public virtual ApplicationRole Role { get; set; }

    public string PermissionId { get; set; }
    [ForeignKey("PermissionId")]
    public virtual Permission Permission { get; set; }
}
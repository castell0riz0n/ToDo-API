using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamA.ToDo.Core.Models;

public class UserActivity
{
    [Key]
    public int Id { get; set; }

    public string? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; }

    [Required]
    [MaxLength(255)]
    public string Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string IpAddress { get; set; }

    public bool IsSuccessful { get; set; }

    public DateTime Timestamp { get; set; }

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
}
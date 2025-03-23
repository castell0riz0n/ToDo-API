using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Core.Models.Todo;

public class ToDoItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DueDate { get; set; }

    [Required]
    public string UserId { get; set; }

    [MaxLength(50)]
    public string Priority { get; set; } // Low, Medium, High

    [MaxLength(50)]
    public string Status { get; set; } // NotStarted, InProgress, Completed, Cancelled

    [MaxLength(50)]
    public string Category { get; set; }

    public bool IsReminded { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; }

    // Tags or labels as a JSON string
    [MaxLength(500)]
    public string Tags { get; set; }

    // Attachment URLs as a JSON string
    [MaxLength(1000)]
    public string Attachments { get; set; }

    // Recurrence pattern as a JSON string (for recurring tasks)
    [MaxLength(200)]
    public string RecurrencePattern { get; set; }
}
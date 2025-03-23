using Microsoft.AspNetCore.Identity;
using TeamA.ToDo.Core.Models.Todo;

namespace TeamA.ToDo.Core.Models;


public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties for ToDo items
    public virtual ICollection<ToDoItem> ToDoItems { get; set; }

    // For refresh tokens
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
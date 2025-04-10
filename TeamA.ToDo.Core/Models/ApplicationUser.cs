using Microsoft.AspNetCore.Identity;
using TeamA.ToDo.Core.Models.Expenses;
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

    // Navigation properties for TodoTask items (explicit relationship)
    public virtual ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();

    // For refresh tokens
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    
    // Expense tracker navigation properties
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public virtual ICollection<ExpenseCategory> ExpenseCategories { get; set; } = new List<ExpenseCategory>();
    public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
    public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public virtual ICollection<ExpenseTag> ExpenseTags { get; set; } = new List<ExpenseTag>();
}
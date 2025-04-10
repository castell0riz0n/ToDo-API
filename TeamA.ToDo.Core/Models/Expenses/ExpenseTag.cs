namespace TeamA.ToDo.Core.Models.Expenses;

public class ExpenseTag
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
        
    // Navigation property
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
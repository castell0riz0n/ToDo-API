namespace TeamA.ToDo.Core.Models.Expenses;

public class PaymentMethod
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public bool IsDefault { get; set; }
        
    // Navigation property
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
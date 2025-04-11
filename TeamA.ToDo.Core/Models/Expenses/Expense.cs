
namespace TeamA.ToDo.Core.Models.Expenses
{
    public class Expense
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public Guid CategoryId { get; set; }
        public ExpenseCategory Category { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public Guid? PaymentMethodId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public bool IsRecurring { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }
        public string ReceiptUrl { get; set; }
        
        // Navigation properties
        public ExpenseRecurrence RecurrenceInfo { get; set; }
        public ICollection<ExpenseTag> Tags { get; set; } = new List<ExpenseTag>();
    }
}
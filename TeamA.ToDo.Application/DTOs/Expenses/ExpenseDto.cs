namespace TeamA.ToDo.Application.DTOs.Expenses
{
    public class ExpenseDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryColor { get; set; }
        public Guid? PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; }
        public bool IsRecurring { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string ReceiptUrl { get; set; }
        public ExpenseRecurrenceDto RecurrenceInfo { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}



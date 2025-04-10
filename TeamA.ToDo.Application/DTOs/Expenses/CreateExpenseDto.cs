using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.Expenses;

public class CreateExpenseDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public Guid? PaymentMethodId { get; set; }

    public bool IsRecurring { get; set; } = false;

    public string ReceiptUrl { get; set; }

    public CreateExpenseRecurrenceDto RecurrenceInfo { get; set; }

    public List<string> Tags { get; set; } = new List<string>();
}
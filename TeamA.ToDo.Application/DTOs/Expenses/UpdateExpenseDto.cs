using System.ComponentModel.DataAnnotations;

namespace TeamA.ToDo.Application.DTOs.Expenses;

public class UpdateExpenseDto
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal? Amount { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; }

    public DateTime? Date { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? PaymentMethodId { get; set; }

    public bool? IsRecurring { get; set; }

    public string ReceiptUrl { get; set; }

    public UpdateExpenseRecurrenceDto RecurrenceInfo { get; set; }

    public List<string> Tags { get; set; }
}
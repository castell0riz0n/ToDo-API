using TeamA.ToDo.Core.Shared.Enums.Expenses;

namespace TeamA.ToDo.Application.DTOs.Expenses;

public class ExpenseFilterDto
{
    public string SearchTerm { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? CategoryId { get; set; }
    public List<Guid> CategoryIds { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public List<string> Tags { get; set; }
    public bool? IsRecurring { get; set; }
    public ExpenseType? ExpenseType { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "Date";
    public bool SortAscending { get; set; } = false;
}
namespace TeamA.ToDo.Application.DTOs.Expenses;

public class TopExpenseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string CategoryName { get; set; }
    public string CategoryColor { get; set; }
}
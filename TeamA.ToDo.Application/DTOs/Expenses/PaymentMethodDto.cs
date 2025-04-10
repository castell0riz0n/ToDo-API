namespace TeamA.ToDo.Application.DTOs.Expenses;

public class PaymentMethodDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public bool IsDefault { get; set; }
    public int ExpenseCount { get; set; }
}
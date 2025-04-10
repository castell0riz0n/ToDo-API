namespace TeamA.ToDo.Application.DTOs.Expenses;

public class BudgetStatusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public double Percentage { get; set; }
    public bool IsOverBudget => Percentage > 100;
    public bool IsNearLimit => Percentage >= 80 && Percentage <= 100;
}
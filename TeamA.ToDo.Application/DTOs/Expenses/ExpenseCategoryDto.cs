namespace TeamA.ToDo.Application.DTOs.Expenses;

public class ExpenseCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
    public string Icon { get; set; }
    public bool IsSystem { get; set; }
    public int ExpenseCount { get; set; }
}
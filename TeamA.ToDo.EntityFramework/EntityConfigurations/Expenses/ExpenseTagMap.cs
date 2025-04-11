using TeamA.ToDo.Core.Models.Expenses;

namespace TeamA.ToDo.EntityFramework.EntityConfigurations.Expenses;

public class ExpenseTagMap
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Expense Expense { get; set; }
    public Guid ExpenseTagId { get; set; }
    public ExpenseTag ExpenseTag { get; set; }
}
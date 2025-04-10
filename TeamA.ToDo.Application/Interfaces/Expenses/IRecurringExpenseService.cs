using TeamA.ToDo.Core.Models.Expenses;

namespace TeamA.ToDo.Application.Interfaces.Expenses;

public interface IRecurringExpenseService
{
    Task ProcessRecurringExpensesAsync();
    Task ScheduleRecurringExpenseAsync(Expense expense);
    Task UpdateRecurringExpenseScheduleAsync(Expense expense);
    Task CancelRecurringExpenseAsync(Guid expenseId);
    Task<DateTime?> CalculateNextOccurrenceAsync(ExpenseRecurrence recurrence);
}
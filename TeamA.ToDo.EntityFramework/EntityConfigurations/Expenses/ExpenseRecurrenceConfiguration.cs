using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamA.ToDo.Core.Models.Expenses;

namespace TeamA.ToDo.EntityFramework.EntityConfigurations.Expenses;

public class ExpenseRecurrenceConfiguration : IEntityTypeConfiguration<ExpenseRecurrence>
{
    public void Configure(EntityTypeBuilder<ExpenseRecurrence> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ExpenseId)
            .IsRequired();

        builder.Property(r => r.RecurrenceType)
            .IsRequired();

        builder.Property(r => r.StartDate)
            .IsRequired();

        builder.Property(r => r.CustomCronExpression)
            .HasMaxLength(100);
    }
}
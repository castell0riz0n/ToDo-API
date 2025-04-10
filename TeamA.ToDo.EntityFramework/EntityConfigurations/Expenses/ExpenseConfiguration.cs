// File: TeamA.ToDo.EntityFramework/EntityConfigurations/ExpenseConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamA.ToDo.Core.Models.Expenses;

namespace TeamA.ToDo.EntityFramework.EntityConfigurations.Expenses
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(e => e.Description)
                .HasMaxLength(500);

            builder.Property(e => e.Date)
                .IsRequired();

            builder.Property(e => e.UserId)
                .IsRequired();

            builder.Property(e => e.ReceiptUrl)
                .HasMaxLength(500);

            builder.HasOne(e => e.User)
                .WithMany(u => u.Expenses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Category)
                .WithMany(c => c.Expenses)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);  // Don't cascade delete because of potential circular references

            builder.HasOne(e => e.PaymentMethod)
                .WithMany(p => p.Expenses)
                .HasForeignKey(e => e.PaymentMethodId)
                .OnDelete(DeleteBehavior.NoAction);  // Don't cascade delete because it's optional

            builder.HasOne(e => e.RecurrenceInfo)
                .WithOne(r => r.Expense)
                .HasForeignKey<ExpenseRecurrence>(r => r.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}



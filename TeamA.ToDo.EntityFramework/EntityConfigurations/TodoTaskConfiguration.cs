using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamA.ToDo.Core.Models.Todo;

namespace TeamA.ToDo.EntityFramework.EntityConfigurations
{
    public class TodoTaskConfiguration : IEntityTypeConfiguration<TodoTask>
    {
        public void Configure(EntityTypeBuilder<TodoTask> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Description)
                .HasMaxLength(1000);

            builder.Property(t => t.UserId)
                .IsRequired();

            builder.HasOne(t => t.User)
                .WithMany(u => u.Tasks) // Updated to reference the Tasks navigation property
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Category)
                .WithMany(c => c.Tasks)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(t => t.RecurrenceInfo)
                .WithOne(r => r.TodoTask)
                .HasForeignKey<RecurrenceInfo>(r => r.TodoTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
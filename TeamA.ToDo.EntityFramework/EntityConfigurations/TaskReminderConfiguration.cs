using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamA.ToDo.Core.Models.Todo;

namespace TeamA.ToDo.EntityFramework.EntityConfigurations;

public class TaskReminderConfiguration : IEntityTypeConfiguration<TaskReminder>
{
    public void Configure(EntityTypeBuilder<TaskReminder> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.TodoTask)
            .WithMany(t => t.Reminders)
            .HasForeignKey(r => r.TodoTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
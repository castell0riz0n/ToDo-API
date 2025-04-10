using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamA.ToDo.Core.Models.Todo;

namespace TeamA.ToDo.EntityFramework.EntityConfigurations.Todo;

public class TaskTagConfiguration : IEntityTypeConfiguration<TaskTag>
{
    public void Configure(EntityTypeBuilder<TaskTag> builder)
    {
        builder.HasKey(tt => tt.Id);

        builder.HasOne(tt => tt.TodoTask)
            .WithMany(t => t.TaskTags)
            .HasForeignKey(tt => tt.TodoTaskId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(tt => tt.Tag)
            .WithMany(t => t.TaskTags)
            .HasForeignKey(tt => tt.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure a tag is only associated with a task once
        builder.HasIndex(tt => new { tt.TodoTaskId, tt.TagId })
            .IsUnique();
    }
}
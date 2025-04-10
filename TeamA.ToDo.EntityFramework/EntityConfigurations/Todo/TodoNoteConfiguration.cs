using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamA.ToDo.Core.Models.Todo;

namespace TeamA.ToDo.EntityFramework.EntityConfigurations.Todo;

public class TodoNoteConfiguration : IEntityTypeConfiguration<TodoNote>
{
    public void Configure(EntityTypeBuilder<TodoNote> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(n => n.TodoTask)
            .WithMany(t => t.Notes)
            .HasForeignKey(n => n.TodoTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
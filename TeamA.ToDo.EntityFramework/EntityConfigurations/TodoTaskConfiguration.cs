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
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Category)
                .WithMany(c => c.Tasks)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(t => t.RecurrenceInfo)
                .WithOne(r => r.TodoTask)
                .HasForeignKey<RecurrenceInfo>(r => r.TodoTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class RecurrenceInfoConfiguration : IEntityTypeConfiguration<RecurrenceInfo>
    {
        public void Configure(EntityTypeBuilder<RecurrenceInfo> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.CustomCronExpression)
                .HasMaxLength(100);
        }
    }

    public class TaskCategoryConfiguration : IEntityTypeConfiguration<TaskCategory>
    {
        public void Configure(EntityTypeBuilder<TaskCategory> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(c => c.Color)
                .HasMaxLength(7);  // Hex color code (#RRGGBB)

            builder.Property(c => c.UserId)
                .IsRequired();

            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure unique category names per user
            builder.HasIndex(c => new { c.UserId, c.Name })
                .IsUnique();
        }
    }

    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(t => t.UserId)
                .IsRequired();

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure unique tag names per user
            builder.HasIndex(t => new { t.UserId, t.Name })
                .IsUnique();
        }
    }

    public class TaskTagConfiguration : IEntityTypeConfiguration<TaskTag>
    {
        public void Configure(EntityTypeBuilder<TaskTag> builder)
        {
            builder.HasKey(tt => tt.Id);

            builder.HasOne(tt => tt.TodoTask)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(tt => tt.TodoTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tt => tt.Tag)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure a tag is only associated with a task once
            builder.HasIndex(tt => new { tt.TodoTaskId, tt.TagId })
                .IsUnique();
        }
    }

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
}
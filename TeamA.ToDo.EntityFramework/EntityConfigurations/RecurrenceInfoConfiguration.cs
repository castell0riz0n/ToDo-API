using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamA.ToDo.Core.Models.Todo;

namespace TeamA.ToDo.EntityFramework.EntityConfigurations;

public class RecurrenceInfoConfiguration : IEntityTypeConfiguration<RecurrenceInfo>
{
    public void Configure(EntityTypeBuilder<RecurrenceInfo> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.CustomCronExpression)
            .HasMaxLength(100);
    }
}
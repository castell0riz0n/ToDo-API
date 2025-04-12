using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamA.ToDo.Core.Models.FeatureManagement;

namespace TeamA.ToDo.EntityFramework.EntityConfigurations.FeatureManagement;

public class UserFeatureFlagConfiguration : IEntityTypeConfiguration<UserFeatureFlag>
{
    public void Configure(EntityTypeBuilder<UserFeatureFlag> builder)
    {

        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.FeatureDefinition)
            .WithMany(f => f.UserFeatureFlags)
            .HasForeignKey(e => e.FeatureDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.FeatureDefinitionId, e.UserId }).IsUnique();

    }
}
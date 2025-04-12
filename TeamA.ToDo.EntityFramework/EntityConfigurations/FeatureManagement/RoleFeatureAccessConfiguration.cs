using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamA.ToDo.Core.Models.FeatureManagement;

namespace TeamA.ToDo.EntityFramework.EntityConfigurations.FeatureManagement;

public class RoleFeatureAccessConfiguration : IEntityTypeConfiguration<RoleFeatureAccess>
{
    public void Configure(EntityTypeBuilder<RoleFeatureAccess> builder)
    {

        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.FeatureDefinition)
            .WithMany(f => f.RoleFeatureAccess)
            .HasForeignKey(e => e.FeatureDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Role)
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.FeatureDefinitionId, e.RoleId }).IsUnique();

    }
}
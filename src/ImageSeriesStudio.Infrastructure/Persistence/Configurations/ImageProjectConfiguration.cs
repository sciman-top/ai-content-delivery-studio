using ImageSeriesStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageSeriesStudio.Infrastructure.Persistence.Configurations;

internal sealed class ImageProjectConfiguration : IEntityTypeConfiguration<ImageProject>
{
    public void Configure(EntityTypeBuilder<ImageProject> entity)
    {
        entity.HasKey(project => project.Id);
        entity.Property(project => project.Name).IsRequired();
        entity.HasMany(project => project.Series)
            .WithOne()
            .HasForeignKey(series => series.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(project => project.ProviderProfiles)
            .WithOne()
            .HasForeignKey(profile => profile.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(project => project.SourceAssets)
            .WithOne()
            .HasForeignKey(asset => asset.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(project => project.OutputArtifacts)
            .WithOne()
            .HasForeignKey(artifact => artifact.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(project => project.ArtifactPackages)
            .WithOne()
            .HasForeignKey(package => package.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(project => project.DocumentBriefs)
            .WithOne()
            .HasForeignKey(brief => brief.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(project => project.IllustrationPlans)
            .WithOne()
            .HasForeignKey(plan => plan.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(project => project.RoutedRepairPatches)
            .WithOne()
            .HasForeignKey(patch => patch.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(project => project.Series).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(project => project.ProviderProfiles).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(project => project.SourceAssets).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(project => project.SourceAssets).AutoInclude();
        entity.Navigation(project => project.OutputArtifacts).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(project => project.OutputArtifacts).AutoInclude();
        entity.Navigation(project => project.ArtifactPackages).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(project => project.ArtifactPackages).AutoInclude();
        entity.Navigation(project => project.DocumentBriefs).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(project => project.DocumentBriefs).AutoInclude();
        entity.Navigation(project => project.IllustrationPlans).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(project => project.IllustrationPlans).AutoInclude();
        entity.Navigation(project => project.RoutedRepairPatches).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(project => project.RoutedRepairPatches).AutoInclude();
    }
}

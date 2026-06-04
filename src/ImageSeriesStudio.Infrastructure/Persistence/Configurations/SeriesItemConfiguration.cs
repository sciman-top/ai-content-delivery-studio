using ImageSeriesStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageSeriesStudio.Infrastructure.Persistence.Configurations;

internal sealed class SeriesItemConfiguration : IEntityTypeConfiguration<SeriesItem>
{
    public void Configure(EntityTypeBuilder<SeriesItem> entity)
    {
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Title).IsRequired();
        entity.Property(item => item.Kind).HasDefaultValue(SeriesItemKind.Standard);
        entity.HasMany(item => item.PromptVersions)
            .WithOne()
            .HasForeignKey(prompt => prompt.SeriesItemId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(item => item.GenerationTasks)
            .WithOne()
            .HasForeignKey(task => task.SeriesItemId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(item => item.CandidateImages)
            .WithOne()
            .HasForeignKey(candidate => candidate.SeriesItemId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(item => item.PromptVersions).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(item => item.GenerationTasks).UsePropertyAccessMode(PropertyAccessMode.Field);
        entity.Navigation(item => item.CandidateImages).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

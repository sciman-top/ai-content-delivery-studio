using ContentDeliveryStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class PromptVersionConfiguration : IEntityTypeConfiguration<PromptVersion>
{
    public void Configure(EntityTypeBuilder<PromptVersion> entity)
    {
        entity.HasKey(prompt => prompt.Id);
        entity.Property(prompt => prompt.PromptText).IsRequired();
        entity.OwnsOne(prompt => prompt.Settings, settings =>
        {
            settings.Property(value => value.Width).HasColumnName("Width");
            settings.Property(value => value.Height).HasColumnName("Height");
            settings.Property(value => value.Quality).HasColumnName("Quality");
            settings.Property(value => value.OutputFormat).HasColumnName("OutputFormat");
            settings.Property(value => value.Seed).HasColumnName("Seed");
        });
    }
}

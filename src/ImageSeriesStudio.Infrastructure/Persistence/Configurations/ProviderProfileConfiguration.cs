using ImageSeriesStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageSeriesStudio.Infrastructure.Persistence.Configurations;

internal sealed class ProviderProfileConfiguration : IEntityTypeConfiguration<ProviderProfile>
{
    public void Configure(EntityTypeBuilder<ProviderProfile> entity)
    {
        entity.HasKey(profile => profile.Id);
        entity.Property(profile => profile.DisplayName).IsRequired();
    }
}

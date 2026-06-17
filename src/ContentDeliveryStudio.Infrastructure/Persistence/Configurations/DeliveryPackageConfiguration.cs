using ContentDeliveryStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class DeliveryPackageConfiguration : IEntityTypeConfiguration<DeliveryPackage>
{
    public void Configure(EntityTypeBuilder<DeliveryPackage> entity)
    {
        entity.HasKey(package => package.Id);
        entity.Property(package => package.OutputPath).IsRequired();
        entity.Property(package => package.ManifestJsonPath).IsRequired();
        entity.Property(package => package.ManifestCsvPath).IsRequired();
    }
}

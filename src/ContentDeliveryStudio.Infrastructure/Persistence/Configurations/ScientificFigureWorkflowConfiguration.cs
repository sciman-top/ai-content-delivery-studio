using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class ScientificFigureWorkflowConfiguration
    : IEntityTypeConfiguration<ScientificFigureWorkflowPersistenceRecord>
{
    public void Configure(
        EntityTypeBuilder<ScientificFigureWorkflowPersistenceRecord> entity)
    {
        entity.ToTable("ScientificFigureWorkflows");
        entity.HasKey(record => record.Id);
        entity.Property(record => record.SourceSha256).IsRequired();
        entity.Property(record => record.PayloadSchemaVersion).IsRequired();
        entity.Property(record => record.PayloadJson).IsRequired();
        entity.HasIndex(record => record.ProjectId);
        entity.HasIndex(record => new
        {
            record.ProjectId,
            record.SpecificationId,
            record.SpecificationVersion,
        }).IsUnique();
        entity.HasOne<Core.Projects.ImageProject>()
            .WithMany()
            .HasForeignKey(record => record.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

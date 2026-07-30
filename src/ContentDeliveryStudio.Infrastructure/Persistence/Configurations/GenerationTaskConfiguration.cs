using ContentDeliveryStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class GenerationTaskConfiguration : IEntityTypeConfiguration<GenerationTask>
{
    public void Configure(EntityTypeBuilder<GenerationTask> entity)
    {
        entity.HasKey(task => task.Id);
        entity.Property(task => task.QueuePosition).IsRequired(false);
        entity.Property(task => task.RetryOfTaskId).IsRequired(false);
    }
}

using ContentDeliveryStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class GenerationTaskConfiguration : IEntityTypeConfiguration<GenerationTask>
{
    public void Configure(EntityTypeBuilder<GenerationTask> entity)
    {
        entity.HasKey(task => task.Id);
        entity.Property(task => task.QueuePosition).IsRequired(false);
        entity.Property(task => task.RetryOfTaskId).IsRequired(false);
        entity.Property(task => task.ApprovalReceipt)
            .HasConversion(
                receipt => receipt == null
                    ? null
                    : JsonSerializer.Serialize(receipt, JsonSerializerOptions.Default),
                json => string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize<ContentDeliveryStudio.Core.Generation.GenerationApprovalReceipt>(
                        json,
                        JsonSerializerOptions.Default))
            .IsRequired(false);
    }
}

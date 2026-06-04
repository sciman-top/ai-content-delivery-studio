using ImageSeriesStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageSeriesStudio.Infrastructure.Persistence.Configurations;

internal sealed class GenerationTaskConfiguration : IEntityTypeConfiguration<GenerationTask>
{
    public void Configure(EntityTypeBuilder<GenerationTask> entity)
    {
        entity.HasKey(task => task.Id);
    }
}

using ContentDeliveryStudio.Core.ScientificFigures;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Infrastructure.Persistence;

public sealed class EfScientificFigureWorkflowRepository
{
    private readonly AppDbContext _dbContext;

    public EfScientificFigureWorkflowRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(
        ScientificFigureWorkflowAggregate aggregate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        var projectExists = await _dbContext.Projects
            .AnyAsync(project => project.Id == aggregate.ProjectId, cancellationToken);
        if (!projectExists)
        {
            throw new InvalidOperationException(
                $"Project not found for scientific figure workflow: {aggregate.ProjectId}");
        }

        var replacement = ScientificFigureWorkflowPersistenceRecord.FromAggregate(aggregate);
        var existing = await _dbContext.ScientificFigureWorkflows
            .SingleOrDefaultAsync(record => record.Id == aggregate.Id, cancellationToken);
        if (existing is null)
        {
            _dbContext.ScientificFigureWorkflows.Add(replacement);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(replacement);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScientificFigureWorkflowAggregate?> LoadAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await _dbContext.ScientificFigureWorkflows
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return record?.ToAggregate();
    }

    public async Task<IReadOnlyList<ScientificFigureWorkflowAggregate>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var records = await _dbContext.ScientificFigureWorkflows
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .OrderBy(item => item.Id)
            .ToArrayAsync(cancellationToken);
        return records.Select(item => item.ToAggregate()).ToArray();
    }
}

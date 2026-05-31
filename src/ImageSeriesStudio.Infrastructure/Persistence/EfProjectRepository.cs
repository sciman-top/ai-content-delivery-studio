using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace ImageSeriesStudio.Infrastructure.Persistence;

public sealed class EfProjectRepository : IProjectRepository
{
    private readonly AppDbContext _dbContext;

    public EfProjectRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(ImageProject project, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Projects
            .AnyAsync(existing => existing.Id == project.Id, cancellationToken);

        if (exists)
        {
            _dbContext.Projects.Update(project);
        }
        else
        {
            _dbContext.Projects.Add(project);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .Include(project => project.ProviderProfiles)
            .Include(project => project.Series)
            .ThenInclude(series => series.Items)
            .ThenInclude(item => item.PromptVersions)
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var projects = await _dbContext.Projects
            .Select(project => new ProjectSummary(
                project.Id,
                project.Name,
                project.CreatedAt,
                project.UpdatedAt))
            .ToArrayAsync(cancellationToken);

        return projects
            .OrderByDescending(project => project.UpdatedAt)
            .ToArray();
    }
}

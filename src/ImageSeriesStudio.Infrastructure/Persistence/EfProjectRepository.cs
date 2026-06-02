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
        if (_dbContext.Entry(project).State is EntityState.Detached)
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
        }

        await TrackNewChildrenAsync(project, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .Include(project => project.ProviderProfiles)
            .Include(project => project.DocumentBriefs)
            .Include(project => project.IllustrationPlans)
            .Include(project => project.Series)
            .ThenInclude(series => series.CreativeBriefs)
            .Include(project => project.Series)
            .ThenInclude(series => series.Items)
            .ThenInclude(item => item.PromptVersions)
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    private async Task TrackNewChildrenAsync(ImageProject project, CancellationToken cancellationToken)
    {
        foreach (var profile in project.ProviderProfiles)
        {
            if (!await _dbContext.ProviderProfiles.AnyAsync(existing => existing.Id == profile.Id, cancellationToken))
            {
                _dbContext.Entry(profile).State = EntityState.Added;
            }
        }

        foreach (var series in project.Series)
        {
            if (!await _dbContext.Series.AnyAsync(existing => existing.Id == series.Id, cancellationToken))
            {
                _dbContext.Entry(series).State = EntityState.Added;
            }

            foreach (var brief in series.CreativeBriefs)
            {
                if (!await _dbContext.CreativeBriefs.AnyAsync(existing => existing.Id == brief.Id, cancellationToken))
                {
                    _dbContext.CreativeBriefs.Add(brief);
                }
            }

            foreach (var item in series.Items)
            {
                if (!await _dbContext.SeriesItems.AnyAsync(existing => existing.Id == item.Id, cancellationToken))
                {
                    _dbContext.Entry(item).State = EntityState.Added;
                }

                foreach (var prompt in item.PromptVersions)
                {
                    if (!await _dbContext.PromptVersions.AnyAsync(existing => existing.Id == prompt.Id, cancellationToken))
                    {
                        _dbContext.PromptVersions.Add(prompt);
                    }
                }
            }
        }

        foreach (var brief in project.DocumentBriefs)
        {
            if (!await _dbContext.DocumentBriefs.AnyAsync(existing => existing.Id == brief.Id, cancellationToken))
            {
                _dbContext.DocumentBriefs.Add(brief);
            }
        }

        foreach (var plan in project.IllustrationPlans)
        {
            if (!await _dbContext.IllustrationPlans.AnyAsync(existing => existing.Id == plan.Id, cancellationToken))
            {
                _dbContext.IllustrationPlans.Add(plan);
            }
        }
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

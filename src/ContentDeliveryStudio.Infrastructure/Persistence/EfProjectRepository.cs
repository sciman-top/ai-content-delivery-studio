using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Artifacts;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Sources;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Infrastructure.Persistence;

public sealed class EfProjectRepository : IProjectRepository
{
    private static readonly Type[] AggregateEntityTypes =
    [
        typeof(ImageProject),
        typeof(ImageSeries),
        typeof(CreativeBrief),
        typeof(SeriesItem),
        typeof(PromptVersion),
        typeof(GenerationTask),
        typeof(CandidateImage),
        typeof(ReviewResult),
        typeof(DocumentBrief),
        typeof(IllustrationPlan),
        typeof(ProviderProfile),
        typeof(SourceAsset),
        typeof(OutputArtifact),
        typeof(ArtifactPackage),
        typeof(RoutedRepairPatch),
    ];

    private readonly AppDbContext _dbContext;

    public EfProjectRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(ImageProject project, CancellationToken cancellationToken)
    {
        var snapshot = BuildAggregateIdentitySnapshot(project);
        var existingIds = await LoadExistingIdsAsync(snapshot, cancellationToken);

        if (_dbContext.Entry(project).State is EntityState.Detached)
        {
            if (existingIds.TryGetValue(typeof(ImageProject), out var projects)
                && projects.Contains(project.Id))
            {
                _dbContext.Projects.Update(project);
            }
            else
            {
                _dbContext.Projects.Add(project);
            }
        }

        // EF can discover new children under an existing tracked aggregate as Modified
        // when they already carry assigned Guid keys. Normalize those entries back to Added
        // before SaveChanges so inserts do not become concurrency-failing updates.
        _dbContext.ChangeTracker.DetectChanges();
        ApplyAddedStateToNewEntries(snapshot, existingIds);
        TrackModifiedCreativeBriefs(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .Include(project => project.ProviderProfiles)
            .Include(project => project.SourceAssets)
            .Include(project => project.OutputArtifacts)
            .Include(project => project.ArtifactPackages)
            .Include(project => project.DocumentBriefs)
            .Include(project => project.IllustrationPlans)
            .Include(project => project.RoutedRepairPatches)
            .Include(project => project.Series)
            .ThenInclude(series => series.CreativeBriefs)
            .Include(project => project.Series)
            .ThenInclude(series => series.Items)
            .ThenInclude(item => item.PromptVersions)
            .Include(project => project.Series)
            .ThenInclude(series => series.Items)
            .ThenInclude(item => item.GenerationTasks)
            .Include(project => project.Series)
            .ThenInclude(series => series.Items)
            .ThenInclude(item => item.CandidateImages)
            .ThenInclude(candidate => candidate.ReviewResults)
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    private static Dictionary<Type, HashSet<Guid>> BuildAggregateIdentitySnapshot(ImageProject project)
    {
        var snapshot = AggregateEntityTypes.ToDictionary(
            entityType => entityType,
            _ => new HashSet<Guid>());

        void Add<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            foreach (var entity in entities)
            {
                snapshot[typeof(TEntity)].Add(GetEntityId(entity));
            }
        }

        Add([project]);
        Add(project.ProviderProfiles);
        Add(project.SourceAssets);
        Add(project.OutputArtifacts);
        Add(project.ArtifactPackages);
        Add(project.DocumentBriefs);
        Add(project.IllustrationPlans);
        Add(project.RoutedRepairPatches);
        Add(project.Series);
        Add(project.Series.SelectMany(series => series.CreativeBriefs));
        Add(project.Series.SelectMany(series => series.Items));
        Add(project.Series.SelectMany(series => series.Items).SelectMany(item => item.PromptVersions));
        Add(project.Series.SelectMany(series => series.Items).SelectMany(item => item.GenerationTasks));
        Add(project.Series.SelectMany(series => series.Items).SelectMany(item => item.CandidateImages));
        Add(project.Series.SelectMany(series => series.Items).SelectMany(item => item.CandidateImages).SelectMany(candidate => candidate.ReviewResults));

        return snapshot;
    }

    private async Task<Dictionary<Type, HashSet<Guid>>> LoadExistingIdsAsync(
        IReadOnlyDictionary<Type, HashSet<Guid>> snapshot,
        CancellationToken cancellationToken)
    {
        var existingIds = AggregateEntityTypes.ToDictionary(
            entityType => entityType,
            _ => new HashSet<Guid>());

        await LoadExistingIdsAsync(
            snapshot,
            existingIds,
            typeof(ImageProject),
            ids => _dbContext.Projects.Where(project => ids.Contains(project.Id)).Select(project => project.Id),
            cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(ImageSeries), ids => _dbContext.Series.Where(series => ids.Contains(series.Id)).Select(series => series.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(CreativeBrief), ids => _dbContext.CreativeBriefs.Where(brief => ids.Contains(brief.Id)).Select(brief => brief.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(SeriesItem), ids => _dbContext.SeriesItems.Where(item => ids.Contains(item.Id)).Select(item => item.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(PromptVersion), ids => _dbContext.PromptVersions.Where(prompt => ids.Contains(prompt.Id)).Select(prompt => prompt.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(GenerationTask), ids => _dbContext.GenerationTasks.Where(task => ids.Contains(task.Id)).Select(task => task.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(CandidateImage), ids => _dbContext.CandidateImages.Where(candidate => ids.Contains(candidate.Id)).Select(candidate => candidate.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(ReviewResult), ids => _dbContext.ReviewResults.Where(review => ids.Contains(review.Id)).Select(review => review.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(DocumentBrief), ids => _dbContext.DocumentBriefs.Where(brief => ids.Contains(brief.Id)).Select(brief => brief.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(IllustrationPlan), ids => _dbContext.IllustrationPlans.Where(plan => ids.Contains(plan.Id)).Select(plan => plan.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(ProviderProfile), ids => _dbContext.ProviderProfiles.Where(profile => ids.Contains(profile.Id)).Select(profile => profile.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(SourceAsset), ids => _dbContext.SourceAssets.Where(asset => ids.Contains(asset.Id)).Select(asset => asset.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(OutputArtifact), ids => _dbContext.OutputArtifacts.Where(artifact => ids.Contains(artifact.Id)).Select(artifact => artifact.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(ArtifactPackage), ids => _dbContext.ArtifactPackages.Where(package => ids.Contains(package.Id)).Select(package => package.Id), cancellationToken);
        await LoadExistingIdsAsync(snapshot, existingIds, typeof(RoutedRepairPatch), ids => _dbContext.RoutedRepairPatches.Where(patch => ids.Contains(patch.Id)).Select(patch => patch.Id), cancellationToken);

        return existingIds;
    }

    private async Task LoadExistingIdsAsync(
        IReadOnlyDictionary<Type, HashSet<Guid>> snapshot,
        IDictionary<Type, HashSet<Guid>> existingIds,
        Type entityType,
        Func<HashSet<Guid>, IQueryable<Guid>> queryFactory,
        CancellationToken cancellationToken)
    {
        if (!snapshot.TryGetValue(entityType, out var ids) || ids.Count == 0)
        {
            return;
        }

        existingIds[entityType] = (await queryFactory(ids).ToArrayAsync(cancellationToken)).ToHashSet();
    }

    private void ApplyAddedStateToNewEntries(
        IReadOnlyDictionary<Type, HashSet<Guid>> snapshot,
        IReadOnlyDictionary<Type, HashSet<Guid>> existingIds)
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries().Where(entry => entry.Entity is not null))
        {
            var entityType = entry.Entity.GetType();
            if (!snapshot.TryGetValue(entityType, out var aggregateIds)
                || !existingIds.TryGetValue(entityType, out var persistedIds))
            {
                continue;
            }

            var entityId = GetEntityId(entry.Entity);
            if (!aggregateIds.Contains(entityId))
            {
                continue;
            }

            if (!persistedIds.Contains(entityId))
            {
                MarkEntryGraphAsAdded(entry);
            }
        }
    }

    private static void MarkEntryGraphAsAdded(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.State is not EntityState.Added)
        {
            entry.State = EntityState.Added;
        }

        foreach (var reference in entry.References)
        {
            if (reference.TargetEntry?.Metadata.IsOwned() is true)
            {
                MarkEntryGraphAsAdded(reference.TargetEntry);
            }
        }

        foreach (var collection in entry.Collections)
        {
            if (collection.Metadata.TargetEntityType.IsOwned()
                && collection.CurrentValue is IEnumerable<object> ownedEntities)
            {
                foreach (var ownedEntity in ownedEntities)
                {
                    var ownedEntry = entry.Context.Entry(ownedEntity);
                    MarkEntryGraphAsAdded(ownedEntry);
                }
            }
        }
    }

    private static Guid GetEntityId(object entity)
    {
        var idProperty = entity.GetType().GetProperty(nameof(ImageProject.Id))
            ?? throw new InvalidOperationException($"Entity type '{entity.GetType().FullName}' does not expose an Id property.");
        var idValue = idProperty.GetValue(entity);
        return idValue is Guid id
            ? id
            : throw new InvalidOperationException($"Entity type '{entity.GetType().FullName}' returned a non-Guid Id value.");
    }

    private void TrackModifiedCreativeBriefs(ImageProject project)
    {
        foreach (var brief in project.Series.SelectMany(series => series.CreativeBriefs))
        {
            var entry = _dbContext.Entry(brief);
            var hasBriefNotes = brief.RepairNotes.Count > 0;
            var hasBlueprintNotes = brief.DesignBlueprints.Any(blueprint => blueprint.RepairNotes.Count > 0);

            if (hasBriefNotes || hasBlueprintNotes)
            {
                entry.State = EntityState.Modified;
                entry.Property(nameof(CreativeBrief.RepairNotesJson)).IsModified = hasBriefNotes;
                if (hasBlueprintNotes)
                {
                    entry.Property(nameof(CreativeBrief.DesignBlueprints)).IsModified = true;
                }
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

    public async Task SaveReviewResultAsync(Guid projectId, ReviewResult reviewResult, CancellationToken cancellationToken)
    {
        var candidateBelongsToProject = await (
                from candidate in _dbContext.CandidateImages
                join item in _dbContext.SeriesItems on candidate.SeriesItemId equals item.Id
                join series in _dbContext.Series on item.SeriesId equals series.Id
                where candidate.Id == reviewResult.CandidateImageId && series.ProjectId == projectId
                select candidate.Id)
            .AnyAsync(cancellationToken);
        if (!candidateBelongsToProject)
        {
            throw new InvalidOperationException(
                $"Candidate image {reviewResult.CandidateImageId} does not belong to project {projectId}.");
        }

        var existing = await _dbContext.ReviewResults
            .SingleOrDefaultAsync(
                review => review.CandidateImageId == reviewResult.CandidateImageId,
                cancellationToken);
        if (existing is null)
        {
            _dbContext.ReviewResults.Add(reviewResult);
        }
        else
        {
            _dbContext.Entry(existing).Property(review => review.Decision).CurrentValue = reviewResult.Decision;
            _dbContext.Entry(existing).Property(review => review.Scores).CurrentValue = reviewResult.Scores;
            _dbContext.Entry(existing).Property(review => review.HardFailures).CurrentValue = reviewResult.HardFailures;
            _dbContext.Entry(existing).Property(review => review.Comments).CurrentValue = reviewResult.Comments;
            _dbContext.Entry(existing).Property(review => review.SuggestedFix).CurrentValue = reviewResult.SuggestedFix;
            _dbContext.Entry(existing).Property(review => review.HumanApproved).CurrentValue = reviewResult.HumanApproved;
            _dbContext.Entry(existing).Property(review => review.FinalReviewer).CurrentValue = reviewResult.FinalReviewer;
            _dbContext.Entry(existing).Property(review => review.FinalApprovalNotes).CurrentValue = reviewResult.FinalApprovalNotes;
            _dbContext.Entry(existing).Property(review => review.FinalApprovalDecidedAt).CurrentValue = reviewResult.FinalApprovalDecidedAt;
            _dbContext.Entry(existing).Property(review => review.CreatedAt).CurrentValue = reviewResult.CreatedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ReviewResult?> LoadLatestReviewResultAsync(Guid candidateImageId, CancellationToken cancellationToken)
    {
        return _dbContext.ReviewResults
            .Where(review => review.CandidateImageId == candidateImageId)
            .OrderByDescending(review => review.FinalApprovalDecidedAt ?? review.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

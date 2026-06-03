using System.Text.Json;
using ImageSeriesStudio.Core.Artifacts;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Sources;
using Microsoft.EntityFrameworkCore;

namespace ImageSeriesStudio.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ImageProject> Projects => Set<ImageProject>();

    public DbSet<ImageSeries> Series => Set<ImageSeries>();

    public DbSet<CreativeBrief> CreativeBriefs => Set<CreativeBrief>();

    public DbSet<DocumentBrief> DocumentBriefs => Set<DocumentBrief>();

    public DbSet<IllustrationPlan> IllustrationPlans => Set<IllustrationPlan>();

    public DbSet<SeriesItem> SeriesItems => Set<SeriesItem>();

    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();

    public DbSet<GenerationTask> GenerationTasks => Set<GenerationTask>();

    public DbSet<CandidateImage> CandidateImages => Set<CandidateImage>();

    public DbSet<ReviewRubric> ReviewRubrics => Set<ReviewRubric>();

    public DbSet<ReviewResult> ReviewResults => Set<ReviewResult>();

    public DbSet<DeliveryPackage> DeliveryPackages => Set<DeliveryPackage>();

    public DbSet<ProviderProfile> ProviderProfiles => Set<ProviderProfile>();

    public DbSet<SourceAsset> SourceAssets => Set<SourceAsset>();

    public DbSet<OutputArtifact> OutputArtifacts => Set<OutputArtifact>();

    public DbSet<ArtifactPackage> ArtifactPackages => Set<ArtifactPackage>();

    public DbSet<RoutedRepairPatch> RoutedRepairPatches => Set<RoutedRepairPatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<ArtifactManifest>();
        modelBuilder.Ignore<ArtifactManifestItem>();
        modelBuilder.Ignore<ExtractedContent>();
        modelBuilder.Ignore<EvidenceAnchor>();
        modelBuilder.Ignore<IllustrationTarget>();
        modelBuilder.Ignore<RoutedRepairPatchApplicationNote>();
        modelBuilder.Ignore<RoutedRepairPatchItem>();

        modelBuilder.Entity<ImageProject>(entity =>
        {
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Name).IsRequired();
            entity.HasMany(project => project.Series)
                .WithOne()
                .HasForeignKey(series => series.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.ProviderProfiles)
                .WithOne()
                .HasForeignKey(profile => profile.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.SourceAssets)
                .WithOne()
                .HasForeignKey(asset => asset.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.OutputArtifacts)
                .WithOne()
                .HasForeignKey(artifact => artifact.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.ArtifactPackages)
                .WithOne()
                .HasForeignKey(package => package.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.DocumentBriefs)
                .WithOne()
                .HasForeignKey(brief => brief.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.IllustrationPlans)
                .WithOne()
                .HasForeignKey(plan => plan.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.RoutedRepairPatches)
                .WithOne()
                .HasForeignKey(patch => patch.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(project => project.Series).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.ProviderProfiles).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.SourceAssets).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.SourceAssets).AutoInclude();
            entity.Navigation(project => project.OutputArtifacts).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.OutputArtifacts).AutoInclude();
            entity.Navigation(project => project.ArtifactPackages).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.ArtifactPackages).AutoInclude();
            entity.Navigation(project => project.DocumentBriefs).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.DocumentBriefs).AutoInclude();
            entity.Navigation(project => project.IllustrationPlans).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.IllustrationPlans).AutoInclude();
            entity.Navigation(project => project.RoutedRepairPatches).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.RoutedRepairPatches).AutoInclude();
        });

        modelBuilder.Entity<ImageSeries>(entity =>
        {
            entity.HasKey(series => series.Id);
            entity.Property(series => series.Title).IsRequired();
            entity.HasMany(series => series.Items)
                .WithOne()
                .HasForeignKey(item => item.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(series => series.CreativeBriefs)
                .WithOne()
                .HasForeignKey(brief => brief.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(series => series.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(series => series.CreativeBriefs).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<CreativeBrief>(entity =>
        {
            entity.HasKey(brief => brief.Id);
            entity.Property(brief => brief.Goal).IsRequired();
            entity.Property(brief => brief.Audience).IsRequired();
            entity.Property(brief => brief.StyleIntent).IsRequired();
            entity.Property(brief => brief.MustInclude)
                .HasConversion(
                    values => JsonSerializer.Serialize(values, JsonOptions),
                    json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
            entity.Property(brief => brief.MustAvoid)
                .HasConversion(
                    values => JsonSerializer.Serialize(values, JsonOptions),
                    json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
            entity.Property(brief => brief.PromptDirections)
                .HasConversion(
                    values => JsonSerializer.Serialize(values, JsonOptions),
                    json => JsonSerializer.Deserialize<List<PromptDirection>>(json, JsonOptions) ?? new List<PromptDirection>());
            entity.Property(brief => brief.DesignBlueprints)
                .HasConversion(
                    values => JsonSerializer.Serialize(values, JsonOptions),
                    json => JsonSerializer.Deserialize<List<DesignBlueprint>>(json, JsonOptions) ?? new List<DesignBlueprint>());
            entity.Property(brief => brief.RepairNotesJson);
        });

        modelBuilder.Entity<DocumentBrief>(entity =>
        {
            entity.HasKey(brief => brief.Id);
            entity.Property(brief => brief.ProjectId);
            entity.Property(brief => brief.SourceKind);
            entity.Property(brief => brief.SourceDisplayName).IsRequired();
            entity.Property(brief => brief.Title).IsRequired();
            entity.Property(brief => brief.DocumentFamily);
            entity.Property(brief => brief.Audience).IsRequired();
            entity.Property(brief => brief.StrictnessLevel);
            entity.Property(brief => brief.CreatedAt);
            entity.Property(brief => brief.Sections)
                .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
            entity.Property(brief => brief.KeyClaims)
                .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
            entity.Property(brief => brief.VisualOpportunities)
                .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
            entity.Property(brief => brief.KnownConstraints)
                .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
        });

        modelBuilder.Entity<IllustrationPlan>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.ProjectId);
            entity.Property(plan => plan.DocumentBriefId);
            entity.Property(plan => plan.Summary).IsRequired();
            entity.Property(plan => plan.CreatedAt);
            entity.Property(plan => plan.UpdatedAt);
            entity.Ignore(plan => plan.ApprovedTargets);
            entity.HasOne<DocumentBrief>()
                .WithMany()
                .HasForeignKey(plan => plan.DocumentBriefId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(plan => plan.Targets)
                .HasConversion(targets => SerializeTargets(targets), json => DeserializeTargets(json));
            entity.Property(plan => plan.CoverageNotes)
                .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
            entity.Property(plan => plan.RiskNotes)
                .HasConversion(values => SerializeStringList(values), json => DeserializeStringList(json));
        });

        modelBuilder.Entity<SeriesItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).IsRequired();
            entity.Property(item => item.Kind).HasDefaultValue(SeriesItemKind.Standard);
            entity.HasMany(item => item.PromptVersions)
                .WithOne()
                .HasForeignKey(prompt => prompt.SeriesItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(item => item.GenerationTasks)
                .WithOne()
                .HasForeignKey(task => task.SeriesItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(item => item.CandidateImages)
                .WithOne()
                .HasForeignKey(candidate => candidate.SeriesItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(item => item.PromptVersions).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(item => item.GenerationTasks).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(item => item.CandidateImages).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PromptVersion>(entity =>
        {
            entity.HasKey(prompt => prompt.Id);
            entity.Property(prompt => prompt.PromptText).IsRequired();
            entity.OwnsOne(prompt => prompt.Settings, settings =>
            {
                settings.Property(value => value.Width).HasColumnName("Width");
                settings.Property(value => value.Height).HasColumnName("Height");
                settings.Property(value => value.Quality).HasColumnName("Quality");
                settings.Property(value => value.OutputFormat).HasColumnName("OutputFormat");
                settings.Property(value => value.Seed).HasColumnName("Seed");
            });
        });

        modelBuilder.Entity<GenerationTask>(entity =>
        {
            entity.HasKey(task => task.Id);
        });

        modelBuilder.Entity<CandidateImage>(entity =>
        {
            entity.HasKey(candidate => candidate.Id);
            entity.Property(candidate => candidate.AssetPath).IsRequired();
            entity.Property(candidate => candidate.MetadataPath).IsRequired();
        });

        modelBuilder.Entity<ReviewRubric>(entity =>
        {
            entity.HasKey(rubric => rubric.Id);
            entity.Property(rubric => rubric.Name).IsRequired();
            entity.Property(rubric => rubric.Dimensions)
                .HasConversion(
                    dimensions => JsonSerializer.Serialize(dimensions, JsonOptions),
                    json => JsonSerializer.Deserialize<List<ReviewRubricDimension>>(json, JsonOptions) ?? new List<ReviewRubricDimension>());
        });

        modelBuilder.Entity<ReviewResult>(entity =>
        {
            entity.HasKey(review => review.Id);
            entity.HasOne<CandidateImage>()
                .WithMany()
                .HasForeignKey(review => review.CandidateImageId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(review => review.Scores)
                .HasConversion(
                    scores => JsonSerializer.Serialize(scores, JsonOptions),
                    json => JsonSerializer.Deserialize<Dictionary<string, int>>(json, JsonOptions) ?? new Dictionary<string, int>());
            entity.Property(review => review.HardFailures)
                .HasConversion(
                    failures => JsonSerializer.Serialize(failures, JsonOptions),
                    json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>());
        });

        modelBuilder.Entity<SourceAsset>(entity =>
        {
            entity.HasKey(asset => asset.Id);
            entity.Property(asset => asset.ProjectId);
            entity.Property(asset => asset.Kind);
            entity.Property(asset => asset.DisplayName).IsRequired();
            entity.Property(asset => asset.OriginalPath);
            entity.Property(asset => asset.MimeType);
            entity.Property(asset => asset.SizeBytes);
            entity.Property(asset => asset.Sha256);
            entity.Property(asset => asset.CreatedAt);
            entity.Property(asset => asset.UpdatedAt);
            entity.Property(asset => asset.ExtractedContents)
                .HasConversion(contents => SerializeExtractedContents(contents), json => DeserializeExtractedContents(json));
            entity.Property(asset => asset.EvidenceAnchors)
                .HasConversion(anchors => SerializeEvidenceAnchors(anchors), json => DeserializeEvidenceAnchors(json));
        });

        modelBuilder.Entity<OutputArtifact>(entity =>
        {
            entity.HasKey(artifact => artifact.Id);
            entity.Property(artifact => artifact.ProjectId);
            entity.Property(artifact => artifact.Kind);
            entity.Property(artifact => artifact.Status);
            entity.Property(artifact => artifact.DisplayName).IsRequired();
            entity.Property(artifact => artifact.RelativePath).IsRequired();
            entity.Property(artifact => artifact.MimeType).IsRequired();
            entity.Property(artifact => artifact.Role).IsRequired();
            entity.Property(artifact => artifact.CreatedAt);
            entity.Property(artifact => artifact.UpdatedAt);
            entity.Property(artifact => artifact.SourceAssetIds)
                .HasConversion(values => SerializeGuidList(values), json => DeserializeGuidList(json));
            entity.Property(artifact => artifact.EvidenceAnchorIds)
                .HasConversion(values => SerializeGuidList(values), json => DeserializeGuidList(json));
            entity.Property(artifact => artifact.Metadata)
                .HasConversion(values => SerializeStringDictionary(values), json => DeserializeStringDictionary(json));
        });

        modelBuilder.Entity<ArtifactPackage>(entity =>
        {
            entity.HasKey(package => package.Id);
            entity.Property(package => package.ProjectId);
            entity.Property(package => package.Name).IsRequired();
            entity.Property(package => package.OutputDirectory).IsRequired();
            entity.Property(package => package.CreatedAt);
            entity.Property(package => package.Manifest)
                .HasConversion(manifest => SerializeArtifactManifest(manifest), json => DeserializeArtifactManifest(json));
        });

        modelBuilder.Entity<RoutedRepairPatch>(entity =>
        {
            entity.HasKey(patch => patch.Id);
            entity.Property(patch => patch.ProjectId);
            entity.Property(patch => patch.Items)
                .HasConversion(items => SerializeRoutedRepairPatchItems(items), json => DeserializeRoutedRepairPatchItems(json));
        });

        modelBuilder.Entity<DeliveryPackage>(entity =>
        {
            entity.HasKey(package => package.Id);
            entity.Property(package => package.OutputPath).IsRequired();
            entity.Property(package => package.ManifestJsonPath).IsRequired();
            entity.Property(package => package.ManifestCsvPath).IsRequired();
        });

        modelBuilder.Entity<ProviderProfile>(entity =>
        {
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.DisplayName).IsRequired();
        });
    }

    private static string SerializeStringList(IReadOnlyList<string> values)
    {
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeStringList(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }

    private static string SerializeTargets(IReadOnlyList<IllustrationTarget> targets)
    {
        return JsonSerializer.Serialize(targets, JsonOptions);
    }

    private static IReadOnlyList<IllustrationTarget> DeserializeTargets(string json)
    {
        return JsonSerializer.Deserialize<List<IllustrationTarget>>(json, JsonOptions) ?? [];
    }

    private static string SerializeExtractedContents(IReadOnlyCollection<ExtractedContent> contents)
    {
        return JsonSerializer.Serialize(contents, JsonOptions);
    }

    private static IReadOnlyCollection<ExtractedContent> DeserializeExtractedContents(string json)
    {
        return JsonSerializer.Deserialize<List<ExtractedContent>>(json, JsonOptions) ?? [];
    }

    private static string SerializeEvidenceAnchors(IReadOnlyCollection<EvidenceAnchor> anchors)
    {
        return JsonSerializer.Serialize(anchors, JsonOptions);
    }

    private static IReadOnlyCollection<EvidenceAnchor> DeserializeEvidenceAnchors(string json)
    {
        return JsonSerializer.Deserialize<List<EvidenceAnchor>>(json, JsonOptions) ?? [];
    }

    private static string SerializeGuidList(IReadOnlyList<Guid> values)
    {
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static IReadOnlyList<Guid> DeserializeGuidList(string json)
    {
        return JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions) ?? [];
    }

    private static string SerializeStringDictionary(IReadOnlyDictionary<string, string> values)
    {
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static IReadOnlyDictionary<string, string> DeserializeStringDictionary(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>();
    }

    private static string SerializeArtifactManifest(ArtifactManifest manifest)
    {
        return JsonSerializer.Serialize(manifest, JsonOptions);
    }

    private static ArtifactManifest DeserializeArtifactManifest(string json)
    {
        return JsonSerializer.Deserialize<ArtifactManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException("Artifact manifest JSON could not be deserialized.");
    }

    private static string SerializeRoutedRepairPatchItems(IReadOnlyList<RoutedRepairPatchItem> items)
    {
        return JsonSerializer.Serialize(items, JsonOptions);
    }

    private static IReadOnlyList<RoutedRepairPatchItem> DeserializeRoutedRepairPatchItems(string json)
    {
        return JsonSerializer.Deserialize<List<RoutedRepairPatchItem>>(json, JsonOptions) ?? [];
    }

}

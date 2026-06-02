using System.Text.Json;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
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

    public DbSet<SeriesItem> SeriesItems => Set<SeriesItem>();

    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();

    public DbSet<GenerationTask> GenerationTasks => Set<GenerationTask>();

    public DbSet<CandidateImage> CandidateImages => Set<CandidateImage>();

    public DbSet<ReviewRubric> ReviewRubrics => Set<ReviewRubric>();

    public DbSet<ReviewResult> ReviewResults => Set<ReviewResult>();

    public DbSet<DeliveryPackage> DeliveryPackages => Set<DeliveryPackage>();

    public DbSet<ProviderProfile> ProviderProfiles => Set<ProviderProfile>();

    public DbSet<DocumentBrief> DocumentBriefs => Set<DocumentBrief>();

    public DbSet<IllustrationPlan> IllustrationPlans => Set<IllustrationPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.HasMany(project => project.DocumentBriefs)
                .WithOne()
                .HasForeignKey(brief => brief.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.IllustrationPlans)
                .WithOne()
                .HasForeignKey(plan => plan.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(project => project.Series).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.ProviderProfiles).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.DocumentBriefs).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.DocumentBriefs).AutoInclude();
            entity.Navigation(project => project.IllustrationPlans).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.IllustrationPlans).AutoInclude();
        });

        modelBuilder.Entity<ImageSeries>(entity =>
        {
            entity.HasKey(series => series.Id);
            entity.Property(series => series.Title).IsRequired();
            entity.HasMany(series => series.Items)
                .WithOne()
                .HasForeignKey(item => item.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(series => series.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SeriesItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).IsRequired();
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

        modelBuilder.Entity<DocumentBrief>(entity =>
        {
            entity.HasKey(brief => brief.Id);
            entity.Property(brief => brief.SourceDisplayName).IsRequired();
            entity.Property(brief => brief.Title).IsRequired();
            entity.Property(brief => brief.Audience).IsRequired();
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
            entity.Property(plan => plan.Summary).IsRequired();
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
}

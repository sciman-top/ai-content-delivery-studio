using ContentDeliveryStudio.Core.Artifacts;
using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ContentDeliveryStudio.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
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

        modelBuilder.ApplyConfiguration(new ImageProjectConfiguration());

        modelBuilder.ApplyConfiguration(new ImageSeriesConfiguration());

        modelBuilder.ApplyConfiguration(new CreativeBriefConfiguration());

        modelBuilder.ApplyConfiguration(new DocumentBriefConfiguration());

        modelBuilder.ApplyConfiguration(new IllustrationPlanConfiguration());

        modelBuilder.ApplyConfiguration(new SeriesItemConfiguration());

        modelBuilder.ApplyConfiguration(new PromptVersionConfiguration());

        modelBuilder.ApplyConfiguration(new GenerationTaskConfiguration());

        modelBuilder.ApplyConfiguration(new CandidateImageConfiguration());

        modelBuilder.ApplyConfiguration(new ReviewRubricConfiguration());

        modelBuilder.ApplyConfiguration(new ReviewResultConfiguration());

        modelBuilder.ApplyConfiguration(new SourceAssetConfiguration());

        modelBuilder.ApplyConfiguration(new OutputArtifactConfiguration());

        modelBuilder.ApplyConfiguration(new ArtifactPackageConfiguration());

        modelBuilder.ApplyConfiguration(new RoutedRepairPatchConfiguration());

        modelBuilder.ApplyConfiguration(new DeliveryPackageConfiguration());

        modelBuilder.ApplyConfiguration(new ProviderProfileConfiguration());
    }

}

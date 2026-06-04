using ImageSeriesStudio.Core.Artifacts;
using ImageSeriesStudio.Core.Documents;
using ImageSeriesStudio.Core.Projects;
using ImageSeriesStudio.Core.Sources;
using ImageSeriesStudio.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ImageSeriesStudio.Infrastructure.Persistence;

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

        modelBuilder.ApplyConfiguration(new CreativeBriefConfiguration());

        modelBuilder.ApplyConfiguration(new DocumentBriefConfiguration());

        modelBuilder.ApplyConfiguration(new IllustrationPlanConfiguration());

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

        modelBuilder.ApplyConfiguration(new ReviewRubricConfiguration());

        modelBuilder.ApplyConfiguration(new ReviewResultConfiguration());

        modelBuilder.ApplyConfiguration(new SourceAssetConfiguration());

        modelBuilder.ApplyConfiguration(new OutputArtifactConfiguration());

        modelBuilder.ApplyConfiguration(new ArtifactPackageConfiguration());

        modelBuilder.ApplyConfiguration(new RoutedRepairPatchConfiguration());

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

}

using System.Text.Json;
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
            entity.Navigation(project => project.Series).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(project => project.ProviderProfiles).UsePropertyAccessMode(PropertyAccessMode.Field);
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
    }
}

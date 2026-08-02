using ContentDeliveryStudio.Application.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class LocalStudioDataPathsTests
{
    [Fact]
    public void ResolveStudioRootPath_PrefersNewRootWhenPresent()
    {
        var baseRoot = CreateTempRoot();

        try
        {
            var newRoot = Path.Combine(baseRoot, "ContentDeliveryStudio");
            var legacyRoot = Path.Combine(baseRoot, "ImageSeriesStudio");
            Directory.CreateDirectory(newRoot);
            Directory.CreateDirectory(legacyRoot);

            var resolved = LocalStudioDataPaths.ResolveStudioRootPath(baseRoot);

            Assert.Equal(Path.GetFullPath(newRoot), resolved);
        }
        finally
        {
            Cleanup(baseRoot);
        }
    }

    [Fact]
    public void ResolveStudioRootPath_FallsBackToLegacyRootWhenOnlyLegacyExists()
    {
        var baseRoot = CreateTempRoot();

        try
        {
            var legacyRoot = Path.Combine(baseRoot, "ImageSeriesStudio");
            Directory.CreateDirectory(legacyRoot);

            var resolved = LocalStudioDataPaths.ResolveStudioRootPath(baseRoot);

            Assert.Equal(Path.GetFullPath(legacyRoot), resolved);
        }
        finally
        {
            Cleanup(baseRoot);
        }
    }

    [Fact]
    public void ResolveStudioRootPath_UsesNewRootWhenNeitherExists()
    {
        var baseRoot = CreateTempRoot();

        try
        {
            var resolved = LocalStudioDataPaths.ResolveStudioRootPath(baseRoot);

            Assert.Equal(
                Path.GetFullPath(Path.Combine(baseRoot, "ContentDeliveryStudio")),
                resolved);
        }
        finally
        {
            Cleanup(baseRoot);
        }
    }

    [Fact]
    public void ResolveStudioRoot_UsesNormalizedEnvironmentOverride()
    {
        var baseRoot = CreateTempRoot();

        try
        {
            var relativeRoot = Path.GetRelativePath(Environment.CurrentDirectory, baseRoot);

            var resolved = LocalStudioDataPaths.ResolveStudioRoot(relativeRoot);

            Assert.Equal(Path.GetFullPath(baseRoot), resolved);
        }
        finally
        {
            Cleanup(baseRoot);
        }
    }

    [Fact]
    public void ResolveStudioRoot_ExplicitScopeOverridesEnvironmentOverride()
    {
        var environmentRoot = CreateTempRoot();
        var scopedRoot = CreateTempRoot();

        try
        {
            using var scope = LocalStudioDataPaths.PushRootOverride(scopedRoot);

            var resolved = LocalStudioDataPaths.ResolveStudioRoot(environmentRoot);

            Assert.Equal(Path.GetFullPath(scopedRoot), resolved);
        }
        finally
        {
            Cleanup(environmentRoot);
            Cleanup(scopedRoot);
        }
    }

    [Fact]
    public void ResolveProjectDirectory_RejectsUnsafeAreaName()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            LocalStudioDataPaths.ResolveProjectDirectory("..\\deliveries", Guid.NewGuid()));

        Assert.Contains("safe", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveWorkspaceAndFinalDeliveryDirectories_KeepFinalAndTemporaryOutputsSeparate()
    {
        var root = CreateTempRoot();
        var projectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        try
        {
            using var scope = LocalStudioDataPaths.PushRootOverride(root);

            var workspace = LocalStudioDataPaths.ResolveWorkspaceProjectDirectory("generated", projectId);
            var delivery = LocalStudioDataPaths.ResolveFinalDeliveryDirectory(
                projectId,
                DateTimeOffset.Parse("2026-08-01T08:30:00Z"));

            Assert.Equal(Path.Combine(root, "workspace", projectId.ToString("N"), "generated"), workspace);
            Assert.Equal(Path.Combine(root, "deliveries", projectId.ToString("N"), "20260801-083000"), delivery);
            Assert.DoesNotContain("workspace", delivery, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void ResolveDeliveryRoot_UsesExplicitExternalDeliveryRootWithoutMovingWorkspace()
    {
        var root = CreateTempRoot();
        var externalDeliveryRoot = Path.Combine(root, "classroom-answer-toolkit", "正式交付", "科学配图", "文章图组");

        try
        {
            Assert.Equal(
                Path.GetFullPath(externalDeliveryRoot),
                LocalStudioDataPaths.ResolveDeliveryRoot(externalDeliveryRoot));

            using var scope = LocalStudioDataPaths.PushRootOverride(root);

            var workspace = LocalStudioDataPaths.ResolveWorkspaceProjectDirectory(
                "generated",
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            Assert.Equal(
                Path.Combine(root, "deliveries"),
                LocalStudioDataPaths.ResolveDeliveryRoot(externalDeliveryRoot));
            Assert.StartsWith(
                Path.Combine(root, "workspace"),
                workspace,
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void ResolveFinalDeliveryCategoryPaths_UseStableCategoryAndImagesLeaf()
    {
        var root = CreateTempRoot();
        var projectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var timestamp = DateTimeOffset.Parse("2026-08-01T09:45:00+08:00");

        try
        {
            using var scope = LocalStudioDataPaths.PushRootOverride(root);

            var categoryRoot = LocalStudioDataPaths.ResolveFinalDeliveryCategoryRoot(
                FinalImageDeliveryCategory.ArticleFigureSets);
            var package = LocalStudioDataPaths.ResolveFinalDeliveryPackageDirectory(
                FinalImageDeliveryCategory.ArticleFigureSets,
                projectId,
                timestamp);
            var images = LocalStudioDataPaths.ResolveFinalImageDirectory(
                FinalImageDeliveryCategory.ArticleFigureSets,
                projectId,
                timestamp);

            Assert.Equal(
                Path.Combine(root, "deliveries", "article-figure-sets"),
                categoryRoot);
            Assert.Equal(
                Path.Combine(
                    root,
                    "deliveries",
                    "article-figure-sets",
                    projectId.ToString("N"),
                    "20260801-014500"),
                package);
            Assert.Equal(Path.Combine(package, "images"), images);
            Assert.DoesNotContain("workspace", images, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Theory]
    [InlineData(FinalImageDeliveryCategory.ImageSeries, "image-series")]
    [InlineData(FinalImageDeliveryCategory.ImageEdits, "image-edits")]
    [InlineData(FinalImageDeliveryCategory.ArticleFigureSets, "article-figure-sets")]
    [InlineData(FinalImageDeliveryCategory.ScientificFigures, "scientific-figures")]
    [InlineData(FinalImageDeliveryCategory.DocumentIllustrations, "document-illustrations")]
    [InlineData(FinalImageDeliveryCategory.CoursewareVisuals, "courseware-visuals")]
    [InlineData(FinalImageDeliveryCategory.PosterReportVisuals, "poster-report-visuals")]
    public void ResolveFinalDeliveryCategoryPaths_CoversEveryStableScenario(
        FinalImageDeliveryCategory category,
        string expectedSlug)
    {
        var root = CreateTempRoot();
        var projectId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var timestamp = DateTimeOffset.Parse("2026-08-01T10:00:00Z");

        try
        {
            using var scope = LocalStudioDataPaths.PushRootOverride(root);

            var package = LocalStudioDataPaths.ResolveFinalDeliveryPackageDirectory(
                category,
                projectId,
                timestamp);

            Assert.Equal(
                Path.Combine(root, "deliveries", expectedSlug, projectId.ToString("N"), "20260801-100000"),
                package);
            Assert.Equal(
                Path.Combine(package, "images"),
                LocalStudioDataPaths.ResolveFinalImageDirectory(category, projectId, timestamp));
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void ResolveFinalDeliveryCategoryPaths_ExplicitCustomRootWinsOverStudioRoot()
    {
        var root = CreateTempRoot();
        var customRoot = Path.Combine(root, "custom-final-root");
        var projectId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        try
        {
            using var scope = LocalStudioDataPaths.PushRootOverride(root);

            var images = LocalStudioDataPaths.ResolveFinalImageDirectory(
                FinalImageDeliveryCategory.PosterReportVisuals,
                projectId,
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
                customRoot);

            Assert.StartsWith(
                Path.Combine(Path.GetFullPath(customRoot), "poster-report-visuals"),
                images,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                Path.Combine(Path.GetFullPath(root), "deliveries"),
                images,
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void GetFinalDeliveryCategorySlug_RejectsUnknownCategory()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            LocalStudioDataPaths.GetFinalDeliveryCategorySlug((FinalImageDeliveryCategory)999));

        Assert.Equal("category", exception.ParamName);
    }

    [Fact]
    public void ResolveFinalDeliveryCategoryRoot_RejectsWorkspaceRootOverride()
    {
        var root = CreateTempRoot();

        try
        {
            using var scope = LocalStudioDataPaths.PushRootOverride(root);
            var workspaceRoot = Path.Combine(root, "workspace", "final-by-mistake");

            var exception = Assert.Throws<ArgumentException>(() =>
                LocalStudioDataPaths.ResolveFinalDeliveryCategoryRoot(
                    FinalImageDeliveryCategory.ImageSeries,
                    workspaceRoot));

            Assert.Contains("temporary workspace", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "ContentDeliveryStudio.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

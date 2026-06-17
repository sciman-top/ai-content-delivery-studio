using ContentDeliveryStudio.Core.Artifacts;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class OutputArtifactModelTests
{
    [Fact]
    public void OutputArtifactCreate_NormalizesProvenanceAndMetadata()
    {
        var projectId = Guid.NewGuid();
        var sourceAssetId = Guid.NewGuid();
        var evidenceAnchorId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-06-03T10:00:00Z");

        var artifact = OutputArtifact.Create(
            projectId,
            OutputArtifactKind.Pdf,
            " Teacher packet ",
            " delivery/teacher-packet.pdf ",
            " application/pdf ",
            " final-report ",
            [sourceAssetId, sourceAssetId],
            [evidenceAnchorId, evidenceAnchorId],
            new Dictionary<string, string>
            {
                [" language "] = " zh-CN ",
                [""] = "ignored",
            },
            createdAt);

        Assert.NotEqual(Guid.Empty, artifact.Id);
        Assert.Equal(projectId, artifact.ProjectId);
        Assert.Equal(OutputArtifactKind.Pdf, artifact.Kind);
        Assert.Equal(OutputArtifactStatus.Generated, artifact.Status);
        Assert.Equal("Teacher packet", artifact.DisplayName);
        Assert.Equal("delivery/teacher-packet.pdf", artifact.RelativePath);
        Assert.Equal("application/pdf", artifact.MimeType);
        Assert.Equal("final-report", artifact.Role);
        Assert.Equal([sourceAssetId], artifact.SourceAssetIds);
        Assert.Equal([evidenceAnchorId], artifact.EvidenceAnchorIds);
        Assert.Equal("zh-CN", artifact.Metadata["language"]);
        Assert.Equal(createdAt, artifact.CreatedAt);
        Assert.Equal(createdAt, artifact.UpdatedAt);
    }

    [Fact]
    public void ArtifactPackageCreate_CapturesManifestItemsFromArtifacts()
    {
        var projectId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-06-03T10:00:00Z");
        var pdf = OutputArtifact.Create(
            projectId,
            OutputArtifactKind.Pdf,
            "Teacher packet",
            "delivery/teacher-packet.pdf",
            "application/pdf",
            "final-report",
            [],
            [],
            new Dictionary<string, string>(),
            createdAt);
        var reviewReport = OutputArtifact.Create(
            projectId,
            OutputArtifactKind.ReviewReport,
            "Review report",
            "delivery/review-report.md",
            "text/markdown",
            "review-evidence",
            [],
            [],
            new Dictionary<string, string>(),
            createdAt.AddMinutes(1));

        var package = ArtifactPackage.Create(
            projectId,
            " Teacher delivery ",
            " delivery ",
            version: 1,
            [pdf, reviewReport],
            createdAt.AddMinutes(2));

        Assert.NotEqual(Guid.Empty, package.Id);
        Assert.Equal(projectId, package.ProjectId);
        Assert.Equal("Teacher delivery", package.Name);
        Assert.Equal("delivery", package.OutputDirectory);
        Assert.Equal(1, package.Manifest.Version);
        Assert.Equal(2, package.Manifest.Items.Count);
        Assert.Contains(package.Manifest.Items, item =>
            item.OutputArtifactId == pdf.Id
            && item.Kind == OutputArtifactKind.Pdf
            && item.RelativePath == "delivery/teacher-packet.pdf");
        Assert.Contains(package.Manifest.Items, item =>
            item.OutputArtifactId == reviewReport.Id
            && item.Kind == OutputArtifactKind.ReviewReport
            && item.RelativePath == "delivery/review-report.md");
    }

    [Fact]
    public void ImageProject_AddArtifactPackageRequiresKnownArtifacts()
    {
        var timestamp = DateTimeOffset.Parse("2026-06-03T10:00:00Z");
        var project = ImageProject.Create("Artifact project", timestamp);
        var knownArtifact = OutputArtifact.Create(
            project.Id,
            OutputArtifactKind.Markdown,
            "Summary",
            "delivery/summary.md",
            "text/markdown",
            "summary",
            [],
            [],
            new Dictionary<string, string>(),
            timestamp.AddMinutes(1));
        var unknownArtifact = OutputArtifact.Create(
            project.Id,
            OutputArtifactKind.Docx,
            "Handout",
            "delivery/handout.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "handout",
            [],
            [],
            new Dictionary<string, string>(),
            timestamp.AddMinutes(2));

        project.AddOutputArtifact(knownArtifact, timestamp.AddMinutes(3));
        var validPackage = ArtifactPackage.Create(
            project.Id,
            "Known package",
            "delivery",
            version: 1,
            [knownArtifact],
            timestamp.AddMinutes(4));
        var invalidPackage = ArtifactPackage.Create(
            project.Id,
            "Unknown package",
            "delivery",
            version: 1,
            [unknownArtifact],
            timestamp.AddMinutes(5));

        project.AddArtifactPackage(validPackage, timestamp.AddMinutes(6));
        var exception = Assert.Throws<InvalidOperationException>(() =>
            project.AddArtifactPackage(invalidPackage, timestamp.AddMinutes(7)));

        Assert.Same(knownArtifact, Assert.Single(project.OutputArtifacts));
        Assert.Same(validPackage, Assert.Single(project.ArtifactPackages));
        Assert.Contains("Output artifact not found", exception.Message);
    }
}

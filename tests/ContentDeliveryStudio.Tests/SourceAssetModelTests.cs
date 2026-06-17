using ContentDeliveryStudio.Core.Sources;

namespace ContentDeliveryStudio.Tests;

public sealed class SourceAssetModelTests
{
    [Fact]
    public void SourceAsset_AddsExtractedContentAndEvidenceAnchor()
    {
        var projectId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-06-03T09:00:00Z");
        var asset = SourceAsset.Create(
            projectId,
            SourceAssetKind.Pdf,
            " lesson.pdf ",
            @"C:\Users\teacher\lesson.pdf",
            "application/pdf",
            2048,
            "abc123",
            createdAt);

        var extracted = asset.AddExtractedContent(
            ExtractedContentKind.PlainText,
            " Energy is quantized. Diagrams should preserve the claim. ",
            " page 2 ",
            pageNumber: 2,
            startOffset: 10,
            endOffset: 64,
            createdAt.AddMinutes(1));
        var anchor = asset.AddEvidenceAnchor(
            extracted.Id,
            " Key claim ",
            " Energy is quantized. ",
            " page 2, paragraph 1 ",
            createdAt.AddMinutes(2));

        Assert.NotEqual(Guid.Empty, asset.Id);
        Assert.Equal(projectId, asset.ProjectId);
        Assert.Equal(SourceAssetKind.Pdf, asset.Kind);
        Assert.Equal("lesson.pdf", asset.DisplayName);
        Assert.Equal(@"C:\Users\teacher\lesson.pdf", asset.OriginalPath);
        Assert.Equal("application/pdf", asset.MimeType);
        Assert.Equal(2048, asset.SizeBytes);
        Assert.Equal("abc123", asset.Sha256);
        Assert.Equal(createdAt, asset.CreatedAt);
        Assert.Equal(createdAt.AddMinutes(2), asset.UpdatedAt);

        Assert.Same(extracted, Assert.Single(asset.ExtractedContents));
        Assert.Equal(asset.Id, extracted.SourceAssetId);
        Assert.Equal(ExtractedContentKind.PlainText, extracted.Kind);
        Assert.Equal("Energy is quantized. Diagrams should preserve the claim.", extracted.Text);
        Assert.Equal("page 2", extracted.LocationHint);
        Assert.Equal(2, extracted.PageNumber);
        Assert.Equal(10, extracted.StartOffset);
        Assert.Equal(64, extracted.EndOffset);

        Assert.Same(anchor, Assert.Single(asset.EvidenceAnchors));
        Assert.Equal(asset.Id, anchor.SourceAssetId);
        Assert.Equal(extracted.Id, anchor.ExtractedContentId);
        Assert.Equal("Key claim", anchor.Label);
        Assert.Equal("Energy is quantized.", anchor.Quote);
        Assert.Equal("page 2, paragraph 1", anchor.LocationHint);
    }

    [Fact]
    public void SourceAsset_RejectsEvidenceAnchorForUnknownExtractedContent()
    {
        var asset = SourceAsset.Create(
            Guid.NewGuid(),
            SourceAssetKind.Text,
            "notes.txt",
            originalPath: null,
            mimeType: "text/plain",
            sizeBytes: null,
            sha256: null,
            DateTimeOffset.Parse("2026-06-03T09:00:00Z"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            asset.AddEvidenceAnchor(
                Guid.NewGuid(),
                "missing",
                "Missing quote",
                "line 1",
                DateTimeOffset.Parse("2026-06-03T09:01:00Z")));

        Assert.Contains("Extracted content not found", exception.Message);
    }
}

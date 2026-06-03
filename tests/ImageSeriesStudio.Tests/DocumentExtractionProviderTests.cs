using ImageSeriesStudio.Application.Sources;
using ImageSeriesStudio.Core.Sources;
using ImageSeriesStudio.Infrastructure.Sources;

namespace ImageSeriesStudio.Tests;

public sealed class DocumentExtractionProviderTests
{
    [Theory]
    [InlineData(SourceAssetKind.Pdf, ExtractedContentKind.PlainText)]
    [InlineData(SourceAssetKind.Docx, ExtractedContentKind.PlainText)]
    [InlineData(SourceAssetKind.Presentation, ExtractedContentKind.PlainText)]
    [InlineData(SourceAssetKind.Markdown, ExtractedContentKind.Markdown)]
    [InlineData(SourceAssetKind.Image, ExtractedContentKind.OcrText)]
    public async Task FakeDocumentExtractionProvider_ExtractsFixtureTextForSupportedKinds(
        SourceAssetKind sourceKind,
        ExtractedContentKind expectedContentKind)
    {
        var provider = new FakeDocumentExtractionProvider();

        var result = await provider.ExtractAsync(
            new DocumentExtractionRequest(
                sourceKind,
                "source-file",
                "Key claim: keep generated artifacts traceable to source evidence.",
                OriginalPath: @"C:\uploads\source-file",
                UseOcr: sourceKind == SourceAssetKind.Image),
            CancellationToken.None);

        var content = Assert.Single(result.ExtractedContents);
        var anchor = Assert.Single(result.EvidenceAnchors);

        Assert.Equal("fake-document-extraction", result.ProviderTraceId);
        Assert.Equal(expectedContentKind, content.Kind);
        Assert.Contains("traceable", content.Text);
        Assert.Equal(0, content.StartOffset);
        Assert.Equal(content.Text.Length, content.EndOffset);
        Assert.Equal(0, anchor.ExtractedContentIndex);
        Assert.Contains("traceable", anchor.Quote);
        Assert.Contains(sourceKind, provider.Capabilities.SupportedSourceKinds);
    }

    [Fact]
    public void FakeDocumentExtractionProvider_DeclaresProviderNeutralCapabilities()
    {
        var provider = new FakeDocumentExtractionProvider();

        Assert.Equal("fake-document-extraction", provider.Capabilities.ProviderId);
        Assert.False(provider.Capabilities.RequiresExplicitApproval);
        Assert.True(provider.Capabilities.SupportsOcr);
        Assert.Contains(SourceAssetKind.Pdf, provider.Capabilities.SupportedSourceKinds);
        Assert.Contains(SourceAssetKind.Docx, provider.Capabilities.SupportedSourceKinds);
        Assert.Contains(SourceAssetKind.Presentation, provider.Capabilities.SupportedSourceKinds);
        Assert.Contains(SourceAssetKind.Markdown, provider.Capabilities.SupportedSourceKinds);
        Assert.Contains(SourceAssetKind.Image, provider.Capabilities.SupportedSourceKinds);
    }

    [Fact]
    public async Task FakeDocumentExtractionProvider_RejectsUnsupportedSourceKind()
    {
        var provider = new FakeDocumentExtractionProvider();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.ExtractAsync(
                new DocumentExtractionRequest(
                    SourceAssetKind.WebPage,
                    "page.html",
                    "Web extraction is not part of the fake document extractor."),
                CancellationToken.None));

        Assert.Contains("not supported", exception.Message);
    }
}

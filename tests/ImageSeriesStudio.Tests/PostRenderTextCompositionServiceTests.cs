using ImageSeriesStudio.Application.Composition;

namespace ImageSeriesStudio.Tests;

public sealed class PostRenderTextCompositionServiceTests
{
    [Fact]
    public async Task ComposeEducationalVisualAsync_CreatesStableOutputPathsAndDelegatesToComposer()
    {
        var composer = new RecordingComposer();
        var service = new PostRenderTextCompositionService(composer);
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ImageSeriesStudio.Tests", Guid.NewGuid().ToString("N"));

        var result = await service.ComposeEducationalVisualAsync(
            new PostRenderTextCompositionRequest(
                BackgroundPath: "workspace/candidates/background.png",
                OutputDirectory: outputDirectory,
                ArtifactName: "Physics Poster 01",
                Overlays:
                [
                    new DeterministicTextOverlay(
                        "F = ma",
                        X: 24,
                        Y: 32,
                        FontSize: 28,
                        HexColor: "#111111",
                        DeterministicTextOverlayKind.Formula),
                ]),
            CancellationToken.None);

        Assert.Equal(Path.Combine(outputDirectory, "physics-poster-01.composed.png"), result.ComposedImagePath);
        Assert.Equal(Path.Combine(outputDirectory, "physics-poster-01.layout-report.json"), result.LayoutReportPath);
        Assert.Equal(1, result.OverlayCount);

        var request = Assert.Single(composer.Requests);
        Assert.Equal("workspace/candidates/background.png", request.BackgroundPath);
        Assert.Equal(result.ComposedImagePath, request.ComposedImagePath);
        Assert.Equal(result.LayoutReportPath, request.LayoutReportPath);
        Assert.Equal(DeterministicTextOverlayKind.Formula, Assert.Single(request.Overlays).Kind);
    }

    private sealed class RecordingComposer : IDeterministicTextComposer
    {
        public List<DeterministicTextCompositionRequest> Requests { get; } = [];

        public Task<DeterministicTextCompositionResult> ComposeAsync(
            DeterministicTextCompositionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);

            return Task.FromResult(new DeterministicTextCompositionResult(
                request.ComposedImagePath,
                request.LayoutReportPath,
                request.Overlays.Count));
        }
    }
}

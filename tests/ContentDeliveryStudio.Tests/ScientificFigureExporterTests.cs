using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;
using SkiaSharp;
using System.Security.Cryptography;
using System.Text;
using UglyToad.PdfPig;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificFigureExporterTests
{
    [Fact]
    public void Export_RejectsSvgWhoseHashIsNotApproved()
    {
        var svg = new DeterministicSvgRenderer().Render(
            DeterministicSvgRendererTests.Plan());
        var request = new ScientificFigureExportRequest(
            svg,
            $"sha256:{new string('0', 64)}",
            Width: 600,
            Height: 400);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ScientificFigureExporter().Export(request));

        Assert.Contains("approved SVG hash", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Export_ProducesPngAndPdfWithTraceableMetadata()
    {
        var svg = new DeterministicSvgRenderer().Render(
            DeterministicSvgRendererTests.Plan());

        var bundle = new ScientificFigureExporter().Export(
            new ScientificFigureExportRequest(svg, svg.Sha256, Width: 600, Height: 400));

        Assert.Equal(svg.Sha256, bundle.SourceSvgSha256);
        Assert.Equal("content-delivery-studio.skia-scientific-exporter", bundle.ExporterId);
        Assert.Equal("1.0", bundle.ExporterVersion);
        Assert.Equal(600, bundle.Width);
        Assert.Equal(400, bundle.Height);

        var png = Assert.Single(bundle.Artifacts, artifact => artifact.Format == "png");
        using var bitmap = SKBitmap.Decode(png.Bytes);
        Assert.NotNull(bitmap);
        Assert.Equal(600, bitmap.Width);
        Assert.Equal(400, bitmap.Height);
        Assert.StartsWith("sha256:", png.Sha256, StringComparison.Ordinal);

        var pdf = Assert.Single(bundle.Artifacts, artifact => artifact.Format == "pdf");
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Bytes, 0, 4));
        Assert.StartsWith("sha256:", pdf.Sha256, StringComparison.Ordinal);

        Assert.All(
            bundle.Artifacts,
            artifact =>
            {
                Assert.Equal(svg.Sha256, artifact.SourceSvgSha256);
                Assert.Equal(bundle.SemanticSha256, artifact.SemanticSha256);
            });
    }

    [Fact]
    public void Export_ConcurrentPdfRendersRemainReadable()
    {
        var svg = new DeterministicSvgRenderer().Render(
            DeterministicSvgRendererTests.Plan());
        var failures = new System.Collections.Concurrent.ConcurrentQueue<Exception>();

        Parallel.For(
            0,
            32,
            _ =>
            {
                try
                {
                    var bundle = new ScientificFigureExporter().Export(
                        new ScientificFigureExportRequest(
                            svg,
                            svg.Sha256,
                            Width: 1200,
                            Height: 800));
                    var pdf = Assert.Single(
                        bundle.Artifacts,
                        artifact => artifact.Format == "pdf");
                    using var document = PdfDocument.Open(pdf.Bytes);
                    var text = string.Join(" ", document.GetPages().Select(page => page.Text));
                    Assert.Contains("Net force", text, StringComparison.Ordinal);
                    Assert.Contains("bounded", text, StringComparison.Ordinal);
                }
                catch (Exception exception)
                {
                    failures.Enqueue(exception);
                }
            });

        Assert.Empty(failures);
    }

    [Fact]
    public void Export_RejectsApprovedSvgWithUnboundVisibleText()
    {
        var original = new DeterministicSvgRenderer().Render(
            DeterministicSvgRendererTests.Plan());
        var mutatedSvg = original.Svg.Replace(
            "</svg>",
            "<text xmlns=\"http://www.w3.org/2000/svg\" x=\"10\" y=\"10\">unapproved</text></svg>",
            StringComparison.Ordinal);
        var hash = $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(mutatedSvg))).ToLowerInvariant()}";
        var mutated = original with { Svg = mutatedSvg, Sha256 = hash };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ScientificFigureExporter().Export(
                new ScientificFigureExportRequest(mutated, hash, Width: 600, Height: 400)));

        Assert.Contains("unbound visible text", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ContentDeliveryStudio.Application.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed class ScientificFigurePackageWriter : IScientificFigurePackageWriter
{
    private static readonly DateTimeOffset StableTimestamp =
        new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public byte[] Write(ScientificFigureDeliveryPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(
                   output,
                   ZipArchiveMode.Create,
                   leaveOpen: true,
                   Encoding.UTF8))
        {
            WriteText(archive, "figure.svg", package.Svg.Svg);
            foreach (var artifact in package.Exports.Artifacts
                         .OrderBy(item => item.Format, StringComparer.Ordinal))
            {
                WriteBytes(archive, $"figure.{artifact.Format}", artifact.Bytes);
            }

            WriteJson(archive, "specification.json", package.Specification);
            WriteJson(
                archive,
                "claim-evidence-item-map.json",
                package.ClaimEvidenceItemMap);
            WriteJson(
                archive,
                "reviews.json",
                new ScientificDeliveryReviewEnvelope(
                    package.ContractReview,
                    package.MachineReview));
            WriteJson(archive, "repairs.json", package.Repairs);
            WriteJson(archive, "providers.json", package.Providers);
            if (package.ChartProvenance is not null)
            {
                WriteJson(archive, "chart-provenance.json", package.ChartProvenance);
            }
            WriteJson(
                archive,
                "approvals.json",
                new ScientificDeliveryApprovalEnvelope(
                    package.GateOneApproval,
                    package.GateTwoApproval));
            WriteJson(
                archive,
                "manifest.json",
                new ScientificDeliveryManifest(
                    package.SpecificationId,
                    package.SpecificationVersion,
                    package.Svg.Sha256,
                    package.Exports.SemanticSha256,
                    package.Exports.Artifacts.ToDictionary(
                        item => item.Format,
                        item => item.Sha256,
                        StringComparer.OrdinalIgnoreCase),
                    package.ChartProvenance?.DataSha256,
                    package.ChartProvenance?.SpecificationSha256,
                    package.ChartProvenance?.RendererVersion));
        }

        return output.ToArray();
    }

    private static void WriteJson<T>(ZipArchive archive, string name, T value)
    {
        WriteBytes(archive, name, JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions));
    }

    private static void WriteText(ZipArchive archive, string name, string value)
    {
        WriteBytes(archive, name, Encoding.UTF8.GetBytes(value));
    }

    private static void WriteBytes(ZipArchive archive, string name, byte[] bytes)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = StableTimestamp;
        using var stream = entry.Open();
        stream.Write(bytes);
    }

    private sealed record ScientificDeliveryReviewEnvelope(
        object Contract,
        object Machine);

    private sealed record ScientificDeliveryApprovalEnvelope(
        object GateOne,
        object GateTwo);

    private sealed record ScientificDeliveryManifest(
        Guid SpecificationId,
        int SpecificationVersion,
        string SvgSha256,
        string SemanticSha256,
        IReadOnlyDictionary<string, string> ArtifactSha256,
        string? ChartDataSha256,
        string? ChartSpecificationSha256,
        string? ChartRendererVersion);
}

using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public interface IScientificFigureRenderer
{
    ScientificSvgArtifact Render(SvgRenderPlan plan);
}

public sealed record ScientificSvgArtifact(
    string PlanId,
    Guid SpecificationId,
    int SpecificationVersion,
    string Svg,
    string Sha256);

public interface IScientificFigureExporter
{
    ScientificFigureExportBundle Export(ScientificFigureExportRequest request);
}

public sealed record ScientificFigureExportRequest(
    ScientificSvgArtifact SourceSvg,
    string ApprovedSvgSha256,
    int Width,
    int Height);

public sealed record ScientificFigureExportBundle(
    string SourceSvgSha256,
    string ExporterId,
    string ExporterVersion,
    int Width,
    int Height,
    ScientificExportSemantics Semantics,
    string SemanticSha256,
    IReadOnlyList<ScientificFigureExportArtifact> Artifacts);

public sealed record ScientificFigureExportArtifact(
    string Format,
    string MimeType,
    byte[] Bytes,
    string Sha256,
    string SourceSvgSha256,
    string SemanticSha256);

public sealed record ScientificExportSemantics(
    string AccessibilityTitle,
    string AccessibilityDescription,
    IReadOnlyList<ScientificExportElementFixture> ElementFixtures,
    IReadOnlyList<ScientificExportTextFixture> TextFixtures,
    IReadOnlyList<ScientificExportRelationFixture> RelationFixtures);

public sealed record ScientificExportElementFixture(
    string ElementId,
    string SourceSpecificationItemId,
    string ElementKind,
    bool IsAuthoritative,
    string? ProvenanceKind);

public sealed record ScientificExportTextFixture(
    string ElementId,
    string SourceSpecificationItemId,
    string ContentKind,
    string Text);

public sealed record ScientificExportRelationFixture(
    string RelationId,
    string SourceSpecificationItemId,
    string Direction,
    string? Label,
    string ProvenanceKind);

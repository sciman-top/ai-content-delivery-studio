using System.Collections.ObjectModel;

namespace ContentDeliveryStudio.Core.ScientificFigures;

public sealed record SvgCanvas(
    int Width,
    int Height,
    string ViewBox);

public sealed record SvgRenderLayer(
    string LayerId,
    int ZIndex,
    bool IsScientific);

public sealed record SvgRenderElement(
    string RenderElementId,
    string SourceSpecificationItemId,
    FigureElementKind Kind,
    string ScientificMeaning,
    string? ExactContent,
    string RenderStrategy,
    string LayerId,
    bool IsCritical,
    ScientificProvenanceKind? ProvenanceKind);

public sealed record SvgRenderConnection(
    string RenderConnectionId,
    string SourceSpecificationItemId,
    string SourceRenderElementId,
    string TargetRenderElementId,
    FigureRelationKind Kind,
    FigureRelationDirection Direction,
    string? Label,
    string RepresentationConstraint,
    bool IsCritical,
    ScientificProvenanceKind ProvenanceKind);

public sealed record SvgAccessibilityMetadata(
    string Title,
    string Description);

public sealed record SvgExportSettings(
    string Format,
    bool IncludeMetadata);

public sealed record SvgLayoutConstraint(
    string ConstraintId,
    string Kind,
    double Value);

public sealed record SvgRenderPlan
{
    private SvgRenderPlan(
        string planId,
        Guid specificationId,
        int specificationVersion,
        SvgCanvas canvas,
        IReadOnlyList<SvgRenderLayer> layers,
        IReadOnlyList<SvgRenderElement> elements,
        IReadOnlyList<SvgRenderConnection> connections,
        SvgAccessibilityMetadata accessibility,
        SvgExportSettings export,
        IReadOnlyList<SvgLayoutConstraint> layoutConstraints,
        IReadOnlyDictionary<string, string> styleTokens)
    {
        PlanId = planId;
        SpecificationId = specificationId;
        SpecificationVersion = specificationVersion;
        Canvas = canvas;
        Layers = layers;
        Elements = elements;
        Connections = connections;
        Accessibility = accessibility;
        Export = export;
        LayoutConstraints = layoutConstraints;
        StyleTokens = styleTokens;
    }

    public string PlanId { get; }

    public Guid SpecificationId { get; }

    public int SpecificationVersion { get; }

    public SvgCanvas Canvas { get; }

    public IReadOnlyList<SvgRenderLayer> Layers { get; }

    public IReadOnlyList<SvgRenderElement> Elements { get; }

    public IReadOnlyList<SvgRenderConnection> Connections { get; }

    public SvgAccessibilityMetadata Accessibility { get; }

    public SvgExportSettings Export { get; }

    public IReadOnlyList<SvgLayoutConstraint> LayoutConstraints { get; }

    public IReadOnlyDictionary<string, string> StyleTokens { get; }

    public IReadOnlyList<SvgRenderElement> Labels =>
        Elements.Where(element => element.Kind is FigureElementKind.Label
            or FigureElementKind.Symbol
            or FigureElementKind.Value
            or FigureElementKind.Unit
            or FigureElementKind.Legend
            or FigureElementKind.Annotation).ToArray();

    public IReadOnlyList<SvgRenderElement> Formulas =>
        Elements.Where(element => element.Kind == FigureElementKind.Formula).ToArray();

    public IReadOnlyList<SvgRenderElement> Legends =>
        Elements.Where(element => element.Kind == FigureElementKind.Legend).ToArray();

    public static SvgRenderPlan Create(
        string planId,
        Guid specificationId,
        int specificationVersion,
        SvgCanvas canvas,
        IReadOnlyList<SvgRenderLayer> layers,
        IReadOnlyList<SvgRenderElement> elements,
        IReadOnlyList<SvgRenderConnection> connections,
        SvgAccessibilityMetadata accessibility,
        SvgExportSettings export,
        IReadOnlyList<SvgLayoutConstraint>? layoutConstraints = null,
        IReadOnlyDictionary<string, string>? styleTokens = null)
    {
        if (string.IsNullOrWhiteSpace(planId))
        {
            throw new ArgumentException("Render plan id cannot be empty.", nameof(planId));
        }

        if (specificationId == Guid.Empty)
        {
            throw new ArgumentException("Specification id cannot be empty.", nameof(specificationId));
        }

        if (specificationVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(specificationVersion),
                specificationVersion,
                "Specification version must be positive.");
        }

        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(layers);
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(connections);
        ArgumentNullException.ThrowIfNull(accessibility);
        ArgumentNullException.ThrowIfNull(export);
        return new SvgRenderPlan(
            planId.Trim(),
            specificationId,
            specificationVersion,
            canvas,
            Array.AsReadOnly(layers.ToArray()),
            Array.AsReadOnly(elements.ToArray()),
            Array.AsReadOnly(connections.ToArray()),
            accessibility,
            export,
            Array.AsReadOnly((layoutConstraints ?? []).ToArray()),
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(
                    styleTokens ?? new Dictionary<string, string>(),
                    StringComparer.Ordinal)));
    }
}

public sealed record SvgRenderPlanValidationError(
    string Code,
    string ItemId,
    string Message);

public sealed record SvgRenderPlanValidationResult(
    IReadOnlyList<SvgRenderPlanValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static class SvgRenderPlanValidator
{
    private static readonly HashSet<string> SupportedRenderStrategies =
    [
        "deterministic-node",
        "deterministic-formula",
        "deterministic-label",
        "deterministic-symbol",
        "deterministic-value",
        "deterministic-unit",
        "deterministic-legend",
        "deterministic-annotation",
        "bounded-raster-asset",
    ];

    public static SvgRenderPlanValidationResult Validate(
        SvgRenderPlan plan,
        IReadOnlyCollection<string>? approvedElementIds = null,
        IReadOnlyCollection<string>? approvedRelationIds = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var errors = new List<SvgRenderPlanValidationError>();
        ValidateCanvas(plan, errors);
        ValidateLayers(plan, errors);
        ValidateElements(plan, approvedElementIds, errors);
        ValidateConnections(plan, approvedRelationIds, errors);
        return new SvgRenderPlanValidationResult(Array.AsReadOnly(errors.ToArray()));
    }

    public static void ValidateOrThrow(
        SvgRenderPlan plan,
        IReadOnlyCollection<string> approvedElementIds,
        IReadOnlyCollection<string> approvedRelationIds)
    {
        var result = Validate(plan, approvedElementIds, approvedRelationIds);
        if (!result.IsValid)
        {
            throw new InvalidOperationException(
                $"SVG render plan validation failed: {string.Join(", ", result.Errors.Select(error => $"{error.Code}:{error.ItemId}"))}");
        }
    }

    private static void ValidateCanvas(
        SvgRenderPlan plan,
        ICollection<SvgRenderPlanValidationError> errors)
    {
        if (plan.Canvas.Width <= 0
            || plan.Canvas.Height <= 0
            || string.IsNullOrWhiteSpace(plan.Canvas.ViewBox))
        {
            errors.Add(new SvgRenderPlanValidationError(
                "invalid-canvas",
                plan.PlanId,
                "Canvas dimensions and viewBox must be defined."));
        }
    }

    private static void ValidateLayers(
        SvgRenderPlan plan,
        ICollection<SvgRenderPlanValidationError> errors)
    {
        foreach (var duplicate in plan.Layers
                     .GroupBy(layer => layer.LayerId, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            errors.Add(new SvgRenderPlanValidationError(
                "duplicate-layer-id",
                duplicate.Key,
                "Layer identifiers must be unique."));
        }
    }

    private static void ValidateElements(
        SvgRenderPlan plan,
        IReadOnlyCollection<string>? approvedElementIds,
        ICollection<SvgRenderPlanValidationError> errors)
    {
        var layerIds = plan.Layers
            .Select(layer => layer.LayerId)
            .ToHashSet(StringComparer.Ordinal);
        var scientificLayerIds = plan.Layers
            .Where(layer => layer.IsScientific)
            .Select(layer => layer.LayerId)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var duplicate in plan.Elements
                     .GroupBy(element => element.RenderElementId, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            errors.Add(new SvgRenderPlanValidationError(
                "duplicate-render-element-id",
                duplicate.Key,
                "Render element identifiers must be unique."));
        }

        foreach (var element in plan.Elements)
        {
            if (!SupportedRenderStrategies.Contains(element.RenderStrategy))
            {
                errors.Add(new SvgRenderPlanValidationError(
                    "unsupported-render-strategy",
                    element.RenderElementId,
                    $"Render strategy is unsupported: {element.RenderStrategy}"));
            }

            if (!layerIds.Contains(element.LayerId))
            {
                errors.Add(new SvgRenderPlanValidationError(
                    "missing-layer",
                    element.RenderElementId,
                    $"Render layer was not found: {element.LayerId}"));
            }

            if (RequiresExactContent(element.Kind)
                && string.IsNullOrWhiteSpace(element.ExactContent))
            {
                errors.Add(new SvgRenderPlanValidationError(
                    "missing-exact-content",
                    element.RenderElementId,
                    "This render element requires exact text or formula content."));
            }

            if (element.IsCritical
                && string.IsNullOrWhiteSpace(element.SourceSpecificationItemId))
            {
                errors.Add(new SvgRenderPlanValidationError(
                    "missing-specification-authority",
                    element.RenderElementId,
                    "Critical render elements require specification authority."));
            }

            if (element.IsCritical && !scientificLayerIds.Contains(element.LayerId))
            {
                errors.Add(new SvgRenderPlanValidationError(
                    "critical-item-outside-scientific-layer",
                    element.RenderElementId,
                    "Critical scientific elements must use a scientific layer."));
            }

            if (approvedElementIds is not null
                && !approvedElementIds.Contains(element.SourceSpecificationItemId))
            {
                errors.Add(new SvgRenderPlanValidationError(
                    "unapproved-specification-item",
                    element.RenderElementId,
                    $"Element is not present in the approved specification: {element.SourceSpecificationItemId}"));
            }
        }
    }

    private static void ValidateConnections(
        SvgRenderPlan plan,
        IReadOnlyCollection<string>? approvedRelationIds,
        ICollection<SvgRenderPlanValidationError> errors)
    {
        var elementIds = plan.Elements
            .Select(element => element.RenderElementId)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var duplicate in plan.Connections
                     .GroupBy(connection => connection.RenderConnectionId, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            errors.Add(new SvgRenderPlanValidationError(
                "duplicate-render-connection-id",
                duplicate.Key,
                "Render connection identifiers must be unique."));
        }

        foreach (var connection in plan.Connections)
        {
            if (!elementIds.Contains(connection.SourceRenderElementId)
                || !elementIds.Contains(connection.TargetRenderElementId))
            {
                errors.Add(new SvgRenderPlanValidationError(
                    "missing-connection-endpoint",
                    connection.RenderConnectionId,
                    "Connection source and target must exist in the render plan."));
            }

            if (approvedRelationIds is not null
                && !approvedRelationIds.Contains(connection.SourceSpecificationItemId))
            {
                errors.Add(new SvgRenderPlanValidationError(
                    "unapproved-specification-item",
                    connection.RenderConnectionId,
                    $"Relation is not present in the approved specification: {connection.SourceSpecificationItemId}"));
            }
        }
    }

    private static bool RequiresExactContent(FigureElementKind kind)
    {
        return kind is FigureElementKind.Label
            or FigureElementKind.Symbol
            or FigureElementKind.Formula
            or FigureElementKind.Value
            or FigureElementKind.Unit
            or FigureElementKind.Legend
            or FigureElementKind.Annotation;
    }
}

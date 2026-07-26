using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public sealed class ScientificFigureSpecCompiler
{
    public SvgRenderPlan Compile(ScientificFigureWorkflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var specification = workflow.Specification;
        if (workflow.Gate1Approval is null
            || workflow.Gate1Approval.ApprovedSpecVersion != specification.Version
            || workflow.Gate1Approval.SpecificationId != specification.SpecificationId)
        {
            throw new InvalidOperationException(
                "Only the current Gate-1-approved specification can compile.");
        }

        var includedElements = specification.Elements
            .Where(element => element.Requirement != FigureContentRequirement.Forbidden)
            .ToArray();
        var elementRenderIds = includedElements.ToDictionary(
            element => element.ElementId,
            element => $"render-{element.ElementId}",
            StringComparer.Ordinal);
        var layers = includedElements.Any(element =>
            element.Kind == FigureElementKind.DecorativeAsset)
            ? new[]
            {
                new SvgRenderLayer("scientific-content", 0, IsScientific: true),
                new SvgRenderLayer("decorative", -1, IsScientific: false),
            }
            : [new SvgRenderLayer("scientific-content", 0, IsScientific: true)];
        var elements = includedElements.Select(element =>
            new SvgRenderElement(
                elementRenderIds[element.ElementId],
                element.ElementId,
                element.Kind,
                element.ScientificMeaning,
                element.LabelOrFormula,
                element.RenderStrategy,
                element.Kind == FigureElementKind.DecorativeAsset
                    ? "decorative"
                    : "scientific-content",
                element.IsCritical,
                element.Provenance?.Kind)).ToArray();
        var connections = specification.Relations
            .Where(relation => relation.Requirement != FigureContentRequirement.Forbidden)
            .Select(relation =>
            {
                if (!elementRenderIds.TryGetValue(
                        relation.SourceElementId,
                        out var sourceRenderId)
                    || !elementRenderIds.TryGetValue(
                        relation.TargetElementId,
                        out var targetRenderId))
                {
                    throw new InvalidOperationException(
                        $"Render relation endpoint is missing: {relation.RelationId}");
                }

                return new SvgRenderConnection(
                    $"render-{relation.RelationId}",
                    relation.RelationId,
                    sourceRenderId,
                    targetRenderId,
                    relation.Kind,
                    relation.Direction,
                    relation.Label,
                    relation.RepresentationConstraint,
                    relation.IsCritical,
                    relation.Provenance.Kind);
            })
            .ToArray();
        var plan = SvgRenderPlan.Create(
            $"render-plan:{specification.SpecificationId:N}:v{specification.Version}",
            specification.SpecificationId,
            specification.Version,
            new SvgCanvas(1200, 800, "0 0 1200 800"),
            layers,
            elements,
            connections,
            new SvgAccessibilityMetadata(
                specification.CentralMessage,
                specification.Purpose),
            new SvgExportSettings("svg", IncludeMetadata: true),
            [
                new SvgLayoutConstraint("canvas-padding", "padding", 48),
                new SvgLayoutConstraint("minimum-item-spacing", "minimum-spacing", 24),
            ],
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["font-family"] = "Segoe UI",
                ["scientific-stroke"] = "#1F2937",
                ["scientific-fill"] = "#F8FAFC",
                ["accent-fill"] = "#0F766E",
            });
        SvgRenderPlanValidator.ValidateOrThrow(
            plan,
            specification.Elements
                .Where(element => element.Requirement != FigureContentRequirement.Forbidden)
                .Select(element => element.ElementId)
                .ToArray(),
            specification.Relations
                .Where(relation => relation.Requirement != FigureContentRequirement.Forbidden)
                .Select(relation => relation.RelationId)
                .ToArray());
        return plan;
    }
}

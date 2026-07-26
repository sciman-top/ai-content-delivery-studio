using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed class DeterministicSvgRenderer : IScientificFigureRenderer
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    public ScientificSvgArtifact Render(SvgRenderPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var validation = SvgRenderPlanValidator.Validate(plan);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot render invalid SVG plan: {string.Join(", ", validation.Errors.Select(error => $"{error.Code}:{error.ItemId}"))}");
        }

        var positions = BuildPositions(plan);
        var root = new XElement(
            Svg + "svg",
            new XAttribute("xmlns", Svg.NamespaceName),
            new XAttribute("width", plan.Canvas.Width),
            new XAttribute("height", plan.Canvas.Height),
            new XAttribute("viewBox", plan.Canvas.ViewBox),
            new XAttribute("role", "img"),
            new XAttribute("aria-labelledby", "svg-title svg-description"),
            new XAttribute("data-plan-id", plan.PlanId),
            new XAttribute("data-specification-id", plan.SpecificationId.ToString("D")),
            new XAttribute("data-specification-version", plan.SpecificationVersion),
            new XElement(
                Svg + "title",
                new XAttribute("id", "svg-title"),
                plan.Accessibility.Title),
            new XElement(
                Svg + "desc",
                new XAttribute("id", "svg-description"),
                plan.Accessibility.Description),
            CreateMetadata(plan),
            CreateDefinitions(plan));

        var orderedLayers = plan.Layers.OrderBy(layer => layer.ZIndex).ToArray();
        var connectionLayerId = orderedLayers.FirstOrDefault(layer => layer.IsScientific)?.LayerId;
        foreach (var layer in orderedLayers)
        {
            var layerElement = new XElement(
                Svg + "g",
                new XAttribute("id", $"layer-{EncodeId(layer.LayerId)}"),
                new XAttribute("data-layer-id", layer.LayerId),
                new XAttribute("data-scientific", LowerBoolean(layer.IsScientific)));
            if (string.Equals(layer.LayerId, connectionLayerId, StringComparison.Ordinal))
            {
                foreach (var connection in plan.Connections)
                {
                    layerElement.Add(RenderConnection(connection, positions, plan));
                }
            }

            foreach (var element in plan.Elements.Where(item =>
                         string.Equals(item.LayerId, layer.LayerId, StringComparison.Ordinal)))
            {
                layerElement.Add(RenderElement(element, positions[element.RenderElementId], plan));
            }

            root.Add(layerElement);
        }

        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        var svg = document.ToString(SaveOptions.DisableFormatting);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(svg)))
            .ToLowerInvariant();
        return new ScientificSvgArtifact(
            plan.PlanId,
            plan.SpecificationId,
            plan.SpecificationVersion,
            svg,
            $"sha256:{hash}");
    }

    private static XElement CreateMetadata(SvgRenderPlan plan)
    {
        var content =
            $"plan={plan.PlanId};specification={plan.SpecificationId:D};version={plan.SpecificationVersion};format={plan.Export.Format}";
        return new XElement(Svg + "metadata", content);
    }

    private static XElement CreateDefinitions(SvgRenderPlan plan)
    {
        var stroke = Style(plan, "scientific-stroke", "#1F2937");
        return new XElement(
            Svg + "defs",
            new XElement(
                Svg + "marker",
                new XAttribute("id", "arrowhead"),
                new XAttribute("markerWidth", 10),
                new XAttribute("markerHeight", 7),
                new XAttribute("refX", 9),
                new XAttribute("refY", 3.5),
                new XAttribute("orient", "auto-start-reverse"),
                new XAttribute("markerUnits", "strokeWidth"),
                new XElement(
                    Svg + "path",
                    new XAttribute("d", "M 0 0 L 10 3.5 L 0 7 z"),
                    new XAttribute("fill", stroke))));
    }

    private static XElement RenderConnection(
        SvgRenderConnection connection,
        IReadOnlyDictionary<string, ElementPosition> positions,
        SvgRenderPlan plan)
    {
        var source = positions[connection.SourceRenderElementId];
        var target = positions[connection.TargetRenderElementId];
        var sourcePoint = source.EdgeToward(target.CenterX, target.CenterY);
        var targetPoint = target.EdgeToward(source.CenterX, source.CenterY);
        var path = new XElement(
            Svg + "path",
            new XAttribute("id", EncodeId(connection.RenderConnectionId)),
            new XAttribute("data-spec-id", connection.SourceSpecificationItemId),
            new XAttribute("data-provenance-kind", connection.ProvenanceKind),
            new XAttribute("data-relation-kind", connection.Kind),
            new XAttribute("data-direction", connection.Direction),
            new XAttribute(
                "d",
                FormattableString.Invariant(
                    $"M {sourcePoint.X:0.###} {sourcePoint.Y:0.###} L {targetPoint.X:0.###} {targetPoint.Y:0.###}")),
            new XAttribute("fill", "none"),
            new XAttribute("stroke", Style(plan, "scientific-stroke", "#1F2937")),
            new XAttribute("stroke-width", 2));
        if (connection.Direction is FigureRelationDirection.Directed
            or FigureRelationDirection.Bidirectional)
        {
            path.Add(new XAttribute("marker-end", "url(#arrowhead)"));
        }

        if (connection.Direction == FigureRelationDirection.Bidirectional)
        {
            path.Add(new XAttribute("marker-start", "url(#arrowhead)"));
        }

        if (string.IsNullOrWhiteSpace(connection.Label))
        {
            return path;
        }

        return new XElement(
            Svg + "g",
            new XAttribute("data-connection-group", connection.RenderConnectionId),
            path,
            new XElement(
                Svg + "text",
                new XAttribute("x", Number((sourcePoint.X + targetPoint.X) / 2)),
                new XAttribute("y", Number((sourcePoint.Y + targetPoint.Y) / 2 - 8)),
                new XAttribute("text-anchor", "middle"),
                new XAttribute("font-family", Style(plan, "font-family", "Segoe UI")),
                new XAttribute("font-size", 16),
                new XAttribute("fill", Style(plan, "scientific-stroke", "#1F2937")),
                connection.Label));
    }

    private static XElement RenderElement(
        SvgRenderElement element,
        ElementPosition position,
        SvgRenderPlan plan)
    {
        var group = new XElement(
            Svg + "g",
            new XAttribute("id", EncodeId(element.RenderElementId)),
            new XAttribute("data-spec-id", element.SourceSpecificationItemId),
            new XAttribute("data-element-kind", element.Kind),
            new XAttribute("data-layer-id", element.LayerId),
            new XAttribute("data-critical", LowerBoolean(element.IsCritical)),
            new XAttribute(
                "data-authoritative",
                LowerBoolean(element.Kind != FigureElementKind.DecorativeAsset)));
        if (element.ProvenanceKind is not null)
        {
            group.Add(new XAttribute("data-provenance-kind", element.ProvenanceKind));
        }

        group.Add(new XElement(Svg + "title", element.ScientificMeaning));
        if (element.Kind == FigureElementKind.DecorativeAsset)
        {
            group.Add(new XElement(
                Svg + "rect",
                new XAttribute("x", Number(position.X)),
                new XAttribute("y", Number(position.Y)),
                new XAttribute("width", Number(position.Width)),
                new XAttribute("height", Number(position.Height)),
                new XAttribute("fill", Style(plan, "accent-fill", "#0F766E")),
                new XAttribute("fill-opacity", "0.08"),
                new XAttribute("stroke", "none"),
                new XAttribute("data-non-authoritative-asset", "true")));
            return group;
        }

        group.Add(new XElement(
            Svg + "rect",
            new XAttribute("x", Number(position.X)),
            new XAttribute("y", Number(position.Y)),
            new XAttribute("width", Number(position.Width)),
            new XAttribute("height", Number(position.Height)),
            new XAttribute("rx", 4),
            new XAttribute("fill", Style(plan, "scientific-fill", "#F8FAFC")),
            new XAttribute("stroke", Style(plan, "scientific-stroke", "#1F2937")),
            new XAttribute("stroke-width", element.IsCritical ? 2 : 1)));
        if (!string.IsNullOrWhiteSpace(element.ExactContent))
        {
            group.Add(new XElement(
                Svg + "text",
                new XAttribute("x", Number(position.CenterX)),
                new XAttribute("y", Number(position.CenterY + 6)),
                new XAttribute("text-anchor", "middle"),
                new XAttribute("font-family", Style(plan, "font-family", "Segoe UI")),
                new XAttribute(
                    "font-size",
                    element.Kind == FigureElementKind.Formula ? 22 : 18),
                new XAttribute("fill", Style(plan, "scientific-stroke", "#1F2937")),
                new XAttribute("data-content-kind", element.Kind),
                element.ExactContent));
        }

        return group;
    }

    private static IReadOnlyDictionary<string, ElementPosition> BuildPositions(
        SvgRenderPlan plan)
    {
        var padding = Constraint(plan, "padding", 48);
        var spacing = Constraint(plan, "minimum-spacing", 24);
        var count = Math.Max(1, plan.Elements.Count);
        var columns = (int)Math.Ceiling(Math.Sqrt(count));
        var rows = (int)Math.Ceiling((double)count / columns);
        var cellWidth = (plan.Canvas.Width - (padding * 2) - (spacing * (columns - 1))) / columns;
        var cellHeight = (plan.Canvas.Height - (padding * 2) - (spacing * (rows - 1))) / rows;
        var nodeWidth = Math.Min(280, Math.Max(80, cellWidth));
        var nodeHeight = Math.Min(120, Math.Max(64, cellHeight));
        var result = new Dictionary<string, ElementPosition>(StringComparer.Ordinal);
        for (var index = 0; index < plan.Elements.Count; index++)
        {
            var row = index / columns;
            var column = index % columns;
            var cellX = padding + (column * (cellWidth + spacing));
            var cellY = padding + (row * (cellHeight + spacing));
            result[plan.Elements[index].RenderElementId] = new ElementPosition(
                cellX + ((cellWidth - nodeWidth) / 2),
                cellY + ((cellHeight - nodeHeight) / 2),
                nodeWidth,
                nodeHeight);
        }

        return result;
    }

    private static double Constraint(
        SvgRenderPlan plan,
        string kind,
        double fallback)
    {
        return plan.LayoutConstraints
            .FirstOrDefault(item => string.Equals(item.Kind, kind, StringComparison.Ordinal))
            ?.Value ?? fallback;
    }

    private static string Style(
        SvgRenderPlan plan,
        string key,
        string fallback)
    {
        return plan.StyleTokens.TryGetValue(key, out var value)
            ? value
            : fallback;
    }

    private static string EncodeId(string value)
    {
        return XmlConvert.EncodeLocalName(value);
    }

    private static string Number(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string LowerBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    private sealed record ElementPosition(
        double X,
        double Y,
        double Width,
        double Height)
    {
        public double CenterX => X + (Width / 2);

        public double CenterY => Y + (Height / 2);

        public ElementPoint EdgeToward(double targetX, double targetY)
        {
            var deltaX = targetX - CenterX;
            var deltaY = targetY - CenterY;
            if (deltaX == 0 && deltaY == 0)
            {
                return new ElementPoint(CenterX, CenterY);
            }

            var scale = 1 / Math.Max(
                Math.Abs(deltaX) / (Width / 2),
                Math.Abs(deltaY) / (Height / 2));
            return new ElementPoint(
                CenterX + (deltaX * scale),
                CenterY + (deltaY * scale));
        }
    }

    private sealed record ElementPoint(double X, double Y);
}

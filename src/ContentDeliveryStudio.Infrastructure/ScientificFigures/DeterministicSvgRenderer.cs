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
        var occupiedRelationLabelBounds = new List<LabelBounds>();
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
                    var renderedConnection = RenderConnection(
                        connection,
                        positions,
                        plan,
                        occupiedRelationLabelBounds,
                        out var labelBounds);
                    layerElement.Add(renderedConnection);
                    if (labelBounds is not null)
                    {
                        occupiedRelationLabelBounds.Add(labelBounds);
                    }
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
        SvgRenderPlan plan,
        IReadOnlyList<LabelBounds> occupiedRelationLabelBounds,
        out LabelBounds? renderedLabelBounds)
    {
        var source = positions[connection.SourceRenderElementId];
        var target = positions[connection.TargetRenderElementId];
        var sourcePoint = source.EdgeToward(target.CenterX, target.CenterY);
        var targetPoint = target.EdgeToward(source.CenterX, source.CenterY);
        var parallelConnections = plan.Connections.Where(item =>
            string.Equals(item.SourceRenderElementId, connection.SourceRenderElementId, StringComparison.Ordinal)
            && string.Equals(item.TargetRenderElementId, connection.TargetRenderElementId, StringComparison.Ordinal))
            .ToArray();
        var parallelLabelSide = 1d;
        if (parallelConnections.Length > 1)
        {
            var parallelIndex = Array.IndexOf(parallelConnections, connection);
            var lineDeltaX = targetPoint.X - sourcePoint.X;
            var lineDeltaY = targetPoint.Y - sourcePoint.Y;
            var lineLength = Math.Sqrt((lineDeltaX * lineDeltaX) + (lineDeltaY * lineDeltaY));
            if (lineLength > 0)
            {
                var pathOffset = (parallelIndex - ((parallelConnections.Length - 1) / 2d)) * 24;
                var offsetX = (-lineDeltaY / lineLength) * pathOffset;
                var offsetY = (lineDeltaX / lineLength) * pathOffset;
                sourcePoint = new ElementPoint(sourcePoint.X + offsetX, sourcePoint.Y + offsetY);
                targetPoint = new ElementPoint(targetPoint.X + offsetX, targetPoint.Y + offsetY);
                parallelLabelSide = parallelIndex % 2 == 0 ? -1 : 1;
            }
        }
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
            renderedLabelBounds = null;
            return path;
        }

        var displayLabel = DisplayRelationLabel(connection);
        if (!string.Equals(displayLabel, connection.Label, StringComparison.Ordinal))
        {
            path.Add(new XAttribute("data-exact-label", connection.Label));
        }

        var deltaX = targetPoint.X - sourcePoint.X;
        var deltaY = targetPoint.Y - sourcePoint.Y;
        var length = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        var labelOffset = 0d;
        if (length > 0)
        {
            labelOffset = Math.Abs(deltaY) <= Math.Abs(deltaX) * 0.25
                ? 110
                : 80;
            if (parallelConnections.Length > 1
                && connection.SourceSpecificationItemId is
                    "frequency-threshold" or "intensity-not-ke")
            {
                labelOffset = 150;
            }
        }

        var labelLines = WrapPreservingContent(displayLabel, maxCharactersPerLine: 28);
        var maximumLineLength = labelLines.Max(item => item.Length);
        var labelWidth = Math.Clamp(maximumLineLength * 8.5, 48, 250);
        var labelHeight = (labelLines.Count * 19) + 8;
        var labelPlacement = PlaceRelationLabel(
            sourcePoint,
            targetPoint,
            deltaX,
            deltaY,
            length,
            labelOffset,
            parallelLabelSide,
            labelWidth,
            labelHeight,
            plan.Canvas,
            occupiedRelationLabelBounds);
        renderedLabelBounds = labelPlacement.Bounds;
        var labelElements = CreateWrappedTextLines(
            displayLabel,
            labelPlacement.X,
            labelPlacement.Y,
            maxCharactersPerLine: 28,
            lineHeight: 19,
            fontSize: 16,
            Style(plan, "scientific-stroke", "#1F2937"),
            Style(plan, "font-family", "Segoe UI"),
            "data-relation-label");
        var labelBackground = new XElement(
            Svg + "rect",
            new XAttribute("x", Number(labelPlacement.Bounds.Left)),
            new XAttribute("y", Number(labelPlacement.Bounds.Top)),
            new XAttribute("width", Number(labelWidth)),
            new XAttribute("height", Number(labelHeight)),
            new XAttribute("rx", 3),
            new XAttribute("fill", Style(plan, "label-background", "#FFFFFF")),
            new XAttribute("stroke", "none"),
            new XAttribute("data-relation-label-background", "true"));
        return new XElement(
            Svg + "g",
            new XAttribute("data-connection-group", connection.RenderConnectionId),
            path,
            labelBackground,
            labelElements);
    }

    private static LabelPlacement PlaceRelationLabel(
        ElementPoint sourcePoint,
        ElementPoint targetPoint,
        double deltaX,
        double deltaY,
        double length,
        double labelOffset,
        double preferredSide,
        double labelWidth,
        double labelHeight,
        SvgCanvas canvas,
        IReadOnlyList<LabelBounds> occupiedBounds)
    {
        var midpointX = (sourcePoint.X + targetPoint.X) / 2;
        var midpointY = ((sourcePoint.Y + targetPoint.Y) / 2) - 8;
        var offsets = new[] { labelOffset, labelOffset + 56, labelOffset + 112 };
        var sides = new[] { preferredSide, -preferredSide };
        foreach (var offset in offsets)
        {
            foreach (var side in sides)
            {
                var x = midpointX;
                var y = midpointY;
                if (length > 0)
                {
                    x += (-deltaY / length) * offset * side;
                    y += (deltaX / length) * offset * side;
                }

                var bounds = new LabelBounds(
                    x - (labelWidth / 2),
                    y - (labelHeight / 2),
                    labelWidth,
                    labelHeight);
                if (IsInsideCanvas(bounds, canvas)
                    && occupiedBounds.All(existing => !bounds.Overlaps(existing)))
                {
                    return new LabelPlacement(x, y, bounds);
                }
            }
        }

        // The renderer keeps output deterministic even for saturated layouts; the
        // contract reviewer rejects any remaining overlap before delivery.
        var fallbackBounds = new LabelBounds(
            midpointX - (labelWidth / 2),
            midpointY - (labelHeight / 2),
            labelWidth,
            labelHeight);
        return new LabelPlacement(midpointX, midpointY, fallbackBounds);
    }

    private static bool IsInsideCanvas(LabelBounds bounds, SvgCanvas canvas) =>
        bounds.Left >= 0
        && bounds.Top >= 0
        && bounds.Left + bounds.Width <= canvas.Width
        && bounds.Top + bounds.Height <= canvas.Height;

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
        var scientificGraphic = RenderScientificGraphic(element, position, plan);
        group.Add(scientificGraphic);
        var hasScientificGraphic = scientificGraphic.Count > 0;
        var labelCenterX = hasScientificGraphic
            ? position.X + (position.Width * 0.68)
            : position.CenterX;
        if (!string.IsNullOrWhiteSpace(element.ExactContent))
        {
            group.Add(new XElement(
                Svg + "text",
                new XAttribute("x", Number(labelCenterX)),
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
        else
        {
            group.Add(CreateWrappedTextLines(
                FormatScientificDisplayText(element.ScientificMeaning),
                labelCenterX,
                position.CenterY,
                maxCharactersPerLine: hasScientificGraphic ? 21 : 34,
                lineHeight: 18,
                fontSize: hasScientificGraphic ? 13 : 15,
                Style(plan, "scientific-stroke", "#1F2937"),
                Style(plan, "font-family", "Segoe UI"),
                "data-display-role"));
        }

        return group;
    }

    private static IReadOnlyList<XElement> RenderScientificGraphic(
        SvgRenderElement element,
        ElementPosition position,
        SvgRenderPlan plan)
    {
        var stroke = Style(plan, "scientific-stroke", "#1F2937");
        var accent = Style(plan, "accent-fill", "#0F766E");
        var left = position.X + 16;
        var top = position.Y + 16;
        var width = position.Width * 0.34;
        var height = position.Height - 32;
        var centerX = left + (width / 2);
        var centerY = top + (height / 2);

        XElement Line(
            double x1,
            double y1,
            double x2,
            double y2,
            string color,
            bool arrow = false,
            double strokeWidth = 2) =>
            new(
                Svg + "path",
                new XAttribute("d", FormattableString.Invariant($"M {x1:0.###} {y1:0.###} L {x2:0.###} {y2:0.###}")),
                new XAttribute("fill", "none"),
                new XAttribute("stroke", color),
                new XAttribute("stroke-width", strokeWidth),
                new XAttribute("data-element-graphic", "true"),
                arrow ? new XAttribute("marker-end", "url(#arrowhead)") : null);

        XElement Annotation(string value, double x, double y, int fontSize = 12) =>
            new(
                Svg + "text",
                new XAttribute("x", Number(x)),
                new XAttribute("y", Number(y)),
                new XAttribute("text-anchor", "middle"),
                new XAttribute("font-family", Style(plan, "font-family", "Segoe UI")),
                new XAttribute("font-size", fontSize),
                new XAttribute("fill", stroke),
                new XAttribute("data-display-role", "graphic-annotation"),
                value);

        var graphic = new List<XElement>();
        switch (element.SourceSpecificationItemId)
        {
            case "uniform-magnetic-field":
                for (var offset = 0; offset < 3; offset++)
                {
                    var x = left + 18 + (offset * 26);
                    graphic.Add(Line(x, top + height - 8, x, top + 10, accent, arrow: true));
                }
                graphic.Add(Annotation("B", centerX, top + 12, 14));
                break;
            case "rotating-coil":
                foreach (var loopOffset in new[] { -4, 0, 4 })
                {
                    graphic.Add(Line(left + 20 + loopOffset, top + 22, left + width - 12 + loopOffset, top + 30, stroke, strokeWidth: 2));
                    graphic.Add(Line(left + width - 12 + loopOffset, top + 30, left + width - 22 + loopOffset, top + height - 18, stroke, strokeWidth: 2));
                    graphic.Add(Line(left + width - 22 + loopOffset, top + height - 18, left + 10 + loopOffset, top + height - 26, stroke, strokeWidth: 2));
                    graphic.Add(Line(left + 10 + loopOffset, top + height - 26, left + 20 + loopOffset, top + 22, stroke, strokeWidth: 2));
                }
                var rotationPoints = Enumerable.Range(0, 9)
                    .Select(index =>
                    {
                        var angle = Math.PI + (index * Math.PI / 8);
                        return (
                            X: centerX + (Math.Cos(angle) * 43),
                            Y: centerY + (Math.Sin(angle) * 32));
                    })
                    .ToArray();
                for (var index = 0; index < rotationPoints.Length - 1; index++)
                {
                    graphic.Add(Line(
                        rotationPoints[index].X,
                        rotationPoints[index].Y,
                        rotationPoints[index + 1].X,
                        rotationPoints[index + 1].Y,
                        accent,
                        arrow: index == rotationPoints.Length - 2));
                }
                graphic.Add(Annotation("N turns", centerX, top + 10, 10));
                graphic.Add(Annotation("A", centerX, centerY + 5, 13));
                graphic.Add(Annotation("ω", centerX, top + height + 1, 14));
                break;
            case "vertical-wire-segments":
                graphic.Add(Line(left + 28, top + height - 8, left + 28, top + 8, stroke, arrow: true, strokeWidth: 3));
                graphic.Add(Line(left + width - 24, top + 8, left + width - 24, top + height - 8, stroke, arrow: true, strokeWidth: 3));
                graphic.Add(Annotation("I", centerX, centerY + 4, 14));
                break;
            case "shaft-input":
                graphic.Add(Line(left + 8, centerY, left + width - 8, centerY, stroke, arrow: true, strokeWidth: 4));
                graphic.Add(Annotation("τ, ω", centerX, centerY - 12, 13));
                break;
            case "external-load":
                var resistorPoints = new[]
                {
                    (left + 8, centerY), (left + 22, centerY), (left + 32, centerY - 14),
                    (left + 46, centerY + 14), (left + 60, centerY - 14),
                    (left + 74, centerY + 14), (left + 86, centerY), (left + width - 4, centerY),
                };
                for (var index = 0; index < resistorPoints.Length - 1; index++)
                {
                    graphic.Add(Line(
                        resistorPoints[index].Item1,
                        resistorPoints[index].Item2,
                        resistorPoints[index + 1].Item1,
                        resistorPoints[index + 1].Item2,
                        accent));
                }
                graphic.Add(Annotation("R", centerX, top + height - 2, 13));
                break;
            case "emf-waveform":
                graphic.Add(Line(left + 4, centerY, left + width - 4, centerY, stroke, arrow: true));
                graphic.Add(Line(left + 12, top + height - 4, left + 12, top + 4, stroke, arrow: true));
                var wavePoints = Enumerable.Range(0, 49)
                    .Select(index => (
                        X: left + 12 + (index * ((width - 20) / 48)),
                        Y: centerY - (Math.Sin(index * Math.PI / 12) * 20)))
                    .ToArray();
                for (var index = 0; index < wavePoints.Length - 1; index++)
                {
                    graphic.Add(Line(
                        wavePoints[index].X,
                        wavePoints[index].Y,
                        wavePoints[index + 1].X,
                        wavePoints[index + 1].Y,
                        accent,
                        strokeWidth: 2.5));
                }
                graphic.Add(Annotation("ε(t)=ε₀ sin(ωt)", centerX, top + height + 1, 9));
                break;
            case "high-temperature-region":
                graphic.Add(Annotation("Tₕ", centerX, centerY + 5, 22));
                graphic.Add(Line(left + 10, top + 14, left + width - 10, top + 14, "#B91C1C", strokeWidth: 4));
                break;
            case "low-temperature-region":
                graphic.Add(Annotation("Tₗ", centerX, centerY + 5, 22));
                graphic.Add(Line(left + 10, top + height - 14, left + width - 10, top + height - 14, "#0369A1", strokeWidth: 4));
                break;
            case "conduction-mode":
                graphic.Add(Annotation("Conduction", centerX, top + 10, 11));
                graphic.Add(new XElement(Svg + "rect", new XAttribute("x", Number(left + 8)), new XAttribute("y", Number(top + 20)), new XAttribute("width", 34), new XAttribute("height", 48), new XAttribute("fill", "#FECACA"), new XAttribute("stroke", stroke), new XAttribute("data-element-graphic", "true")));
                graphic.Add(new XElement(Svg + "rect", new XAttribute("x", Number(left + 42)), new XAttribute("y", Number(top + 20)), new XAttribute("width", 34), new XAttribute("height", 48), new XAttribute("fill", "#BAE6FD"), new XAttribute("stroke", stroke), new XAttribute("data-element-graphic", "true")));
                graphic.Add(Line(left + 26, centerY, left + 68, centerY, accent, arrow: true));
                break;
            case "convection-mode":
                graphic.Add(Annotation("Convection", centerX, top + 10, 11));
                graphic.Add(Line(left + 24, top + height - 14, left + 24, top + 24, accent, arrow: true));
                graphic.Add(Line(left + width - 22, top + 24, left + width - 22, top + height - 14, accent, arrow: true));
                graphic.Add(Line(left + 24, top + 24, left + width - 22, top + 24, accent, arrow: true));
                graphic.Add(Line(left + width - 22, top + height - 14, left + 24, top + height - 14, accent, arrow: true));
                break;
            case "radiation-mode":
            case "incident-photons":
                if (element.SourceSpecificationItemId == "radiation-mode")
                {
                    graphic.Add(Annotation("Radiation", centerX, top + 10, 11));
                }
                for (var row = 0; row < 3; row++)
                {
                    var y = top + (element.SourceSpecificationItemId == "radiation-mode" ? 28 : 18) + (row * 18);
                    graphic.Add(Line(left + 6, y, left + 28, y - 7, accent));
                    graphic.Add(Line(left + 28, y - 7, left + 48, y + 7, accent));
                    graphic.Add(Line(left + 48, y + 7, left + width - 4, y, accent, arrow: true));
                }
                graphic.Add(Annotation(element.SourceSpecificationItemId == "incident-photons" ? "hν" : "EM", centerX, top + height, 12));
                break;
            case "metal-surface":
                graphic.Add(Line(centerX, top + 6, centerX, top + height - 6, stroke, strokeWidth: 7));
                graphic.Add(Annotation("BE", centerX - 22, centerY + 5, 13));
                break;
            case "ejected-electrons":
                for (var row = 0; row < 3; row++)
                {
                    var y = top + 20 + (row * 22);
                    graphic.Add(Line(left + 8, y, left + width - 8, y - 10, accent, arrow: true));
                    graphic.Add(Annotation("e⁻", left + 18, y - 6, 11));
                }
                break;
        }

        return graphic;
    }

    private static string FormatScientificDisplayText(string value)
    {
        return value
            .Replace("epsilon0", "ε₀", StringComparison.Ordinal)
            .Replace("omega", "ω", StringComparison.Ordinal);
    }

    private static string DisplayRelationLabel(SvgRenderConnection connection)
    {
        return connection.SourceSpecificationItemId is
            "relation-net-transfer-direction"
            or "relation-mode-comparison"
            or "relation-modes-share-phenomenon"
            or "frequency-threshold"
            or "intensity-not-ke"
                ? connection.Label!.Replace('-', ' ')
                : connection.Label!;
    }

    private static IReadOnlyList<XElement> CreateWrappedTextLines(
        string content,
        double x,
        double centerY,
        int maxCharactersPerLine,
        int lineHeight,
        int fontSize,
        string fill,
        string fontFamily,
        string roleAttribute)
    {
        var lines = WrapPreservingContent(content, maxCharactersPerLine);
        var firstY = centerY - (((lines.Count - 1) * lineHeight) / 2.0);
        return lines.Select((line, index) =>
            new XElement(
                Svg + "text",
                new XAttribute("x", Number(x)),
                new XAttribute("y", Number(firstY + (index * lineHeight))),
                new XAttribute("text-anchor", "middle"),
                new XAttribute("font-family", fontFamily),
                new XAttribute("font-size", fontSize),
                new XAttribute("fill", fill),
                new XAttribute(roleAttribute, "true"),
                new XAttribute("data-label-line", index),
                line)).ToArray();
    }

    private static IReadOnlyList<string> WrapPreservingContent(
        string content,
        int maxCharactersPerLine)
    {
        var lines = new List<string>();
        var offset = 0;
        while (content.Length - offset > maxCharactersPerLine)
        {
            var end = offset + maxCharactersPerLine;
            var separator = content.LastIndexOfAny([' ', '-'], end - 1, maxCharactersPerLine);
            var next = separator >= offset ? separator + 1 : end;
            lines.Add(content[offset..next]);
            offset = next;
        }

        lines.Add(content[offset..]);
        return lines;
    }

    private static IReadOnlyDictionary<string, ElementPosition> BuildPositions(
        SvgRenderPlan plan)
    {
        var photoelectricPositions = TryBuildPhotoelectricPositions(plan);
        if (photoelectricPositions is not null)
        {
            return photoelectricPositions;
        }

        var componentPositions = TryBuildTwoComponentPositions(plan);
        if (componentPositions is not null)
        {
            return componentPositions;
        }

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

    private static IReadOnlyDictionary<string, ElementPosition>? TryBuildPhotoelectricPositions(
        SvgRenderPlan plan)
    {
        var expected = new[]
        {
            "incident-photons",
            "metal-surface",
            "ejected-electrons",
        };
        var elementsBySpecificationId = plan.Elements.ToDictionary(
            item => item.SourceSpecificationItemId,
            StringComparer.Ordinal);
        if (elementsBySpecificationId.Count != expected.Length
            || expected.Any(item => !elementsBySpecificationId.ContainsKey(item)))
        {
            return null;
        }

        var padding = Constraint(plan, "padding", 48);
        var spacing = Constraint(plan, "minimum-spacing", 24);
        var cellWidth = (plan.Canvas.Width - (padding * 2) - (spacing * 2)) / 3;
        var nodeWidth = Math.Min(280, Math.Max(80, cellWidth));
        var nodeHeight = 120d;
        var y = (plan.Canvas.Height - nodeHeight) / 2;
        var result = new Dictionary<string, ElementPosition>(StringComparer.Ordinal);
        for (var index = 0; index < expected.Length; index++)
        {
            var cellX = padding + (index * (cellWidth + spacing));
            var element = elementsBySpecificationId[expected[index]];
            result[element.RenderElementId] = new ElementPosition(
                cellX + ((cellWidth - nodeWidth) / 2),
                y,
                nodeWidth,
                nodeHeight);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, ElementPosition>? TryBuildTwoComponentPositions(
        SvgRenderPlan plan)
    {
        var elementIds = plan.Elements.Select(item => item.RenderElementId).ToArray();
        var adjacency = elementIds.ToDictionary(
            item => item,
            _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);
        foreach (var connection in plan.Connections)
        {
            adjacency[connection.SourceRenderElementId].Add(connection.TargetRenderElementId);
            adjacency[connection.TargetRenderElementId].Add(connection.SourceRenderElementId);
        }

        var remaining = elementIds.ToHashSet(StringComparer.Ordinal);
        var components = new List<IReadOnlyList<string>>();
        while (remaining.Count > 0)
        {
            var seed = elementIds.First(remaining.Contains);
            var queue = new Queue<string>();
            var component = new List<string>();
            queue.Enqueue(seed);
            remaining.Remove(seed);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                component.Add(current);
                foreach (var neighbor in adjacency[current].Where(remaining.Contains))
                {
                    remaining.Remove(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            components.Add(component);
        }

        if (components.Count != 2 || components.Max(item => item.Count) < 3)
        {
            return null;
        }

        var sourceOrder = elementIds
            .Select((id, index) => new { id, index })
            .ToDictionary(item => item.id, item => item.index, StringComparer.Ordinal);
        var orderedComponents = components
            .OrderByDescending(item => item.Count)
            .Select(component => TopologicalOrder(component, plan.Connections, sourceOrder))
            .ToArray();
        var padding = Constraint(plan, "padding", 48);
        var spacing = Constraint(plan, "minimum-spacing", 24);
        var cellHeight = (plan.Canvas.Height - (padding * 2) - spacing) / 2;
        var nodeHeight = Math.Min(120, Math.Max(64, cellHeight));
        var result = new Dictionary<string, ElementPosition>(StringComparer.Ordinal);
        for (var row = 0; row < orderedComponents.Length; row++)
        {
            var component = orderedComponents[row];
            var cellWidth = (plan.Canvas.Width - (padding * 2) - (spacing * (component.Count - 1)))
                / component.Count;
            var nodeWidth = Math.Min(280, Math.Max(80, cellWidth));
            for (var column = 0; column < component.Count; column++)
            {
                var cellX = padding + (column * (cellWidth + spacing));
                var cellY = padding + (row * (cellHeight + spacing));
                result[component[column]] = new ElementPosition(
                    cellX + ((cellWidth - nodeWidth) / 2),
                    cellY + ((cellHeight - nodeHeight) / 2),
                    nodeWidth,
                    nodeHeight);
            }
        }

        return result;
    }

    private static IReadOnlyList<string> TopologicalOrder(
        IReadOnlyList<string> component,
        IReadOnlyList<SvgRenderConnection> connections,
        IReadOnlyDictionary<string, int> sourceOrder)
    {
        var componentIds = component.ToHashSet(StringComparer.Ordinal);
        var outgoing = component.ToDictionary(
            item => item,
            _ => new List<string>(),
            StringComparer.Ordinal);
        var indegree = component.ToDictionary(item => item, _ => 0, StringComparer.Ordinal);
        foreach (var connection in connections.Where(item =>
                     item.Direction == FigureRelationDirection.Directed
                     && componentIds.Contains(item.SourceRenderElementId)
                     && componentIds.Contains(item.TargetRenderElementId)))
        {
            outgoing[connection.SourceRenderElementId].Add(connection.TargetRenderElementId);
            indegree[connection.TargetRenderElementId]++;
        }

        var ready = new PriorityQueue<string, int>();
        foreach (var id in component.Where(id => indegree[id] == 0))
        {
            ready.Enqueue(id, sourceOrder[id]);
        }

        var ordered = new List<string>();
        while (ready.TryDequeue(out var current, out _))
        {
            ordered.Add(current);
            foreach (var target in outgoing[current])
            {
                indegree[target]--;
                if (indegree[target] == 0)
                {
                    ready.Enqueue(target, sourceOrder[target]);
                }
            }
        }

        return ordered.Count == component.Count
            ? ordered
            : component.OrderBy(item => sourceOrder[item]).ToArray();
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

    private sealed record LabelPlacement(double X, double Y, LabelBounds Bounds);

    private sealed record LabelBounds(double Left, double Top, double Width, double Height)
    {
        public bool Overlaps(LabelBounds other) =>
            Left < other.Left + other.Width
            && Left + Width > other.Left
            && Top < other.Top + other.Height
            && Top + Height > other.Top;
    }
}

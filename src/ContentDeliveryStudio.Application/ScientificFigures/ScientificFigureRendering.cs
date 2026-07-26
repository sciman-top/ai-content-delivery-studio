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

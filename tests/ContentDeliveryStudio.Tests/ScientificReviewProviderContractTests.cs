using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Tests;

public sealed class ScientificReviewProviderContractTests
{
    [Fact]
    public void SemanticRequest_ContainsOnlyApprovedClaimMinimumEvidenceSpecAndRenderSummary()
    {
        var fixture = ScientificReviewTestFixture.Create();

        var request = ScientificSemanticReviewRequest.Create(
            fixture.Understanding,
            fixture.Specification,
            fixture.Plan);

        var claim = Assert.Single(request.ApprovedClaims);
        Assert.Equal(ScientificClaimStatus.Accepted, fixture.Understanding.Claims
            .Single(item => item.ClaimId == claim.ClaimId).Status);
        var evidence = Assert.Single(claim.Evidence);
        Assert.Equal("block-dynamics", evidence.SourceBlockId);
        Assert.Equal(ClaimEvidenceRole.Support, evidence.Role);
        Assert.Equal(fixture.Specification, request.Specification);
        Assert.Equal(fixture.Plan.PlanId, request.RenderSummary.PlanId);
        Assert.Equal(
            fixture.Plan.Elements.Count,
            request.RenderSummary.Elements.Count);
        Assert.DoesNotContain(
            request.RenderSummary.Elements,
            item => item.SpecificationItemId.StartsWith("render-", StringComparison.Ordinal));
    }

    [Fact]
    public void VisualRequest_AcceptsFullResolutionOutputAndTypedRegionCrops()
    {
        var fixture = ScientificReviewTestFixture.Create();

        var request = fixture.VisualRequest;

        Assert.Equal(1200, request.FullResolutionOutput.PixelWidth);
        var crop = Assert.Single(request.RegionCrops);
        Assert.Equal(ScientificVisualRegionKind.Element, crop.Kind);
        Assert.Equal("element-force", crop.ResponsibleItemId);
    }

    [Fact]
    public void VisualRequest_RejectsDownscaledOutput()
    {
        var image = ScientificReviewTestFixture.FullResolutionImage() with
        {
            PixelWidth = 600,
            PixelHeight = 400,
        };

        var error = Assert.Throws<ArgumentException>(() =>
            ScientificVisualReviewRequest.Create(image, []));

        Assert.Contains("full-resolution", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record ScientificReviewTestFixture(
    ScientificDocumentUnderstanding Understanding,
    ScientificFigureSpec Specification,
    SvgRenderPlan Plan,
    ScientificSemanticReviewRequest SemanticRequest,
    ScientificVisualReviewRequest VisualRequest)
{
    public static ScientificReviewTestFixture Create()
    {
        var understanding = ScientificFigureTestFixture.ReadyUnderstanding();
        var claim = Assert.Single(understanding.Claims);
        var evidence = Assert.Single(claim.SupportingEvidence);
        var element = ScientificFigureTestFixture.RequiredElement(claim, evidence);
        var specification = ScientificFigureTestFixture.CreateSpec(
            understanding,
            [element],
            [],
            []);
        var workflow = ScientificFigureWorkflow.Create(specification)
            .ApproveGate1("reviewer", "Approved.", DateTimeOffset.UtcNow);
        var plan = new ScientificFigureSpecCompiler().Compile(workflow);
        var semanticRequest = ScientificSemanticReviewRequest.Create(
            understanding,
            specification,
            plan);
        var crop = new ScientificVisualRegionCrop(
            "crop-element-force",
            ScientificVisualRegionKind.Element,
            element.ElementId,
            X: 100,
            Y: 100,
            Width: 300,
            Height: 200,
            "image/png",
            [1, 2, 3],
            new ScientificExpectedVisualCheck(
                "expected-element-force",
                element.ElementId,
                element.ScientificMeaning,
                element.LabelOrFormula,
                RelationshipDirection: null,
                Conditions: [],
                ForbiddenContent: [],
                EvidenceSourceBlockIds: [evidence.SourceBlockId],
                ScientificExpectedVisualAuthority.ApprovedSpecification));
        var visualRequest = ScientificVisualReviewRequest.Create(
            FullResolutionImage(),
            [crop]);
        return new ScientificReviewTestFixture(
            understanding,
            specification,
            plan,
            semanticRequest,
            visualRequest);
    }

    public static ScientificFullResolutionImage FullResolutionImage()
    {
        return new ScientificFullResolutionImage(
            "png",
            "image/png",
            [1, 2, 3, 4],
            $"sha256:{new string('a', 64)}",
            PixelWidth: 1200,
            PixelHeight: 800,
            SourcePixelWidth: 1200,
            SourcePixelHeight: 800);
    }
}

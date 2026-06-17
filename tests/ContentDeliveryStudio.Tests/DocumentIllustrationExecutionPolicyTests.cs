using ContentDeliveryStudio.Core.Documents;
using ContentDeliveryStudio.Core.Providers;

namespace ContentDeliveryStudio.Tests;

public sealed class DocumentIllustrationExecutionPolicyTests
{
    [Fact]
    public void Defaults_UseBoundedSourceEvidencePlanning()
    {
        Assert.Equal(6000, DocumentIllustrationExecutionPolicy.DefaultMaxSourceTextCharacters);
        Assert.Equal(12, DocumentIllustrationExecutionPolicy.DefaultMaxEvidenceRows);
    }

    [Fact]
    public void EstimateInputWeight_CountsSourceAndEvidenceFields()
    {
        var request = new DocumentIllustrationPlanningRequest(
            "Quantum teaching note",
            "Teachers need an intuitive explanation of superposition.",
            "teachers",
            DocumentFamily.Educational,
            IllustrationStrictnessLevel.Educational,
            ["Introduction", "Analogy"],
            ["Key claim one", "Key claim two"],
            ["avoid fake lab data"]);

        Assert.True(DocumentIllustrationExecutionPolicy.EstimateInputWeight(request) > 0);
    }
}

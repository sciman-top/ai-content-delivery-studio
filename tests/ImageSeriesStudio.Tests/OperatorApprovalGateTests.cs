using ImageSeriesStudio.Core.Operators;

namespace ImageSeriesStudio.Tests;

public sealed class OperatorApprovalGateTests
{
    [Fact]
    public void OperatorAction_ApproveRecordsApprovalAuditForMediumAndHighRiskActions()
    {
        var action = CreateAction(OperatorRiskLevel.Medium);
        var approvedAt = DateTimeOffset.Parse("2026-06-03T18:00:00Z");

        var approved = action.Approve(
            approvedBy: "human-reviewer",
            approvalNote: "Approved after dry-run preview.",
            approvedAt);

        Assert.Equal(OperatorActionStatus.Approved, approved.Status);
        Assert.Equal("human-reviewer", approved.ApprovedBy);
        Assert.Equal("Approved after dry-run preview.", approved.ApprovalNote);
        Assert.Equal(approvedAt, approved.ApprovedAt);
    }

    [Fact]
    public void OperatorAction_RejectsApprovalForLowRiskOrAlreadyApprovedActions()
    {
        var lowRisk = CreateAction(OperatorRiskLevel.Low);
        var mediumRisk = CreateAction(OperatorRiskLevel.Medium);
        var approved = mediumRisk.Approve(
            "human-reviewer",
            "Approved once.",
            DateTimeOffset.Parse("2026-06-03T18:00:00Z"));

        Assert.Throws<InvalidOperationException>(() =>
            lowRisk.Approve("human-reviewer", "No approval needed.", DateTimeOffset.Parse("2026-06-03T18:00:00Z")));
        Assert.Throws<InvalidOperationException>(() =>
            approved.Approve("human-reviewer", "Approve again.", DateTimeOffset.Parse("2026-06-03T18:01:00Z")));
    }

    private static OperatorAction CreateAction(OperatorRiskLevel riskLevel)
    {
        return OperatorAction.CreateDraft(
            Guid.NewGuid(),
            repairPlanStepOrder: 1,
            toolAdapterId: "deterministic-text-composer",
            displayName: "Compose deterministic labels",
            riskLevel,
            dryRunSupported: true,
            inputs: new Dictionary<string, string> { ["artifactId"] = "artifact-001" },
            expectedOutputs: ["workspace/render/final.png"],
            sideEffects: ["Writes a composed image under workspace outputs."],
            timeout: TimeSpan.FromMinutes(2),
            cleanupPath: "workspace/render/.tmp",
            DateTimeOffset.Parse("2026-06-03T17:00:00Z"));
    }
}

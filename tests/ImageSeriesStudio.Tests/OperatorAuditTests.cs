using ImageSeriesStudio.Core.Operators;

namespace ImageSeriesStudio.Tests;

public sealed class OperatorAuditTests
{
    [Fact]
    public void OperatorAction_CreateDraftRecordsRiskDryRunSideEffectsAndApprovalStatus()
    {
        var repairPlanId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-06-03T17:00:00Z");

        var action = OperatorAction.CreateDraft(
            repairPlanId,
            repairPlanStepOrder: 2,
            toolAdapterId: "deterministic-text-composer",
            displayName: "Compose deterministic labels",
            OperatorRiskLevel.Medium,
            dryRunSupported: true,
            inputs: new Dictionary<string, string>
            {
                ["artifactId"] = "artifact-001",
                ["sourcePath"] = "workspace/render/background.png",
            },
            expectedOutputs: ["workspace/render/final.png"],
            sideEffects: ["Writes a composed image under the workspace render folder."],
            timeout: TimeSpan.FromMinutes(2),
            cleanupPath: "workspace/render/.tmp",
            createdAt);

        Assert.Equal(repairPlanId, action.RepairPlanId);
        Assert.Equal(2, action.RepairPlanStepOrder);
        Assert.Equal("deterministic-text-composer", action.ToolAdapterId);
        Assert.Equal(OperatorRiskLevel.Medium, action.RiskLevel);
        Assert.True(action.DryRunSupported);
        Assert.True(action.RequiresApproval);
        Assert.Equal(OperatorActionStatus.PendingApproval, action.Status);
        Assert.Equal("artifact-001", action.Inputs["artifactId"]);
        Assert.Equal(["workspace/render/final.png"], action.ExpectedOutputs);
        Assert.Equal(["Writes a composed image under the workspace render folder."], action.SideEffects);
        Assert.Equal(TimeSpan.FromMinutes(2), action.Timeout);
        Assert.Equal("workspace/render/.tmp", action.CleanupPath);
        Assert.Equal(createdAt, action.CreatedAt);
    }

    [Fact]
    public void OperatorAction_CreateDraftKeepsLowRiskActionsReadyWithoutApproval()
    {
        var action = OperatorAction.CreateDraft(
            Guid.NewGuid(),
            repairPlanStepOrder: 1,
            toolAdapterId: "artifact-validator",
            displayName: "Validate artifact manifest",
            OperatorRiskLevel.Low,
            dryRunSupported: true,
            inputs: new Dictionary<string, string> { ["manifestPath"] = "delivery/manifest.json" },
            expectedOutputs: ["validation report"],
            sideEffects: ["Reads manifest and produces validation summary."],
            timeout: TimeSpan.FromSeconds(30),
            cleanupPath: null,
            DateTimeOffset.Parse("2026-06-03T17:00:00Z"));

        Assert.False(action.RequiresApproval);
        Assert.Equal(OperatorActionStatus.Ready, action.Status);
    }

    [Fact]
    public void OperatorRun_RecordsDryRunExecutionAndCompletion()
    {
        var actionId = Guid.NewGuid();
        var startedAt = DateTimeOffset.Parse("2026-06-03T17:05:00Z");
        var completedAt = startedAt.AddSeconds(8);

        var run = OperatorRun.Start(
            actionId,
            toolAdapterId: "artifact-validator",
            dryRun: true,
            inputSnapshot: """{"manifestPath":"delivery/manifest.json"}""",
            startedAt);
        var completed = run.CompleteSucceeded(
            outputSummary: "Manifest dry-run validation succeeded.",
            completedAt);

        Assert.Equal(actionId, completed.OperatorActionId);
        Assert.Equal("artifact-validator", completed.ToolAdapterId);
        Assert.True(completed.DryRun);
        Assert.Equal(OperatorRunStatus.Succeeded, completed.Status);
        Assert.Equal(startedAt, completed.StartedAt);
        Assert.Equal(completedAt, completed.CompletedAt);
        Assert.Equal("Manifest dry-run validation succeeded.", completed.OutputSummary);
        Assert.Null(completed.ErrorMessage);
    }

    [Fact]
    public void OperatorRun_RequiresFailureMessageAndCompletionAfterStart()
    {
        var run = OperatorRun.Start(
            Guid.NewGuid(),
            toolAdapterId: "artifact-validator",
            dryRun: true,
            inputSnapshot: "{}",
            DateTimeOffset.Parse("2026-06-03T17:05:00Z"));

        Assert.Throws<ArgumentException>(() =>
            run.CompleteFailed("", run.StartedAt.AddSeconds(1)));
        Assert.Throws<ArgumentException>(() =>
            run.CompleteSucceeded("finished", run.StartedAt.AddSeconds(-1)));
    }
}

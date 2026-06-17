using ContentDeliveryStudio.Application.RemoteWorkflows;
using ContentDeliveryStudio.Core.Operators;
using ContentDeliveryStudio.Infrastructure.RemoteWorkflows;

namespace ContentDeliveryStudio.Tests;

public sealed class RemoteWorkflowEngineAdapterTests
{
    [Fact]
    public void RemoteWorkflowEngineDescriptor_CapturesRiskStateAndIoBoundary()
    {
        var descriptor = RemoteWorkflowEngineDescriptor.Create(
            "Hosted Engine",
            "Hosted Engine",
            RemoteWorkflowEngineKind.HostedWorkflowEngine,
            OperatorRiskLevel.Medium,
            dryRunSupported: true,
            storesRemoteStateByDefault: false,
            inputNames: ["workflowSpecPath", "sourceEvidencePath"],
            outputNames: ["remoteRunReceiptPath"],
            sideEffects: ["May call a hosted workflow API after approval."],
            defaultTimeout: TimeSpan.FromMinutes(3));

        Assert.Equal("hosted-engine", descriptor.Id);
        Assert.Equal(RemoteWorkflowEngineKind.HostedWorkflowEngine, descriptor.Kind);
        Assert.Equal(OperatorRiskLevel.Medium, descriptor.RiskLevel);
        Assert.True(descriptor.RequiresApproval);
        Assert.True(descriptor.DryRunSupported);
        Assert.False(descriptor.StoresRemoteStateByDefault);
        Assert.Equal(["workflowSpecPath", "sourceEvidencePath"], descriptor.InputNames);
        Assert.Equal(["remoteRunReceiptPath"], descriptor.OutputNames);
    }

    [Fact]
    public void RemoteWorkflowRunRequest_RejectsMissingInputsAndUnsupportedDryRun()
    {
        var descriptor = RemoteWorkflowEngineDescriptor.Create(
            "hosted-engine",
            "Hosted Engine",
            RemoteWorkflowEngineKind.HostedWorkflowEngine,
            OperatorRiskLevel.Medium,
            dryRunSupported: false,
            storesRemoteStateByDefault: false,
            inputNames: ["workflowSpecPath"],
            outputNames: ["remoteRunReceiptPath"],
            sideEffects: ["May call a hosted workflow API after approval."],
            defaultTimeout: TimeSpan.FromMinutes(3));

        Assert.Throws<InvalidOperationException>(() =>
            RemoteWorkflowRunRequest.Create(
                descriptor,
                "courseware-pack",
                dryRun: true,
                Guid.NewGuid(),
                new Dictionary<string, string> { ["workflowSpecPath"] = "workspace/spec.json" },
                DateTimeOffset.Parse("2026-06-07T16:00:00Z")));
        Assert.Throws<ArgumentException>(() =>
            RemoteWorkflowRunRequest.Create(
                descriptor,
                "courseware-pack",
                dryRun: false,
                Guid.NewGuid(),
                new Dictionary<string, string>(),
                DateTimeOffset.Parse("2026-06-07T16:00:00Z")));
    }

    [Fact]
    public void RemoteWorkflowRunResult_RejectsDryRunWithRemoteCalls()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RemoteWorkflowRunResult.Create(
                "hosted-engine",
                "courseware-pack",
                dryRun: true,
                remoteCallsMade: true,
                "remote-123",
                new Dictionary<string, string> { ["remoteRunReceiptPath"] = "workspace/receipt.json" },
                [],
                "Invalid dry-run result.",
                DateTimeOffset.Parse("2026-06-07T16:00:00Z"),
                DateTimeOffset.Parse("2026-06-07T16:00:01Z")));
    }

    [Fact]
    public async Task FakeRemoteWorkflowEngineAdapter_ReturnsLocalReceiptWithoutRemoteCalls()
    {
        var adapter = new FakeRemoteWorkflowEngineAdapter();
        var correlationId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var request = RemoteWorkflowRunRequest.Create(
            adapter.Descriptor,
            "article-illustration-pack",
            dryRun: true,
            correlationId,
            new Dictionary<string, string>
            {
                ["workflowSpecPath"] = "workspace/workflows/article.json",
                ["sourceEvidencePath"] = "workspace/evidence/source.json",
            },
            DateTimeOffset.Parse("2026-06-07T16:00:00Z"));

        var result = await adapter.RunAsync(request, CancellationToken.None);

        Assert.Equal("fake-remote-workflow-engine", result.AdapterId);
        Assert.Equal("article-illustration-pack", result.WorkflowKey);
        Assert.True(result.DryRun);
        Assert.False(result.RemoteCallsMade);
        Assert.StartsWith("fake-", result.RemoteRunId, StringComparison.Ordinal);
        Assert.Equal(
            "workspace/remote-workflows/11111111222233334444555555555555/receipt.json",
            result.Outputs["remoteRunReceiptPath"]);
        Assert.Contains(result.Warnings, warning => warning.Contains("did not call", StringComparison.OrdinalIgnoreCase));
    }
}

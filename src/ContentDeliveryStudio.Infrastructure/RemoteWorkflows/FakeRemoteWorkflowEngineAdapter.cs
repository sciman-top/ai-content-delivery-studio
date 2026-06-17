using ContentDeliveryStudio.Application.RemoteWorkflows;
using ContentDeliveryStudio.Core.Operators;

namespace ContentDeliveryStudio.Infrastructure.RemoteWorkflows;

public sealed class FakeRemoteWorkflowEngineAdapter : IRemoteWorkflowEngineAdapter
{
    public FakeRemoteWorkflowEngineAdapter()
    {
        Descriptor = RemoteWorkflowEngineDescriptor.Create(
            "fake-remote-workflow-engine",
            "Fake Remote Workflow Engine",
            RemoteWorkflowEngineKind.FakeNoNetwork,
            OperatorRiskLevel.Medium,
            dryRunSupported: true,
            storesRemoteStateByDefault: false,
            inputNames: ["workflowSpecPath", "sourceEvidencePath"],
            outputNames: ["remoteRunReceiptPath", "artifactManifestPath"],
            sideEffects: ["Writes local fake remote-workflow receipts only; performs no network calls."],
            defaultTimeout: TimeSpan.FromMinutes(2));
    }

    public RemoteWorkflowEngineDescriptor Descriptor { get; }

    public Task<RemoteWorkflowRunResult> RunAsync(
        RemoteWorkflowRunRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.Descriptor.Id, Descriptor.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Remote workflow descriptor mismatch: {request.Descriptor.Id}");
        }

        var startedAt = request.RequestedAt;
        var completedAt = startedAt.AddMilliseconds(1);
        var receiptRoot = $"workspace/remote-workflows/{request.CorrelationId:N}";

        return Task.FromResult(RemoteWorkflowRunResult.Create(
            Descriptor.Id,
            request.WorkflowKey,
            request.DryRun,
            remoteCallsMade: false,
            remoteRunId: $"fake-{request.CorrelationId:N}",
            outputs: new Dictionary<string, string>
            {
                ["remoteRunReceiptPath"] = $"{receiptRoot}/receipt.json",
                ["artifactManifestPath"] = $"{receiptRoot}/artifact-manifest.json",
            },
            warnings:
            [
                "Fake remote workflow adapter did not call any external workflow engine.",
            ],
            summary: "Simulated remote workflow execution boundary without network side effects.",
            startedAt,
            completedAt));
    }
}

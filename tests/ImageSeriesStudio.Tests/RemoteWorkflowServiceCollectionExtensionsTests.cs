using ImageSeriesStudio.Application.RemoteWorkflows;
using ImageSeriesStudio.Infrastructure.RemoteWorkflows;
using Microsoft.Extensions.DependencyInjection;

namespace ImageSeriesStudio.Tests;

public sealed class RemoteWorkflowServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBuiltInRemoteWorkflowEngineAdapters_RegistersFakeNoNetworkBoundary()
    {
        var services = new ServiceCollection();
        services.AddBuiltInRemoteWorkflowEngineAdapters();

        using var serviceProvider = services.BuildServiceProvider();
        var adapters = serviceProvider.GetServices<IRemoteWorkflowEngineAdapter>().ToArray();

        var adapter = Assert.Single(adapters);
        Assert.IsType<FakeRemoteWorkflowEngineAdapter>(adapter);
        Assert.Equal("fake-remote-workflow-engine", adapter.Descriptor.Id);
        Assert.Equal(RemoteWorkflowEngineKind.FakeNoNetwork, adapter.Descriptor.Kind);
        Assert.False(adapter.Descriptor.StoresRemoteStateByDefault);
        Assert.True(adapter.Descriptor.RequiresApproval);
    }

    [Fact]
    public async Task AddBuiltInRemoteWorkflowEngineAdapters_ResolvesRunnableAdapterWithoutNetworkCalls()
    {
        var services = new ServiceCollection();
        services.AddBuiltInRemoteWorkflowEngineAdapters();

        using var serviceProvider = services.BuildServiceProvider();
        var adapter = serviceProvider.GetRequiredService<IRemoteWorkflowEngineAdapter>();
        var correlationId = Guid.Parse("99999999-8888-7777-6666-555555555555");
        var request = RemoteWorkflowRunRequest.Create(
            adapter.Descriptor,
            "courseware-pack",
            dryRun: true,
            correlationId,
            new Dictionary<string, string>
            {
                ["workflowSpecPath"] = "workspace/workflows/courseware.json",
                ["sourceEvidencePath"] = "workspace/evidence/courseware.json",
            },
            DateTimeOffset.Parse("2026-06-14T10:00:00Z"));

        var result = await adapter.RunAsync(request, CancellationToken.None);

        Assert.False(result.RemoteCallsMade);
        Assert.Equal(
            "workspace/remote-workflows/99999999888877776666555555555555/receipt.json",
            result.Outputs["remoteRunReceiptPath"]);
    }
}

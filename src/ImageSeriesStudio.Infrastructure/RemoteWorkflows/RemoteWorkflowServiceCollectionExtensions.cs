using ImageSeriesStudio.Application.RemoteWorkflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImageSeriesStudio.Infrastructure.RemoteWorkflows;

public static class RemoteWorkflowServiceCollectionExtensions
{
    public static IServiceCollection AddBuiltInRemoteWorkflowEngineAdapters(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRemoteWorkflowEngineAdapter, FakeRemoteWorkflowEngineAdapter>());
        return services;
    }
}

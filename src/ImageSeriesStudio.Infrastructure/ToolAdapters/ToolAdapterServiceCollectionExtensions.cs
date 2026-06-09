using ImageSeriesStudio.Application.ToolAdapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImageSeriesStudio.Infrastructure.ToolAdapters;

public static class ToolAdapterServiceCollectionExtensions
{
    public static IServiceCollection AddBuiltInLocalToolAdapters(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IToolAdapter, ArtifactValidationToolAdapter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IToolAdapter, DeterministicTextCompositionToolAdapter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IToolAdapter, OpenAiLaunchPreflightToolAdapter>());
        services.TryAddSingleton<LowRiskAutoRepairService>(serviceProvider =>
            new LowRiskAutoRepairService(serviceProvider.GetServices<IToolAdapter>().ToArray()));

        return services;
    }
}

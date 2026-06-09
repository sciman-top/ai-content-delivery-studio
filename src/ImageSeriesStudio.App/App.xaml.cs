using System.IO;
using System.Net.Http;
using ImageSeriesStudio.Application.Composition;
using ImageSeriesStudio.Application.Delivery;
using System.Windows;
using ImageSeriesStudio.Application.Projects;
using ImageSeriesStudio.Application.ToolAdapters;
using ImageSeriesStudio.App.Services;
using ImageSeriesStudio.App.ViewModels;
using ImageSeriesStudio.App.Telemetry;
using ImageSeriesStudio.Application.Localization;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Composition;
using ImageSeriesStudio.Infrastructure.Delivery;
using ImageSeriesStudio.Infrastructure.Fakes;
using ImageSeriesStudio.Infrastructure.OpenAI;
using ImageSeriesStudio.Infrastructure.Persistence;
using ImageSeriesStudio.Infrastructure.ToolAdapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImageSeriesStudio.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = Host.CreateApplicationBuilder(e.Args);
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageSeriesStudio");
        Directory.CreateDirectory(dataDirectory);
        var databasePath = Path.Combine(dataDirectory, "studio.sqlite");

        builder.Services.AddDbContext<AppDbContext>(
            options => options.UseSqlite($"Data Source={databasePath}"),
            contextLifetime: ServiceLifetime.Transient);
        builder.Services.AddSingleton<LocalizationService>();
        builder.Services.AddTransient<IProjectRepository, EfProjectRepository>();
        builder.Services.AddTransient<ProjectApplicationService>();
        builder.Services.AddSingleton<IProviderCenterConfigurationService, DotEnvProviderCenterConfigurationService>();
        builder.Services.AddOpenAiProviderHttpClient(new OpenAiProviderOptions());
        builder.AddImageSeriesStudioOpenTelemetry();
        builder.Services.AddTransient(serviceProvider => new ProviderHealthCheckService(
            serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(OpenAiHttpClientNames.Provider),
            serviceProvider.GetRequiredService<IOpenAiSecretStore>()));
        builder.Services.AddSingleton<IProviderCenterHealthCheckService, DotEnvProviderCenterHealthCheckService>();
        builder.Services.AddSingleton<ITextPlanningProvider, FakeTextPlanningProvider>();
        builder.Services.AddSingleton<FakeImageGenerationProvider>();
        builder.Services.AddSingleton<IImageGenerationProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<FakeImageGenerationProvider>());
        builder.Services.AddSingleton<IImageEditProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<FakeImageGenerationProvider>());
        builder.Services.AddSingleton<IVisionReviewProvider, FakeVisionReviewProvider>();
        // V1 keeps exact label and formula rendering on the local deterministic path instead of asking image generation to render trusted text.
        builder.Services.AddSingleton<IDeterministicTextComposer, SkiaDeterministicTextComposer>();
        // Keep the currently executable local operator adapters wired into the desktop host, including the
        // read-only OpenAI launch preflight that only inspects readiness and writes local diagnostics.
        builder.Services.AddBuiltInLocalToolAdapters();
        builder.Services.AddSingleton<IDeliveryPackageWriter, DeliveryPackageWriter>();
        builder.Services.AddTransient<ProviderCenterViewModel>();
        builder.Services.AddTransient<MainWindowViewModel>();
        builder.Services.AddTransient<MainWindow>();

        _host = builder.Build();
        await _host.StartAsync();
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

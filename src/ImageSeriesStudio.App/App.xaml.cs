using System.Windows;
using ImageSeriesStudio.App.ViewModels;
using ImageSeriesStudio.Core.Providers;
using ImageSeriesStudio.Infrastructure.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImageSeriesStudio.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = Host.CreateApplicationBuilder(e.Args);
        builder.Services.AddSingleton<ITextPlanningProvider, FakeTextPlanningProvider>();
        builder.Services.AddSingleton<IImageGenerationProvider, FakeImageGenerationProvider>();
        builder.Services.AddSingleton<IVisionReviewProvider, FakeVisionReviewProvider>();
        builder.Services.AddSingleton<MainWindowViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
        await _host.StartAsync();

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

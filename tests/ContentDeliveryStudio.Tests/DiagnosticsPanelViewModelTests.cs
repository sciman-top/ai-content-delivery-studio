using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.App.ViewModels;
using ContentDeliveryStudio.Application.Diagnostics;
using ContentDeliveryStudio.Application.Localization;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class DiagnosticsPanelViewModelTests
{
    [Fact]
    public async Task BrowseAndExport_UpdatesPathAndRestoresCommandAvailability()
    {
        var outputParent = Path.Combine(Path.GetTempPath(), $"diagnostics-panel-{Guid.NewGuid():N}");
        var writer = new RecordingWriter();
        var localization = new LocalizationService();
        localization.SetLanguage(LanguagePreference.English);
        var viewModel = new DiagnosticsPanelViewModel(
            localization,
            new DiagnosticsPackageApplicationService(new EmptyRepository(), writer),
            new StubSnapshotFactory(),
            new StubPicker(outputParent));

        Assert.False(viewModel.ExportCommand.CanExecute(null));

        await viewModel.BrowseCommand.ExecuteAsync(null);
        Assert.Equal(outputParent, viewModel.OutputParentDirectory);
        Assert.True(viewModel.ExportCommand.CanExecute(null));

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.ExportCommand.CanExecute(null));
        Assert.Contains("Diagnostics exported to", viewModel.StatusText, StringComparison.Ordinal);
        Assert.Single(writer.Requests);
    }

    [Fact]
    public void RefreshLocalizedText_UpdatesVisibleLabels()
    {
        var localization = new LocalizationService();
        var viewModel = new DiagnosticsPanelViewModel(
            localization,
            new DiagnosticsPackageApplicationService(new EmptyRepository(), new RecordingWriter()),
            new StubSnapshotFactory(),
            new StubPicker(null));

        localization.SetLanguage(LanguagePreference.Chinese);
        viewModel.RefreshLocalizedText();

        Assert.Equal("本地诊断", viewModel.Title);
        Assert.Equal("浏览", viewModel.BrowseText);
        Assert.Equal("导出", viewModel.ExportText);
        Assert.Equal("请选择用于创建本地诊断包的文件夹。", viewModel.StatusText);
    }

    private sealed class StubPicker(string? path) : IDiagnosticsDirectoryPickerService
    {
        public Task<string?> PickAsync(CancellationToken cancellationToken) => Task.FromResult(path);
    }

    private sealed class StubSnapshotFactory : IDesktopDiagnosticsSnapshotFactory
    {
        public Task<DesktopDiagnosticsSnapshots> CreateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new DesktopDiagnosticsSnapshots(
                new DiagnosticsApplicationSnapshot("App", "1.0", "Test", DateTimeOffset.MinValue),
                new DiagnosticsMachineSnapshot("OS", "Framework", "X64", "en-US", "en-US"),
                [],
                []));
        }
    }

    private sealed class RecordingWriter : IDiagnosticsPackageWriter
    {
        public List<DiagnosticsExportRequest> Requests { get; } = [];

        public Task<DiagnosticsExportResult> WriteAsync(
            DiagnosticsExportRequest request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new DiagnosticsExportResult(
                request.OutputDirectory,
                Path.Combine(request.OutputDirectory, "diagnostics.json"),
                Path.Combine(request.OutputDirectory, "diagnostics.md")));
        }
    }

    private sealed class EmptyRepository : IProjectRepository
    {
        public Task SaveAsync(ImageProject project, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<ImageProject?> LoadAsync(Guid projectId, CancellationToken cancellationToken) =>
            Task.FromResult<ImageProject?>(null);

        public Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProjectSummary>>([]);

        public Task SaveReviewResultAsync(Guid projectId, ReviewResult reviewResult, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<ReviewResult?> LoadLatestReviewResultAsync(Guid candidateImageId, CancellationToken cancellationToken) =>
            Task.FromResult<ReviewResult?>(null);
    }
}

using CommunityToolkit.Mvvm.Input;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private async Task CreateProjectAsync()
    {
        var result = await _operationGate.RunExclusiveAsync(
            cancellationToken => _workbenchInspectorCoordinator.CreateProjectAsync(
                NewProjectName,
                cancellationToken));

        if (!result.Executed || result.Value is null)
        {
            return;
        }

        ApplyWorkspaceResult(result.Value);
    }

    private bool CanCreateProject()
    {
        return CanRunMutation() && !string.IsNullOrWhiteSpace(NewProjectName);
    }

    [RelayCommand]
    private Task RefreshProviderCenterAsync()
    {
        return _operationGate.RunLatestWinsAsync(
            MainWindowOperationLane.ProviderCenterRefresh,
            cancellationToken => _workbenchInspectorCoordinator.RefreshProviderCenterAsync(
                ProviderCenter,
                cancellationToken));
    }

    [RelayCommand]
    private Task CheckProviderHealthAsync()
    {
        return _operationGate.RunLatestWinsAsync(
            MainWindowOperationLane.ProviderHealthCheck,
            cancellationToken => _workbenchInspectorCoordinator.CheckProviderHealthAsync(
                ProviderCenter,
                cancellationToken));
    }
}

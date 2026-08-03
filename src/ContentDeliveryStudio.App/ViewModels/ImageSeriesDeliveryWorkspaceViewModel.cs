using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.App.Services;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.App.ViewModels;

internal delegate Task<IReadOnlyList<DeliveryRowViewModel>?> ImageSeriesDeliveryRunner(
    FinalImageDeliveryCategory category,
    string deliveryRootPath);

public sealed partial class ImageSeriesDeliveryWorkspaceViewModel : ObservableObject
{
    private readonly ImageSeriesDeliveryRunner _exportDelivery;
    private readonly IFinalDeliveryRootPickerService? _finalDeliveryRootPickerService;
    private readonly Func<bool> _canMutate;
    private readonly Func<bool> _hasSelectedProject;
    private readonly Func<IReadOnlyList<GalleryRowViewModel>> _getGalleryRows;
    private readonly Func<IReadOnlyList<ReviewRowViewModel>> _getReviewRows;
    private readonly Func<IReadOnlyList<FinalImageDeliveryCategoryOptionViewModel>> _buildCategoryOptions;
    private readonly Action _projectionChanged;

    [ObservableProperty]
    private IReadOnlyList<DeliveryRowViewModel> _deliveryRows = [];

    [ObservableProperty]
    private IReadOnlyList<FinalImageDeliveryCategoryOptionViewModel> _finalDeliveryCategoryOptions = [];

    [ObservableProperty]
    private FinalImageDeliveryCategoryOptionViewModel? _selectedFinalDeliveryCategoryOption;

    [ObservableProperty]
    private string _finalDeliveryRootPath = LocalStudioDataPaths.ResolveDeliveryRoot();

    [ObservableProperty]
    private string _exportDeliveryText = string.Empty;

    [ObservableProperty]
    private string _finalDeliveryCategoryLabel = string.Empty;

    [ObservableProperty]
    private string _finalDeliveryRootLabel = string.Empty;

    [ObservableProperty]
    private string _browseFinalDeliveryRootText = string.Empty;

    [ObservableProperty]
    private string _finalDeliveryDestinationLabel = string.Empty;

    [ObservableProperty]
    private string _deliveryPackageColumn = string.Empty;

    [ObservableProperty]
    private string _deliveryManifestColumn = string.Empty;

    [ObservableProperty]
    private string _deliveryReportColumn = string.Empty;

    [ObservableProperty]
    private string _deliveryFinalImagesColumn = string.Empty;

    [ObservableProperty]
    private string _noDeliveryRowsText = string.Empty;

    internal ImageSeriesDeliveryWorkspaceViewModel(
        ImageSeriesDeliveryRunner exportDelivery,
        IFinalDeliveryRootPickerService? finalDeliveryRootPickerService,
        Func<bool> canMutate,
        Func<bool> hasSelectedProject,
        Func<IReadOnlyList<GalleryRowViewModel>> getGalleryRows,
        Func<IReadOnlyList<ReviewRowViewModel>> getReviewRows,
        Func<IReadOnlyList<FinalImageDeliveryCategoryOptionViewModel>> buildCategoryOptions,
        Action projectionChanged)
    {
        _exportDelivery = exportDelivery ?? throw new ArgumentNullException(nameof(exportDelivery));
        _finalDeliveryRootPickerService = finalDeliveryRootPickerService;
        _canMutate = canMutate ?? throw new ArgumentNullException(nameof(canMutate));
        _hasSelectedProject = hasSelectedProject ?? throw new ArgumentNullException(nameof(hasSelectedProject));
        _getGalleryRows = getGalleryRows ?? throw new ArgumentNullException(nameof(getGalleryRows));
        _getReviewRows = getReviewRows ?? throw new ArgumentNullException(nameof(getReviewRows));
        _buildCategoryOptions = buildCategoryOptions ?? throw new ArgumentNullException(nameof(buildCategoryOptions));
        _projectionChanged = projectionChanged ?? throw new ArgumentNullException(nameof(projectionChanged));
    }

    public bool HasDeliveryRows => DeliveryRows.Count > 0;

    public string FinalDeliveryDestinationPreview => ResolveFinalDeliveryDestinationPreview();

    internal void ApplyProjection(IReadOnlyList<DeliveryRowViewModel> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        DeliveryRows = rows;
    }

    internal void ApplyLocalization(MainWindowLocalizationPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var previousCategory = SelectedFinalDeliveryCategoryOption?.Category
            ?? FinalImageDeliveryCategory.ImageSeries;
        ExportDeliveryText = payload.ExportDeliveryText;
        FinalDeliveryCategoryLabel = payload.FinalDeliveryCategoryLabel;
        FinalDeliveryRootLabel = payload.FinalDeliveryRootLabel;
        BrowseFinalDeliveryRootText = payload.BrowseFinalDeliveryRootText;
        FinalDeliveryDestinationLabel = payload.FinalDeliveryDestinationLabel;
        DeliveryPackageColumn = payload.DeliveryPackageColumn;
        DeliveryManifestColumn = payload.DeliveryManifestColumn;
        DeliveryReportColumn = payload.DeliveryReportColumn;
        DeliveryFinalImagesColumn = payload.DeliveryFinalImagesColumn;
        NoDeliveryRowsText = payload.NoDeliveryRowsText;
        FinalDeliveryCategoryOptions = _buildCategoryOptions();
        SelectedFinalDeliveryCategoryOption = FinalDeliveryCategoryOptions.FirstOrDefault(
            option => option.Category == previousCategory)
            ?? FinalDeliveryCategoryOptions.FirstOrDefault();
    }

    internal void NotifyCommandStatesChanged()
    {
        BrowseFinalDeliveryRootCommand.NotifyCanExecuteChanged();
        ExportDeliveryCommand.NotifyCanExecuteChanged();
    }

    partial void OnDeliveryRowsChanged(IReadOnlyList<DeliveryRowViewModel> value)
    {
        OnPropertyChanged(nameof(HasDeliveryRows));
        _projectionChanged();
    }

    partial void OnSelectedFinalDeliveryCategoryOptionChanged(FinalImageDeliveryCategoryOptionViewModel? value)
    {
        OnPropertyChanged(nameof(FinalDeliveryDestinationPreview));
        ExportDeliveryCommand.NotifyCanExecuteChanged();
    }

    partial void OnFinalDeliveryRootPathChanged(string value)
    {
        OnPropertyChanged(nameof(FinalDeliveryDestinationPreview));
        ExportDeliveryCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanExportDelivery))]
    private async Task ExportDeliveryAsync()
    {
        if (SelectedFinalDeliveryCategoryOption is null)
        {
            return;
        }

        var rows = await _exportDelivery(
            SelectedFinalDeliveryCategoryOption.Category,
            FinalDeliveryRootPath);
        if (rows is not null)
        {
            ApplyProjection(rows);
        }
    }

    private bool CanExportDelivery()
    {
        return CanRunProjectMutation()
            && _getGalleryRows().Count > 0
            && SelectedFinalDeliveryCategoryOption is not null
            && !string.IsNullOrWhiteSpace(FinalDeliveryDestinationPreview)
            && _getReviewRows().Any(
                row => row.HumanApproved && row.Decision == ReviewDecision.Pass.ToString());
    }

    [RelayCommand(CanExecute = nameof(CanBrowseFinalDeliveryRoot))]
    private async Task BrowseFinalDeliveryRootAsync()
    {
        if (_finalDeliveryRootPickerService is null)
        {
            return;
        }

        var selectedRoot = await _finalDeliveryRootPickerService.PickAsync(
            FinalDeliveryRootPath,
            BrowseFinalDeliveryRootText,
            CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(selectedRoot))
        {
            FinalDeliveryRootPath = selectedRoot;
        }
    }

    private bool CanBrowseFinalDeliveryRoot()
    {
        return _canMutate() && _finalDeliveryRootPickerService is not null;
    }

    private bool CanRunProjectMutation()
    {
        return _canMutate() && _hasSelectedProject();
    }

    private string ResolveFinalDeliveryDestinationPreview()
    {
        if (SelectedFinalDeliveryCategoryOption is null)
        {
            return string.Empty;
        }

        try
        {
            return LocalStudioDataPaths.ResolveFinalDeliveryCategoryRoot(
                SelectedFinalDeliveryCategoryOption.Category,
                FinalDeliveryRootPath);
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }
}

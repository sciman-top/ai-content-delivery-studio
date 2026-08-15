using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Core.Providers;
using ContentDeliveryStudio.Core.References;

namespace ContentDeliveryStudio.App.ViewModels;

internal delegate Task<ImageSeriesGalleryEditResult?> ImageSeriesGalleryEditRunner(
    GalleryRowViewModel selectedRow,
    string editPrompt,
    string? maskPath,
    IReadOnlyList<GalleryRowViewModel> currentRows);

public sealed partial class ImageSeriesGalleryWorkspaceViewModel : ObservableObject
{
    private readonly ImageSeriesGalleryEditRunner _runFakeImageEdit;
    private readonly Func<bool> _canMutate;
    private readonly Func<bool> _hasSelectedProject;
    private readonly Action<IReadOnlyList<string>> _queueThumbnailWarmup;
    private readonly Action<ImageSeriesGalleryEditResult> _editApplied;
    private readonly Func<GalleryRowViewModel?, string> _buildSelectedCandidateSummary;
    private readonly Action _projectionChanged;
    private bool _supportsApprovedImageEdit;

    [ObservableProperty]
    private IReadOnlyList<GalleryRowViewModel> _galleryRows = [];

    [ObservableProperty]
    private GalleryRowViewModel? _selectedGalleryRow;

    [ObservableProperty]
    private string _galleryItemColumn = string.Empty;

    [ObservableProperty]
    private string _galleryImageColumn = string.Empty;

    [ObservableProperty]
    private string _galleryMetadataColumn = string.Empty;

    [ObservableProperty]
    private string _noGalleryRowsText = string.Empty;

    [ObservableProperty]
    private string _imageEditTitle = string.Empty;

    [ObservableProperty]
    private string _selectedCandidateLabel = string.Empty;

    [ObservableProperty]
    private string _imageEditPromptLabel = string.Empty;

    [ObservableProperty]
    private string _imageEditMaskPathLabel = string.Empty;

    [ObservableProperty]
    private string _runFakeImageEditText = string.Empty;

    [ObservableProperty]
    private string _runApprovedImageEditText = string.Empty;

    [ObservableProperty]
    private string _approvedImageEditAvailabilityText = string.Empty;

    [ObservableProperty]
    private string _newImageEditPrompt = string.Empty;

    [ObservableProperty]
    private string _newImageEditMaskPath = string.Empty;

    internal ImageSeriesGalleryWorkspaceViewModel(
        ImageSeriesGalleryEditRunner runFakeImageEdit,
        Func<bool> canMutate,
        Func<bool> hasSelectedProject,
        Action<IReadOnlyList<string>> queueThumbnailWarmup,
        Action<ImageSeriesGalleryEditResult> editApplied,
        Func<GalleryRowViewModel?, string> buildSelectedCandidateSummary,
        Action projectionChanged)
    {
        _runFakeImageEdit = runFakeImageEdit ?? throw new ArgumentNullException(nameof(runFakeImageEdit));
        _canMutate = canMutate ?? throw new ArgumentNullException(nameof(canMutate));
        _hasSelectedProject = hasSelectedProject ?? throw new ArgumentNullException(nameof(hasSelectedProject));
        _queueThumbnailWarmup = queueThumbnailWarmup ?? throw new ArgumentNullException(nameof(queueThumbnailWarmup));
        _editApplied = editApplied ?? throw new ArgumentNullException(nameof(editApplied));
        _buildSelectedCandidateSummary = buildSelectedCandidateSummary
            ?? throw new ArgumentNullException(nameof(buildSelectedCandidateSummary));
        _projectionChanged = projectionChanged ?? throw new ArgumentNullException(nameof(projectionChanged));
    }

    public bool HasGalleryRows => GalleryRows.Count > 0;

    public bool IsApprovedImageEditAvailable => false;

    public string SelectedCandidateSummary => _buildSelectedCandidateSummary(SelectedGalleryRow);

    internal void ApplyProjection(IReadOnlyList<GalleryRowViewModel> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var selectedCandidateId = SelectedGalleryRow?.CandidateImageId;
        GalleryRows = rows;
        SelectedGalleryRow = rows.FirstOrDefault(row => row.CandidateImageId == selectedCandidateId)
            ?? rows.FirstOrDefault();
    }

    internal void ApplyLocalization(MainWindowLocalizationPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        GalleryItemColumn = payload.GalleryItemColumn;
        GalleryImageColumn = payload.GalleryImageColumn;
        GalleryMetadataColumn = payload.GalleryMetadataColumn;
        NoGalleryRowsText = payload.NoGalleryRowsText;
        ImageEditTitle = payload.ImageEditTitle;
        SelectedCandidateLabel = payload.SelectedCandidateLabel;
        ImageEditPromptLabel = payload.ImageEditPromptLabel;
        ImageEditMaskPathLabel = payload.ImageEditMaskPathLabel;
        RunFakeImageEditText = payload.RunFakeImageEditText;
        RunApprovedImageEditText = payload.RunApprovedImageEditText;
        ApprovedImageEditAvailabilityText = _supportsApprovedImageEdit
            ? payload.ImageEditLiveApprovalRequiredText
            : payload.ImageEditLiveUnsupportedText;
        OnPropertyChanged(nameof(SelectedCandidateSummary));
    }

    internal void ApplyProviderCapability(IProviderCapabilities? capabilities)
    {
        _supportsApprovedImageEdit = capabilities is not null
            && !capabilities.ProviderId.StartsWith("fake", StringComparison.OrdinalIgnoreCase)
            && capabilities.SupportsImageEditing
            && capabilities.SupportsReferenceImages
            && capabilities.MaxReferenceImageCount >= 1
            && capabilities.SupportedReferenceImageRoles.Contains(ReferenceImageRole.Subject);
        OnPropertyChanged(nameof(IsApprovedImageEditAvailable));
        RunApprovedImageEditCommand.NotifyCanExecuteChanged();
    }

    internal void NotifyCommandStatesChanged()
    {
        RunFakeImageEditCommand.NotifyCanExecuteChanged();
        RunApprovedImageEditCommand.NotifyCanExecuteChanged();
    }

    partial void OnGalleryRowsChanged(IReadOnlyList<GalleryRowViewModel> value)
    {
        OnPropertyChanged(nameof(HasGalleryRows));
        _queueThumbnailWarmup(value.Select(row => row.AssetPath).ToArray());
        NotifyCommandStatesChanged();
        _projectionChanged();
    }

    partial void OnSelectedGalleryRowChanged(GalleryRowViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedCandidateSummary));
        NotifyCommandStatesChanged();
    }

    partial void OnNewImageEditPromptChanged(string value)
    {
        NotifyCommandStatesChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeImageEdit))]
    private async Task RunFakeImageEditAsync()
    {
        if (SelectedGalleryRow is null)
        {
            return;
        }

        var result = await _runFakeImageEdit(
            SelectedGalleryRow,
            NewImageEditPrompt,
            NewImageEditMaskPath,
            GalleryRows);
        if (result is null)
        {
            return;
        }

        ApplyProjection(result.GalleryRows);
        SelectedGalleryRow = result.SelectedGalleryRow;
        _editApplied(result);
    }

    private bool CanRunFakeImageEdit()
    {
        return _canMutate()
            && _hasSelectedProject()
            && SelectedGalleryRow is not null
            && !string.IsNullOrWhiteSpace(NewImageEditPrompt);
    }

    [RelayCommand(CanExecute = nameof(CanRunApprovedImageEdit))]
    private Task RunApprovedImageEditAsync()
    {
        return Task.CompletedTask;
    }

    private bool CanRunApprovedImageEdit()
    {
        // The desktop host intentionally has no paid-authority source. The
        // application service can execute a matching receipt, while this command
        // remains fail-closed until an authority owner is explicitly added.
        return IsApprovedImageEditAvailable;
    }
}

internal sealed record ImageSeriesGalleryEditResult(
    IReadOnlyList<GalleryRowViewModel> GalleryRows,
    GalleryRowViewModel SelectedGalleryRow,
    IReadOnlyList<string> ActivityItems);

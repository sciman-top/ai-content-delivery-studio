using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.App.ViewModels;

internal delegate Task<IReadOnlyList<ReviewRowViewModel>?> ImageSeriesReviewRunner(
    IReadOnlyList<GalleryRowViewModel> galleryRows);

internal delegate Task<bool> ImageSeriesFinalApprovalRunner(
    ReviewRowViewModel selectedRow,
    bool approve,
    string reviewer,
    string notes);

public sealed partial class ImageSeriesReviewWorkspaceViewModel : ObservableObject
{
    private readonly ImageSeriesReviewRunner _runFakeReview;
    private readonly ImageSeriesFinalApprovalRunner _applyFinalApproval;
    private readonly Func<bool> _canMutate;
    private readonly Func<bool> _hasSelectedProject;
    private readonly Func<IReadOnlyList<GalleryRowViewModel>> _getGalleryRows;
    private readonly Action _reviewMutated;
    private readonly Action _projectionChanged;

    [ObservableProperty]
    private IReadOnlyList<ReviewRowViewModel> _reviewRows = [];

    [ObservableProperty]
    private ReviewRowViewModel? _selectedReviewRow;

    [ObservableProperty]
    private string _finalApprovalReviewer = string.Empty;

    [ObservableProperty]
    private string _finalApprovalNotes = string.Empty;

    [ObservableProperty]
    private string _runFakeReviewText = string.Empty;

    [ObservableProperty]
    private string _reviewItemColumn = string.Empty;

    [ObservableProperty]
    private string _reviewDecisionColumn = string.Empty;

    [ObservableProperty]
    private string _reviewScoreColumn = string.Empty;

    [ObservableProperty]
    private string _reviewCommentsColumn = string.Empty;

    [ObservableProperty]
    private string _reviewFixColumn = string.Empty;

    [ObservableProperty]
    private string _reviewRouteColumn = string.Empty;

    [ObservableProperty]
    private string _humanApprovalColumn = string.Empty;

    [ObservableProperty]
    private string _noReviewRowsText = string.Empty;

    [ObservableProperty]
    private string _finalApprovalReviewerLabel = string.Empty;

    [ObservableProperty]
    private string _finalApprovalNotesLabel = string.Empty;

    [ObservableProperty]
    private string _approveSelectedReviewText = string.Empty;

    [ObservableProperty]
    private string _rejectSelectedReviewText = string.Empty;

    internal ImageSeriesReviewWorkspaceViewModel(
        ImageSeriesReviewRunner runFakeReview,
        ImageSeriesFinalApprovalRunner applyFinalApproval,
        Func<bool> canMutate,
        Func<bool> hasSelectedProject,
        Func<IReadOnlyList<GalleryRowViewModel>> getGalleryRows,
        Action reviewMutated,
        Action projectionChanged)
    {
        _runFakeReview = runFakeReview ?? throw new ArgumentNullException(nameof(runFakeReview));
        _applyFinalApproval = applyFinalApproval ?? throw new ArgumentNullException(nameof(applyFinalApproval));
        _canMutate = canMutate ?? throw new ArgumentNullException(nameof(canMutate));
        _hasSelectedProject = hasSelectedProject ?? throw new ArgumentNullException(nameof(hasSelectedProject));
        _getGalleryRows = getGalleryRows ?? throw new ArgumentNullException(nameof(getGalleryRows));
        _reviewMutated = reviewMutated ?? throw new ArgumentNullException(nameof(reviewMutated));
        _projectionChanged = projectionChanged ?? throw new ArgumentNullException(nameof(projectionChanged));
    }

    public bool HasReviewRows => ReviewRows.Count > 0;

    internal void ApplyProjection(IReadOnlyList<ReviewRowViewModel> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var selectedCandidateId = SelectedReviewRow?.CandidateImageId;
        ReviewRows = rows;
        SelectedReviewRow = rows.FirstOrDefault(row => row.CandidateImageId == selectedCandidateId)
            ?? rows.FirstOrDefault();
    }

    internal void ApplyLocalization(MainWindowLocalizationPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        RunFakeReviewText = payload.RunFakeReviewText;
        ReviewItemColumn = payload.ReviewItemColumn;
        ReviewDecisionColumn = payload.ReviewDecisionColumn;
        ReviewScoreColumn = payload.ReviewScoreColumn;
        ReviewCommentsColumn = payload.ReviewCommentsColumn;
        ReviewFixColumn = payload.ReviewFixColumn;
        ReviewRouteColumn = payload.ReviewRouteColumn;
        HumanApprovalColumn = payload.HumanApprovalColumn;
        NoReviewRowsText = payload.NoReviewRowsText;
        FinalApprovalReviewerLabel = payload.FinalApprovalReviewerLabel;
        FinalApprovalNotesLabel = payload.FinalApprovalNotesLabel;
        ApproveSelectedReviewText = payload.ApproveSelectedReviewText;
        RejectSelectedReviewText = payload.RejectSelectedReviewText;
    }

    internal void NotifyCommandStatesChanged()
    {
        RunFakeReviewCommand.NotifyCanExecuteChanged();
        ApproveSelectedReviewCommand.NotifyCanExecuteChanged();
        RejectSelectedReviewCommand.NotifyCanExecuteChanged();
    }

    partial void OnReviewRowsChanged(IReadOnlyList<ReviewRowViewModel> value)
    {
        OnPropertyChanged(nameof(HasReviewRows));
        NotifyCommandStatesChanged();
        _projectionChanged();
    }

    partial void OnSelectedReviewRowChanged(ReviewRowViewModel? value)
    {
        NotifyCommandStatesChanged();
    }

    partial void OnFinalApprovalReviewerChanged(string value)
    {
        NotifyCommandStatesChanged();
    }

    partial void OnFinalApprovalNotesChanged(string value)
    {
        RejectSelectedReviewCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRunFakeReview))]
    private async Task RunFakeReviewAsync()
    {
        var rows = await _runFakeReview(_getGalleryRows());
        if (rows is null)
        {
            return;
        }

        ApplyProjection(rows);
        _reviewMutated();
    }

    private bool CanRunFakeReview()
    {
        return CanRunProjectMutation() && _getGalleryRows().Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanApproveSelectedReview))]
    private Task ApproveSelectedReviewAsync()
    {
        return ApplyFinalApprovalAsync(approve: true);
    }

    private bool CanApproveSelectedReview()
    {
        return CanRunProjectMutation()
            && SelectedReviewRow is { Review.Decision: ReviewDecision.Pass, Review.NeedsRepair: false }
            && !string.IsNullOrWhiteSpace(FinalApprovalReviewer);
    }

    [RelayCommand(CanExecute = nameof(CanRejectSelectedReview))]
    private Task RejectSelectedReviewAsync()
    {
        return ApplyFinalApprovalAsync(approve: false);
    }

    private bool CanRejectSelectedReview()
    {
        return CanRunProjectMutation()
            && SelectedReviewRow is not null
            && !string.IsNullOrWhiteSpace(FinalApprovalReviewer)
            && !string.IsNullOrWhiteSpace(FinalApprovalNotes);
    }

    private async Task ApplyFinalApprovalAsync(bool approve)
    {
        if (SelectedReviewRow is null)
        {
            return;
        }

        var applied = await _applyFinalApproval(
            SelectedReviewRow,
            approve,
            FinalApprovalReviewer,
            FinalApprovalNotes);
        if (applied)
        {
            _reviewMutated();
        }
    }

    private bool CanRunProjectMutation()
    {
        return _canMutate() && _hasSelectedProject();
    }
}

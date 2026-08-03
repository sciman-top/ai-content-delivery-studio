using CommunityToolkit.Mvvm.ComponentModel;

namespace ContentDeliveryStudio.App.ViewModels;

public sealed class ImageSeriesWorkspaceViewModel : ObservableObject
{
    private IReadOnlyList<GalleryRowViewModel> _galleryRows = [];
    private GalleryRowViewModel? _selectedGalleryRow;
    private string _galleryItemColumn = string.Empty;
    private string _galleryImageColumn = string.Empty;
    private string _galleryMetadataColumn = string.Empty;
    private string _noGalleryRowsText = string.Empty;
    private IReadOnlyList<ReviewRowViewModel> _reviewRows = [];
    private ReviewRowViewModel? _selectedReviewRow;
    private string _finalApprovalReviewer = string.Empty;
    private string _finalApprovalNotes = string.Empty;
    private string _reviewItemColumn = string.Empty;
    private string _reviewDecisionColumn = string.Empty;
    private string _reviewScoreColumn = string.Empty;
    private string _reviewCommentsColumn = string.Empty;
    private string _reviewFixColumn = string.Empty;
    private string _reviewRouteColumn = string.Empty;
    private string _humanApprovalColumn = string.Empty;
    private string _noReviewRowsText = string.Empty;
    private string _finalApprovalReviewerLabel = string.Empty;
    private string _finalApprovalNotesLabel = string.Empty;

    public IReadOnlyList<GalleryRowViewModel> GalleryRows
    {
        get => _galleryRows;
        internal set
        {
            if (!SetProperty(ref _galleryRows, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasGalleryRows));
            if (value.Count == 0)
            {
                SelectedGalleryRow = null;
            }
            else if (SelectedGalleryRow is null
                || !value.Any(row => row.CandidateImageId == SelectedGalleryRow.CandidateImageId))
            {
                SelectedGalleryRow = value[0];
            }
        }
    }

    public bool HasGalleryRows => GalleryRows.Count > 0;

    public GalleryRowViewModel? SelectedGalleryRow
    {
        get => _selectedGalleryRow;
        set => SetProperty(ref _selectedGalleryRow, value);
    }

    public string GalleryItemColumn
    {
        get => _galleryItemColumn;
        private set => SetProperty(ref _galleryItemColumn, value);
    }

    public string GalleryImageColumn
    {
        get => _galleryImageColumn;
        private set => SetProperty(ref _galleryImageColumn, value);
    }

    public string GalleryMetadataColumn
    {
        get => _galleryMetadataColumn;
        private set => SetProperty(ref _galleryMetadataColumn, value);
    }

    public string NoGalleryRowsText
    {
        get => _noGalleryRowsText;
        private set => SetProperty(ref _noGalleryRowsText, value);
    }

    public IReadOnlyList<ReviewRowViewModel> ReviewRows
    {
        get => _reviewRows;
        internal set
        {
            if (!SetProperty(ref _reviewRows, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasReviewRows));
            SelectedReviewRow = value.FirstOrDefault(row =>
                SelectedReviewRow is null || row.CandidateImageId == SelectedReviewRow.CandidateImageId);
        }
    }

    public bool HasReviewRows => ReviewRows.Count > 0;

    public ReviewRowViewModel? SelectedReviewRow
    {
        get => _selectedReviewRow;
        set => SetProperty(ref _selectedReviewRow, value);
    }

    public string FinalApprovalReviewer
    {
        get => _finalApprovalReviewer;
        set => SetProperty(ref _finalApprovalReviewer, value);
    }

    public string FinalApprovalNotes
    {
        get => _finalApprovalNotes;
        set => SetProperty(ref _finalApprovalNotes, value);
    }

    public string ReviewItemColumn { get => _reviewItemColumn; private set => SetProperty(ref _reviewItemColumn, value); }

    public string ReviewDecisionColumn { get => _reviewDecisionColumn; private set => SetProperty(ref _reviewDecisionColumn, value); }

    public string ReviewScoreColumn { get => _reviewScoreColumn; private set => SetProperty(ref _reviewScoreColumn, value); }

    public string ReviewCommentsColumn { get => _reviewCommentsColumn; private set => SetProperty(ref _reviewCommentsColumn, value); }

    public string ReviewFixColumn { get => _reviewFixColumn; private set => SetProperty(ref _reviewFixColumn, value); }

    public string ReviewRouteColumn { get => _reviewRouteColumn; private set => SetProperty(ref _reviewRouteColumn, value); }

    public string HumanApprovalColumn { get => _humanApprovalColumn; private set => SetProperty(ref _humanApprovalColumn, value); }

    public string NoReviewRowsText { get => _noReviewRowsText; private set => SetProperty(ref _noReviewRowsText, value); }

    public string FinalApprovalReviewerLabel { get => _finalApprovalReviewerLabel; private set => SetProperty(ref _finalApprovalReviewerLabel, value); }

    public string FinalApprovalNotesLabel { get => _finalApprovalNotesLabel; private set => SetProperty(ref _finalApprovalNotesLabel, value); }

    internal void ApplyLocalization(
        string galleryItemColumn,
        string galleryImageColumn,
        string galleryMetadataColumn,
        string noGalleryRowsText,
        string reviewItemColumn,
        string reviewDecisionColumn,
        string reviewScoreColumn,
        string reviewCommentsColumn,
        string reviewFixColumn,
        string reviewRouteColumn,
        string humanApprovalColumn,
        string noReviewRowsText,
        string finalApprovalReviewerLabel,
        string finalApprovalNotesLabel)
    {
        GalleryItemColumn = galleryItemColumn;
        GalleryImageColumn = galleryImageColumn;
        GalleryMetadataColumn = galleryMetadataColumn;
        NoGalleryRowsText = noGalleryRowsText;
        ReviewItemColumn = reviewItemColumn;
        ReviewDecisionColumn = reviewDecisionColumn;
        ReviewScoreColumn = reviewScoreColumn;
        ReviewCommentsColumn = reviewCommentsColumn;
        ReviewFixColumn = reviewFixColumn;
        ReviewRouteColumn = reviewRouteColumn;
        HumanApprovalColumn = humanApprovalColumn;
        NoReviewRowsText = noReviewRowsText;
        FinalApprovalReviewerLabel = finalApprovalReviewerLabel;
        FinalApprovalNotesLabel = finalApprovalNotesLabel;
    }
}

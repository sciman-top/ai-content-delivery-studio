namespace ImageSeriesStudio.Core.Providers;

public static class DocumentIllustrationExecutionPolicy
{
    public const int DefaultMaxSourceTextCharacters = 6000;
    public const int DefaultMaxEvidenceRows = 12;

    public static int EstimateInputWeight(DocumentIllustrationPlanningRequest request)
    {
        return request.SourceText.Length
            + request.Title.Length
            + request.Audience.Length
            + request.Sections.Sum(value => value.Length)
            + request.KeyClaims.Sum(value => value.Length)
            + request.KnownConstraints.Sum(value => value.Length);
    }
}

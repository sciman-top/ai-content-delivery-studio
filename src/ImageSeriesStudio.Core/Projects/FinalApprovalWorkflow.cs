namespace ImageSeriesStudio.Core.Projects;

public sealed record FinalApprovalRequest(
    StructuredReviewOutput Review,
    bool Approve,
    string Reviewer,
    string Notes);

public sealed record FinalApprovalDecision(
    Guid CandidateImageId,
    bool HumanApproved,
    string Reviewer,
    string Notes,
    DateTimeOffset DecidedAt,
    ReviewResult ReviewResult);

public static class FinalApprovalWorkflow
{
    public static FinalApprovalDecision Decide(
        FinalApprovalRequest request,
        DateTimeOffset decidedAt)
    {
        if (string.IsNullOrWhiteSpace(request.Reviewer))
        {
            throw new ArgumentException("Reviewer is required.", nameof(request));
        }

        if (request.Approve && request.Review.Decision is not ReviewDecision.Pass)
        {
            throw new InvalidOperationException("Only candidates with a passing AI review can be approved.");
        }

        if (request.Approve && request.Review.NeedsRepair)
        {
            throw new InvalidOperationException("Candidates that still need repair cannot be approved.");
        }

        if (!request.Approve && string.IsNullOrWhiteSpace(request.Notes))
        {
            throw new InvalidOperationException("Rejected candidates require reviewer notes.");
        }

        return new FinalApprovalDecision(
            request.Review.CandidateImageId,
            request.Approve,
            request.Reviewer.Trim(),
            request.Notes.Trim(),
            decidedAt,
            request.Review.ToReviewResult(decidedAt, humanApproved: request.Approve));
    }
}

namespace ContentDeliveryStudio.Core.Projects;

public sealed record PromptRepairSuggestion(
    Guid CandidateImageId,
    RepairSeverity Severity,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Actions,
    string SuggestedPromptText)
{
    public bool HasRepair => Severity is not RepairSeverity.None;

    public static PromptRepairSuggestion FromReview(
        string originalPrompt,
        StructuredReviewOutput review)
    {
        if (!review.NeedsRepair)
        {
            return new PromptRepairSuggestion(
                review.CandidateImageId,
                RepairSeverity.None,
                [],
                [],
                originalPrompt);
        }

        var reasons = new List<string>();
        var actions = new List<string>();

        reasons.AddRange(review.HardFailures.Select(failure => $"Hard failure: {failure}"));
        reasons.AddRange(review.Scores
            .Where(score => score.Score > 0 && score.Score < 3)
            .Select(score => $"Low score: {score.Name}={score.Score} ({score.Requirement})"));

        if (review.Decision is ReviewDecision.Fail)
        {
            reasons.Add("AI review decision: fail");
            actions.Add("Regenerate the candidate after revising the prompt.");
        }

        if (!string.IsNullOrWhiteSpace(review.SuggestedFix))
        {
            reasons.Add($"Suggested fix: {review.SuggestedFix.Trim()}");
            actions.Add(review.SuggestedFix.Trim());
        }

        if (review.HardFailures.Count > 0)
        {
            actions.Add("Address every hard failure before final approval.");
        }

        var severity = review.HardFailures.Count > 0
            ? RepairSeverity.Regenerate
            : review.Decision is ReviewDecision.Fail
                ? RepairSeverity.Major
                : RepairSeverity.Minor;

        return new PromptRepairSuggestion(
            review.CandidateImageId,
            severity,
            reasons.Distinct(StringComparer.Ordinal).ToArray(),
            actions.Distinct(StringComparer.Ordinal).ToArray(),
            BuildSuggestedPrompt(originalPrompt, actions));
    }

    private static string BuildSuggestedPrompt(string originalPrompt, IReadOnlyList<string> actions)
    {
        if (actions.Count == 0)
        {
            return originalPrompt;
        }

        return string.Join(
            Environment.NewLine,
            [
                originalPrompt.Trim(),
                string.Empty,
                "Revision notes:",
                .. actions.Select(action => $"- {action}"),
            ]);
    }
}

public enum RepairSeverity
{
    None = 0,
    Minor = 1,
    Major = 2,
    Regenerate = 3,
}

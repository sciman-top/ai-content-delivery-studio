namespace ImageSeriesStudio.Core.Projects;

public sealed record ReviewOutcomeRoutingPlan(
    Guid CandidateImageId,
    IReadOnlyList<ReviewOutcomeRoute> Routes)
{
    public bool RequiresRepair => Routes.Any(route => route.TargetLayer is not ReviewOutcomeTargetLayer.None);

    public ReviewOutcomeRoute PrimaryRoute => Routes.FirstOrDefault()
        ?? new ReviewOutcomeRoute(
            ReviewOutcomeTargetLayer.None,
            RepairSeverity.None,
            [],
            ["Ready for human final approval."]);

    public static ReviewOutcomeRoutingPlan FromReview(StructuredReviewOutput review)
    {
        ArgumentNullException.ThrowIfNull(review);

        if (!review.NeedsRepair)
        {
            return new ReviewOutcomeRoutingPlan(
                review.CandidateImageId,
                [
                    new ReviewOutcomeRoute(
                        ReviewOutcomeTargetLayer.None,
                        RepairSeverity.None,
                        [],
                        ["Ready for human final approval."]),
                ]);
        }

        var severity = ResolveSeverity(review);
        var groups = new Dictionary<ReviewOutcomeTargetLayer, List<string>>();

        AddEvidence(groups, review.HardFailures);
        AddEvidence(
            groups,
            review.Scores
                .Where(score => score.Score > 0 && score.Score < 3)
                .Select(score => $"Low score: {score.Name}={score.Score} ({score.Requirement})"));

        if (review.Decision is ReviewDecision.Fail)
        {
            AddEvidence(groups, ["AI review decision: fail"]);
        }

        if (!string.IsNullOrWhiteSpace(review.SuggestedFix))
        {
            AddEvidence(groups, [$"Suggested fix: {review.SuggestedFix.Trim()}"]);
        }

        var routes = groups
            .Select(group => new ReviewOutcomeRoute(
                group.Key,
                severity,
                group.Value.Distinct(StringComparer.Ordinal).ToArray(),
                GetActions(group.Key)))
            .OrderBy(route => GetLayerPriority(route.TargetLayer))
            .ToArray();

        return new ReviewOutcomeRoutingPlan(review.CandidateImageId, routes);
    }

    private static void AddEvidence(
        Dictionary<ReviewOutcomeTargetLayer, List<string>> groups,
        IEnumerable<string> evidenceRows)
    {
        foreach (var evidence in evidenceRows.Select(row => row.Trim()).Where(row => row.Length > 0))
        {
            var layer = ClassifyLayer(evidence);
            if (!groups.TryGetValue(layer, out var rows))
            {
                rows = [];
                groups[layer] = rows;
            }

            rows.Add(evidence);
        }
    }

    private static RepairSeverity ResolveSeverity(StructuredReviewOutput review)
    {
        if (review.HardFailures.Count > 0)
        {
            return RepairSeverity.Regenerate;
        }

        return review.Decision is ReviewDecision.Fail
            ? RepairSeverity.Major
            : RepairSeverity.Minor;
    }

    private static ReviewOutcomeTargetLayer ClassifyLayer(string evidence)
    {
        if (ContainsAny(evidence, "requirement", "brief", "goal", "audience", "claim", "evidence", "must include"))
        {
            return ReviewOutcomeTargetLayer.Brief;
        }

        if (ContainsAny(evidence, "consistency", "sequence", "character", "panel", "story", "style drift", "blueprint"))
        {
            return ReviewOutcomeTargetLayer.Blueprint;
        }

        if (ContainsAny(evidence, "size", "resolution", "quality", "aspect", "format", "background", "transparent", "seed", "recipe", "settings"))
        {
            return ReviewOutcomeTargetLayer.Settings;
        }

        return ReviewOutcomeTargetLayer.Prompt;
    }

    private static bool ContainsAny(string value, params string[] keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> GetActions(ReviewOutcomeTargetLayer layer)
    {
        return layer switch
        {
            ReviewOutcomeTargetLayer.Brief => ["Clarify the creative brief requirement, audience, or factual constraint before regenerating."],
            ReviewOutcomeTargetLayer.Blueprint => ["Revise or select a blueprint with stronger consistency, sequence, or style rules."],
            ReviewOutcomeTargetLayer.Settings => ["Adjust generation settings or recipe parameters before regenerating."],
            ReviewOutcomeTargetLayer.Prompt => ["Revise prompt wording and regenerate the candidate."],
            _ => ["Ready for human final approval."],
        };
    }

    private static int GetLayerPriority(ReviewOutcomeTargetLayer layer)
    {
        return layer switch
        {
            ReviewOutcomeTargetLayer.Brief => 0,
            ReviewOutcomeTargetLayer.Blueprint => 1,
            ReviewOutcomeTargetLayer.Prompt => 2,
            ReviewOutcomeTargetLayer.Settings => 3,
            _ => 4,
        };
    }
}

public sealed record ReviewOutcomeRoute(
    ReviewOutcomeTargetLayer TargetLayer,
    RepairSeverity Severity,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> Actions);

public enum ReviewOutcomeTargetLayer
{
    None = 0,
    Brief = 1,
    Blueprint = 2,
    Prompt = 3,
    Settings = 4,
}

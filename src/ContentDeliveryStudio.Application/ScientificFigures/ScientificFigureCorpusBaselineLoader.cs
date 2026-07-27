using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContentDeliveryStudio.Application.ScientificFigures;

internal static class ScientificFigureCorpusBaselineLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<ScientificFigureCorpusDefinition> LoadAsync(
        string corpusPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(corpusPath))
        {
            throw new ArgumentException("Corpus path cannot be empty.", nameof(corpusPath));
        }

        var fullCorpusPath = Path.GetFullPath(corpusPath);
        var repositoryRoot = FindRepositoryRoot(Path.GetDirectoryName(fullCorpusPath)!);
        var corpus = await ReadAsync<ScientificFigureCorpusDefinition>(
            fullCorpusPath,
            cancellationToken);
        if (corpus.Items is null
            || !string.Equals(corpus.AdmissionState, "human-approved", StringComparison.Ordinal)
            || corpus.Items.Count != corpus.RequiredItemCount)
        {
            throw new InvalidDataException(
                "Scientific figure corpus must be human-approved and contain its required item count.");
        }

        foreach (var item in corpus.Items)
        {
            if (item is null
                || !string.Equals(item.AdmissionStatus, "accepted", StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Corpus item is not accepted: {item?.ItemId ?? "<null>"}.");
            }

            var baselinePath = ResolveInsideRepository(repositoryRoot, item.GoldBaselinePath);
            item.Baseline = await ReadAsync<ScientificFigureGoldBaseline>(
                baselinePath,
                cancellationToken);
            if (item.Source is null
                || item.Baseline.HumanReview is null
                || item.Baseline.Claims is null
                || item.Baseline.Anchors is null
                || item.Baseline.Elements is null
                || item.Baseline.Relations is null
                || item.Baseline.Mutations is null
                || !string.Equals(item.ItemId, item.Baseline.ItemId, StringComparison.Ordinal)
                || !string.Equals(item.Source.ContentHash, item.Baseline.SourceHash, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(item.Baseline.HumanReview.Status, "accepted", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(item.Baseline.HumanReview.Reviewer)
                || string.IsNullOrWhiteSpace(item.Baseline.HumanReview.ReviewedAt))
            {
                throw new InvalidDataException(
                    $"Corpus and accepted baseline authority do not match: {item.ItemId}.");
            }
        }

        return corpus;
    }

    private static async Task<T> ReadAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException($"JSON document was empty: {Path.GetFileName(path)}.");
    }

    private static string FindRepositoryRoot(string startPath)
    {
        for (var directory = new DirectoryInfo(startPath);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ContentDeliveryStudio.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate ContentDeliveryStudio.sln.");
    }

    private static string ResolveInsideRepository(string repositoryRoot, string relativePath)
    {
        var resolved = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath));
        var rootPrefix = repositoryRoot.TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Baseline path escapes the repository root.");
        }

        return resolved;
    }
}

internal sealed class ScientificFigureCorpusDefinition
{
    public string CorpusId { get; init; } = string.Empty;

    public int RequiredItemCount { get; init; }

    public string AdmissionState { get; init; } = string.Empty;

    public List<ScientificFigureCorpusDefinitionItem> Items { get; init; } = [];
}

internal sealed class ScientificFigureCorpusDefinitionItem
{
    public string ItemId { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string AdmissionStatus { get; init; } = string.Empty;

    public ScientificFigureCorpusSource Source { get; init; } = new();

    public string FigureObjective { get; init; } = string.Empty;

    public string GoldBaselinePath { get; init; } = string.Empty;

    [JsonIgnore]
    public ScientificFigureGoldBaseline Baseline { get; set; } = new();
}

internal sealed class ScientificFigureCorpusSource
{
    public string ContentHash { get; init; } = string.Empty;
}

internal sealed class ScientificFigureGoldBaseline
{
    public string ItemId { get; init; } = string.Empty;

    public string SourceHash { get; init; } = string.Empty;

    public string FigureObjective { get; init; } = string.Empty;

    public List<ScientificFigureBaselineClaim> Claims { get; init; } = [];

    public List<ScientificFigureBaselineAnchor> Anchors { get; init; } = [];

    public List<ScientificFigureBaselineElement> Elements { get; init; } = [];

    public List<ScientificFigureBaselineRelation> Relations { get; init; } = [];

    public List<ScientificFigureBaselineMutation> Mutations { get; init; } = [];

    public ScientificFigureBaselineHumanReview HumanReview { get; init; } = new();
}

internal sealed class ScientificFigureBaselineClaim
{
    public string ClaimId { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public List<string> AnchorIds { get; init; } = [];
}

internal sealed class ScientificFigureBaselineAnchor
{
    public string AnchorId { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public string EvidenceKind { get; init; } = string.Empty;

    public string? ExactQuote { get; init; }

    public string? NormalizedText { get; init; }

    public string? NormalizedEquation { get; init; }

    [JsonIgnore]
    public string EvidenceText => ExactQuote ?? NormalizedText ?? NormalizedEquation
        ?? throw new InvalidDataException($"Baseline anchor has no evidence text: {AnchorId}.");
}

internal sealed class ScientificFigureBaselineElement
{
    public string ElementId { get; init; } = string.Empty;

    public string Meaning { get; init; } = string.Empty;

    public List<string> AnchorIds { get; init; } = [];
}

internal sealed class ScientificFigureBaselineRelation
{
    public string RelationId { get; init; } = string.Empty;

    public string SourceElementId { get; init; } = string.Empty;

    public string TargetElementId { get; init; } = string.Empty;

    public string RelationClass { get; init; } = string.Empty;

    public string RelationType { get; init; } = string.Empty;

    public List<string> AnchorIds { get; init; } = [];
}

internal sealed class ScientificFigureBaselineMutation
{
    public string MutationId { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ExpectedOutcome { get; init; } = string.Empty;
}

internal sealed class ScientificFigureBaselineHumanReview
{
    public string Status { get; init; } = string.Empty;

    public string Reviewer { get; init; } = string.Empty;

    public string ReviewedAt { get; init; } = string.Empty;
}

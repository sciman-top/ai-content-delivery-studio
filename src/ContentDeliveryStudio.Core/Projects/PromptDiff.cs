namespace ContentDeliveryStudio.Core.Projects;

public sealed record PromptDiff(
    IReadOnlyList<PromptDiffLine> Lines)
{
    public bool HasChanges => Lines.Any(line => line.Kind is not PromptDiffLineKind.Unchanged);

    public static PromptDiff Compare(string originalPrompt, string revisedPrompt)
    {
        var originalLines = SplitLines(originalPrompt);
        var revisedLines = SplitLines(revisedPrompt);
        var lines = new List<PromptDiffLine>();
        var maxLineCount = Math.Max(originalLines.Count, revisedLines.Count);

        for (var index = 0; index < maxLineCount; index++)
        {
            var oldLine = index < originalLines.Count ? originalLines[index] : null;
            var newLine = index < revisedLines.Count ? revisedLines[index] : null;

            if (oldLine is null && newLine is not null)
            {
                lines.Add(new PromptDiffLine(PromptDiffLineKind.Added, null, newLine, index + 1));
            }
            else if (oldLine is not null && newLine is null)
            {
                lines.Add(new PromptDiffLine(PromptDiffLineKind.Removed, oldLine, null, index + 1));
            }
            else if (string.Equals(oldLine, newLine, StringComparison.Ordinal))
            {
                lines.Add(new PromptDiffLine(PromptDiffLineKind.Unchanged, oldLine, newLine, index + 1));
            }
            else
            {
                lines.Add(new PromptDiffLine(PromptDiffLineKind.Modified, oldLine, newLine, index + 1));
            }
        }

        return new PromptDiff(lines);
    }

    private static IReadOnlyList<string> SplitLines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
    }
}

public sealed record PromptDiffLine(
    PromptDiffLineKind Kind,
    string? OriginalText,
    string? RevisedText,
    int LineNumber);

public enum PromptDiffLineKind
{
    Unchanged = 0,
    Added = 1,
    Removed = 2,
    Modified = 3,
}

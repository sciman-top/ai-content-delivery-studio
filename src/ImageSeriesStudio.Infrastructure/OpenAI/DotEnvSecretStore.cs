namespace ImageSeriesStudio.Infrastructure.OpenAI;

public sealed class DotEnvSecretStore : IOpenAiSecretStore
{
    private readonly string _envPath;

    public DotEnvSecretStore(string? envPath = null)
    {
        _envPath = string.IsNullOrWhiteSpace(envPath)
            ? Path.Combine(Environment.CurrentDirectory, ".env")
            : envPath;
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(secretName) || !File.Exists(_envPath))
        {
            return null;
        }

        var lines = await File.ReadAllLinesAsync(_envPath, cancellationToken);
        foreach (var line in lines)
        {
            var parsed = ParseLine(line);
            if (parsed is null)
            {
                continue;
            }

            var (name, value) = parsed.Value;
            if (name.Equals(secretName, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static (string Name, string Value)? ParseLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return null;
        }

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return null;
        }

        var name = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim();
        if ((value.StartsWith('"') && value.EndsWith('"'))
            || (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1];
        }

        return (name, value);
    }
}

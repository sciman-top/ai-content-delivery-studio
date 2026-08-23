namespace ContentDeliveryStudio.Infrastructure.OpenAI;

/// <summary>
/// Builds provider failure messages that keep the upstream error body (for
/// example OpenAI's <c>error.message</c> for quota or key problems), which the
/// status line alone never reveals. Bodies are bounded so a pathological
/// error response cannot flood logs or diagnostics.
/// </summary>
internal static class OpenAiHttpError
{
    private const int MaxBodyLength = 512;

    public static string Describe(string operation, HttpResponseMessage response, string? body)
    {
        var prefix = $"{operation} failed with status {(int)response.StatusCode} {response.ReasonPhrase}.";
        var trimmed = body?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return prefix;
        }

        return trimmed.Length <= MaxBodyLength
            ? $"{prefix} {trimmed}"
            : $"{prefix} {trimmed[..MaxBodyLength]}…";
    }

    /// <summary>
    /// Reads the (already buffered) error response body for the message. A
    /// body that cannot be read must not mask the original status failure.
    /// </summary>
    public static async Task<string> ReadAndDescribeAsync(
        string operation,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string? body = null;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
        }

        return Describe(operation, response, body);
    }
}

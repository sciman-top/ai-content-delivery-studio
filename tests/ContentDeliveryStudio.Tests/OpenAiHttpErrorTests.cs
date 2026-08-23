using System.Net;
using ContentDeliveryStudio.Infrastructure.OpenAI;

namespace ContentDeliveryStudio.Tests;

public sealed class OpenAiHttpErrorTests
{
    [Fact]
    public void Describe_AppendsBoundedErrorBodyToStatusLine()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            ReasonPhrase = "Too Many Requests",
            Content = new StringContent("""{"error":{"message":"Rate limit reached for requests."}}"""),
        };

        var message = OpenAiHttpError.Describe("OpenAI image generation request", response,
            """{"error":{"message":"Rate limit reached for requests."}}""");

        Assert.StartsWith(
            "OpenAI image generation request failed with status 429 Too Many Requests.",
            message,
            StringComparison.Ordinal);
        Assert.Contains("Rate limit reached", message, StringComparison.Ordinal);
    }

    [Fact]
    public void Describe_TruncatesPathologicalBodies()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var body = new string('x', 10_000);

        var message = OpenAiHttpError.Describe("OpenAI vision review request", response, body);

        Assert.True(message.Length <= "OpenAI vision review request failed with status 500 InternalServerError.".Length + 600,
            $"Message was not bounded: {message.Length}");
        Assert.EndsWith("…", message, StringComparison.Ordinal);
    }

    [Fact]
    public void Describe_KeepsStatusLineWhenBodyIsMissing()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var message = OpenAiHttpError.Describe("OpenAI text planning request", response, body: null);

        Assert.Equal(
            "OpenAI text planning request failed with status 401 Unauthorized.",
            message);
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentDeliveryStudio.Application.Projects;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.IO;

namespace ContentDeliveryStudio.Infrastructure.OpenAI;

public sealed record OpenAiScientificReviewCheckpointIdentity(
    string SchemaVersion,
    string RequestFingerprint,
    string PayloadSha256,
    string Operation,
    string Endpoint,
    string Model,
    string ReasoningEffort)
{
    public const string CurrentSchemaVersion = "openai-scientific-review-checkpoint-v1";

    public static OpenAiScientificReviewCheckpointIdentity Create(
        string operation,
        Uri endpoint,
        string model,
        string reasoningEffort,
        ReadOnlySpan<byte> payloadBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasoningEffort);
        if (!endpoint.IsAbsoluteUri)
        {
            throw new ArgumentException("Scientific review checkpoint endpoint must be absolute.", nameof(endpoint));
        }

        var payloadSha256 = Convert.ToHexString(SHA256.HashData(payloadBytes)).ToLowerInvariant();
        var identityText = string.Join(
            '\n',
            CurrentSchemaVersion,
            operation.Trim(),
            endpoint.AbsoluteUri,
            model.Trim(),
            reasoningEffort.Trim(),
            payloadSha256);
        var fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(identityText))).ToLowerInvariant();

        return new OpenAiScientificReviewCheckpointIdentity(
            CurrentSchemaVersion,
            fingerprint,
            payloadSha256,
            operation.Trim(),
            endpoint.AbsoluteUri,
            model.Trim(),
            reasoningEffort.Trim());
    }
}

public interface IOpenAiScientificReviewCheckpointStore
{
    Task<ScientificProviderReviewResult?> TryLoadAsync(
        OpenAiScientificReviewCheckpointIdentity identity,
        CancellationToken cancellationToken);

    Task SaveAsync(
        OpenAiScientificReviewCheckpointIdentity identity,
        ScientificProviderReviewResult result,
        CancellationToken cancellationToken);
}

public sealed class NullOpenAiScientificReviewCheckpointStore
    : IOpenAiScientificReviewCheckpointStore
{
    public static NullOpenAiScientificReviewCheckpointStore Instance { get; } = new();

    private NullOpenAiScientificReviewCheckpointStore()
    {
    }

    public Task<ScientificProviderReviewResult?> TryLoadAsync(
        OpenAiScientificReviewCheckpointIdentity identity,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<ScientificProviderReviewResult?>(null);
    }

    public Task SaveAsync(
        OpenAiScientificReviewCheckpointIdentity identity,
        ScientificProviderReviewResult result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

public sealed class JsonOpenAiScientificReviewCheckpointStore
    : IOpenAiScientificReviewCheckpointStore
{
    private const int MaximumCheckpointBytes = 256 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _directoryPath;

    public JsonOpenAiScientificReviewCheckpointStore()
        : this(Path.Combine(
            LocalStudioDataPaths.ResolveStudioRoot(),
            "provider-checkpoints",
            "scientific-review"))
    {
    }

    public JsonOpenAiScientificReviewCheckpointStore(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        _directoryPath = Path.GetFullPath(directoryPath.Trim());
    }

    public async Task<ScientificProviderReviewResult?> TryLoadAsync(
        OpenAiScientificReviewCheckpointIdentity identity,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(identity);
        var path = GetCheckpointPath(identity);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length is <= 0 or > MaximumCheckpointBytes)
            {
                throw new InvalidDataException("Scientific review checkpoint size is invalid.");
            }

            var json = await File.ReadAllTextAsync(path, cancellationToken);
            var checkpoint = JsonSerializer.Deserialize<OpenAiScientificReviewCheckpoint>(json, JsonOptions)
                ?? throw new InvalidDataException("Scientific review checkpoint is empty.");
            if (checkpoint.Identity != identity)
            {
                throw new InvalidDataException("Scientific review checkpoint identity does not match the request.");
            }

            ValidateResult(checkpoint.Result);
            return checkpoint.Result with
            {
                Findings = Array.AsReadOnly(checkpoint.Result.Findings.ToArray()),
            };
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException("Scientific review checkpoint could not be read safely.", exception);
        }
    }

    public async Task SaveAsync(
        OpenAiScientificReviewCheckpointIdentity identity,
        ScientificProviderReviewResult result,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(identity);
        ValidateResult(result);
        var checkpoint = new OpenAiScientificReviewCheckpoint(
            identity,
            result with { Findings = Array.AsReadOnly(result.Findings.ToArray()) },
            DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(checkpoint, JsonOptions);
        if (Encoding.UTF8.GetByteCount(json) > MaximumCheckpointBytes)
        {
            throw new InvalidDataException("Scientific review checkpoint exceeds the size limit.");
        }

        await AtomicFileWriter.WriteAllTextAsync(
            GetCheckpointPath(identity),
            json,
            cancellationToken,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    internal string GetCheckpointPath(OpenAiScientificReviewCheckpointIdentity identity)
    {
        ValidateIdentity(identity);
        return Path.Combine(_directoryPath, $"{identity.RequestFingerprint}.json");
    }

    private static void ValidateIdentity(OpenAiScientificReviewCheckpointIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (identity.SchemaVersion != OpenAiScientificReviewCheckpointIdentity.CurrentSchemaVersion
            || !IsLowerHexSha256(identity.RequestFingerprint)
            || !IsLowerHexSha256(identity.PayloadSha256)
            || string.IsNullOrWhiteSpace(identity.Operation)
            || string.IsNullOrWhiteSpace(identity.Endpoint)
            || string.IsNullOrWhiteSpace(identity.Model)
            || string.IsNullOrWhiteSpace(identity.ReasoningEffort)
            || !Uri.TryCreate(identity.Endpoint, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Scientific review checkpoint identity is invalid.", nameof(identity));
        }
    }

    internal static void ValidateResult(ScientificProviderReviewResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!Enum.IsDefined(result.Verdict)
            || result.Origin != ScientificProviderReviewOrigin.ProviderResponse
            || result.Findings is null
            || string.IsNullOrWhiteSpace(result.ProviderTraceId)
            || result.Verdict == ScientificReviewVerdict.Fail && result.Findings.Count == 0
            || result.Findings.Any(item => item is null
                || string.IsNullOrWhiteSpace(item.Code)
                || !Enum.IsDefined(item.Kind)
                || string.IsNullOrWhiteSpace(item.ResponsibleItemId)
                || string.IsNullOrWhiteSpace(item.Evidence)))
        {
            throw new InvalidDataException("Scientific review checkpoint result is invalid.");
        }
    }

    private static bool IsLowerHexSha256(string value) =>
        value.Length == 64
        && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private sealed record OpenAiScientificReviewCheckpoint(
        OpenAiScientificReviewCheckpointIdentity Identity,
        ScientificProviderReviewResult Result,
        DateTimeOffset SavedAtUtc);
}

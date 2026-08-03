using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ContentDeliveryStudio.Core.Generation;

public sealed record GenerationApprovalOperation(
    Guid TaskId,
    Guid SeriesId,
    Guid PromptVersionId,
    Guid ProviderProfileId,
    string PromptText,
    string PromptSha256,
    int Width,
    int Height,
    string Quality,
    string OutputFormat,
    string BackgroundMode,
    int? Seed,
    int MaxRetries,
    decimal EstimatedCostUsd);

public sealed record GenerationApprovalRequestSet(
    Guid ProjectId,
    string ProviderId,
    string EndpointClass,
    string ModelId,
    IReadOnlyList<GenerationApprovalOperation> Operations)
{
    public string ComputeCanonicalHash()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("projectId", ProjectId);
            writer.WriteString("providerId", RequireText(ProviderId, nameof(ProviderId)));
            writer.WriteString("endpointClass", RequireText(EndpointClass, nameof(EndpointClass)));
            writer.WriteString("modelId", RequireText(ModelId, nameof(ModelId)));
            writer.WriteStartArray("operations");
            foreach (var operation in Operations)
            {
                writer.WriteStartObject();
                writer.WriteString("taskId", operation.TaskId);
                writer.WriteString("seriesId", operation.SeriesId);
                writer.WriteString("promptVersionId", operation.PromptVersionId);
                writer.WriteString("providerProfileId", operation.ProviderProfileId);
                writer.WriteString("promptText", operation.PromptText);
                writer.WriteString("promptSha256", operation.PromptSha256);
                writer.WriteNumber("width", operation.Width);
                writer.WriteNumber("height", operation.Height);
                writer.WriteString("quality", operation.Quality);
                writer.WriteString("outputFormat", operation.OutputFormat);
                writer.WriteString("backgroundMode", operation.BackgroundMode);
                if (operation.Seed is { } seed)
                {
                    writer.WriteNumber("seed", seed);
                }
                else
                {
                    writer.WriteNull("seed");
                }

                writer.WriteNumber("maxRetries", operation.MaxRetries);
                writer.WriteNumber("estimatedCostUsd", operation.EstimatedCostUsd);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private static string RequireText(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty.", parameterName)
            : value.Trim();
    }
}

public sealed record GenerationApprovalReceipt(
    string SchemaVersion,
    Guid Id,
    Guid ProjectId,
    IReadOnlyList<Guid> SeriesIds,
    string RequestSetHash,
    string ProviderId,
    string EndpointClass,
    string ModelId,
    int OperationCount,
    int RetryCeiling,
    decimal EstimatedCostPerOperationUsd,
    decimal EstimatedCostUsd,
    decimal MaximumCostUsd,
    string ApprovedBy,
    string AuthorityReference,
    DateTimeOffset ApprovedAt,
    DateTimeOffset ExpiresAt)
{
    public static GenerationApprovalReceipt Issue(
        GenerationApprovalRequestSet requestSet,
        decimal estimatedCostUsd,
        decimal maximumCostUsd,
        string approvedBy,
        string authorityReference,
        DateTimeOffset approvedAt,
        DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(requestSet);
        if (requestSet.Operations.Count == 0)
        {
            throw new ArgumentException("At least one generation operation is required.", nameof(requestSet));
        }

        if (estimatedCostUsd < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedCostUsd));
        }

        var operationEstimate = requestSet.Operations.Sum(operation => operation.EstimatedCostUsd);
        if (estimatedCostUsd != operationEstimate)
        {
            throw new ArgumentException(
                "Estimated cost must equal the canonical operation estimates.",
                nameof(estimatedCostUsd));
        }

        if (maximumCostUsd < estimatedCostUsd)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumCostUsd),
                "Maximum cost must cover the current estimate.");
        }

        if (expiresAt <= approvedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Approval must expire after it is issued.");
        }

        return new GenerationApprovalReceipt(
            "generation-approval-receipt.v1",
            Guid.NewGuid(),
            requestSet.ProjectId,
            requestSet.Operations.Select(operation => operation.SeriesId).Distinct().ToArray(),
            requestSet.ComputeCanonicalHash(),
            requestSet.ProviderId.Trim(),
            requestSet.EndpointClass.Trim(),
            requestSet.ModelId.Trim(),
            requestSet.Operations.Count,
            requestSet.Operations.Max(operation => operation.MaxRetries),
            requestSet.Operations.Select(operation => operation.EstimatedCostUsd).Distinct().Count() == 1
                ? requestSet.Operations[0].EstimatedCostUsd
                : 0m,
            estimatedCostUsd,
            maximumCostUsd,
            string.IsNullOrWhiteSpace(approvedBy)
                ? throw new ArgumentException("Approval source is required.", nameof(approvedBy))
                : approvedBy.Trim(),
            string.IsNullOrWhiteSpace(authorityReference)
                ? throw new ArgumentException("Authority reference is required.", nameof(authorityReference))
                : authorityReference.Trim(),
            approvedAt,
            expiresAt);
    }

    public void Validate(GenerationApprovalRequestSet requestSet, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(requestSet);
        if (now < ApprovedAt || now >= ExpiresAt)
        {
            throw new InvalidOperationException("Generation approval is not currently valid.");
        }

        if (SchemaVersion != "generation-approval-receipt.v1"
            || ProjectId != requestSet.ProjectId
            || !string.Equals(ProviderId, requestSet.ProviderId, StringComparison.Ordinal)
            || !string.Equals(EndpointClass, requestSet.EndpointClass, StringComparison.Ordinal)
            || !string.Equals(ModelId, requestSet.ModelId, StringComparison.Ordinal)
            || OperationCount != requestSet.Operations.Count
            || !string.Equals(RequestSetHash, requestSet.ComputeCanonicalHash(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Generation approval does not match the current request set.");
        }

        if (EstimatedCostUsd > MaximumCostUsd)
        {
            throw new InvalidOperationException("Generation approval exceeds its cost ceiling.");
        }
    }
}

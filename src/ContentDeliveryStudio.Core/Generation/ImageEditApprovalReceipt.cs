using System.Security.Cryptography;
using System.Text.Json;
using ContentDeliveryStudio.Core.References;

namespace ContentDeliveryStudio.Core.Generation;

public sealed record ImageEditApprovalReference(
    Guid ReferenceId,
    ReferenceImageRole Role,
    string Sha256);

public sealed record ImageEditApprovalRequestSet(
    Guid ProjectId,
    Guid SeriesItemId,
    Guid SourceCandidateImageId,
    string SourceSha256,
    string? MaskSha256,
    string InstructionSha256,
    string ProviderId,
    string EndpointClass,
    string ModelId,
    int Width,
    int Height,
    string Quality,
    string OutputFormat,
    IReadOnlyList<ImageEditApprovalReference> References,
    decimal EstimatedCostUsd)
{
    public string ComputeCanonicalHash()
    {
        return Convert.ToHexStringLower(
            SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(this)));
    }
}

public sealed record ImageEditApprovalReceipt(
    string SchemaVersion,
    Guid Id,
    string RequestSetHash,
    decimal EstimatedCostUsd,
    decimal MaximumCostUsd,
    string ApprovedBy,
    string AuthorityReference,
    DateTimeOffset ApprovedAt,
    DateTimeOffset ExpiresAt)
{
    public static ImageEditApprovalReceipt Issue(
        ImageEditApprovalRequestSet requestSet,
        decimal maximumCostUsd,
        string approvedBy,
        string authorityReference,
        DateTimeOffset approvedAt,
        DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(requestSet);
        if (requestSet.EstimatedCostUsd < 0m || maximumCostUsd < requestSet.EstimatedCostUsd)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCostUsd));
        }

        if (expiresAt <= approvedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
        }

        return new ImageEditApprovalReceipt(
            "image-edit-approval-receipt.v1",
            Guid.NewGuid(),
            requestSet.ComputeCanonicalHash(),
            requestSet.EstimatedCostUsd,
            maximumCostUsd,
            RequireText(approvedBy, nameof(approvedBy)),
            RequireText(authorityReference, nameof(authorityReference)),
            approvedAt,
            expiresAt);
    }

    public void Validate(ImageEditApprovalRequestSet requestSet, DateTimeOffset now)
    {
        if (now < ApprovedAt || now >= ExpiresAt
            || SchemaVersion != "image-edit-approval-receipt.v1"
            || RequestSetHash != requestSet.ComputeCanonicalHash()
            || EstimatedCostUsd > MaximumCostUsd)
        {
            throw new InvalidOperationException("Image edit approval is absent, expired, or does not match the request.");
        }
    }

    private static string RequireText(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty.", parameterName)
            : value.Trim();
    }
}

using ContentDeliveryStudio.Core.Generation;

namespace ContentDeliveryStudio.Tests;

public sealed class GenerationApprovalReceiptTests
{
    [Fact]
    public void Issue_BindsCanonicalOrderedRequestSetAndCostCeiling()
    {
        var now = DateTimeOffset.Parse("2026-08-03T08:00:00Z");
        var requestSet = CreateRequestSet();

        var receipt = GenerationApprovalReceipt.Issue(
            requestSet,
            estimatedCostUsd: 0.24m,
            maximumCostUsd: 0.30m,
            approvedBy: "operator:test",
            authorityReference: "authority:test-001",
            now,
            now.AddMinutes(10));

        receipt.Validate(CreateRequestSet(), now.AddMinutes(1));
        Assert.Equal(2, receipt.OperationCount);
        Assert.Equal(0.24m, receipt.EstimatedCostUsd);
        Assert.Equal(0.30m, receipt.MaximumCostUsd);
        Assert.Equal(requestSet.ComputeCanonicalHash(), receipt.RequestSetHash);
    }

    [Fact]
    public void Validate_RejectsMaterialDriftAndExpiredReceipt()
    {
        var now = DateTimeOffset.Parse("2026-08-03T08:00:00Z");
        var requestSet = CreateRequestSet();
        var receipt = GenerationApprovalReceipt.Issue(
            requestSet,
            0.24m,
            0.30m,
            "operator:test",
            "authority:test-001",
            now,
            now.AddMinutes(10));
        var drifted = requestSet with
        {
            Operations = requestSet.Operations
                .Select((operation, index) => index == 0 ? operation with { Width = 1536 } : operation)
                .ToArray(),
        };

        Assert.Throws<InvalidOperationException>(() => receipt.Validate(drifted, now.AddMinutes(1)));
        Assert.Throws<InvalidOperationException>(() => receipt.Validate(requestSet, now.AddMinutes(10)));
    }

    [Fact]
    public void CanonicalHash_ChangesWhenOperationOrderChanges()
    {
        var requestSet = CreateRequestSet();
        var reordered = requestSet with { Operations = requestSet.Operations.Reverse().ToArray() };

        Assert.NotEqual(requestSet.ComputeCanonicalHash(), reordered.ComputeCanonicalHash());
    }

    private static GenerationApprovalRequestSet CreateRequestSet()
    {
        return new GenerationApprovalRequestSet(
            Guid.Parse("00000000-0000-0000-0000-000000000100"),
            "paid-image",
            "images",
            "paid-image-v1",
            [
                new GenerationApprovalOperation(
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Guid.Parse("00000000-0000-0000-0000-000000000101"),
                    Guid.Parse("00000000-0000-0000-0000-000000000011"),
                    Guid.Parse("00000000-0000-0000-0000-000000000021"),
                    "first prompt",
                    "prompt-sha-1",
                    1024,
                    1024,
                    "standard",
                    "png",
                    "auto",
                    null,
                    0,
                    0.12m),
                new GenerationApprovalOperation(
                    Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Guid.Parse("00000000-0000-0000-0000-000000000101"),
                    Guid.Parse("00000000-0000-0000-0000-000000000012"),
                    Guid.Parse("00000000-0000-0000-0000-000000000022"),
                    "second prompt",
                    "prompt-sha-2",
                    1024,
                    1536,
                    "high",
                    "png",
                    "auto",
                    42,
                    1,
                    0.12m),
            ]);
    }
}

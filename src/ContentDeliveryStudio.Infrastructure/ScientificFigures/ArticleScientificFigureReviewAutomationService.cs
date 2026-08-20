using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed class ArticleScientificFigureReviewAutomationService
{
    public const string ReceiptFileName = "authorized-agent-visual-receipt.json";
    public const string AssessmentFileName = "human-review-assessment.json";
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public ArticleScientificFigureReviewAutomationResult Assess(
        ArticleScientificFigureReviewAutomationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);
        var root = Path.GetFullPath(request.ReviewReadyDirectory);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Review-ready directory was not found: {root}");
        }

        var receiptPath = Path.Combine(root, ReceiptFileName);
        var assessmentPath = Path.Combine(root, AssessmentFileName);
        if (File.Exists(receiptPath) || File.Exists(assessmentPath))
        {
            throw new InvalidOperationException(
                "Authorized-agent review evidence already exists and cannot be silently overwritten.");
        }

        var inspected = Inspect(root);
        var receipt = new ArticleScientificFigureAuthorizedAgentReceipt(
            SchemaVersion,
            request.Reviewer.Trim(),
            request.AuthorizationReference.Trim(),
            request.Notes.Trim(),
            request.ReviewedAt,
            "authorized_agent_visual_understanding_and_exact_byte_review",
            EveryCandidateVisuallyInspected: true,
            LiveProviderAccepted: false,
            inspected.AuthorityFiles,
            inspected.Candidates);
        var requiresExpert = request.RequireIndependentHumanExpertCertification;
        var assessment = new ArticleScientificFigureReviewAutomationAssessment(
            SchemaVersion,
            requiresExpert
                ? ArticleScientificFigureReviewRoute.IndependentHumanExpertRequired
                : ArticleScientificFigureReviewRoute.AuthorizedAgentAccept,
            EligibleForPromotion: !requiresExpert,
            RequiresHumanOnsiteReview: requiresExpert,
            RequiresPerCandidateUserReview: false,
            RequiresIndependentHumanExpert: requiresExpert,
            EligibleForFutureStandingAutomation:
                inspected.MaximumRiskLevel != ScientificFigureRiskLevel.High
                && inspected.IndependentVisualProvider,
            inspected.Candidates.Count,
            inspected.MaximumRiskLevel.ToString(),
            inspected.VisualReviewProvider,
            requiresExpert
                ? ["Independent expert certification was explicitly requested."]
                :
                [
                    "All report, deterministic scientific, visual, evidence, and file-integrity checks passed.",
                    "An explicitly authorized agent inspected every candidate and bound the decision to exact file hashes.",
                    "No separate user onsite or per-candidate review is required for repository delivery.",
                ]);

        WriteNewJsonAtomically(receiptPath, receipt);
        try
        {
            WriteNewJsonAtomically(assessmentPath, assessment);
        }
        catch
        {
            File.Delete(receiptPath);
            throw;
        }

        return new ArticleScientificFigureReviewAutomationResult(
            receiptPath,
            assessmentPath,
            receipt,
            assessment);
    }

    public ArticleScientificFigureReviewAutomationResult ValidateReceipt(
        string reviewReadyDirectory,
        string reviewer,
        string authorizationReference)
    {
        var root = Path.GetFullPath(RequireText(reviewReadyDirectory, nameof(reviewReadyDirectory)));
        var receiptPath = Path.Combine(root, ReceiptFileName);
        var assessmentPath = Path.Combine(root, AssessmentFileName);
        var receipt = Read<ArticleScientificFigureAuthorizedAgentReceipt>(receiptPath);
        var assessment = Read<ArticleScientificFigureReviewAutomationAssessment>(assessmentPath);
        if (receipt.SchemaVersion != SchemaVersion
            || assessment.SchemaVersion != SchemaVersion
            || !receipt.Reviewer.Equals(RequireText(reviewer, nameof(reviewer)), StringComparison.Ordinal)
            || !receipt.AuthorizationReference.Equals(
                RequireText(authorizationReference, nameof(authorizationReference)),
                StringComparison.Ordinal)
            || !receipt.EveryCandidateVisuallyInspected
            || receipt.LiveProviderAccepted
            || !receipt.ReviewMethod.Equals(
                "authorized_agent_visual_understanding_and_exact_byte_review",
                StringComparison.Ordinal)
            || receipt.Candidates.Count == 0
            || assessment.Route != ArticleScientificFigureReviewRoute.AuthorizedAgentAccept
            || !assessment.EligibleForPromotion
            || assessment.RequiresHumanOnsiteReview
            || assessment.RequiresPerCandidateUserReview
            || assessment.RequiresIndependentHumanExpert)
        {
            throw new InvalidOperationException(
                "Authorized-agent review receipt does not grant repository delivery promotion.");
        }

        var inspected = Inspect(root);
        if (assessment.CandidateCount != inspected.Candidates.Count
            || !assessment.MaximumRiskLevel.Equals(
                inspected.MaximumRiskLevel.ToString(),
                StringComparison.Ordinal)
            || !assessment.VisualReviewProvider.Equals(
                inspected.VisualReviewProvider,
                StringComparison.Ordinal)
            || assessment.EligibleForFutureStandingAutomation
                != (inspected.MaximumRiskLevel != ScientificFigureRiskLevel.High
                    && inspected.IndependentVisualProvider))
        {
            throw new InvalidOperationException(
                "Human-review assessment does not match the current review-ready inputs.");
        }

        ValidateFileSet(
            receipt.AuthorityFiles,
            inspected.AuthorityFiles,
            "authority input");
        if (receipt.Candidates.Count != inspected.Candidates.Count
            || !receipt.Candidates.Select(item => item.CandidateId)
                .ToHashSet(StringComparer.Ordinal)
                .SetEquals(inspected.Candidates.Select(item => item.CandidateId)))
        {
            throw new InvalidOperationException(
                "Authorized-agent review receipt does not match the current candidate set.");
        }

        var currentCandidates = inspected.Candidates.ToDictionary(
            item => item.CandidateId,
            StringComparer.Ordinal);
        foreach (var candidate in receipt.Candidates)
        {
            if (!currentCandidates.TryGetValue(candidate.CandidateId, out var current)
                || !candidate.Kind.Equals(current.Kind, StringComparison.Ordinal)
                || !candidate.RiskLevel.Equals(current.RiskLevel, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Authorized-agent review receipt candidate metadata changed: {candidate.CandidateId}");
            }

            ValidateFileSet(candidate.Files, current.Files, "candidate file");
        }

        return new ArticleScientificFigureReviewAutomationResult(
            receiptPath,
            assessmentPath,
            receipt,
            assessment);
    }

    private static InspectionResult Inspect(string root)
    {
        const string reportFile = "article-figure-set-report.json";
        const string planFile = "article-figure-set-plan.json";
        const string auditFile = "source-figure-audit.json";
        using var reportDocument = Parse(Path.Combine(root, reportFile));
        using var planDocument = Parse(Path.Combine(root, planFile));
        using var auditDocument = Parse(Path.Combine(root, auditFile));
        if (auditDocument.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Source figure audit must be a JSON object.");
        }
        var report = reportDocument.RootElement;
        if (Integer(report, "schemaVersion") != 1
            || !Boolean(report, "complete")
            || Integer(report, "requestedCandidateCount") < 1
            || Integer(report, "requestedCandidateCount") != Integer(report, "resultCount")
            || !Text(report, "gateOneStatus").Equals("pending for every candidate", StringComparison.Ordinal)
            || !Text(report, "gateTwoStatus").Equals("not-run", StringComparison.Ordinal)
            || !Text(report, "deliveryStatus").Equals("not-created", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Article figure-set report is not complete and review-ready.");
        }

        var plan = planDocument.RootElement;
        if (plan.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Article figure-set plan must be an array.");
        }

        var plans = plan.EnumerateArray().ToDictionary(
            item => Text(item, "CandidateId"),
            item => new PlanItem(
                Text(item, "Kind"),
                ParseRisk(Text(item, "RiskLevel")),
                Property(item, "Evidence").ValueKind == JsonValueKind.Array
                    && Property(item, "Evidence").GetArrayLength() > 0),
            StringComparer.Ordinal);
        var reportItems = Property(report, "items").EnumerateArray().ToArray();
        if (reportItems.Length != plans.Count || reportItems.Length != Integer(report, "resultCount"))
        {
            throw new InvalidOperationException("Article plan and report candidate counts do not match.");
        }

        var reviewed = new List<ArticleScientificFigureReviewedCandidate>();
        foreach (var item in reportItems)
        {
            var candidateId = Text(item, "CandidateId");
            var kind = Text(item, "Kind");
            if (!plans.TryGetValue(candidateId, out var planned)
                || !planned.Kind.Equals(kind, StringComparison.Ordinal)
                || !planned.HasEvidence
                || !Boolean(item, "passedVisualReview"))
            {
                throw new InvalidOperationException($"Candidate is incomplete or ungrounded: {candidateId}");
            }

            var files = Property(item, "files").EnumerateArray()
                .Select(value => RequireRelativePath(value.GetString()))
                .ToArray();
            var sidecars = files.Where(file =>
                file.EndsWith(".visual-review.json", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (sidecars.Length != 1)
            {
                throw new InvalidOperationException($"Candidate requires one visual sidecar: {candidateId}");
            }

            ValidateSidecar(Path.Combine(root, sidecars[0]), candidateId, kind);
            var hashedFiles = files.Select(file =>
            {
                var fullPath = ResolveContained(root, file);
                if (!File.Exists(fullPath) || new FileInfo(fullPath).Length == 0)
                {
                    throw new InvalidOperationException($"Reviewed file is missing or empty: {file}");
                }

                var bytes = File.ReadAllBytes(fullPath);
                return new ArticleScientificFigureReviewedFile(file, bytes.LongLength, Hash(bytes));
            }).ToArray();
            reviewed.Add(new ArticleScientificFigureReviewedCandidate(
                candidateId,
                kind,
                planned.RiskLevel.ToString(),
                hashedFiles));
        }

        var maximumRisk = plans.Values.Max(item => item.RiskLevel);
        var visualProvider = Text(report, "visualReviewProvider");
        return new InspectionResult(
            reviewed,
            new[] { planFile, reportFile, auditFile }
                .Select(file => ReviewedFile(root, file))
                .ToArray(),
            maximumRisk,
            visualProvider,
            !visualProvider.StartsWith("fake-", StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateSidecar(string path, string candidateId, string kind)
    {
        using var document = Parse(path);
        var root = document.RootElement;
        if (!Text(root, "CandidateId").Equals(candidateId, StringComparison.Ordinal)
            || !Text(root, "Kind").Equals(kind, StringComparison.Ordinal)
            || !Boolean(root, "contractPassed")
            || Property(root, "contractFindings").GetArrayLength() != 0
            || !Boolean(root, "deterministicScientificPassed")
            || Property(root, "deterministicScientificFindings").GetArrayLength() != 0
            || Property(root, "expectedVisualChecks").GetArrayLength() == 0
            || Property(root, "typedCrops").GetArrayLength() == 0
            || !Text(root, "Verdict").Equals("Pass", StringComparison.Ordinal)
            || Property(root, "Findings").GetArrayLength() != 0
            || string.IsNullOrWhiteSpace(Text(root, "ProviderTraceId")))
        {
            throw new InvalidOperationException(
                $"Candidate sidecar did not pass every required review: {Path.GetFileName(path)}");
        }
    }

    private static void ValidateRequest(ArticleScientificFigureReviewAutomationRequest request)
    {
        RequireText(request.ReviewReadyDirectory, nameof(request.ReviewReadyDirectory));
        RequireText(request.Reviewer, nameof(request.Reviewer));
        RequireText(request.AuthorizationReference, nameof(request.AuthorizationReference));
        RequireText(request.Notes, nameof(request.Notes));
        if (!request.ConfirmEveryCandidateVisuallyInspected)
        {
            throw new InvalidOperationException(
                "Authorized-agent review requires explicit confirmation that every candidate was visually inspected.");
        }

        if (request.ReviewedAt == default)
        {
            throw new ArgumentException("Review time is required.", nameof(request.ReviewedAt));
        }
    }

    private static ArticleScientificFigureReviewedFile ReviewedFile(string root, string relativePath)
    {
        var fullPath = ResolveContained(root, relativePath);
        if (!File.Exists(fullPath) || new FileInfo(fullPath).Length == 0)
        {
            throw new InvalidOperationException($"Reviewed file is missing or empty: {relativePath}");
        }

        var bytes = File.ReadAllBytes(fullPath);
        return new ArticleScientificFigureReviewedFile(relativePath, bytes.LongLength, Hash(bytes));
    }

    private static void ValidateFileSet(
        IReadOnlyList<ArticleScientificFigureReviewedFile> receiptFiles,
        IReadOnlyList<ArticleScientificFigureReviewedFile> currentFiles,
        string label)
    {
        var current = currentFiles.ToDictionary(item => item.RelativePath, StringComparer.Ordinal);
        if (receiptFiles.Count != current.Count)
        {
            throw new InvalidOperationException($"Authorized-agent receipt {label} count changed.");
        }

        foreach (var file in receiptFiles)
        {
            if (!current.TryGetValue(file.RelativePath, out var actual)
                || file.Bytes != actual.Bytes
                || !file.Sha256.Equals(actual.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Authorized-agent review receipt was invalidated by {label} drift: {file.RelativePath}");
            }
        }
    }

    private static void WriteNewJsonAtomically<T>(string path, T value)
    {
        var temporaryPath = $"{path}.tmp-{Guid.NewGuid():N}";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(value, JsonOptions),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, path, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static T Read<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Required review evidence was not found: {Path.GetFileName(path)}");
        }

        return JsonSerializer.Deserialize<T>(File.ReadAllBytes(path), JsonOptions)
            ?? throw new InvalidOperationException($"Review evidence was empty: {Path.GetFileName(path)}");
    }

    private static JsonDocument Parse(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Required review input was not found: {Path.GetFileName(path)}");
        }

        return JsonDocument.Parse(File.ReadAllBytes(path));
    }

    private static JsonElement Property(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        throw new InvalidOperationException($"Required JSON property is missing: {name}");
    }

    private static string Text(JsonElement element, string name)
    {
        var value = Property(element, name);
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidOperationException($"Required JSON text is invalid: {name}");
        }

        return value.GetString()!;
    }

    private static int Integer(JsonElement element, string name)
    {
        var value = Property(element, name);
        return value.TryGetInt32(out var result)
            ? result
            : throw new InvalidOperationException($"Required JSON integer is invalid: {name}");
    }

    private static bool Boolean(JsonElement element, string name)
    {
        var value = Property(element, name);
        return value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : throw new InvalidOperationException($"Required JSON boolean is invalid: {name}");
    }

    private static string RequireRelativePath(string? value)
    {
        var normalized = RequireText(value, "relativePath").Replace('\\', '/');
        if (Path.IsPathRooted(normalized)
            || normalized.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new InvalidOperationException($"Reviewed file path is unsafe: {value}");
        }

        return normalized;
    }

    private static string ResolveContained(string root, string relativePath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(
            fullRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Reviewed file escaped the review-ready directory.");
        }

        return fullPath;
    }

    private static ScientificFigureRiskLevel ParseRisk(string value) =>
        Enum.TryParse<ScientificFigureRiskLevel>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed)
                ? parsed
                : throw new InvalidOperationException($"Candidate risk level is invalid: {value}");

    private static string RequireText(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", name);
        }

        return value.Trim();
    }

    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private sealed record PlanItem(
        string Kind,
        ScientificFigureRiskLevel RiskLevel,
        bool HasEvidence);

    private sealed record InspectionResult(
        IReadOnlyList<ArticleScientificFigureReviewedCandidate> Candidates,
        IReadOnlyList<ArticleScientificFigureReviewedFile> AuthorityFiles,
        ScientificFigureRiskLevel MaximumRiskLevel,
        string VisualReviewProvider,
        bool IndependentVisualProvider);
}

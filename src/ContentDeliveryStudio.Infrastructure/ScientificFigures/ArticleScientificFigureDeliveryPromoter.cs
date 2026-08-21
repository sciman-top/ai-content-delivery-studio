using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ContentDeliveryStudio.Application.ScientificFigures;

namespace ContentDeliveryStudio.Infrastructure.ScientificFigures;

public sealed partial class ArticleScientificFigureDeliveryPromoter
{
    private const int DeliverySchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public ArticleScientificFigureDeliveryPromotionResult Promote(
        ArticleScientificFigureDeliveryPromotionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var sourceDirectory = FullPath(request.ReviewReadyDirectory);
        var deliveryRoot = FullPath(request.DeliveryRoot);
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Review-ready article figure set was not found: {sourceDirectory}");
        }

        var articleDirectory = Path.Combine(deliveryRoot, request.ArticleSlug);
        var packageDirectory = Path.Combine(articleDirectory, request.PackageId);
        if (Directory.Exists(packageDirectory) || File.Exists(packageDirectory))
        {
            throw new InvalidOperationException(
                $"Delivery package already exists and cannot be merged: {packageDirectory}");
        }

        var reportPath = Path.Combine(sourceDirectory, "article-figure-set-report.json");
        using var reportDocument = ParseJson(reportPath);
        var report = ReadAndValidateReport(reportDocument.RootElement);
        ValidatePlan(sourceDirectory, report);
        var audit = ReadAndValidateAudit(sourceDirectory, report.SourceSha256);
        var mappings = BuildMappings(sourceDirectory, report, audit).ToList();
        if (request.Actor == ArticleScientificFigureApprovalActor.AuthorizedAgent)
        {
            new ArticleScientificFigureReviewAutomationService().ValidateReceipt(
                sourceDirectory,
                request.Reviewer,
                request.AuthorizationReference!);
            mappings.Add(Mapping(
                sourceDirectory,
                ArticleScientificFigureReviewAutomationService.ReceiptFileName,
                $"reviews/{ArticleScientificFigureReviewAutomationService.ReceiptFileName}",
                "review"));
            mappings.Add(Mapping(
                sourceDirectory,
                ArticleScientificFigureReviewAutomationService.AssessmentFileName,
                $"reviews/{ArticleScientificFigureReviewAutomationService.AssessmentFileName}",
                "review"));
        }

        Directory.CreateDirectory(articleDirectory);
        EnsureNoReparsePoints(deliveryRoot, articleDirectory);
        var stagingDirectory = Path.Combine(
            articleDirectory,
            $".{request.PackageId}.staging-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(stagingDirectory);
            var manifestFiles = CopyAndHash(stagingDirectory, sourceDirectory, mappings);
            var approvalsPath = Path.Combine(stagingDirectory, "approvals.json");
            WriteJson(approvalsPath, BuildApprovals(request, report));
            manifestFiles.Add(ManifestFile.FromGenerated(
                "approvals.json",
                "approval",
                approvalsPath));

            var reviewReportPath = Path.Combine(stagingDirectory, "review-report.md");
            File.WriteAllText(
                reviewReportPath,
                BuildReviewReport(request, report, manifestFiles),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            manifestFiles.Add(ManifestFile.FromGenerated(
                "review-report.md",
                "review-summary",
                reviewReportPath));

            ValidateNoAbsolutePathLeakage(stagingDirectory);
            var manifestPath = Path.Combine(stagingDirectory, "manifest.json");
            WriteJson(manifestPath, new DeliveryManifest(
                DeliverySchemaVersion,
                request.ArticleSlug,
                request.PackageId,
                report.SourceFileName,
                report.SourceSha256,
                report.DeterministicReview,
                report.VisualReviewProvider,
                report.ItemCount,
                request.ApprovedAt,
                NormalizeActor(request.Actor),
                "explicit_user_authorized_operator",
                LiveProviderAccepted: false,
                IndependentHumanExpertAccepted: false,
                manifestFiles.OrderBy(item => item.PackageRelativePath, StringComparer.Ordinal).ToArray()));
            ValidateNoAbsolutePathLeakage(stagingDirectory);

            Directory.Move(stagingDirectory, packageDirectory);
            var finalManifestPath = Path.Combine(packageDirectory, "manifest.json");
            return new ArticleScientificFigureDeliveryPromotionResult(
                packageDirectory,
                finalManifestPath,
                Hash(File.ReadAllBytes(finalManifestPath)),
                manifestFiles.Count(item => item.Role == "figure"),
                manifestFiles.Count(item => item.Role == "evidence"),
                manifestFiles.Count(item => item.Role == "review"),
                manifestFiles.Count(item => item.Role == "metadata"));
        }
        catch
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }

            throw;
        }
    }

    private static void ValidateRequest(ArticleScientificFigureDeliveryPromotionRequest request)
    {
        RequireText(request.ReviewReadyDirectory, nameof(request.ReviewReadyDirectory));
        RequireText(request.DeliveryRoot, nameof(request.DeliveryRoot));
        RequireSafeSegment(request.ArticleSlug, nameof(request.ArticleSlug));
        RequireSafeSegment(request.PackageId, nameof(request.PackageId));
        RequireText(request.Reviewer, nameof(request.Reviewer));
        if (!Enum.IsDefined(request.Actor))
        {
            throw new ArgumentOutOfRangeException(nameof(request.Actor));
        }

        if (request.Actor == ArticleScientificFigureApprovalActor.AuthorizedAgent
            && string.IsNullOrWhiteSpace(request.AuthorizationReference))
        {
            throw new InvalidOperationException(
                "An authorized-agent approval requires a non-empty authorization reference.");
        }

        if (!request.GateOneApproved || !request.GateTwoApproved)
        {
            throw new InvalidOperationException(
                "Article delivery promotion requires explicit Gate 1 and Gate 2 approvals.");
        }

        RequireText(request.GateOneNotes, nameof(request.GateOneNotes));
        RequireText(request.GateTwoNotes, nameof(request.GateTwoNotes));
        if (request.ApprovedAt == default)
        {
            throw new ArgumentException("Approval time is required.", nameof(request.ApprovedAt));
        }
    }

    private static ReportContext ReadAndValidateReport(JsonElement root)
    {
        if (Integer(root, "schemaVersion") != 1
            || !Boolean(root, "complete")
            || Integer(root, "requestedCandidateCount") < 1
            || Integer(root, "requestedCandidateCount") != Integer(root, "resultCount")
            || !Text(root, "gateOneStatus").Equals("pending for every candidate", StringComparison.Ordinal)
            || !Text(root, "gateTwoStatus").Equals("not-run", StringComparison.Ordinal)
            || !Text(root, "deliveryStatus").Equals("not-created", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Article figure-set report is incomplete or is not in the review-ready state.");
        }

        var deterministicReview = Text(root, "deterministicReview");
        if (deterministicReview is not ("article-optics-v1" or "article-thermal-v1" or "article-gravity-v1" or "article-thermistor-v1" or "article-archimedes-v1"))
        {
            throw new InvalidOperationException("Article deterministic review package is unsupported.");
        }

        var source = Property(root, "source");
        var sourceFileName = Text(source, "fileName");
        var sourceSha256 = Text(source, "SourceSha256");
        RequireSha256(sourceSha256, "report source hash");
        if (Path.IsPathRooted(sourceFileName))
        {
            throw new InvalidOperationException("Article source file name must not be an absolute path.");
        }

        var itemsElement = Property(root, "items");
        if (itemsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Article report items must be an array.");
        }

        var items = new List<ReportItem>();
        foreach (var item in itemsElement.EnumerateArray())
        {
            var candidateId = Text(item, "CandidateId");
            var kind = Text(item, "Kind");
            if (!Boolean(item, "passedVisualReview"))
            {
                throw new InvalidOperationException(
                    $"Candidate did not pass visual review: {candidateId}");
            }

            var files = Property(item, "files").EnumerateArray()
                .Select(value => RequireRelativeFile(value.GetString(), "candidate file"))
                .ToArray();
            if (files.Length == 0 || files.Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Length)
            {
                throw new InvalidOperationException($"Candidate file list is empty or duplicated: {candidateId}");
            }

            items.Add(new ReportItem(candidateId, kind, files));
        }

        var expectedCount = Integer(root, "resultCount");
        if (items.Count != expectedCount
            || items.Select(item => item.CandidateId).Distinct(StringComparer.Ordinal).Count() != items.Count)
        {
            throw new InvalidOperationException("Article report item count or candidate identity is invalid.");
        }

        return new ReportContext(
            sourceFileName,
            sourceSha256,
            deterministicReview,
            Text(root, "visualReviewProvider"),
            items);
    }

    private static void ValidatePlan(string sourceDirectory, ReportContext report)
    {
        using var plan = ParseJson(Path.Combine(sourceDirectory, "article-figure-set-plan.json"));
        if (plan.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Article figure-set plan must be an array.");
        }

        var candidateIds = plan.RootElement.EnumerateArray()
            .Select(item => Text(item, "CandidateId"))
            .ToArray();
        if (candidateIds.Length != report.ItemCount
            || !candidateIds.ToHashSet(StringComparer.Ordinal)
                .SetEquals(report.Items.Select(item => item.CandidateId)))
        {
            throw new InvalidOperationException(
                "Article figure-set plan does not match the completed report.");
        }
    }

    private static AuditContext ReadAndValidateAudit(string sourceDirectory, string reportSourceSha256)
    {
        using var audit = ParseJson(Path.Combine(sourceDirectory, "source-figure-audit.json"));
        var root = audit.RootElement;
        var sourceSha256 = Text(root, "SourceSha256");
        if (!sourceSha256.Equals(reportSourceSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source audit hash does not match the article report.");
        }

        var assetsElement = Property(root, "assets");
        if (assetsElement.ValueKind != JsonValueKind.Array || assetsElement.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Source figure audit contains no assets.");
        }

        var assets = assetsElement.EnumerateArray().Select(item =>
        {
            var relativePath = RequireRelativeFile(Text(item, "fileName"), "source asset");
            if (!relativePath.StartsWith("source-assets/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Source assets must remain under source-assets/.");
            }

            var sha256 = Text(item, "Sha256");
            RequireSha256(sha256, "source asset hash");
            var sourcePath = ResolveContainedPath(sourceDirectory, relativePath);
            if (!File.Exists(sourcePath)
                || !Hash(File.ReadAllBytes(sourcePath)).Equals(sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Source asset is missing or hash-drifted: {relativePath}");
            }

            return new AuditAsset(relativePath, sha256);
        }).ToArray();
        if (assets.Select(item => item.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != assets.Length)
        {
            throw new InvalidOperationException("Source audit contains duplicate asset paths.");
        }

        return new AuditContext(assets);
    }

    private static IReadOnlyList<FileMapping> BuildMappings(
        string sourceDirectory,
        ReportContext report,
        AuditContext audit)
    {
        var mappings = new List<FileMapping>
        {
            Mapping(sourceDirectory, "article-figure-set-plan.json", "metadata/article-figure-set-plan.json", "metadata"),
            Mapping(sourceDirectory, "source-figure-audit.json", "metadata/source-figure-audit.json", "metadata"),
            Mapping(sourceDirectory, "article-figure-set-report.json", "reviews/article-figure-set-report.json", "review"),
        };

        foreach (var item in report.Items)
        {
            var reviewFiles = item.Files.Where(file => file.EndsWith(".visual-review.json", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (reviewFiles.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Candidate requires exactly one visual-review sidecar: {item.CandidateId}");
            }

            ValidateReviewSidecar(
                Path.Combine(sourceDirectory, reviewFiles[0]),
                item,
                report.DeterministicReview);
            mappings.Add(Mapping(
                sourceDirectory,
                reviewFiles[0],
                $"reviews/{reviewFiles[0]}",
                "review"));

            var assets = item.Files.Except(reviewFiles, StringComparer.OrdinalIgnoreCase).ToArray();
            if (item.Kind.Equals("SourceEvidenceBoard", StringComparison.Ordinal))
            {
                if (assets.Length != 1 || !assets[0].EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Source evidence board must contain one PNG and no publishable figure formats.");
                }

                mappings.Add(Mapping(sourceDirectory, assets[0], $"evidence/{assets[0]}", "evidence"));
            }
            else
            {
                var extensions = assets.Select(Path.GetExtension).ToArray();
                if (assets.Length != 3
                    || !extensions.ToHashSet(StringComparer.OrdinalIgnoreCase)
                        .SetEquals([".svg", ".png", ".pdf"]))
                {
                    throw new InvalidOperationException(
                        $"Publishable candidate requires exactly one SVG, PNG, and PDF: {item.CandidateId}");
                }

                mappings.AddRange(assets.Select(asset =>
                    Mapping(sourceDirectory, asset, $"figures/{asset}", "figure")));
            }
        }

        mappings.AddRange(audit.Assets.Select(asset => Mapping(
            sourceDirectory,
            asset.RelativePath,
            $"evidence/{asset.RelativePath}",
            "evidence",
            asset.Sha256)));
        if (mappings.Select(item => item.PackageRelativePath)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() != mappings.Count)
        {
            throw new InvalidOperationException("Delivery package contains duplicate target paths.");
        }

        return mappings;
    }

    private static void ValidateReviewSidecar(
        string path,
        ReportItem item,
        string deterministicReview)
    {
        using var review = ParseJson(path);
        var root = review.RootElement;
        if (!Text(root, "CandidateId").Equals(item.CandidateId, StringComparison.Ordinal)
            || !Text(root, "Kind").Equals(item.Kind, StringComparison.Ordinal)
            || !Text(root, "gateOneStatus").Equals("PendingHumanApproval", StringComparison.Ordinal)
            || !Boolean(root, "contractPassed")
            || Property(root, "contractFindings").GetArrayLength() != 0
            || !Boolean(root, "deterministicScientificPassed")
            || !Text(root, "deterministicScientificPackage").Equals(deterministicReview, StringComparison.Ordinal)
            || Property(root, "deterministicScientificFindings").GetArrayLength() != 0
            || Property(root, "expectedVisualChecks").GetArrayLength() == 0
            || Property(root, "typedCrops").GetArrayLength() == 0
            || !Text(root, "Verdict").Equals("Pass", StringComparison.Ordinal)
            || Property(root, "Findings").GetArrayLength() != 0
            || string.IsNullOrWhiteSpace(Text(root, "ProviderTraceId")))
        {
            throw new InvalidOperationException(
                $"Candidate review sidecar is incomplete or failed: {Path.GetFileName(path)}");
        }
    }

    private static List<ManifestFile> CopyAndHash(
        string stagingDirectory,
        string sourceDirectory,
        IReadOnlyList<FileMapping> mappings)
    {
        var files = new List<ManifestFile>();
        foreach (var mapping in mappings)
        {
            if (!File.Exists(mapping.SourcePath) || new FileInfo(mapping.SourcePath).Length == 0)
            {
                throw new InvalidOperationException(
                    $"Required delivery source file is missing or empty: {mapping.SourceRelativePath}");
            }

            var sha256 = Hash(File.ReadAllBytes(mapping.SourcePath));
            if (mapping.ExpectedSha256 is not null
                && !sha256.Equals(mapping.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Delivery source hash mismatch: {mapping.SourceRelativePath}");
            }

            var destination = ResolveContainedPath(stagingDirectory, mapping.PackageRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            EnsureNoReparsePoints(
                sourceDirectory,
                mapping.SourcePath);
            File.Copy(mapping.SourcePath, destination, overwrite: false);
            var deliveredSha256 = Hash(File.ReadAllBytes(destination));
            if (!sha256.Equals(deliveredSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Delivery source changed while it was being copied: {mapping.SourceRelativePath}");
            }
            files.Add(new ManifestFile(
                mapping.SourceRelativePath,
                mapping.PackageRelativePath,
                mapping.Role,
                new FileInfo(destination).Length,
                deliveredSha256));
        }

        return files;
    }

    private static object BuildApprovals(
        ArticleScientificFigureDeliveryPromotionRequest request,
        ReportContext report)
    {
        var actor = NormalizeActor(request.Actor);
        var decision = new
        {
            approved = true,
            reviewer = request.Reviewer.Trim(),
            actor,
            authorizationReference = request.AuthorizationReference?.Trim(),
            decisionAuthority = "explicit_user_authorized_operator",
            approvedAt = request.ApprovedAt,
        };
        return new
        {
            schemaVersion = DeliverySchemaVersion,
            gateOne = new
            {
                decision.approved,
                decision.reviewer,
                decision.actor,
                decision.authorizationReference,
                decision.decisionAuthority,
                decision.approvedAt,
                notes = request.GateOneNotes.Trim(),
                scope = "scientific meaning, conditions, directions, values, and exceptions for every candidate",
            },
            gateTwo = new
            {
                decision.approved,
                decision.reviewer,
                decision.actor,
                decision.authorizationReference,
                decision.decisionAuthority,
                decision.approvedAt,
                notes = request.GateTwoNotes.Trim(),
                scope = "per-candidate visual spot check and immutable package promotion",
            },
            reviewedCandidateCount = report.ItemCount,
            sourceVisualReviewProvider = report.VisualReviewProvider,
            authorizedAgentVisualReceiptValidated =
                request.Actor == ArticleScientificFigureApprovalActor.AuthorizedAgent,
            liveProviderAccepted = false,
            independentHumanExpertAccepted = false,
        };
    }

    private static string BuildReviewReport(
        ArticleScientificFigureDeliveryPromotionRequest request,
        ReportContext report,
        IReadOnlyList<ManifestFile> files)
    {
        var figureFiles = files.Count(file => file.Role == "figure");
        var evidenceFiles = files.Count(file => file.Role == "evidence");
        return $"""
            # Article scientific figure delivery review

            - Article: {report.SourceFileName}
            - Package: {request.ArticleSlug}/{request.PackageId}
            - Candidate count: {report.ItemCount}
            - Figure assets: {figureFiles}
            - Evidence assets: {evidenceFiles}
            - Deterministic review: {report.DeterministicReview}
            - Source visual review: {report.VisualReviewProvider}
            - Reviewer: {request.Reviewer.Trim()}
            - Actor: {NormalizeActor(request.Actor)}
            - Gate 1: approved
            - Gate 2: approved
            - Approval time: {request.ApprovedAt:O}
            - Live multimodal provider acceptance: not run
            - Independent human physics-expert acceptance: not run

            Gate 1 notes: {request.GateOneNotes.Trim()}

            Gate 2 notes: {request.GateTwoNotes.Trim()}

            The source report and sidecars passed deterministic, scientific, and visual
            preflight checks. This package records an explicit authorized-operator decision;
            it does not rewrite fake-first evidence as a live-provider or independent-human verdict.
            """;
    }

    private static void ValidateNoAbsolutePathLeakage(string root)
    {
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                     .Where(path => Path.GetExtension(path) is ".json" or ".md" or ".svg"))
        {
            var text = File.ReadAllText(path);
            if (WindowsAbsolutePathRegex().IsMatch(text)
                || UnixAbsolutePathRegex().IsMatch(text))
            {
                throw new InvalidOperationException(
                    $"Delivery metadata contains an absolute path: {Path.GetFileName(path)}");
            }
        }
    }

    private static FileMapping Mapping(
        string sourceDirectory,
        string sourceRelativePath,
        string packageRelativePath,
        string role,
        string? expectedSha256 = null) =>
        new(
            RequireRelativeFile(sourceRelativePath, "delivery source"),
            ResolveContainedPath(sourceDirectory, sourceRelativePath),
            RequireRelativeFile(packageRelativePath, "delivery target"),
            role,
            expectedSha256);

    private static JsonDocument ParseJson(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Required JSON file was not found: {Path.GetFileName(path)}");
        }

        try
        {
            return JsonDocument.Parse(File.ReadAllBytes(path));
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Required JSON file is invalid: {Path.GetFileName(path)}",
                exception);
        }
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
        if (!value.TryGetInt32(out var result))
        {
            throw new InvalidOperationException($"Required JSON integer is invalid: {name}");
        }

        return result;
    }

    private static bool Boolean(JsonElement element, string name)
    {
        var value = Property(element, name);
        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new InvalidOperationException($"Required JSON boolean is invalid: {name}");
        }

        return value.GetBoolean();
    }

    private static string RequireRelativeFile(string? value, string label)
    {
        var normalized = RequireText(value, label).Replace('\\', '/');
        if (Path.IsPathRooted(normalized)
            || normalized.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new InvalidOperationException($"{label} must be a safe relative file path: {value}");
        }

        return normalized;
    }

    private static string ResolveContainedPath(string root, string relativePath)
    {
        var fullRoot = FullPath(root);
        var fullPath = Path.GetFullPath(
            Path.Combine(fullRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Relative path escapes its package root.");
        }

        return fullPath;
    }

    private static string FullPath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static void EnsureNoReparsePoints(string root, string path)
    {
        var fullRoot = FullPath(root);
        var current = FullPath(path);
        while (true)
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    $"Delivery paths cannot traverse a reparse point: {Path.GetFileName(current)}");
            }

            if (current.Equals(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            current = Path.GetDirectoryName(current)
                ?? throw new InvalidOperationException("Delivery path is outside its declared root.");
            if (!current.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Delivery path is outside its declared root.");
            }
        }
    }

    private static string RequireText(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private static void RequireSafeSegment(string value, string parameterName)
    {
        if (!SafeSegmentRegex().IsMatch(RequireText(value, parameterName)))
        {
            throw new ArgumentException(
                "Value must be a lowercase ASCII slug using single hyphens.",
                parameterName);
        }
    }

    private static void RequireSha256(string value, string label)
    {
        var normalized = value.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? value[7..]
            : value;
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException($"{label} is not SHA-256.");
        }
    }

    private static string NormalizeActor(ArticleScientificFigureApprovalActor actor) => actor switch
    {
        ArticleScientificFigureApprovalActor.Human => "human",
        ArticleScientificFigureApprovalActor.AuthorizedAgent => "authorized_agent",
        _ => throw new ArgumentOutOfRangeException(nameof(actor), actor, null),
    };

    private static string Hash(byte[] bytes) =>
        $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

    private static void WriteJson(string path, object value) =>
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(value, JsonOptions),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    [GeneratedRegex(@"(?<![A-Za-z0-9])[A-Za-z]:[\\/]", RegexOptions.CultureInvariant)]
    private static partial Regex WindowsAbsolutePathRegex();

    [GeneratedRegex(@"(?<![A-Za-z0-9])/(?:home|Users|var|tmp|opt|mnt)/", RegexOptions.CultureInvariant)]
    private static partial Regex UnixAbsolutePathRegex();

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeSegmentRegex();

    private sealed record ReportItem(
        string CandidateId,
        string Kind,
        IReadOnlyList<string> Files);

    private sealed record ReportContext(
        string SourceFileName,
        string SourceSha256,
        string DeterministicReview,
        string VisualReviewProvider,
        IReadOnlyList<ReportItem> Items)
    {
        public int ItemCount => Items.Count;
    }

    private sealed record AuditAsset(string RelativePath, string Sha256);
    private sealed record AuditContext(IReadOnlyList<AuditAsset> Assets);
    private sealed record FileMapping(
        string SourceRelativePath,
        string SourcePath,
        string PackageRelativePath,
        string Role,
        string? ExpectedSha256);

    private sealed record ManifestFile(
        string? SourceRelativePath,
        string PackageRelativePath,
        string Role,
        long Bytes,
        string Sha256)
    {
        public static ManifestFile FromGenerated(string relativePath, string role, string fullPath) =>
            new(null, relativePath, role, new FileInfo(fullPath).Length, Hash(File.ReadAllBytes(fullPath)));
    }

    private sealed record DeliveryManifest(
        int SchemaVersion,
        string ArticleSlug,
        string PackageId,
        string SourceFileName,
        string SourceSha256,
        string DeterministicReview,
        string SourceVisualReviewProvider,
        int CandidateCount,
        DateTimeOffset ApprovedAt,
        string Actor,
        string DecisionAuthority,
        bool LiveProviderAccepted,
        bool IndependentHumanExpertAccepted,
        IReadOnlyList<ManifestFile> Files);
}

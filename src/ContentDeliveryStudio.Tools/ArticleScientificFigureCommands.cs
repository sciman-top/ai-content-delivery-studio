using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Core.ScientificFigures;
using ContentDeliveryStudio.Core.Sources;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

namespace ContentDeliveryStudio.Tools;

/// <summary>
/// Production command seam for fake-first article figure generation.
/// The implementation owns extraction, planning, rendering, review, and artifact persistence;
/// PowerShell and tests call this seam instead of treating an xUnit method as a CLI.
/// </summary>
public static class ArticleScientificFigureCommands
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task RunSingleAsync(
        string sourcePath,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var resolvedSourcePath = ResolveSourcePath(sourcePath);
        var resolvedOutputDirectory = ResolveOutputDirectory(outputDirectory);
        var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
            new ScientificDocumentExtractionRequest(
                Guid.NewGuid(),
                await HashFileAsync(resolvedSourcePath, cancellationToken),
                SourceAssetKind.Pdf,
                Path.GetFileNameWithoutExtension(resolvedSourcePath),
                string.Empty,
                resolvedSourcePath,
                IsScanned: false,
                UseOcr: false,
                ReadingOrder: ScientificReadingOrderStatus.Reliable,
                RequiredContent: []),
            cancellationToken);
        EnsureReady(extraction);

        var candidates = new ArticleScientificFigurePlanningService().Plan(
            extraction,
            Path.GetFileNameWithoutExtension(resolvedSourcePath),
            "初中物理教师与学生");
        var opticalCandidate = candidates.Single(item =>
            item.Kind == ArticleScientificFigureCandidateKind.Mechanism);
        var preview = new ArticleScientificFigurePreviewRenderer().RenderOpticalPathPreview(opticalCandidate);
        var workflowService = new ArticleScientificFigureWorkflowService(
            new InMemoryScientificFigureWorkflowRepository(),
            new DeterministicSvgRenderer(),
            new ScientificFigureExporter(),
            new SkiaScientificReviewImageCropper(),
            new FakeScientificSemanticReviewProvider(),
            new FakeScientificVisualReviewProvider());
        var gateOneTime = DateTimeOffset.UtcNow;
        var approved = await workflowService.CreateApprovedMechanismAsync(
            Guid.NewGuid(),
            extraction,
            opticalCandidate,
            new ScientificGateOneDecision(
                Approved: true,
                "fake-first-command",
                "Fake-first command output remains pending explicit human Gate 1 and Gate 2 decisions.",
                gateOneTime),
            cancellationToken);
        var review = await workflowService.RunMachineReviewAsync(
            approved,
            gateOneTime.AddMinutes(1),
            cancellationToken);
        if (!review.ContractReview.Passed || !review.MachineReview.CanProceedToGate2)
        {
            throw new InvalidOperationException("Article scientific figure command did not produce a review-ready candidate.");
        }

        Directory.CreateDirectory(resolvedOutputDirectory);
        var reportPath = Path.Combine(resolvedOutputDirectory, "article-scientific-figure-report.json");
        var previewPath = Path.Combine(resolvedOutputDirectory, "candidate-01-secondary-lens-imaging-path.svg");
        var approvedSvgPath = Path.Combine(resolvedOutputDirectory, "approved-mechanism.svg");
        var workflowPath = Path.Combine(resolvedOutputDirectory, "approved-scientific-workflow.json");
        var reviewsPath = Path.Combine(resolvedOutputDirectory, "machine-review.json");
        await File.WriteAllTextAsync(previewPath, preview.Svg, cancellationToken);
        await File.WriteAllTextAsync(approvedSvgPath, review.Svg.Svg, cancellationToken);
        await WriteJsonAsync(workflowPath, review.Aggregate, cancellationToken);
        foreach (var artifact in review.Exports.Artifacts)
        {
            await File.WriteAllBytesAsync(
                Path.Combine(resolvedOutputDirectory, $"approved-mechanism.{artifact.Format}"),
                artifact.Bytes,
                cancellationToken);
        }

        await WriteJsonAsync(reviewsPath, new
        {
            contractPassed = review.ContractReview.Passed,
            contractHardFailures = review.ContractReview.HardFailures,
            machineReviewPassed = review.MachineReview.CanProceedToGate2,
            machineReviewBlockers = review.MachineReview.Blockers,
            visualReview = new
            {
                provider = "fake-scientific-visual",
                fullResolutionSha256 = review.ReviewPreparation.Manifest.FullResolutionSha256,
                cropCount = review.ReviewPreparation.VisualRequest.RegionCrops.Count,
                cropIds = review.ReviewPreparation.Manifest.CropIds,
            },
            semanticReview = new
            {
                provider = "fake-scientific-semantic",
                approvedClaimCount = review.ReviewPreparation.SemanticRequest.ApprovedClaims.Count,
            },
            gateTwoStatus = "not-run: a separate explicit human Gate 2 decision is required",
        }, cancellationToken);
        await WriteJsonAsync(reportPath, new
        {
            schemaVersion = 1,
            source = new
            {
                fileName = Path.GetFileName(resolvedSourcePath),
                extraction.Status,
                extraction.SourceSha256,
                pageCount = extraction.Blocks.Select(block => block.Location.PageNumber).Distinct().Count(),
                blockCount = extraction.Blocks.Count,
            },
            candidates,
            preview = new
            {
                fileName = Path.GetFileName(previewPath),
                preview.PreviewKind,
                preview.Sha256,
                preview.GateOneStatus,
            },
            formalWorkflow = new
            {
                workflowFileName = Path.GetFileName(workflowPath),
                reviewFileName = Path.GetFileName(reviewsPath),
                approvedSvgFileName = Path.GetFileName(approvedSvgPath),
                review.Aggregate.Workflow.State,
                gateOneReviewer = review.Aggregate.Workflow.Gate1Approval!.Reviewer,
                review.ContractReview.Passed,
                machineReviewPassed = review.MachineReview.CanProceedToGate2,
            },
            gateTwoStatus = "not-run: a separate explicit human Gate 2 decision is required",
            deliveryStatus = ArticleScientificFigureDeliveryStatus.NotCreated,
        }, cancellationToken);
    }

    public static async Task RunSetAsync(
        string sourcePath,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var resolvedSourcePath = ResolveSourcePath(sourcePath);
        var resolvedOutputDirectory = ResolveOutputDirectory(outputDirectory);
        var extraction = await new PdfPigScientificDocumentExtractor().ExtractAsync(
            new ScientificDocumentExtractionRequest(
                Guid.NewGuid(),
                await HashFileAsync(resolvedSourcePath, cancellationToken),
                SourceAssetKind.Pdf,
                Path.GetFileNameWithoutExtension(resolvedSourcePath),
                string.Empty,
                resolvedSourcePath,
                IsScanned: false,
                UseOcr: false,
                ReadingOrder: ScientificReadingOrderStatus.Reliable,
                RequiredContent: []),
            cancellationToken);
        EnsureReady(extraction);

        var candidates = new ArticleScientificFigurePlanningService().Plan(
            extraction,
            Path.GetFileNameWithoutExtension(resolvedSourcePath),
            "初中物理教师与学生");
        var run = await new ArticleScientificFigureSetService(
            new PdfPigArticleSourceFigureExtractor(),
            new SkiaArticleSourceEvidenceBoardRenderer(),
            new ArticleScientificFigureCandidateRenderer(),
            new ScientificFigureExporter(),
            new FakeScientificVisualReviewProvider(),
            new SkiaScientificReviewImageCropper(),
            scientificReviewer: ArticleScientificFigureReviewerFactory.CreateFor(candidates)).RunAsync(
                resolvedSourcePath,
                candidates,
                cancellationToken);
        if (!run.Complete)
        {
            var failures = string.Join(
                Environment.NewLine,
                run.Items.Where(item => !item.PassedVisualReview).Select(item =>
                    $"{item.Candidate.Kind}: contract=[{string.Join(',', item.ContractReview.Findings.Select(f => f.Code))}] "
                    + $"science=[{string.Join(',', item.DeterministicScientificReview.Findings.Select(f => f.Code))}] "
                    + $"visual=[{string.Join(',', item.VisualReview.Findings.Select(f => f.Code))}]"));
            throw new InvalidOperationException($"Article scientific figure set is incomplete.{Environment.NewLine}{failures}");
        }

        await PersistSetRunAsync(resolvedSourcePath, extraction, run, resolvedOutputDirectory, cancellationToken);
    }

    private static string ResolveSourcePath(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var resolved = Path.GetFullPath(sourcePath);
        if (!File.Exists(resolved) || !string.Equals(Path.GetExtension(resolved), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException("Article source PDF was not found.", resolved);
        }

        return resolved;
    }

    private static string ResolveOutputDirectory(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var resolved = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(resolved);
        return resolved;
    }

    private static void EnsureReady(ScientificDocumentExtraction extraction)
    {
        if (extraction.Status != ScientificExtractionStatus.Ready)
        {
            throw new InvalidOperationException($"Article source extraction was not ready: {extraction.Status}.");
        }
    }

    private static async Task PersistSetRunAsync(
        string sourcePath,
        ScientificDocumentExtraction extraction,
        ArticleScientificFigureSetRun run,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        var sourceAssetsDirectory = Path.Combine(outputDirectory, "source-assets");
        Directory.CreateDirectory(sourceAssetsDirectory);
        foreach (var asset in run.SourceAudit.Assets)
        {
            await File.WriteAllBytesAsync(
                Path.Combine(sourceAssetsDirectory, $"{asset.AssetId}.png"),
                asset.PngBytes,
                cancellationToken);
        }

        await WriteJsonAsync(
            Path.Combine(outputDirectory, "article-figure-set-plan.json"),
            run.Items.Select(item => item.Candidate),
            cancellationToken);
        await WriteJsonAsync(
            Path.Combine(outputDirectory, "source-figure-audit.json"),
            new
            {
                run.SourceAudit.SourceSha256,
                run.SourceAudit.PageCount,
                assets = run.SourceAudit.Assets.Select(asset => new
                {
                    asset.AssetId,
                    asset.PageNumber,
                    asset.PageImageIndex,
                    asset.PixelWidth,
                    asset.PixelHeight,
                    asset.PageLeft,
                    asset.PageBottom,
                    asset.PageWidth,
                    asset.PageHeight,
                    asset.Sha256,
                    fileName = $"source-assets/{asset.AssetId}.png",
                }),
            },
            cancellationToken);

        var itemReports = new List<object>();
        var evidenceBoardPrefix = ArticleScientificFigureFileNaming.EvidenceBoardPrefix(
            run.Items.Select(item => item.Candidate.Kind).ToArray());
        foreach (var item in run.Items)
        {
            var highStandardContract = ArticleHighStandardFigureProfileCatalog.TryGetEffectiveContract(item.Candidate);
            var prefix = ArticleScientificFigureFileNaming.GetFileNamePrefix(item.Candidate, evidenceBoardPrefix);
            var files = new List<string>();
            if (item.Svg is not null)
            {
                var name = $"{prefix}.svg";
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, name), item.Svg.Svg, cancellationToken);
                files.Add(name);
            }

            if (item.Exports is not null)
            {
                foreach (var artifact in item.Exports.Artifacts)
                {
                    var name = $"{prefix}.{artifact.Format}";
                    await File.WriteAllBytesAsync(Path.Combine(outputDirectory, name), artifact.Bytes, cancellationToken);
                    files.Add(name);
                }
            }

            if (item.EvidenceBoard is not null)
            {
                var name = $"{prefix}.png";
                await File.WriteAllBytesAsync(Path.Combine(outputDirectory, name), item.EvidenceBoard.PngBytes, cancellationToken);
                files.Add(name);
            }

            var reviewName = $"{prefix}.visual-review.json";
            await WriteJsonAsync(
                Path.Combine(outputDirectory, reviewName),
                new
                {
                    item.Candidate.CandidateId,
                    item.Candidate.Kind,
                    item.Candidate.SourceFigureReferences,
                    item.Candidate.Disposition,
                    gateOneStatus = item.Candidate.GateOneStatus,
                    authorityBoundary = "visual review only; no scientific Gate 1 or human acceptance",
                    contractPassed = item.ContractReview.Passed,
                    contractFindings = item.ContractReview.Findings,
                    deterministicScientificPassed = item.DeterministicScientificReview.Passed,
                    deterministicScientificPackage = item.DeterministicScientificReview.PackageId,
                    deterministicScientificAuthority = item.DeterministicScientificReview.AuthorityBoundary,
                    deterministicScientificFindings = item.DeterministicScientificReview.Findings,
                    expectedVisualChecks = item.DeterministicScientificReview.ExpectedVisualChecks,
                    highStandardContract,
                    visualReviewRequested = item.VisualReviewRequest is not null,
                    typedCrops = item.VisualReviewRequest is null
                        ? []
                        : item.VisualReviewRequest.RegionCrops.Select(crop => new
                        {
                            crop.CropId,
                            crop.Kind,
                            crop.ResponsibleItemId,
                            crop.X,
                            crop.Y,
                            crop.Width,
                            crop.Height,
                            crop.ExpectedCheck,
                        }).ToArray(),
                    item.VisualReview.Verdict,
                    item.VisualReview.Findings,
                    item.VisualReview.ProviderTraceId,
                    item.PresentationAttempts,
                    item.Repairs,
                },
                cancellationToken);
            files.Add(reviewName);
            itemReports.Add(new
            {
                item.Candidate.CandidateId,
                item.Candidate.Kind,
                files,
                item.PassedVisualReview,
                item.PresentationAttempts,
            });
        }

        await WriteJsonAsync(
            Path.Combine(outputDirectory, "article-figure-set-report.json"),
            new
            {
                schemaVersion = 1,
                source = new
                {
                    fileName = Path.GetFileName(sourcePath),
                    extraction.Status,
                    extraction.SourceSha256,
                    run.SourceAudit.PageCount,
                    sourceAssetCount = run.SourceAudit.Assets.Count,
                },
                requestedCandidateCount = run.RequestedCandidateIds.Count,
                resultCount = run.Items.Count,
                run.Complete,
                visualReviewProvider = "fake-scientific-visual",
                visualReviewBoundary = "fake-first contract path; not a live multimodal-model or scientific-expert verdict",
                deterministicReview = run.Items.Select(item => item.DeterministicScientificReview.PackageId).Distinct().Single(),
                deterministicReviewBoundary = "machine-checkable domain invariants; not human Gate 1",
                highStandardProfile = run.Items
                    .Select(item => ArticleHighStandardFigureProfileCatalog.TryGetEffectiveContract(item.Candidate))
                    .FirstOrDefault(contract => contract is not null)?.PackageId,
                machinePreflightComplete = run.Complete,
                independentVisualReviewPassed = run.HumanReviewRecommendation.IndependentVisualReviewPassed,
                humanReviewMode = run.HumanReviewRecommendation.Mode,
                requiresEveryCandidateVisualSpotCheck = run.HumanReviewRecommendation.RequiresEveryCandidateVisualSpotCheck,
                humanReviewRationale = run.HumanReviewRecommendation.Rationale,
                gateOneStatus = "pending for every candidate",
                gateTwoStatus = "not-run",
                deliveryStatus = "not-created",
                items = itemReports,
            },
            cancellationToken);
    }


    private static Task WriteJsonAsync(string path, object value, CancellationToken cancellationToken) =>
        File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, JsonOptions), cancellationToken);

    private static async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return $"sha256:{Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant()}";
    }

}

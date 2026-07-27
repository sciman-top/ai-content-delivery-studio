using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentDeliveryStudio.Core.ScientificFigures;

namespace ContentDeliveryStudio.Application.ScientificFigures;

public sealed record ScientificFigureCorpusCoverage(bool AllMapped);

public sealed record ScientificFigureCorpusMutationResult(
    string MutationId,
    string Category,
    string ExpectedOutcome,
    string ActualOutcome,
    string FindingCode,
    string ResponsibleItemId);

public sealed record ScientificFigureCorpusItemResult(
    string ItemId,
    string Category,
    ScientificFigureWorkflowState WorkflowState,
    bool ContractReviewPassed,
    bool MachineReviewPassed,
    ScientificFigureCorpusCoverage Coverage,
    IReadOnlyList<ScientificFigureCorpusMutationResult> Mutations);

public sealed record ScientificFigureCorpusAcceptanceReport(
    string CorpusId,
    bool Passed,
    int ItemCount,
    int BlockedMutationCount,
    IReadOnlyList<ScientificFigureCorpusItemResult> Items)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions) + Environment.NewLine;
}

public sealed class ScientificFigureCorpusRunner
{
    private readonly IScientificFigureRenderer _renderer;
    private readonly IScientificFigureExporter _exporter;
    private readonly IScientificReviewImageCropper _cropper;

    public ScientificFigureCorpusRunner(
        IScientificFigureRenderer renderer,
        IScientificFigureExporter exporter,
        IScientificReviewImageCropper cropper)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _cropper = cropper ?? throw new ArgumentNullException(nameof(cropper));
    }

    public async Task<ScientificFigureCorpusAcceptanceReport> RunAsync(
        string corpusPath,
        CancellationToken cancellationToken)
    {
        var corpus = await ScientificFigureCorpusBaselineLoader.LoadAsync(
            corpusPath,
            cancellationToken);
        var results = new List<ScientificFigureCorpusItemResult>(corpus.Items.Count);
        foreach (var item in corpus.Items.OrderBy(item => item.ItemId, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await RunItemAsync(item, cancellationToken));
        }

        var snapshot = Array.AsReadOnly(results.ToArray());
        var blockedMutationCount = snapshot.Sum(item => item.Mutations.Count(mutation =>
            string.Equals(mutation.ActualOutcome, "blocked", StringComparison.Ordinal)));
        var passed = snapshot.Count == corpus.RequiredItemCount
            && snapshot.All(item => item.WorkflowState == ScientificFigureWorkflowState.ReviewPassed
                && item.ContractReviewPassed
                && item.MachineReviewPassed
                && item.Coverage.AllMapped
                && item.Mutations.All(mutation =>
                    string.Equals(mutation.ExpectedOutcome, "block", StringComparison.Ordinal)
                    && string.Equals(mutation.ActualOutcome, "blocked", StringComparison.Ordinal)));
        return new ScientificFigureCorpusAcceptanceReport(
            corpus.CorpusId,
            passed,
            snapshot.Count,
            blockedMutationCount,
            snapshot);
    }

    public static void WriteReport(
        ScientificFigureCorpusAcceptanceReport report,
        string outputPath)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Report path cannot be empty.", nameof(outputPath));
        }

        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, report.ToJson(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private async Task<ScientificFigureCorpusItemResult> RunItemAsync(
        ScientificFigureCorpusDefinitionItem item,
        CancellationToken cancellationToken)
    {
        var model = BuildModel(item);
        var workflow = ScientificFigureWorkflow.Create(model.Specification)
            .ApproveGate1(
                item.Baseline.HumanReview.Reviewer,
                "Accepted Checkpoint 0 corpus authority.",
                ParseReviewTime(item.Baseline.HumanReview.ReviewedAt));
        var plan = new ScientificFigureSpecCompiler().Compile(workflow);
        var svg = _renderer.Render(plan);
        var exports = _exporter.Export(new ScientificFigureExportRequest(
            svg,
            svg.Sha256,
            Width: 1200,
            Height: 800));
        var contractRequest = new ScientificContractReviewRequest(
            model.Specification,
            plan,
            svg,
            exports);
        var contractReport = new ScientificContractReviewer().Review(contractRequest);
        var prep = new ScientificReviewPrepBuilder(_cropper).Build(
            model.Understanding,
            model.Specification,
            plan,
            svg,
            exports);
        var machineDecision = await new ScientificReviewExecutionService(
                PassingReviewProvider.Instance,
                PassingReviewProvider.Instance)
            .ReviewAsync(prep.SemanticRequest, prep.VisualRequest, cancellationToken);
        if (contractReport.Passed && machineDecision.CanProceedToGate2)
        {
            var reviewedAt = ParseReviewTime(item.Baseline.HumanReview.ReviewedAt);
            workflow = workflow
                .RecordDownstreamApproval(
                    ScientificDownstreamApprovalStage.RenderPlan,
                    "corpus-fake-runner",
                    reviewedAt.AddMinutes(1))
                .RecordDownstreamApproval(
                    ScientificDownstreamApprovalStage.ScientificReview,
                    "corpus-fake-runner",
                    reviewedAt.AddMinutes(2));
        }

        var mutations = new List<ScientificFigureCorpusMutationResult>(
            item.Baseline.Mutations.Count);
        foreach (var mutation in item.Baseline.Mutations.OrderBy(
                     mutation => mutation.MutationId,
                     StringComparer.Ordinal))
        {
            mutations.Add(await RunMutationAsync(
                mutation,
                contractRequest,
                prep,
                cancellationToken));
        }

        return new ScientificFigureCorpusItemResult(
            item.ItemId,
            item.Category,
            workflow.State,
            contractReport.Passed,
            machineDecision.CanProceedToGate2,
            new ScientificFigureCorpusCoverage(model.AllMapped),
            Array.AsReadOnly(mutations.ToArray()));
    }

    private static async Task<ScientificFigureCorpusMutationResult> RunMutationAsync(
        ScientificFigureBaselineMutation mutation,
        ScientificContractReviewRequest contractRequest,
        ScientificReviewPrepBundle prep,
        CancellationToken cancellationToken)
    {
        ScientificReviewBlocker blocker;
        if (string.Equals(mutation.Category, "scientific", StringComparison.Ordinal))
        {
            var mutatedPlan = MutateScientificPlan(contractRequest.RenderPlan, mutation);
            var report = new ScientificContractReviewer().Review(
                contractRequest with { RenderPlan = mutatedPlan });
            var finding = report.HardFailures.FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"Scientific mutation was not blocked: {mutation.MutationId}.");
            blocker = new ScientificReviewBlocker(
                ScientificReviewLayer.Semantic,
                finding.Code,
                finding.ResponsibleItemId,
                finding.Evidence);
        }
        else if (string.Equals(mutation.Category, "visual", StringComparison.Ordinal))
        {
            var provider = new MutationFindingProvider(mutation);
            var decision = await new ScientificReviewExecutionService(
                    PassingReviewProvider.Instance,
                    provider)
                .ReviewAsync(prep.SemanticRequest, prep.VisualRequest, cancellationToken);
            blocker = decision.Blockers.FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"Visual mutation was not blocked: {mutation.MutationId}.");
        }
        else
        {
            throw new InvalidDataException(
                $"Unsupported mutation category '{mutation.Category}'.");
        }

        return new ScientificFigureCorpusMutationResult(
            mutation.MutationId,
            mutation.Category,
            mutation.ExpectedOutcome,
            "blocked",
            blocker.Code,
            blocker.ResponsibleItemId);
    }

    private static SvgRenderPlan MutateScientificPlan(
        SvgRenderPlan plan,
        ScientificFigureBaselineMutation mutation)
    {
        IReadOnlyList<SvgRenderElement> elements = plan.Elements;
        IReadOnlyList<SvgRenderConnection> connections = plan.Connections;
        var mutationText = $"{mutation.MutationId} {mutation.Description}";
        var removesAuthority = mutationText.Contains("remove", StringComparison.OrdinalIgnoreCase)
            || mutationText.Contains("omit", StringComparison.OrdinalIgnoreCase)
            || mutationText.Contains("delete", StringComparison.OrdinalIgnoreCase);
        if (removesAuthority && connections.Count > 0)
        {
            connections = connections.Skip(1).ToArray();
        }
        else if (connections.Count > 0)
        {
            var first = connections[0];
            connections =
            [
                first with
                {
                    SourceRenderElementId = first.TargetRenderElementId,
                    TargetRenderElementId = first.SourceRenderElementId,
                    Label = mutation.Description,
                },
                .. connections.Skip(1),
            ];
        }
        else if (elements.Count > 0)
        {
            elements =
            [
                elements[0] with { ScientificMeaning = mutation.Description },
                .. elements.Skip(1),
            ];
        }
        else
        {
            throw new InvalidOperationException("Cannot mutate an empty scientific render plan.");
        }

        return SvgRenderPlan.Create(
            plan.PlanId,
            plan.SpecificationId,
            plan.SpecificationVersion,
            plan.Canvas,
            plan.Layers,
            elements,
            connections,
            plan.Accessibility,
            plan.Export,
            plan.LayoutConstraints,
            plan.StyleTokens);
    }

    internal static CorpusScientificModel BuildModel(ScientificFigureCorpusDefinitionItem item)
    {
        var baseline = item.Baseline;
        var sourceAssetId = StableGuid(item.ItemId, "source");
        var blocks = baseline.Anchors.Select((anchor, index) => ScientificSourceBlock.Create(
            anchor.AnchorId,
            string.Equals(anchor.EvidenceKind, "normalized-equation", StringComparison.Ordinal)
                ? ScientificSourceBlockKind.Formula
                : ScientificSourceBlockKind.Paragraph,
            ScientificSourceLocation.Create(
                index + 1,
                anchor.Location,
                boundingRegion: null,
                ScientificCharacterRange.Create(0, anchor.EvidenceText.Length)),
            anchor.EvidenceText,
            isRequired: true,
            string.Equals(anchor.EvidenceKind, "normalized-equation", StringComparison.Ordinal)
                ? ScientificRecoveryStatus.Recovered
                : ScientificRecoveryStatus.NotRequired)).ToArray();
        var extraction = ScientificDocumentExtraction.Create(
            sourceAssetId,
            baseline.SourceHash,
            ScientificExtractorIdentity.Create("accepted-corpus", "1"),
            ScientificExtractionQuality.Create(
                isScanned: false,
                ocrApplied: false,
                ScientificReadingOrderStatus.Reliable,
                ScientificRequiredContentStatus.Complete),
            blocks,
            []);
        var blocksById = blocks.ToDictionary(block => block.BlockId, StringComparer.Ordinal);
        var claims = baseline.Claims.Select(claim =>
        {
            var evidence = claim.AnchorIds.Select(anchorId => ClaimEvidenceLink.Create(
                extraction,
                blocksById[anchorId],
                blocksById[anchorId].OriginalText!,
                ClaimEvidenceRole.Support,
                confidence: 1,
                EvidenceValidationState.Validated)).ToArray();
            return ScientificClaim.Create(
                claim.ClaimId,
                item.Category == "concept-comparison"
                    ? ScientificClaimCategory.Comparison
                    : ScientificClaimCategory.Mechanism,
                claim.Text,
                claim.Text,
                confidence: 1,
                ScientificClaimStatus.Accepted,
                evidence);
        }).ToArray();
        var understanding = ScientificDocumentUnderstanding.Create(
            StableGuid(item.ItemId, "understanding"),
            extraction,
            baseline.FigureObjective,
            version: 1,
            terminology: [],
            claims,
            conflicts: [],
            coverage:
            [
                ScientificCoverageRequirement.Create(
                    "all-baseline-claims",
                    "All accepted baseline claims must remain mapped.",
                    isRequired: true,
                    ScientificCoverageStatus.Complete,
                    claims.Select(claim => claim.ClaimId).ToArray()),
            ]);
        var evidenceByAnchor = claims
            .SelectMany(claim => claim.EvidenceLinks.Select(evidence => new
            {
                evidence.SourceBlockId,
                Claim = claim,
                Evidence = evidence,
            }))
            .GroupBy(item => item.SourceBlockId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var elements = baseline.Elements.Select(element =>
        {
            var authority = evidenceByAnchor[element.AnchorIds[0]];
            return FigureElementSpec.Create(
                element.ElementId,
                element.Meaning,
                FigureElementKind.Entity,
                labelOrFormula: null,
                "deterministic-node",
                FigureContentRequirement.Required,
                isCritical: true,
                ScientificFigureProvenance.FromEvidence(authority.Claim, authority.Evidence));
        }).ToArray();
        var relations = baseline.Relations.Select(relation =>
        {
            var authority = evidenceByAnchor[relation.AnchorIds[0]];
            var (kind, direction) = MapRelation(relation.RelationClass);
            return FigureRelationSpec.Create(
                relation.RelationId,
                relation.SourceElementId,
                relation.TargetElementId,
                kind,
                direction,
                relation.RelationType,
                relation.RelationType,
                "Preserve baseline relation class, endpoints, and direction.",
                FigureContentRequirement.Required,
                isCritical: true,
                ScientificFigureProvenance.FromEvidence(authority.Claim, authority.Evidence));
        }).ToArray();
        var specification = ScientificFigureSpec.Create(
            StableGuid(item.ItemId, "specification"),
            understanding,
            baseline.FigureObjective,
            baseline.Claims[0].Text,
            "scientific figure evaluator",
            isSchematic: true,
            ScientificFigureRiskLevel.High,
            elements,
            relations,
            issues: []);
        var claimAnchorIds = baseline.Claims.SelectMany(claim => claim.AnchorIds)
            .ToHashSet(StringComparer.Ordinal);
        var allMapped = baseline.Elements.SelectMany(element => element.AnchorIds)
            .Concat(baseline.Relations.SelectMany(relation => relation.AnchorIds))
            .All(claimAnchorIds.Contains);
        return new CorpusScientificModel(extraction, understanding, specification, allMapped);
    }

    private static (FigureRelationKind Kind, FigureRelationDirection Direction) MapRelation(
        string relationClass)
    {
        return relationClass switch
        {
            "causal" => (FigureRelationKind.Causes, FigureRelationDirection.Directed),
            "directional" => (FigureRelationKind.TransfersTo, FigureRelationDirection.Directed),
            "comparative" => (FigureRelationKind.ComparesWith, FigureRelationDirection.Undirected),
            "associative-non-causal" => (FigureRelationKind.AssociatesWith, FigureRelationDirection.Undirected),
            _ => throw new InvalidDataException($"Unsupported relation class '{relationClass}'."),
        };
    }

    private static DateTimeOffset ParseReviewTime(string reviewedAt)
    {
        return DateTimeOffset.ParseExact(
            reviewedAt,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal);
    }

    private static Guid StableGuid(string itemId, string suffix)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{itemId}:{suffix}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    internal sealed record CorpusScientificModel(
        ScientificDocumentExtraction Extraction,
        ScientificDocumentUnderstanding Understanding,
        ScientificFigureSpec Specification,
        bool AllMapped);

    private sealed class PassingReviewProvider
        : IScientificSemanticReviewProvider, IScientificVisualReviewProvider
    {
        public static PassingReviewProvider Instance { get; } = new();

        public Task<ScientificProviderReviewResult> ReviewAsync(
            ScientificSemanticReviewRequest request,
            CancellationToken cancellationToken) => Pass(cancellationToken);

        public Task<ScientificProviderReviewResult> ReviewAsync(
            ScientificVisualReviewRequest request,
            CancellationToken cancellationToken) => Pass(cancellationToken);

        private static Task<ScientificProviderReviewResult> Pass(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ScientificProviderReviewResult(
                ScientificReviewVerdict.Pass,
                [],
                "corpus-fake-pass"));
        }
    }

    private sealed class MutationFindingProvider : IScientificVisualReviewProvider
    {
        private readonly ScientificFigureBaselineMutation _mutation;

        public MutationFindingProvider(ScientificFigureBaselineMutation mutation)
        {
            _mutation = mutation;
        }

        public Task<ScientificProviderReviewResult> ReviewAsync(
            ScientificVisualReviewRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var responsibleItemId = request.RegionCrops.FirstOrDefault()?.ResponsibleItemId
                ?? "full-resolution-output";
            return Task.FromResult(new ScientificProviderReviewResult(
                ScientificReviewVerdict.Fail,
                [new ScientificProviderFinding(
                    $"corpus-{_mutation.MutationId}",
                    ScientificProviderFindingKind.VisualDefect,
                    responsibleItemId,
                    _mutation.Description)],
                $"corpus-fake-{_mutation.MutationId}"));
        }
    }
}

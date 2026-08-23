using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace ContentDeliveryStudio.Application.ScientificFigures;

/// <summary>
/// Template-method skeleton shared by the deterministic per-domain article
/// reviewers. The base owns the admission pipeline (argument guards, Gate 1
/// boundary, located-evidence admission, evidence-board branch, hardened SVG
/// parse, report construction); each domain supplies its verbatim finding
/// codes, messages, corpus rules, and SVG content checks.
/// </summary>
public abstract class ArticleScientificReviewerBase : IArticleScientificFigureReviewer
{
    protected static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    public ArticleScientificReviewReport Review(
        ArticleScientificFigureCandidate candidate,
        ScientificSvgArtifact? artifact,
        ArticleSourceFigureAudit audit,
        ArticleSourceEvidenceBoard? board)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(audit);
        var findings = new List<ArticleScientificFinding>();
        var regions = BuildRegions(candidate);
        if (!candidate.RequiresGateOneApproval
            || candidate.GateOneStatus != ArticleScientificFigureGateStatus.PendingHumanApproval)
        {
            findings.Add(Finding(
                "article-gate-one-boundary-invalid",
                candidate.CandidateId,
                $"{GateSubject} candidates must remain pending explicit human Gate 1 approval."));
        }

        if (candidate.Evidence.Count == 0
            || candidate.Evidence.Any(item => string.IsNullOrWhiteSpace(item.SourceBlockId)
                || (RequireEvidenceExcerpt && string.IsNullOrWhiteSpace(item.Excerpt))))
        {
            findings.Add(Finding(EvidenceMissingCode, candidate.CandidateId, EvidenceMissingMessage));
        }

        CheckExtraAdmission(candidate, findings);

        if (candidate.Kind == ArticleScientificFigureCandidateKind.SourceEvidenceBoard)
        {
            CheckEvidenceBoard(candidate, audit, board, findings);
        }
        else
        {
            ReviewSvgArtifact(candidate, artifact, findings);
        }

        return new ArticleScientificReviewReport(
            PackageId,
            AuthorityBoundary,
            Array.AsReadOnly(findings.ToArray()),
            Array.AsReadOnly(regions.Select(item => item.ExpectedCheck).ToArray()));
    }

    private void ReviewSvgArtifact(
        ArticleScientificFigureCandidate candidate,
        ScientificSvgArtifact? artifact,
        ICollection<ArticleScientificFinding> findings)
    {
        if (artifact is null)
        {
            findings.Add(Finding(SvgMissingCode, candidate.CandidateId, SvgMissingMessage));
            return;
        }

        XDocument document;
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var textReader = new StringReader(artifact.Svg);
            using var xmlReader = XmlReader.Create(textReader, settings);
            document = XDocument.Load(xmlReader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            if (RethrowSvgParseErrors)
            {
                throw;
            }

            findings.Add(Finding(SvgInvalidCode, candidate.CandidateId, exception.Message));
            return;
        }

        var joined = string.Join("\n",
            document.Descendants(Svg + "text").Select(item => item.Value)
                .Concat(IncludeMathTexInCorpus
                    ? document.Descendants()
                        .Select(item => (string?)item.Attribute("data-math-tex"))
                        .Where(value => !string.IsNullOrWhiteSpace(value))!
                    : []));
        ReviewSvgContent(candidate, document, joined, findings);
    }

    public abstract IReadOnlyList<ArticleScientificVisualRegion> BuildRegions(
        ArticleScientificFigureCandidate candidate);

    protected abstract string PackageId { get; }

    protected abstract string AuthorityBoundary { get; }

    protected abstract string GateSubject { get; }

    protected abstract string EvidenceMissingCode { get; }

    protected virtual string EvidenceMissingMessage => "Deterministic checks require located source evidence.";

    protected virtual bool RequireEvidenceExcerpt => false;

    protected virtual void CheckExtraAdmission(
        ArticleScientificFigureCandidate candidate,
        ICollection<ArticleScientificFinding> findings)
    {
    }

    protected abstract string BoardInvalidCode { get; }

    protected abstract string BoardInvalidMessage { get; }

    protected virtual bool ValidateBoardAgainstAudit => false;

    protected virtual void CheckEvidenceBoard(
        ArticleScientificFigureCandidate candidate,
        ArticleSourceFigureAudit audit,
        ArticleSourceEvidenceBoard? board,
        ICollection<ArticleScientificFinding> findings)
    {
        if (board is null
            || board.PngBytes.Length == 0
            || board.SourceAssetIds.Count == 0
            || (ValidateBoardAgainstAudit
                && board.SourceAssetIds.Any(id => !audit.Assets.Any(asset => asset.AssetId == id))))
        {
            findings.Add(Finding(BoardInvalidCode, candidate.CandidateId, BoardInvalidMessage));
        }
    }

    protected abstract string SvgMissingCode { get; }

    protected abstract string SvgMissingMessage { get; }

    protected abstract string SvgInvalidCode { get; }

    /// <summary>Optical fails fast on malformed SVG instead of recording a finding.</summary>
    protected virtual bool RethrowSvgParseErrors => false;

    protected virtual bool IncludeMathTexInCorpus => false;

    protected abstract void ReviewSvgContent(
        ArticleScientificFigureCandidate candidate,
        XDocument document,
        string joined,
        ICollection<ArticleScientificFinding> findings);

    protected static ArticleScientificFinding Finding(string code, string id, string evidence) =>
        new(code, id, evidence);

    protected readonly record struct ArticleSvgLine(double X1, double Y1, double X2, double Y2);

    protected static ArticleSvgLine? TryReadSvgLine(string? value)
    {
        var parts = value?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not ["M", var x1, var y1, "L", var x2, var y2]
            || !double.TryParse(x1, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedX1)
            || !double.TryParse(y1, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedY1)
            || !double.TryParse(x2, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedX2)
            || !double.TryParse(y2, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedY2))
        {
            return null;
        }

        return new ArticleSvgLine(parsedX1, parsedY1, parsedX2, parsedY2);
    }

    protected static double Length(ArticleSvgLine line) =>
        Math.Sqrt(Math.Pow(line.X2 - line.X1, 2) + Math.Pow(line.Y2 - line.Y1, 2));

    protected static IReadOnlyList<ArticleSvgLine> ReadSvgRoleLines(XDocument document, string attributeName, string role) =>
        document.Descendants(Svg + "path")
            .Where(path => string.Equals((string?)path.Attribute(attributeName), role, StringComparison.Ordinal))
            .Select(path => TryReadSvgLine((string?)path.Attribute("d")))
            .Where(line => line is not null)
            .Select(line => line!.Value)
            .ToArray();
}

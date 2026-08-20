using System.Text.Json;
using ContentDeliveryStudio.Application.ScientificFigures;
using ContentDeliveryStudio.Infrastructure.ScientificFigures;

return args.FirstOrDefault() switch
{
    "promote-article-figure-set" => Promote(args[1..]),
    _ => Usage(),
};

static int Promote(string[] args)
{
    try
    {
        var options = ParseOptions(args);
        var actor = Required(options, "operator-kind") switch
        {
            "human" => ArticleScientificFigureApprovalActor.Human,
            "authorized_agent" => ArticleScientificFigureApprovalActor.AuthorizedAgent,
            var value => throw new ArgumentException($"Unsupported operator kind: {value}"),
        };
        var approvedAt = DateTimeOffset.Parse(
            Required(options, "approved-at"),
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind);
        var request = new ArticleScientificFigureDeliveryPromotionRequest(
            Required(options, "source"),
            Required(options, "delivery-root"),
            Required(options, "article-slug"),
            Required(options, "package-id"),
            Required(options, "reviewer"),
            actor,
            Optional(options, "authorization-reference"),
            GateOneApproved: Flag(options, "approve-gate-one"),
            Required(options, "gate-one-notes"),
            GateTwoApproved: Flag(options, "approve-gate-two"),
            Required(options, "gate-two-notes"),
            approvedAt);
        var result = new ArticleScientificFigureDeliveryPromoter().Promote(request);
        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
        }));
        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"[ERROR] {exception.Message}");
        return 1;
    }
}

static Dictionary<string, string?> ParseOptions(string[] args)
{
    var allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "source",
        "delivery-root",
        "article-slug",
        "package-id",
        "reviewer",
        "operator-kind",
        "authorization-reference",
        "approve-gate-one",
        "gate-one-notes",
        "approve-gate-two",
        "gate-two-notes",
        "approved-at",
    };
    var options = new Dictionary<string, string?>(StringComparer.Ordinal);
    for (var index = 0; index < args.Length; index++)
    {
        var token = args[index];
        if (!token.StartsWith("--", StringComparison.Ordinal) || token.Length == 2)
        {
            throw new ArgumentException($"Unexpected argument: {token}");
        }

        var name = token[2..];
        if (!allowed.Contains(name))
        {
            throw new ArgumentException($"Unknown option: --{name}");
        }

        if (!options.TryAdd(name, null))
        {
            throw new ArgumentException($"Duplicate option: --{name}");
        }

        if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            options[name] = args[++index];
        }
    }

    return options;
}

static string Required(IReadOnlyDictionary<string, string?> options, string name)
{
    if (!options.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
    {
        throw new ArgumentException($"Required option is missing: --{name}");
    }

    return value;
}

static string? Optional(IReadOnlyDictionary<string, string?> options, string name) =>
    options.TryGetValue(name, out var value) ? value : null;

static bool Flag(IReadOnlyDictionary<string, string?> options, string name)
{
    if (!options.TryGetValue(name, out var value))
    {
        return false;
    }

    if (value is not null)
    {
        throw new ArgumentException($"Flag does not accept a value: --{name}");
    }

    return true;
}

static int Usage()
{
    Console.Error.WriteLine(
        "Usage: ContentDeliveryStudio.Tools promote-article-figure-set "
        + "--source <review-ready-dir> --delivery-root <dir> --article-slug <slug> "
        + "--package-id <id> --reviewer <name> --operator-kind <human|authorized_agent> "
        + "[--authorization-reference <text>] --approve-gate-one --gate-one-notes <text> "
        + "--approve-gate-two --gate-two-notes <text> --approved-at <ISO-8601>");
    return 2;
}

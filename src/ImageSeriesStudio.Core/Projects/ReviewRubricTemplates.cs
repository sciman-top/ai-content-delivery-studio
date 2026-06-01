namespace ImageSeriesStudio.Core.Projects;

public sealed record ReviewRubricTemplate(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<ReviewRubricDimensionTemplate> Dimensions)
{
    public ReviewRubric CreateRubric(Guid projectId, DateTimeOffset createdAt)
    {
        return new ReviewRubric(
            Guid.NewGuid(),
            projectId,
            Name,
            Dimensions
                .Select(dimension => new ReviewRubricDimension(
                    dimension.Name,
                    dimension.Requirement,
                    dimension.Weight))
                .ToArray(),
            createdAt);
    }
}

public sealed record ReviewRubricDimensionTemplate(
    string Name,
    string Requirement,
    int Weight);

public static class ReviewRubricTemplateCatalog
{
    public const string GeneralImage = "general-image";

    public const string TextHeavyPoster = "text-heavy-poster";

    public const string SeriesConsistency = "series-consistency";

    private static readonly IReadOnlyList<ReviewRubricTemplate> Templates =
    [
        new ReviewRubricTemplate(
            GeneralImage,
            "General image quality",
            "Balanced rubric for prompt alignment, composition, visual quality, and policy-safe output.",
            [
                new ReviewRubricDimensionTemplate("match", "Candidate should satisfy the prompt and item brief.", 3),
                new ReviewRubricDimensionTemplate("composition", "Main subject, framing, and visual hierarchy should be clear.", 2),
                new ReviewRubricDimensionTemplate("visual_quality", "Image should be coherent, polished, and free of obvious artifacts.", 2),
                new ReviewRubricDimensionTemplate("delivery_fit", "Output should be suitable for the intended audience and delivery format.", 1),
            ]),
        new ReviewRubricTemplate(
            TextHeavyPoster,
            "Text-heavy poster",
            "Rubric for educational posters, diagrams, and assets where readable text matters.",
            [
                new ReviewRubricDimensionTemplate("prompt_match", "Candidate should satisfy the prompt and item brief.", 3),
                new ReviewRubricDimensionTemplate("text_space", "Layout should reserve clean areas for deterministic text composition.", 3),
                new ReviewRubricDimensionTemplate("diagram_clarity", "Diagram structure should be easy to understand and not visually crowded.", 2),
                new ReviewRubricDimensionTemplate("visual_quality", "Image should be coherent, polished, and free of obvious artifacts.", 2),
            ]),
        new ReviewRubricTemplate(
            SeriesConsistency,
            "Series consistency",
            "Rubric for comparing candidates across a multi-image series.",
            [
                new ReviewRubricDimensionTemplate("style_consistency", "Candidate should match the established series style.", 3),
                new ReviewRubricDimensionTemplate("character_consistency", "Recurring characters, objects, and motifs should remain recognizable.", 2),
                new ReviewRubricDimensionTemplate("narrative_progression", "Candidate should advance the sequence without repeating prior items.", 2),
                new ReviewRubricDimensionTemplate("delivery_fit", "Output should be suitable for the intended audience and delivery format.", 1),
            ]),
    ];

    public static IReadOnlyList<ReviewRubricTemplate> All => Templates;

    public static ReviewRubricTemplate GetById(string id)
    {
        return Templates.SingleOrDefault(template => template.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Review rubric template not found: {id}");
    }
}

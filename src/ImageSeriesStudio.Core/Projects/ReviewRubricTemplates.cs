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

    public const string EditorialIllustration = "editorial-illustration";

    public const string EducationalAccuracy = "educational-accuracy";

    public const string ScholarlySchematic = "scholarly-schematic";

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
            EditorialIllustration,
            "Editorial illustration",
            "Rubric for article and essay illustrations grounded in source material and ready for publication.",
            [
                new ReviewRubricDimensionTemplate("requirement_match", "Candidate should satisfy the editorial brief and intended placement.", 3),
                new ReviewRubricDimensionTemplate("source_evidence_fit", "Visual claims should align with the supplied source evidence and article context.", 3),
                new ReviewRubricDimensionTemplate("visual_hierarchy", "Composition should guide attention clearly without distracting from the document.", 2),
                new ReviewRubricDimensionTemplate("delivery_readiness", "Output should be polished and suitable for final document delivery.", 1),
            ]),
        new ReviewRubricTemplate(
            EducationalAccuracy,
            "Educational accuracy",
            "Rubric for explanatory diagrams and instructional illustrations that must preserve concept accuracy.",
            [
                new ReviewRubricDimensionTemplate("concept_accuracy", "Concepts, relationships, and labels should be scientifically or educationally accurate.", 3),
                new ReviewRubricDimensionTemplate("source_evidence_fit", "Visual content should match the supplied lesson, source, or reference evidence.", 3),
                new ReviewRubricDimensionTemplate("text_policy", "Text-dependent details should follow the preset text rendering policy.", 2),
                new ReviewRubricDimensionTemplate("diagram_clarity", "Diagram structure should be readable, uncluttered, and easy to explain.", 2),
            ]),
        new ReviewRubricTemplate(
            ScholarlySchematic,
            "Scholarly schematic",
            "Rubric for research-style schematics and graphical abstracts that avoid unsupported evidence.",
            [
                new ReviewRubricDimensionTemplate("no_fake_evidence", "Candidate should not invent citations, measurements, apparatus, data, or findings.", 3),
                new ReviewRubricDimensionTemplate("source_evidence_fit", "Schematic elements should be traceable to the provided source evidence.", 3),
                new ReviewRubricDimensionTemplate("schematic_clarity", "Visual encoding, flow, and grouping should communicate the scholarly argument clearly.", 2),
                new ReviewRubricDimensionTemplate("text_policy", "Labels and text-heavy details should follow the preset text rendering policy.", 2),
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

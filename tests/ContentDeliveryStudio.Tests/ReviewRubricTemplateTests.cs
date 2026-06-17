using ContentDeliveryStudio.Core.Projects;

namespace ContentDeliveryStudio.Tests;

public sealed class ReviewRubricTemplateTests
{
    [Fact]
    public void Catalog_ContainsExpectedTemplatesWithUniqueIds()
    {
        var templates = ReviewRubricTemplateCatalog.All;

        Assert.Contains(templates, template => template.Id == ReviewRubricTemplateCatalog.GeneralImage);
        Assert.Contains(templates, template => template.Id == ReviewRubricTemplateCatalog.TextHeavyPoster);
        Assert.Contains(templates, template => template.Id == ReviewRubricTemplateCatalog.SeriesConsistency);
        Assert.Equal(templates.Count, templates.Select(template => template.Id.ToLowerInvariant()).Distinct().Count());
    }

    [Fact]
    public void Templates_DeclarePositiveWeightedDimensions()
    {
        foreach (var template in ReviewRubricTemplateCatalog.All)
        {
            Assert.NotEmpty(template.Name);
            Assert.NotEmpty(template.Description);
            Assert.NotEmpty(template.Dimensions);
            Assert.All(template.Dimensions, dimension =>
            {
                Assert.NotEmpty(dimension.Name);
                Assert.NotEmpty(dimension.Requirement);
                Assert.True(dimension.Weight > 0);
            });
        }
    }

    [Fact]
    public void Catalog_IncludesDocumentIllustrationRubrics()
    {
        var templates = ReviewRubricTemplateCatalog.All.Select(template => template.Id).ToArray();

        Assert.Contains(ReviewRubricTemplateCatalog.EditorialIllustration, templates);
        Assert.Contains(ReviewRubricTemplateCatalog.EducationalAccuracy, templates);
        Assert.Contains(ReviewRubricTemplateCatalog.ScholarlySchematic, templates);

        var scholarly = ReviewRubricTemplateCatalog.GetById(ReviewRubricTemplateCatalog.ScholarlySchematic);
        Assert.Contains(scholarly.Dimensions, dimension => dimension.Name == "no_fake_evidence");
        Assert.Contains(scholarly.Dimensions, dimension => dimension.Name == "source_evidence_fit");
    }

    [Fact]
    public void CreateRubric_CopiesTemplateDimensionsIntoProjectRubric()
    {
        var projectId = Guid.NewGuid();
        var createdAt = DateTimeOffset.Parse("2026-06-01T10:00:00Z");
        var template = ReviewRubricTemplateCatalog.GetById(ReviewRubricTemplateCatalog.TextHeavyPoster);

        var rubric = template.CreateRubric(projectId, createdAt);

        Assert.Equal(projectId, rubric.ProjectId);
        Assert.Equal(template.Name, rubric.Name);
        Assert.Equal(createdAt, rubric.CreatedAt);
        Assert.Equal(template.Dimensions.Count, rubric.Dimensions.Count);
        Assert.Contains(rubric.Dimensions, dimension => dimension.Name == "text_space");
    }

    [Fact]
    public void GetById_ThrowsForUnknownTemplate()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ReviewRubricTemplateCatalog.GetById("missing-template"));

        Assert.Contains("missing-template", exception.Message);
    }
}

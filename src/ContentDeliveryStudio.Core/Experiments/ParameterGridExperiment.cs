using System.Text;
using ContentDeliveryStudio.Core.Projects;
using ContentDeliveryStudio.Core.Styles;

namespace ContentDeliveryStudio.Core.Experiments;

public sealed record ParameterGridAxis(string Name, IReadOnlyList<string> Values);

public sealed record ParameterGridVariant(
    int Number,
    string Slug,
    string BasePrompt,
    string PromptText,
    GenerationSettings Settings,
    IReadOnlyDictionary<string, string> ParameterValues,
    IReadOnlyList<ParameterGridAxis> Axes,
    GenerationRecipe? Recipe = null,
    Guid? GenerationTaskId = null)
{
    public ParameterGridVariant WithGenerationTask(Guid generationTaskId)
    {
        if (generationTaskId == Guid.Empty)
        {
            throw new ArgumentException("Generation task id cannot be empty.", nameof(generationTaskId));
        }

        return this with { GenerationTaskId = generationTaskId };
    }
}

public static class ParameterGridExperiment
{
    public static IReadOnlyList<ParameterGridVariant> CreateVariants(
        string basePrompt,
        GenerationSettings settings,
        IReadOnlyList<ParameterGridAxis> axes,
        GenerationRecipe? recipe = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(axes);

        var prompt = RequireText(basePrompt, nameof(basePrompt));
        var normalizedAxes = NormalizeAxes(axes);
        var variantAxes = normalizedAxes
            .Select(axis => new ParameterGridAxis(axis.Name, axis.Values))
            .ToArray();
        var combinations = BuildCombinations(normalizedAxes);

        return combinations
            .Select((parameters, index) => new ParameterGridVariant(
                index + 1,
                BuildSlug(index + 1, parameters),
                prompt,
                BuildPrompt(prompt, parameters),
                settings,
                parameters,
                variantAxes,
                recipe))
            .ToArray();
    }

    private static IReadOnlyList<NormalizedAxis> NormalizeAxes(IReadOnlyList<ParameterGridAxis> axes)
    {
        if (axes.Count == 0)
        {
            throw new ArgumentException("At least one parameter axis is required.", nameof(axes));
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalizedAxes = new List<NormalizedAxis>(axes.Count);

        foreach (var axis in axes)
        {
            var name = RequireText(axis.Name, nameof(axis.Name));
            if (!names.Add(name))
            {
                throw new ArgumentException($"Duplicate parameter axis name: {name}.", nameof(axes));
            }

            if (axis.Values.Count == 0)
            {
                throw new ArgumentException($"Parameter axis '{name}' must have at least one value.", nameof(axes));
            }

            var values = axis.Values
                .Select(value => RequireText(value, nameof(axis.Values)))
                .ToArray();

            normalizedAxes.Add(new NormalizedAxis(name, values));
        }

        return normalizedAxes;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> BuildCombinations(
        IReadOnlyList<NormalizedAxis> axes)
    {
        var combinations = new List<IReadOnlyDictionary<string, string>>();
        var current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void AddAxis(int axisIndex)
        {
            if (axisIndex == axes.Count)
            {
                combinations.Add(new Dictionary<string, string>(current, StringComparer.OrdinalIgnoreCase));
                return;
            }

            var axis = axes[axisIndex];
            foreach (var value in axis.Values)
            {
                current[axis.Name] = value;
                AddAxis(axisIndex + 1);
            }
        }

        AddAxis(0);
        return combinations;
    }

    private static string BuildPrompt(string basePrompt, IReadOnlyDictionary<string, string> parameters)
    {
        var result = basePrompt;
        var replacedAnyToken = false;

        foreach (var (name, value) in parameters)
        {
            var token = "{{" + name + "}}";
            if (result.Contains(token, StringComparison.Ordinal))
            {
                replacedAnyToken = true;
                result = result.Replace(token, value, StringComparison.Ordinal);
            }
        }

        if (replacedAnyToken)
        {
            return result;
        }

        return result + "\n\nParameters: " + FormatParameters(parameters);
    }

    private static string BuildSlug(int number, IReadOnlyDictionary<string, string> parameters)
    {
        var suffix = string.Join(
            "-",
            parameters.SelectMany(parameter => new[] { Slugify(parameter.Key), Slugify(parameter.Value) }));
        return $"{number:000}-{suffix}";
    }

    private static string FormatParameters(IReadOnlyDictionary<string, string> parameters)
    {
        return string.Join("; ", parameters.Select(parameter => $"{parameter.Key}={parameter.Value}"));
    }

    private static string Slugify(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    private sealed record NormalizedAxis(string Name, IReadOnlyList<string> Values);
}

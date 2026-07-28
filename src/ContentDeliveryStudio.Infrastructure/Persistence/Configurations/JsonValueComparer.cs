using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ContentDeliveryStudio.Infrastructure.Persistence.Configurations;

internal sealed class JsonValueComparer<T> : ValueComparer<T>
{
    public JsonValueComparer(Func<T, string> serialize, Func<string, T> deserialize)
        : base(
            (left, right) => ValuesEqual(left, right, serialize),
            value => GetValueHashCode(value, serialize),
            value => CreateSnapshot(value, serialize, deserialize))
    {
    }

    private static bool ValuesEqual(T? left, T? right, Func<T, string> serialize)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        var leftJson = serialize(left);
        var rightJson = serialize(right);
        return string.Equals(leftJson, rightJson, StringComparison.Ordinal)
            || string.Equals(Canonicalize(leftJson), Canonicalize(rightJson), StringComparison.Ordinal);
    }

    private static int GetValueHashCode(T? value, Func<T, string> serialize)
    {
        return value is null
            ? 0
            : StringComparer.Ordinal.GetHashCode(Canonicalize(serialize(value)));
    }

    private static T CreateSnapshot(T value, Func<T, string> serialize, Func<string, T> deserialize)
    {
        return value is null
            ? default!
            : deserialize(serialize(value));
    }

    private static string Canonicalize(string json)
    {
        using var document = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonicalJson(writer, document.RootElement);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonicalJson(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalJson(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonicalJson(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText(), skipInputValidation: true);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new InvalidOperationException($"Unsupported JSON value kind: {element.ValueKind}.");
        }
    }
}

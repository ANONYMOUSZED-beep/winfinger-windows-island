using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinFinger.Models;

[JsonConverter(typeof(ClipboardEntryKindConverter))]
public enum ClipboardEntryKind
{
    Text,
    Image
}

/// <summary>Serialises the kind as lowercase "text"/"image" (mac clipboard.json compatible).</summary>
public sealed class ClipboardEntryKindConverter : JsonConverter<ClipboardEntryKind>
{
    public override ClipboardEntryKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString() == "image" ? ClipboardEntryKind.Image : ClipboardEntryKind.Text;

    public override void Write(Utf8JsonWriter writer, ClipboardEntryKind value, JsonSerializerOptions options)
        => writer.WriteStringValue(value == ClipboardEntryKind.Image ? "image" : "text");
}

/// <summary>One clipboard history record (field-compatible with mac's clipboard.json).</summary>
public sealed record ClipboardEntry(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("kind")] ClipboardEntryKind Kind,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("imagePath")] string? ImagePath,
    [property: JsonPropertyName("sourceAppBundleId")] string? SourceAppBundleId,
    [property: JsonPropertyName("sourceAppName")] string? SourceAppName,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("contentHash")] string ContentHash);

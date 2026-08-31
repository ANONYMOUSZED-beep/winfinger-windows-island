using System.Text.Json.Serialization;

namespace WinFinger.Models;

public sealed record ShortcutItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("keys")] string Keys,
    [property: JsonPropertyName("action")] string Action);

public sealed record ShortcutGroup(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("items")] IReadOnlyList<ShortcutItem> Items);

public sealed record ShortcutSet(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("processNames")] IReadOnlyList<string> ProcessNames,
    [property: JsonPropertyName("groups")] IReadOnlyList<ShortcutGroup> Groups);

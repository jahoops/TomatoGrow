using System.Collections.Generic;

namespace TomatoGrow;

public sealed record ThemePackManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> ContentModuleIds { get; init; }
    public required string ThemeVocabularyId { get; init; }
    public required string InterfacePackId { get; init; }
    public bool IsDefault { get; init; }
    public string Notes { get; init; } = string.Empty;
}
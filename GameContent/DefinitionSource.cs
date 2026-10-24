namespace TomatoGrow.GameContent;

public sealed record DefinitionSource(
    string ModuleName,
    string FilePath,
    string DefinitionType,
    string DefinitionId);
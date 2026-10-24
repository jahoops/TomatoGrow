namespace TomatoGrow.GameContent;

public sealed record LoadedDefinition<TDefinition>(
    TDefinition Definition,
    DefinitionSource Source);
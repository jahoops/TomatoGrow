using System.Collections.Generic;

namespace TomatoGrow;

public sealed record GameStateFactoryResult
{
    public bool Success { get; init; }
    public TurnipTestState State { get; init; }
    public GameContent.ContentRegistry ContentRegistry { get; init; }
    public ThemePackManifest Manifest { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static GameStateFactoryResult CreateSuccess(
        TurnipTestState state,
        GameContent.ContentRegistry contentRegistry,
        ThemePackManifest manifest)
    {
        return new GameStateFactoryResult
        {
            Success = true,
            State = state,
            ContentRegistry = contentRegistry,
            Manifest = manifest,
            Errors = []
        };
    }

    public static GameStateFactoryResult CreateFailure(params string[] errors)
    {
        return new GameStateFactoryResult
        {
            Success = false,
            Errors = errors
        };
    }

    public static GameStateFactoryResult CreateFailure(IReadOnlyList<string> errors)
    {
        return new GameStateFactoryResult
        {
            Success = false,
            Errors = errors
        };
    }
}
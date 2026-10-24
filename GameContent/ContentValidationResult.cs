using System.Collections.Generic;

namespace TomatoGrow.GameContent;

public sealed class ContentValidationResult
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public bool IsValid => Errors.Count == 0;
}
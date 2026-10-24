namespace TomatoGrow.GameContent.Definitions;

public sealed class TileProducerDefinition
{
    public string Id { get; set; } = string.Empty;
    public string StarterItemId { get; set; } = string.Empty;
    public string OutputItemId { get; set; } = string.Empty;
    public int ProductionDays { get; set; }
}
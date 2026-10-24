namespace TomatoGrow.GameContent.Definitions;

public sealed class CropDefinition
{
    public string Id { get; set; } = string.Empty;
    public string SeedItemId { get; set; } = string.Empty;
    public string HarvestItemId { get; set; } = string.Empty;
    public int GrowthDays { get; set; }
}
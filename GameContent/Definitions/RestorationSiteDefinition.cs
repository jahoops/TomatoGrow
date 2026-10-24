namespace TomatoGrow.GameContent.Definitions;

public sealed class RestorationSiteDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Cost { get; set; }
    public string RewardType { get; set; } = string.Empty;
    public int RewardValue { get; set; }
    public string RewardDescription { get; set; } = string.Empty;
}
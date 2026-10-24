namespace TomatoGrow.GameContent.Definitions;

public sealed class ItemDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int BuyPrice { get; set; }
    public int SellPrice { get; set; }
}
namespace TomatoGrow.GameContent.Definitions;

public sealed class MarketOfferDefinition
{
    public string Id { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int Price { get; set; }
    public string Category { get; set; } = string.Empty;
}
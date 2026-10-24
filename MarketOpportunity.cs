using System;

namespace TomatoGrow;

public sealed class MarketOpportunity
{
    public string Id { get; set; }
    public string ItemId { get; set; }
    public double SellPriceMultiplier { get; set; }
    public int DaysRemaining { get; set; }
    public string DisplayName { get; set; }
    public string Message { get; set; }

    public bool IsActive => !string.IsNullOrWhiteSpace(ItemId) && SellPriceMultiplier > 0 && DaysRemaining > 0;

    public MarketOpportunity Clone()
    {
        return new MarketOpportunity
        {
            Id = Id,
            ItemId = ItemId,
            SellPriceMultiplier = SellPriceMultiplier,
            DaysRemaining = DaysRemaining,
            DisplayName = DisplayName,
            Message = Message
        };
    }

    public int GetMultiplierPercent()
    {
        return (int)Math.Round(SellPriceMultiplier * 100, MidpointRounding.AwayFromZero);
    }
}

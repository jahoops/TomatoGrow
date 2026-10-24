namespace TomatoGrow;

public enum ActivityLogCategory
{
    Farm,
    Restoration,
    Market,
    Machine,
    Storage,
    Sale,
    Bot,
    Expansion,
    Autoplay,
    System
}

public readonly record struct ActivityLogEntry(
    int Day,
    ActivityLogCategory Category,
    string Message,
    int? ValueAmount = null,
    string ItemId = null,
    int? Quantity = null);
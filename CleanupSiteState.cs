namespace TomatoGrow;

public sealed class CleanupSiteState
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Cost { get; init; }
    public string RewardDescription { get; init; } = string.Empty;
    public bool IsCleared { get; set; }

    public CleanupSiteState Clone()
    {
        return new CleanupSiteState
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Cost = Cost,
            RewardDescription = RewardDescription,
            IsCleared = IsCleared
        };
    }
}
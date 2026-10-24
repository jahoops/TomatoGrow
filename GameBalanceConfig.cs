namespace TomatoGrow;

public sealed record GameBalanceConfig
{
    public static GameBalanceConfig Default { get; } = new()
    {
        PlanterBotCost = 60,
        WateringBotCost = 90,
        HarvesterBotCost = 120,
        FirstExpansionCost = 150,
        SecondExpansionCost = 300,
        AutoPlayTargetMoney = 3000,
        AutoPlayMaxDays = 365,
        AutoPlayMaxSteps = 5000,
        AutoPlayNoActionLimit = 6,
        ActivityLogMaxEntries = 50,
        ProgressionSimulationDefaultTargetMoney = 3000,
        ProgressionSimulationDefaultMaxDays = 365,
        ProgressionSimulationDefaultMaxSteps = 10000,
        MarketOpportunityGenerationIntervalDays = 30,
        MarketOpportunityGeneratedDurationDays = 20,
        MarketOpportunityGeneratedMultiplier = 1.5,
        MarketOpportunityDefaultDurationDays = 30,
        MarketOpportunityDefaultMultiplier = 1.5
    };

    public int PlanterBotCost { get; init; }
    public int WateringBotCost { get; init; }
    public int HarvesterBotCost { get; init; }
    public int FirstExpansionCost { get; init; }
    public int SecondExpansionCost { get; init; }
    public int AutoPlayTargetMoney { get; init; }
    public int AutoPlayMaxDays { get; init; }
    public int AutoPlayMaxSteps { get; init; }
    public int AutoPlayNoActionLimit { get; init; }
    public int ActivityLogMaxEntries { get; init; }
    public int ProgressionSimulationDefaultTargetMoney { get; init; }
    public int ProgressionSimulationDefaultMaxDays { get; init; }
    public int ProgressionSimulationDefaultMaxSteps { get; init; }
    public int MarketOpportunityGenerationIntervalDays { get; init; }
    public int MarketOpportunityGeneratedDurationDays { get; init; }
    public double MarketOpportunityGeneratedMultiplier { get; init; }
    public int MarketOpportunityDefaultDurationDays { get; init; }
    public double MarketOpportunityDefaultMultiplier { get; init; }

    public AutoPlaySettings CreateAutoPlaySettings()
    {
        return AutoPlaySettings.CreateDefault(this);
    }

    public ProgressionSimulationSettings CreateProgressionSimulationSettings()
    {
        return ProgressionSimulationSettings.CreateDefault(this);
    }
}
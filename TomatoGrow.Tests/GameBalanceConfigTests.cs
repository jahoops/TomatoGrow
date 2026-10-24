using System;
using System.Linq;
using System.Reflection;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class GameBalanceConfigTests
{
    [Fact]
    public void DefaultConfig_PreservesCurrentBalanceValues()
    {
        GameBalanceConfig config = GameBalanceConfig.Default;

        Assert.Equal(60, config.PlanterBotCost);
        Assert.Equal(90, config.WateringBotCost);
        Assert.Equal(120, config.HarvesterBotCost);
        Assert.Equal(150, config.FirstExpansionCost);
        Assert.Equal(300, config.SecondExpansionCost);
        Assert.Equal(3000, config.AutoPlayTargetMoney);
        Assert.Equal(365, config.AutoPlayMaxDays);
        Assert.Equal(5000, config.AutoPlayMaxSteps);
        Assert.Equal(6, config.AutoPlayNoActionLimit);
        Assert.Equal(50, config.ActivityLogMaxEntries);
        Assert.Equal(3000, config.ProgressionSimulationDefaultTargetMoney);
        Assert.Equal(365, config.ProgressionSimulationDefaultMaxDays);
        Assert.Equal(10000, config.ProgressionSimulationDefaultMaxSteps);
        Assert.Equal(30, config.MarketOpportunityGenerationIntervalDays);
        Assert.Equal(20, config.MarketOpportunityGeneratedDurationDays);
        Assert.Equal(1.5, config.MarketOpportunityGeneratedMultiplier);
        Assert.Equal(30, config.MarketOpportunityDefaultDurationDays);
        Assert.Equal(1.5, config.MarketOpportunityDefaultMultiplier);
    }

    [Fact]
    public void DefaultConfigBehavior_RemainsUnchanged()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState unlockState = new(registry);
        TurnipTestState expansionState = new(registry);
        SetMoney(unlockState, GameBalanceConfig.Default.PlanterBotCost);
        SetMoney(expansionState, GameBalanceConfig.Default.FirstExpansionCost);

        string unlockMessage = unlockState.UnlockPlanterBot();
        string expansionMessage = expansionState.ExpandFarm();

        Assert.Equal("Unlocked Planter Bot.", unlockMessage);
        Assert.Equal("Expanded farm to 4x3 for 150.", expansionMessage);
    }

    [Fact]
    public void UnlockingBot_UsesCustomConfigValue()
    {
        ContentRegistry registry = BuildRegistry();
        GameBalanceConfig config = GameBalanceConfig.Default with { PlanterBotCost = 1 };
        TurnipTestState state = new(registry, balanceConfig: config);
        SetMoney(state, 1);

        string message = state.UnlockPlanterBot();

        Assert.Equal("Unlocked Planter Bot.", message);
        Assert.True(state.IsPlanterBotUnlocked);
        Assert.Equal(0, state.Money);
    }

    [Fact]
    public void ExpandingFarm_UsesCustomConfigValue()
    {
        ContentRegistry registry = BuildRegistry();
        GameBalanceConfig config = GameBalanceConfig.Default with { FirstExpansionCost = 1 };
        TurnipTestState state = new(registry, balanceConfig: config);
        SetMoney(state, 1);

        string message = state.ExpandFarm();

        Assert.Equal("Expanded farm to 4x3 for 1.", message);
        Assert.Equal(0, state.Money);
        Assert.Equal(4, state.FarmGrid.Width);
    }

    [Fact]
    public void AutoPlayController_DefaultSettings_UseConfigValues()
    {
        ContentRegistry registry = BuildRegistry();
        GameBalanceConfig config = GameBalanceConfig.Default with
        {
            AutoPlayTargetMoney = 1,
            AutoPlayMaxDays = 12,
            AutoPlayMaxSteps = 34,
            AutoPlayNoActionLimit = 2
        };
        TurnipTestState state = new(registry, balanceConfig: config);
        AutoPlayTestController controller = new(state, registry);
        SetMoney(state, 1);
        controller.Enable();

        string message = controller.RunStep();

        Assert.Equal(1, controller.Settings.TargetMoney);
        Assert.Equal(12, controller.Settings.MaxDays);
        Assert.Equal(34, controller.Settings.MaxSteps);
        Assert.Equal(2, controller.Settings.MaxNoActionSteps);
        Assert.Equal(AutoPlayStopReason.TargetMoneyReached, controller.Stats.StopReason);
        Assert.Equal("Auto: stopped, reached $1.", message);
    }

    [Fact]
    public void ProgressionSimulationRunner_DefaultSettings_UseConfigValues()
    {
        ContentRegistry registry = BuildRegistry();
        GameBalanceConfig config = GameBalanceConfig.Default with
        {
            ProgressionSimulationDefaultTargetMoney = 0,
            ProgressionSimulationDefaultMaxDays = 12,
            ProgressionSimulationDefaultMaxSteps = 34,
            AutoPlayNoActionLimit = 2,
            MarketOpportunityDefaultDurationDays = 14,
            MarketOpportunityDefaultMultiplier = 1.75
        };
        ProgressionSimulationRunner runner = new(registry, config);

        ProgressionSimulationResult result = runner.Run();

        Assert.Equal(0, result.Settings.TargetMoney);
        Assert.Equal(12, result.Settings.MaxDays);
        Assert.Equal(34, result.Settings.MaxSteps);
        Assert.Equal(2, result.Settings.MaxNoActionSteps);
        Assert.Equal(14, result.Settings.MarketOpportunityDurationDays);
        Assert.Equal(1.75, result.Settings.MarketOpportunitySellPriceMultiplier);
        Assert.Equal(AutoPlayStopReason.TargetMoneyReached, result.StopReason);
    }

    [Fact]
    public void ActivityLogMaxEntries_UsesConfigValue()
    {
        ContentRegistry registry = BuildRegistry();
        GameBalanceConfig config = GameBalanceConfig.Default with { ActivityLogMaxEntries = 2 };
        TurnipTestState state = new(registry, balanceConfig: config);

        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "turnip_seed");
        state.PlantSeedAtPlot(2, 0, "turnip_seed");

        Assert.Equal(2, state.ActivityLog.Count);
        Assert.Contains("1,0", state.ActivityLog[0].Message, StringComparison.Ordinal);
        Assert.Contains("2,0", state.ActivityLog[1].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveLoadRoundTrip_DoesNotSerializeBalanceConfig()
    {
        ContentRegistry registry = BuildRegistry();
        GameBalanceConfig config = GameBalanceConfig.Default with { PlanterBotCost = 1 };
        TurnipTestState state = new(registry, balanceConfig: config);
        SetMoney(state, 1);
        state.UnlockPlanterBot();
        SaveGameFileService service = new();

        string json = service.Serialize(state.CreateSaveGame());
        SaveGameState saveGame = service.Deserialize(json);
        TurnipTestState loadedState = new(registry);

        loadedState.ApplySaveGame(saveGame);

        Assert.DoesNotContain("PlanterBotCost", json, StringComparison.Ordinal);
        Assert.True(loadedState.IsPlanterBotUnlocked);
        Assert.Equal(GameBalanceConfig.Default.PlanterBotCost, loadedState.BalanceConfig.PlanterBotCost);
    }

    private static void SetMoney(TurnipTestState state, int amount)
    {
        typeof(TurnipTestState)
            .GetProperty("Money", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(state, amount);
    }

    private static ContentRegistry BuildRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "grape_seed", Name = "Grape Seeds", Type = "seed", BuyPrice = 15, SellPrice = 1 },
                new ItemDefinition { Id = "grape", Name = "Grapes", Type = "crop", BuyPrice = 0, SellPrice = 45 },
                new ItemDefinition { Id = "hops_seed", Name = "Hops Seeds", Type = "seed", BuyPrice = 14, SellPrice = 1 },
                new ItemDefinition { Id = "hops", Name = "Hops", Type = "crop", BuyPrice = 0, SellPrice = 40 },
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed", BuyPrice = 10, SellPrice = 1 },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop", BuyPrice = 0, SellPrice = 30 },
                new ItemDefinition { Id = "wine", Name = "Wine", Type = "processed_good", BuyPrice = 0, SellPrice = 160 },
                new ItemDefinition { Id = "beer", Name = "Beer", Type = "processed_good", BuyPrice = 0, SellPrice = 140 },
                new ItemDefinition { Id = "aged_wine", Name = "Aged Wine", Type = "processed_good", BuyPrice = 0, SellPrice = 260 }
            ],
            [
                new CropDefinition { Id = "grape", SeedItemId = "grape_seed", HarvestItemId = "grape", GrowthDays = 4 },
                new CropDefinition { Id = "hops", SeedItemId = "hops_seed", HarvestItemId = "hops", GrowthDays = 4 },
                new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 }
            ],
            [
                new MachineDefinition { Id = "fermenter", Name = "Fermenter", Type = "processor_machine" },
                new MachineDefinition { Id = "aging_barrel", Name = "Aging Barrel", Type = "processor_machine" }
            ],
            [
                new ProcessingRecipeDefinition
                {
                    Id = "grape_to_wine",
                    MachineId = "fermenter",
                    InputItemId = "grape",
                    InputQuantity = 3,
                    OutputItemId = "wine",
                    OutputQuantity = 1,
                    ProcessingDays = 2
                },
                new ProcessingRecipeDefinition
                {
                    Id = "hops_to_beer",
                    MachineId = "fermenter",
                    InputItemId = "hops",
                    InputQuantity = 3,
                    OutputItemId = "beer",
                    OutputQuantity = 1,
                    ProcessingDays = 2
                },
                new ProcessingRecipeDefinition
                {
                    Id = "wine_to_aged_wine",
                    MachineId = "aging_barrel",
                    InputItemId = "wine",
                    InputQuantity = 1,
                    OutputItemId = "aged_wine",
                    OutputQuantity = 1,
                    ProcessingDays = 5
                }
            ],
            [
                new MarketOfferDefinition { Id = "buy_grapes", ItemId = "grape", Quantity = 3, Price = 160, Category = "raw_input" },
                new MarketOfferDefinition { Id = "buy_hops", ItemId = "hops", Quantity = 3, Price = 120, Category = "raw_input" },
                new MarketOfferDefinition { Id = "buy_wine", ItemId = "wine", Quantity = 1, Price = 190, Category = "intermediate_input" }
            ],
            TestContentFactory.DefaultRestorationSites());
    }
}
using System.Reflection;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class GameStatsTests
{
    [Fact]
    public void Stats_StartAtZeroAfterReset()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.Equal(0, state.Stats.DaysAdvanced);
        Assert.Equal(0, state.Stats.MoneyEarnedFromSelling);
        Assert.Equal(0, state.Stats.MoneySpentOnSeeds);
        Assert.Equal(0, state.Stats.MoneySpentOnBotUnlocks);
        Assert.Equal(0, state.Stats.MoneySpentOnExpansion);
        Assert.Equal(0, state.Stats.SeedsBought);
        Assert.Equal(0, state.Stats.TotalCropsPlanted);
        Assert.Equal(0, state.Stats.TotalCropsHarvested);
        Assert.Equal(0, state.Stats.CropsSold);
        Assert.Equal(0, state.Stats.FarmExpansionsPurchased);
        Assert.Equal(0, state.Stats.BotsUnlocked);
        Assert.Equal(0, state.Stats.FarmCyclesRun);
        Assert.Equal(0, state.Stats.AutoPlayStepsRun);
        Assert.Equal(0, state.Stats.MachineRunsStarted);
        Assert.Equal(0, state.Stats.MachineOutputsCollected);
        Assert.Equal(0, state.Stats.MachineInputValueConsumed);
        Assert.Equal(0, state.Stats.MachineOutputValueCreated);
        Assert.Equal(0, state.Stats.MachineValueAdded);
        Assert.Equal(0, state.Stats.ProcessedGoodsProduced);
        Assert.Equal(0, state.Stats.MarketPurchases);
        Assert.Equal(0, state.Stats.MarketMoneySpent);
        Assert.Equal(0, state.Stats.MarketItemsBought);
        Assert.Equal(0, state.Stats.CleanupSitesCleared);
        Assert.Equal(0, state.Stats.MoneySpentOnCleanup);
    }

    [Fact]
    public void AdvancingDay_IncrementsDaysAdvanced()
    {
        TurnipTestState state = new(BuildRegistry());

        state.AdvanceDay();

        Assert.Equal(1, state.Stats.DaysAdvanced);
    }

    [Fact]
    public void SellingCrops_IncrementsMoneyEarnedAndCropsSold()
    {
        TurnipTestState state = new(BuildRegistry());

        PlantGrowAndHarvestTurnip(state);
        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 30.", message);
        Assert.Equal(30, state.Stats.MoneyEarnedFromSelling);
        Assert.Equal(1, state.Stats.CropsSold);
    }

    [Fact]
    public void BuyingSeeds_IncrementsMoneySpentOnSeedsAndSeedsBought()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 20);

        string message = state.BuySelectedSeed();

        Assert.Equal("Bought 1 Turnip Seeds for 10.", message);
        Assert.Equal(10, state.Stats.MoneySpentOnSeeds);
        Assert.Equal(1, state.Stats.SeedsBought);
    }

    [Fact]
    public void BuyingMarketOffer_IncrementsMarketSpendAndItemsBought()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 160);

        string message = state.BuyMarketOffer("buy_grapes");

        Assert.Equal("Bought 3 Grapes for 160.", message);
        Assert.Equal(1, state.Stats.MarketPurchases);
        Assert.Equal(160, state.Stats.MarketMoneySpent);
        Assert.Equal(3, state.Stats.MarketItemsBought);
        Assert.Equal(160, state.Stats.TotalMoneySpent);
    }

    [Fact]
    public void UnlockingBots_IncrementsMoneySpentOnBotsAndBotsUnlocked()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, TurnipTestState.PlanterBotUnlockCost);

        string message = state.UnlockPlanterBot();

        Assert.Equal("Unlocked Planter Bot.", message);
        Assert.Equal(TurnipTestState.PlanterBotUnlockCost, state.Stats.MoneySpentOnBotUnlocks);
        Assert.Equal(1, state.Stats.BotsUnlocked);
    }

    [Fact]
    public void ExpandingFarm_IncrementsMoneySpentOnExpansionAndPurchases()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, TurnipTestState.FirstFarmExpansionCost);

        string message = state.ExpandFarm();

        Assert.Equal("Expanded farm to 4x3 for 150.", message);
        Assert.Equal(TurnipTestState.FirstFarmExpansionCost, state.Stats.MoneySpentOnExpansion);
        Assert.Equal(1, state.Stats.FarmExpansionsPurchased);
    }

    [Fact]
    public void ClearingCleanupSite_IncrementsCleanupStats()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, TurnipTestState.OldJunkPileCleanupCost);

        string message = state.ClearCleanupSite();

        Assert.Equal("Cleared Old Junk Pile for $500.", message);
        Assert.Equal(1, state.Stats.CleanupSitesCleared);
        Assert.Equal(500, state.Stats.MoneySpentOnCleanup);
    }

    [Fact]
    public void FailedCleanup_DoesNotIncrementCleanupStats()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, TurnipTestState.OldJunkPileCleanupCost - 1);

        state.ClearCleanupSite();

        Assert.Equal(0, state.Stats.CleanupSitesCleared);
        Assert.Equal(0, state.Stats.MoneySpentOnCleanup);
    }

    [Fact]
    public void ManualPlanting_IncrementsManualPlantCount()
    {
        TurnipTestState state = new(BuildRegistry());

        state.PlantSelectedSeed();

        Assert.Equal(1, state.Stats.CropsPlantedManually);
        Assert.Equal(0, state.Stats.CropsPlantedByPlanterBot);
    }

    [Fact]
    public void PlanterBot_IncrementsBotPlantCount()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockPlanterBot(state);

        state.RunPlanterBot();

        Assert.True(state.Stats.CropsPlantedByPlanterBot > 0);
        Assert.Equal(0, state.Stats.CropsPlantedManually);
    }

    [Fact]
    public void ManualWatering_IncrementsManualWaterCount()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSelectedSeed();
        state.AdvanceDay();

        state.WaterSelectedPlot();

        Assert.Equal(1, state.Stats.PlotsWateredManually);
        Assert.Equal(0, state.Stats.PlotsWateredByWateringBot);
    }

    [Fact]
    public void WateringBot_IncrementsBotWaterCount()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSelectedSeed();
        state.AdvanceDay();
        UnlockWateringBot(state);

        state.RunWateringBot();

        Assert.True(state.Stats.PlotsWateredByWateringBot > 0);
        Assert.Equal(0, state.Stats.PlotsWateredManually);
    }

    [Fact]
    public void ManualHarvesting_IncrementsManualHarvestCount()
    {
        TurnipTestState state = new(BuildRegistry());

        PlantGrowAndHarvestTurnip(state);

        Assert.Equal(1, state.Stats.CropsHarvestedManually);
        Assert.Equal(0, state.Stats.CropsHarvestedByHarvesterBot);
    }

    [Fact]
    public void HarvesterBot_IncrementsBotHarvestCount()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockHarvesterBot(state);
        state.PlantSelectedSeed();

        for (int day = 0; day < 4; day++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        state.RunHarvesterBot();

        Assert.True(state.Stats.CropsHarvestedByHarvesterBot > 0);
        Assert.Equal(0, state.Stats.CropsHarvestedManually);
    }

    [Fact]
    public void FarmCycle_IncrementsFarmCyclesRunOnce()
    {
        TurnipTestState state = new(BuildRegistry());

        state.RunFarmCycle();

        Assert.Equal(1, state.Stats.FarmCyclesRun);
    }

    [Fact]
    public void AutoPlaySteps_AreTracked_WhenAutoPlayRuns()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry, new AutoPlaySettings { TargetMoney = 50, MaxSteps = 10 });

        controller.RunStep();
        controller.RunStep();

        Assert.Equal(2, state.Stats.AutoPlayStepsRun);
    }

    [Fact]
    public void Reset_ClearsStats()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 20);
        state.BuySelectedSeed();
        state.PlantSelectedSeed();
        state.AdvanceDay();

        state.Reset();

        Assert.Equal(0, state.Stats.DaysAdvanced);
        Assert.Equal(0, state.Stats.MoneySpentOnSeeds);
        Assert.Equal(0, state.Stats.SeedsBought);
        Assert.Equal(0, state.Stats.TotalCropsPlanted);
        Assert.Equal(0, state.Stats.AutoPlayStepsRun);
        Assert.Equal(0, state.Stats.MachineRunsStarted);
        Assert.Equal(0, state.Stats.MachineOutputsCollected);
        Assert.Equal(0, state.Stats.MachineValueAdded);
        Assert.Equal(0, state.Stats.MarketPurchases);
        Assert.Equal(0, state.Stats.MarketMoneySpent);
        Assert.Equal(0, state.Stats.MarketItemsBought);
    }

    private static void PlantGrowAndHarvestTurnip(TurnipTestState state)
    {
        state.PlantSelectedSeed();

        for (int day = 0; day < 4; day++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        state.HarvestCrop();
    }

    private static ContentRegistry BuildRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed", BuyPrice = 10, SellPrice = 1 },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop", BuyPrice = 0, SellPrice = 30 },
                new ItemDefinition { Id = "potato_seed", Name = "Potato Seeds", Type = "seed", BuyPrice = 12, SellPrice = 1 },
                new ItemDefinition { Id = "potato", Name = "Potato", Type = "crop", BuyPrice = 0, SellPrice = 35 },
                new ItemDefinition { Id = "grape", Name = "Grapes", Type = "crop", BuyPrice = 0, SellPrice = 45 }
            ],
            [
                new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 },
                new CropDefinition { Id = "potato", SeedItemId = "potato_seed", HarvestItemId = "potato", GrowthDays = 5 }
            ],
            [],
            [],
            [
                new MarketOfferDefinition { Id = "buy_grapes", ItemId = "grape", Quantity = 3, Price = 160, Category = "raw_input" }
            ],
            TestContentFactory.DefaultRestorationSites());
    }

    private static void UnlockPlanterBot(TurnipTestState state)
    {
        SetMoney(state, TurnipTestState.PlanterBotUnlockCost);
        state.UnlockPlanterBot();
    }

    private static void UnlockWateringBot(TurnipTestState state)
    {
        SetMoney(state, TurnipTestState.WateringBotUnlockCost);
        state.UnlockWateringBot();
    }

    private static void UnlockHarvesterBot(TurnipTestState state)
    {
        SetMoney(state, TurnipTestState.HarvesterBotUnlockCost);
        state.UnlockHarvesterBot();
    }

    private static void SetMoney(TurnipTestState state, int amount)
    {
        typeof(TurnipTestState)
            .GetProperty("Money", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(state, amount);
    }
}
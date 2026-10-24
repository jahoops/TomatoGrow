using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class AutoPlayTestControllerTests
{
    [Fact]
    public void AutoPlay_DefaultTargetMoney_Is3000()
    {
        AutoPlaySettings settings = new();

        Assert.Equal(3000, settings.TargetMoney);
    }

    [Fact]
    public void AutoPlay_SeedSafetyTarget_Is9OnThreeByThreeFarm()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry);

        Assert.Equal(9, controller.RequiredSeedSafetyCount);
    }

    [Fact]
    public void AutoPlay_SeedSafetyTarget_Is12OnFourByThreeFarm()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, TurnipTestState.FirstFarmExpansionCost);
        state.ExpandFarm();
        AutoPlayTestController controller = new(state, registry);

        Assert.Equal(12, controller.RequiredSeedSafetyCount);
    }

    [Fact]
    public void AutoPlay_SeedSafetyTarget_Is15OnFiveByThreeFarm()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, TurnipTestState.FirstFarmExpansionCost + TurnipTestState.OldJunkPileCleanupCost + TurnipTestState.SecondFarmExpansionCost);
        state.ExpandFarm();
        state.ClearCleanupSite();
        state.ExpandFarm();
        AutoPlayTestController controller = new(state, registry);

        Assert.Equal(15, controller.RequiredSeedSafetyCount);
    }

    [Fact]
    public void AutoPlay_CanStartFromResetState_AndReachTargetWithProgressionCounters()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings
            {
                TargetMoney = 500,
                MaxDays = 365,
                MaxSteps = 5000,
                MaxNoActionSteps = 8,
                DesiredSeedCount = 8
            });

        controller.Enable();

        while (controller.IsEnabled)
            controller.RunStep();

        Console.WriteLine(
            $"AutoPlay sample: days={controller.Stats.DaysSimulated}, money={state.Money}, seedsBought={controller.Stats.SeedsBought}, botsUnlocked={controller.Stats.BotsUnlocked}, expansions={controller.Stats.ExpansionsCompleted}, harvested={controller.Stats.CropsHarvested}, sold={controller.Stats.CropsSold}, steps={controller.Stats.StepsRun}");

        Assert.Equal(AutoPlayStopReason.TargetMoneyReached, controller.Stats.StopReason);
        Assert.True(state.Money >= 500);
        Assert.True(controller.Stats.SeedsBought > 0);
        Assert.True(controller.Stats.BotsUnlocked >= 1);
        Assert.True(state.IsPlanterBotUnlocked);
        Assert.True(state.IsWateringBotUnlocked);
        Assert.True(state.IsHarvesterBotUnlocked);
        Assert.True(controller.Stats.ExpansionsCompleted >= 1);
        Assert.True(controller.Stats.PlanterBotUses > 0);
        Assert.True(controller.Stats.WateringBotUses > 0);
        Assert.True(controller.Stats.HarvesterBotUses > 0);
        Assert.True(controller.Stats.ManualPlantActions > 0);
        Assert.True(controller.Stats.ManualWaterActions > 0);
        Assert.True(controller.Stats.ManualHarvestActions > 0);
        Assert.True(controller.Stats.CropsHarvested > 0);
        Assert.True(controller.Stats.CropsSold > 0);
        Assert.All(registry.Items, item => Assert.True(state.GetItemCount(item.Id) >= 0));
        Assert.True(state.Money >= 0);
    }

    [Fact]
    public void AutoPlay_BuysSeedsBeforeUnlockingBots_WhenBelowSeedSafetyTarget()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, TurnipTestState.PlanterBotUnlockCost);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Contains("Auto: buying seeds first", message, StringComparison.Ordinal);
        Assert.False(state.IsPlanterBotUnlocked);
        Assert.True(controller.Stats.SeedsBought > 0);
        Assert.Equal(0, controller.Stats.BotsUnlocked);
    }

    [Fact]
    public void AutoPlay_BuysSeedsBeforeExpanding_WhenBelowSeedSafetyTarget()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, TurnipTestState.FirstFarmExpansionCost);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Contains("Auto: buying seeds first", message, StringComparison.Ordinal);
        Assert.Equal(TurnipTestState.InitialFarmWidth, state.FarmGrid.Width);
        Assert.Equal(0, controller.Stats.ExpansionsCompleted);
    }

    [Fact]
    public void AutoPlay_DoesNotUnlockBot_WhileBelowSeedSafetyTarget()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 500);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();

        Assert.False(state.IsPlanterBotUnlocked);
        Assert.False(state.IsWateringBotUnlocked);
        Assert.False(state.IsHarvesterBotUnlocked);
    }

    [Fact]
    public void AutoPlay_DoesNotExpand_WhileBelowSeedSafetyTarget()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 500);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();

        Assert.Equal(3, state.FarmGrid.Width);
        Assert.False(controller.IsSeedSafetySatisfied);
    }

    [Fact]
    public void AutoPlay_ResumesExpansionAndUnlocks_AfterSeedSafetyIsSatisfied()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 500);
        AutoPlayTestController controller = new(state, registry);

        while (!controller.IsSeedSafetySatisfied)
            controller.RunStep();

        string expansionMessage = controller.RunStep();

        Assert.Contains("seed safety OK, considering expansion", expansionMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, state.FarmGrid.Width);

        while (!controller.IsSeedSafetySatisfied)
            controller.RunStep();

        string unlockMessage = controller.RunStep();

        Assert.StartsWith("Auto: Unlocked Planter Bot.", unlockMessage, StringComparison.Ordinal);
        Assert.True(state.IsPlanterBotUnlocked);
    }

    [Fact]
    public void AutoPlay_DoesNotClearCleanup_WhenSeedSafetyIsUnsafe()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 650);
        state.ExpandFarm();
        AutoPlayTestController controller = new(state, registry, new AutoPlaySettings { DesiredSeedCount = 12 });

        string message = controller.RunStep();

        Assert.Contains("Auto: buying seeds first", message, StringComparison.Ordinal);
        Assert.False(state.CleanupSite.IsCleared);
    }

    [Fact]
    public void AutoPlay_ClearsCleanup_WhenAffordableAndSeedSafetyIsSafe()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 650);
        state.ExpandFarm();
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry, new AutoPlaySettings { DesiredSeedCount = 12 });

        string message = controller.RunStep();

        Assert.Contains("cleared Old Junk Pile to unlock expansion", message, StringComparison.Ordinal);
        Assert.True(state.CleanupSite.IsCleared);
        Assert.Equal(0, state.Money);
    }

    [Fact]
    public void AutoPlay_ExpandsAfterCleanup_WhenFinalExpansionWasGated()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 950);
        state.ExpandFarm();
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry, new AutoPlaySettings { DesiredSeedCount = 12 });

        controller.RunStep();
        string expansionMessage = controller.RunStep();

        Assert.True(state.CleanupSite.IsCleared);
        Assert.Contains("seed safety OK, considering expansion", expansionMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(5, state.FarmGrid.Width);
    }

    [Fact]
    public void AutoPlay_CanBuyCheapestAffordableSeed_WhenSelectedSeedIsNotAffordable()
    {
        ContentRegistry registry = BuildExpensiveSelectedSeedRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 10);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.StartsWith("Auto: buying seeds first", message, StringComparison.Ordinal);
        Assert.Equal(4, state.GetItemCount("bean_seed"));
        Assert.Equal(3, state.GetItemCount("apple_seed"));
    }

    [Fact]
    public void AutoPlay_UsesManualActionsBeforeBotsAreUnlocked()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings
            {
                TargetMoney = 90,
                MaxDays = 20,
                MaxSteps = 200,
                MaxNoActionSteps = 6,
                DesiredSeedCount = 4
            });

        for (int step = 0; step < 40; step++)
            controller.RunStep();

        Assert.False(state.IsHarvesterBotUnlocked);
        Assert.True(controller.Stats.ManualPlantActions > 0);
        Assert.True(controller.Stats.ManualWaterActions > 0);
        Assert.True(controller.Stats.ManualHarvestActions > 0);
        Assert.Equal(0, controller.Stats.HarvesterBotUses);
    }

    [Fact]
    public void AutoPlay_StopsWhenNoUsefulActionIsPossible()
    {
        ContentRegistry registry = BuildStalledRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings
            {
                TargetMoney = 50,
                MaxDays = 30,
                MaxSteps = 200,
                MaxNoActionSteps = 3,
                DesiredSeedCount = 6
            });

        controller.Enable();

        while (controller.IsEnabled)
            controller.RunStep();

        Assert.Equal(AutoPlayStopReason.NoUsefulActionPossible, controller.Stats.StopReason);
        Assert.True(state.Money >= 0);
        Assert.All(registry.Items, item => Assert.True(state.GetItemCount(item.Id) >= 0));
    }

    [Fact]
    public void AutoPlay_ResetStats_ClearsCounters()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry);

        for (int step = 0; step < 12; step++)
            controller.RunStep();

        Assert.True(controller.Stats.StepsRun > 0);

        controller.ResetStats();

        Assert.Equal(0, controller.Stats.StepsRun);
        Assert.Equal(0, controller.Stats.DaysSimulated);
        Assert.Equal(0, controller.Stats.SeedsBought);
        Assert.Equal(0, controller.Stats.CropsHarvested);
        Assert.Equal(0, controller.Stats.CropsSold);
        Assert.Equal(0, controller.Stats.BotsUnlocked);
        Assert.Equal(0, controller.Stats.ExpansionsCompleted);
        Assert.Equal(0, controller.Stats.ManualHarvestActions);
        Assert.Equal(AutoPlayStopReason.None, controller.Stats.StopReason);
    }

    [Fact]
    public void AutoPlay_DiscoversProcessorInputItemsFromRecipes()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry);

        IReadOnlyList<string> processorInputItemIds = controller.GetProcessorInputItemIds();

        Assert.Contains("grape", processorInputItemIds);
        Assert.Contains("hops", processorInputItemIds);
    }

    [Fact]
    public void AutoPlay_IdentifiesProcessorInputSeedsFromRecipeInputs()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry);

        IReadOnlyList<string> processorSeedItemIds = controller.GetSeedItemIdsForProcessorInputs();

        Assert.Contains("grape_seed", processorSeedItemIds);
        Assert.Contains("hops_seed", processorSeedItemIds);
    }

    [Fact]
    public void AutoPlay_SeedPreferenceOrder_PrioritizesProcessorInputSeeds()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry);

        IReadOnlyList<string> seedPreferenceOrder = controller.GetAutoplaySeedPreferenceOrder();

        Assert.Equal(["grape_seed", "hops_seed", "turnip_seed"], seedPreferenceOrder);
        Assert.Equal(seedPreferenceOrder, controller.GetAutoplaySeedPreferenceOrder());
    }

    [Fact]
    public void AutoPlay_CanDiscoverActiveOpportunityInputsAndRecipes()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));
        AutoPlayTestController controller = new(state, registry);

        Assert.Equal("wine", controller.GetActiveOpportunityItemId());
        Assert.Equal(["grape_to_wine"], controller.GetRecipeIdsProducingItem("wine"));
        Assert.Equal(["grape_seed"], controller.GetSeedItemIdsSupportingOpportunity());
    }

    [Fact]
    public void AutoPlay_SeedPreferenceOrder_PrioritizesOpportunitySupportingSeedsFirst()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));
        AutoPlayTestController controller = new(state, registry);

        Assert.Equal(["grape_seed", "hops_seed", "turnip_seed"], controller.GetAutoplaySeedPreferenceOrder());
    }

    [Fact]
    public void AutoPlay_SeedPreferenceOrder_RevertsAfterOpportunityExpires()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        state.SetActiveMarketOpportunity(CreateWineBoom(1));
        AutoPlayTestController controller = new(state, registry);

        Assert.Equal(["grape_seed", "hops_seed", "turnip_seed"], controller.GetAutoplaySeedPreferenceOrder());

        state.AdvanceDay();

        Assert.Equal(["grape_seed", "hops_seed", "turnip_seed"], controller.GetAutoplaySeedPreferenceOrder());
    }

    [Fact]
    public void AutoPlay_BuysProcessorInputSeedsWhenAffordable()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 15);
        AutoPlayTestController controller = new(state, registry, new AutoPlaySettings { DesiredSeedCount = 12 });

        string message = controller.RunStep();

        Assert.Contains("selected Grape Seeds for processing", message, StringComparison.Ordinal);
        Assert.Equal(4, state.GetItemCount("grape_seed"));
    }

    [Fact]
    public void AutoPlay_BuysHopsSeedsWhenAffordableAfterRotatingPastGrapeSeeds()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 40);
        AutoPlayTestController controller = new(state, registry, new AutoPlaySettings { DesiredSeedCount = 12 });

        controller.RunStep();
        string secondMessage = controller.RunStep();

        Assert.Contains("Hops Seeds", secondMessage, StringComparison.Ordinal);
        Assert.Equal(4, state.GetItemCount("hops_seed"));
        Assert.Equal(1, controller.Stats.GetBoughtSeedCount("grape_seed"));
        Assert.Equal(1, controller.Stats.GetBoughtSeedCount("hops_seed"));
    }

    [Fact]
    public void AutoPlay_PlantsAtLeastTwoDifferentSeedTypesDuringLongerRun()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 200);
        AutoPlayTestController controller = new(state, registry, new AutoPlaySettings { DesiredSeedCount = 12, MaxSteps = 200, MaxNoActionSteps = 8 });

        for (int step = 0; step < 40; step++)
            controller.RunStep();

        Assert.True(controller.Stats.PlantedSeedsByItemId.Count(entry => entry.Value > 0) >= 2);
        Assert.True(controller.Stats.GetPlantedSeedCount("grape_seed") > 0 || controller.Stats.GetPlantedSeedCount("hops_seed") > 0);
    }

    [Fact]
    public void AutoPlay_CanGrowAndHarvestGrapesThroughNormalCropSystem()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings { TargetMoney = 250, MaxDays = 120, MaxSteps = 2000, MaxNoActionSteps = 8, DesiredSeedCount = 12 });

        for (int step = 0; step < 400 && controller.Stats.GetHarvestedItemCount("grape") == 0; step++)
            controller.RunStep();

        Assert.True(controller.Stats.GetPlantedSeedCount("grape_seed") > 0);
        Assert.True(controller.Stats.GetHarvestedItemCount("grape") > 0 || state.Stats.GetProcessedOutputCount("wine") > 0);
    }

    [Fact]
    public void AutoPlay_CanGrowAndHarvestHopsThroughNormalCropSystem()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 200);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings { TargetMoney = 250, MaxDays = 120, MaxSteps = 2000, MaxNoActionSteps = 8, DesiredSeedCount = 12 });

        for (int step = 0; step < 400 && controller.Stats.GetHarvestedItemCount("hops") == 0; step++)
            controller.RunStep();

        Assert.True(controller.Stats.GetPlantedSeedCount("hops_seed") > 0);
        Assert.True(controller.Stats.GetHarvestedItemCount("hops") > 0 || state.Stats.GetProcessedOutputCount("beer") > 0);
    }

    [Fact]
    public void AutoPlay_StartsFermenterWhenEnoughGrapesAreAvailableAndIdle()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "grape", 3);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Equal("Auto: started fermenter.", message);
        Assert.True(state.Fermenter.IsWorking);
        Assert.Equal(1, state.Stats.MachineRunsStarted);
    }

    [Fact]
    public void AutoPlay_StartsFermenterWhenEnoughHopsAreAvailableAndIdle()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "hops", 3);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Equal("Auto: started fermenter.", message);
        Assert.True(state.Fermenter.IsWorking);
        Assert.Equal("hops", state.Fermenter.InputItemId);
    }

    [Fact]
    public void AutoPlay_WineOpportunity_PrefersFermenterRecipeThatProducesWine()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "grape", 3);
        AddInventoryItem(state, "hops", 3);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Equal("Auto: started fermenter for Wine Boom.", message);
        Assert.True(state.Fermenter.IsWorking);
        Assert.Equal("grape", state.Fermenter.InputItemId);
    }

    [Fact]
    public void AutoPlay_WineOpportunity_SellsWineInsteadOfStartingAgingBarrel()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "wine", 1);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.StartsWith("Auto: sold processed goods.", message, StringComparison.Ordinal);
        Assert.False(state.AgingBarrel.IsWorking);
        Assert.Equal(240, state.Money);
        Assert.Equal(1, controller.Stats.GetSoldItemCount("wine"));
    }

    [Fact]
    public void AutoPlay_WineOpportunity_PrefersMarketGrapesOverMarketWineForAging()
    {
        ContentRegistry registry = BuildMachineMarketRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 250);
        AddInventoryItem(state, "turnip_seed", 9);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Contains("bought 3 Grapes from market for processing", message, StringComparison.Ordinal);
        Assert.Contains("started fermenter with market Grapes", message, StringComparison.Ordinal);
        Assert.True(state.Fermenter.IsWorking);
        Assert.Equal("grape", state.Fermenter.InputItemId);
        Assert.False(state.AgingBarrel.IsWorking);
    }

    [Fact]
    public void AutoPlay_StartsAgingBarrelWhenWineIsAvailableAndIdle()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "wine", 1);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Equal("Auto: started aging barrel.", message);
        Assert.True(state.AgingBarrel.IsWorking);
        Assert.Equal("wine", state.AgingBarrel.InputItemId);
    }

    [Fact]
    public void AutoPlay_DoesNotBuyFromMarketWhenPaused()
    {
        ContentRegistry registry = BuildMachineMarketRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 250);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep(isPaused: true);

        Assert.Equal("Auto: paused.", message);
        Assert.Equal(0, state.Stats.MarketPurchases);
        Assert.Equal(0, state.Stats.MachineRunsStarted);
        Assert.Equal(0, controller.Stats.StepsRun);
    }

    [Fact]
    public void AutoPlay_DoesNotBuyFromMarketWhenOfferIsUnaffordable()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_grapes", "grape", 3, 160));
        TurnipTestState state = new(registry);
        SetMoney(state, 100);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();

        Assert.Equal(0, state.Stats.MarketPurchases);
        Assert.False(state.Fermenter.IsWorking);
        Assert.Equal(0, state.GetItemCount("grape"));
    }

    [Fact]
    public void AutoPlay_DoesNotBuyFromMarketWhenSeedBufferWouldBeUnsafe()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_grapes", "grape", 3, 160));
        TurnipTestState state = new(registry);
        SetMoney(state, 160);
        AutoPlayTestController controller = new(state, registry, new AutoPlaySettings { DesiredSeedCount = 12 });

        string message = controller.RunStep();

        Assert.Contains("Auto: buying seeds first", message, StringComparison.Ordinal);
        Assert.Equal(0, state.Stats.MarketPurchases);
        Assert.False(state.Fermenter.IsWorking);
    }

    [Fact]
    public void AutoPlay_DoesNotBuyMarketGoodsWithNoProcessingUse()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_turnips", "turnip", 3, 60));
        TurnipTestState state = new(registry);
        SetMoney(state, 200);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();

        Assert.Equal(0, state.Stats.MarketPurchases);
        Assert.False(state.Fermenter.IsWorking);
        Assert.Equal(0, state.GetItemCount("turnip"));
    }

    [Fact]
    public void AutoPlay_BuysMarketGrapesAndStartsFermenter()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_grapes", "grape", 3, 160));
        TurnipTestState state = new(registry);
        SetMoney(state, 200);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Contains("bought 3 Grapes from market for processing", message, StringComparison.Ordinal);
        Assert.Contains("started fermenter with market Grapes", message, StringComparison.Ordinal);
        Assert.True(state.Fermenter.IsWorking);
        Assert.Equal("grape", state.Fermenter.InputItemId);
        Assert.Equal(1, state.Stats.MarketPurchases);
        Assert.Equal(1, state.Stats.MachineRunsStarted);
    }

    [Fact]
    public void AutoPlay_BuysMarketHopsAndStartsFermenter()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_hops", "hops", 3, 120));
        TurnipTestState state = new(registry);
        SetMoney(state, 200);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Contains("bought 3 Hops from market for processing", message, StringComparison.Ordinal);
        Assert.Contains("started fermenter with market Hops", message, StringComparison.Ordinal);
        Assert.True(state.Fermenter.IsWorking);
        Assert.Equal("hops", state.Fermenter.InputItemId);
        Assert.Equal(1, state.Stats.MarketPurchases);
        Assert.Equal(1, state.Stats.MachineRunsStarted);
    }

    [Fact]
    public void AutoPlay_BuysMarketWineAndStartsAgingBarrel()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_wine", "wine", 1, 190));
        TurnipTestState state = new(registry);
        SetMoney(state, 250);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Contains("bought 1 Wine from market for processing", message, StringComparison.Ordinal);
        Assert.Contains("started aging barrel with market Wine", message, StringComparison.Ordinal);
        Assert.True(state.AgingBarrel.IsWorking);
        Assert.Equal("wine", state.AgingBarrel.InputItemId);
        Assert.Equal(1, state.Stats.MarketPurchases);
        Assert.Equal(1, state.Stats.MachineRunsStarted);
    }

    [Fact]
    public void AutoPlay_DoesNotRepeatSameMarketPurchaseWhileMachineIsWorking()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_grapes", "grape", 3, 160));
        TurnipTestState state = new(registry);
        SetMoney(state, 400);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();
        controller.RunStep();

        Assert.Equal(1, state.Stats.MarketPurchases);
        Assert.True(state.Fermenter.IsWorking);
    }

    [Fact]
    public void AutoPlay_CollectsOutputFromMachineStartedWithMarketInput()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_hops", "hops", 3, 120));
        TurnipTestState state = new(registry);
        SetMoney(state, 200);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();
        state.AdvanceDay();
        state.AdvanceDay();
        string message = controller.RunStep();

        Assert.Equal("Auto: collected Beer.", message);
        Assert.Equal(1, state.GetItemCount("beer"));
        Assert.Equal(1, state.Stats.MachineOutputsCollected);
        Assert.Equal(1, state.Stats.GetProcessedOutputCount("beer"));
    }

    [Fact]
    public void AutoPlay_EventuallySellsProcessedOutputFromMarketInput()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_hops", "hops", 3, 120));
        TurnipTestState state = new(registry);
        SetMoney(state, 120);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();
        state.AdvanceDay();
        state.AdvanceDay();
        controller.RunStep();
        string message = controller.RunStep();

        Assert.StartsWith("Auto: sold processed goods.", message, StringComparison.Ordinal);
        Assert.Equal(1, controller.Stats.GetSoldItemCount("beer"));
        Assert.Equal(0, state.GetItemCount("beer"));
        Assert.True(state.Money >= 140);
    }

    [Fact]
    public void AutoPlay_MarketPurchaseAndProcessing_IncreaseMarketAndMachineStats()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_hops", "hops", 3, 120));
        TurnipTestState state = new(registry);
        SetMoney(state, 200);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();
        state.AdvanceDay();
        state.AdvanceDay();
        controller.RunStep();

        Assert.Equal(1, state.Stats.MarketPurchases);
        Assert.Equal(120, state.Stats.MarketMoneySpent);
        Assert.Equal(3, state.Stats.MarketItemsBought);
        Assert.Equal(1, state.Stats.MachineRunsStarted);
        Assert.Equal(1, state.Stats.MachineOutputsCollected);
        Assert.Equal(140, state.Stats.MachineOutputValueCreated);
        Assert.Equal(20, state.Stats.MachineValueAdded);
    }

    [Fact]
    public void AutoPlay_ComputerShowsVisibleMarketBoughtGoodsBeforeProcessing()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_bulk_grapes", "grape", 6, 320));
        TurnipTestState state = new(registry);
        SetMoney(state, 500);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.ActivityLog,
            null,
            state.Stats,
            state.DayNumber,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y));

        Assert.True(state.Fermenter.IsWorking);
        Assert.Equal(3, state.GetItemCount("grape"));
        Assert.Contains(lines, line => line.Contains("Grapes x3", StringComparison.Ordinal));
    }

    [Fact]
    public void AutoPlay_ComputerShowsProcessedOutputAfterCollectionFromMarketInput()
    {
        ContentRegistry registry = BuildMachineMarketRegistry(CreateMarketOffer("buy_hops", "hops", 3, 120));
        TurnipTestState state = new(registry);
        SetMoney(state, 200);
        AddInventoryItem(state, "turnip_seed", 9);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();
        state.AdvanceDay();
        state.AdvanceDay();
        controller.RunStep();

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.ActivityLog,
            null,
            state.Stats,
            state.DayNumber,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y));

        Assert.Contains(lines, line => line.Contains("Beer x1", StringComparison.Ordinal));
    }

    [Fact]
    public void AutoPlay_FullProgressionWithMarketStillGrowsCropsUnlocksBotsAndExpands()
    {
        ContentRegistry registry = BuildMachineMarketRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 250);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings
            {
                TargetMoney = 700,
                MaxDays = 365,
                MaxSteps = 5000,
                MaxNoActionSteps = 8,
                DesiredSeedCount = 12
            });

        controller.Enable();

        while (controller.IsEnabled)
            controller.RunStep();

        Console.WriteLine(
            "AutoPlay market summary: "
            + $"marketPurchases={state.Stats.MarketPurchases}; "
            + $"marketSpent={state.Stats.MarketMoneySpent}; "
            + $"processedProduced={FormatCounts(state.Stats.ProcessedOutputsByItemId)}; "
            + $"sold={FormatCounts(controller.Stats.SoldItemsByItemId)}; "
            + $"harvested={FormatCounts(controller.Stats.HarvestedItemsByItemId)}; "
            + $"planted={FormatCounts(controller.Stats.PlantedSeedsByItemId)}; "
            + $"machineRuns={state.Stats.MachineRunsStarted}; "
            + $"machineOutputs={state.Stats.MachineOutputsCollected}; "
            + $"machineValueAdded={state.Stats.MachineValueAdded}; "
            + $"botsUnlocked={controller.Stats.BotsUnlocked}; "
            + $"expansions={controller.Stats.ExpansionsCompleted}; "
            + $"money={state.Money}");

        Assert.Equal(AutoPlayStopReason.TargetMoneyReached, controller.Stats.StopReason);
        Assert.True(state.Stats.MarketPurchases > 0);
        Assert.True(controller.Stats.CropsHarvested > 0);
        Assert.True(controller.Stats.GetHarvestedItemCount("turnip") > 0 || controller.Stats.GetHarvestedItemCount("grape") > 0 || controller.Stats.GetHarvestedItemCount("hops") > 0);
        Assert.True(controller.Stats.ManualPlantActions > 0 || controller.Stats.PlanterBotUses > 0);
        Assert.True(controller.Stats.BotsUnlocked >= 1);
        Assert.True(controller.Stats.ExpansionsCompleted >= 1);
        Assert.True(state.Stats.MachineRunsStarted > 0);
        Assert.True(controller.Stats.GetSoldItemCount("beer") > 0 || controller.Stats.GetSoldItemCount("wine") > 0 || controller.Stats.GetSoldItemCount("aged_wine") > 0);
    }

    [Fact]
    public void AutoPlay_DoesNotStartFermenterWhileAlreadyWorking()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "grape", 6);
        state.StartFermenterFromInventory();
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();

        Assert.Equal(1, state.Stats.MachineRunsStarted);
    }

    [Fact]
    public void AutoPlay_CollectsFermenterOutputWhenReady()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "grape", 3);
        state.StartFermenterFromInventory();
        state.AdvanceDay();
        state.AdvanceDay();
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Equal("Auto: collected Wine.", message);
        Assert.Equal(1, state.GetItemCount("wine"));
        Assert.Equal(1, state.Stats.MachineOutputsCollected);
    }

    [Fact]
    public void AutoPlay_DoesNotSellReservedRawMachineInputBeforeStartingFermenter()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "grape", 3);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();

        Assert.Equal(0, state.GetItemCount("grape"));
        Assert.True(state.Fermenter.IsWorking);
    }

    [Fact]
    public void AutoPlay_DoesNotSellReservedHopsBeforeStartingFermenter()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "hops", 3);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();

        Assert.Equal(0, state.GetItemCount("hops"));
        Assert.True(state.Fermenter.IsWorking);
        Assert.Equal("hops", state.Fermenter.InputItemId);
    }

    [Fact]
    public void AutoPlay_SellsExcessRawCropsBeyondReservedAmountWhenAppropriate()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "grape", 8);
        state.StartFermenterFromInventory();
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Contains("Sold 2 items for 90.", message, StringComparison.Ordinal);
        Assert.Equal(3, state.GetItemCount("grape"));
        Assert.Equal(2, controller.Stats.GetSoldItemCount("grape"));
    }

    [Fact]
    public void AutoPlay_EventuallySellsAgedWineOutput()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "grape", 3);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();
        state.AdvanceDay();
        state.AdvanceDay();
        controller.RunStep();
        string agingMessage = controller.RunStep();

        Assert.Equal("Auto: started aging barrel.", agingMessage);

        for (int day = 0; day < 5; day++)
            state.AdvanceDay();

        controller.RunStep();
        string sellMessage = controller.RunStep();

        Assert.Contains("Sold 1 items for 260.", sellMessage, StringComparison.Ordinal);
        Assert.Equal(260, state.Money);
        Assert.Equal(0, state.GetItemCount("aged_wine"));
        Assert.Equal(1, controller.Stats.GetSoldItemCount("aged_wine"));
    }

    [Fact]
    public void AutoPlay_MachineUse_IncrementsMachineStats()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "grape", 3);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep();
        state.AdvanceDay();
        state.AdvanceDay();
        controller.RunStep();

        Assert.Equal(1, state.Stats.MachineRunsStarted);
        Assert.Equal(1, state.Stats.MachineOutputsCollected);
        Assert.Equal(160, state.Stats.MachineOutputValueCreated);
        Assert.Equal(25, state.Stats.MachineValueAdded);
    }

    [Fact]
    public void AutoPlay_FullProgressionWithMachines_CanProduceProcessedGoodsAndStillUnlockAndExpand()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings
            {
                TargetMoney = 500,
                MaxDays = 365,
                MaxSteps = 5000,
                MaxNoActionSteps = 8,
                DesiredSeedCount = 8
            });

        controller.Enable();

        while (controller.IsEnabled)
            controller.RunStep();

        Console.WriteLine(
            "AutoPlay machine summary: "
            + $"seedOrder={string.Join(",", controller.GetAutoplaySeedPreferenceOrder())}; "
            + $"seedsBought={FormatCounts(controller.Stats.BoughtSeedsByItemId)}; "
            + $"seedsPlanted={FormatCounts(controller.Stats.PlantedSeedsByItemId)}; "
            + $"harvested={FormatCounts(controller.Stats.HarvestedItemsByItemId)}; "
            + $"processedProduced={FormatCounts(state.Stats.ProcessedOutputsByItemId)}; "
            + $"sold={FormatCounts(controller.Stats.SoldItemsByItemId)}; "
            + $"machineRuns={state.Stats.MachineRunsStarted}; "
            + $"machineOutputs={state.Stats.MachineOutputsCollected}; "
            + $"machineValueAdded={state.Stats.MachineValueAdded}; "
            + $"botsUnlocked={controller.Stats.BotsUnlocked}; "
            + $"expansions={controller.Stats.ExpansionsCompleted}; "
            + $"money={state.Money}");

        Assert.Equal(AutoPlayStopReason.TargetMoneyReached, controller.Stats.StopReason);
        Assert.True(state.Stats.MachineRunsStarted > 0);
        Assert.True(state.Stats.MachineOutputsCollected > 0);
        Assert.True(state.Stats.ProcessedGoodsProduced > 0);
        Assert.True(state.Stats.GetProcessedOutputCount("wine") > 0 || state.Stats.GetProcessedOutputCount("beer") > 0 || state.Stats.GetProcessedOutputCount("aged_wine") > 0);
        Assert.True(controller.Stats.GetSoldItemCount("wine") > 0 || controller.Stats.GetSoldItemCount("beer") > 0 || controller.Stats.GetSoldItemCount("aged_wine") > 0);
        Assert.True(controller.Stats.BotsUnlocked >= 1);
        Assert.True(controller.Stats.ExpansionsCompleted >= 1);
        Assert.True(state.Money >= 500);
        Assert.True(state.Stats.MachineValueAdded > 0);
        Assert.True(controller.Stats.LastAutoAction.Contains("fermenter", StringComparison.OrdinalIgnoreCase)
            || controller.Stats.LastAutoAction.Contains("aging barrel", StringComparison.OrdinalIgnoreCase)
            || state.Stats.MachineRunsStarted > 0);
    }

    [Fact]
    public void AutoPlay_WhileViewingMachinesScreen_StillUpdatesMachineDashboardStats()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        DebugUiState uiState = new();
        uiState.ShowMachinesScreen();
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings { TargetMoney = 300, MaxDays = 120, MaxSteps = 2000, MaxNoActionSteps = 8, DesiredSeedCount = 12 });

        for (int step = 0; step < 400 && state.Stats.MachineOutputsCollected == 0; step++)
            controller.RunStep();

        string[] lines = DebugUiTextFormatter.GetMachineScreenLines(
            registry,
            state.ProcessorMachines,
            state.SelectedProcessorMachine.MachineId,
            registry.GetProcessingRecipesForMachine(state.SelectedProcessorMachine.MachineId).First().Id,
            state.Stats,
            state.GetItemCount);

        Assert.Equal(GameScreen.Machines, uiState.CurrentScreen);
        Assert.True(state.Stats.MachineRunsStarted > 0);
        Assert.True(state.Stats.MachineOutputsCollected > 0);
        Assert.Contains($"Runs: {state.Stats.MachineRunsStarted}", lines);
        Assert.Contains($"Outputs: {state.Stats.MachineOutputsCollected}", lines);
        Assert.Contains($"Processing profit: +${state.Stats.MachineValueAdded}", lines);
    }

    [Fact]
    public void AutoPlay_WhileViewingComputerScreen_StillUpdatesOverviewStats()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        DebugUiState uiState = new();
        uiState.ShowComputerScreen();
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings { TargetMoney = 300, MaxDays = 120, MaxSteps = 2000, MaxNoActionSteps = 8, DesiredSeedCount = 12 });

        for (int step = 0; step < 400 && state.Stats.MachineRunsStarted == 0; step++)
            controller.RunStep();

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.ActivityLog,
            null,
            state.Stats,
            state.DayNumber,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y));

        Assert.Equal(GameScreen.Computer, uiState.CurrentScreen);
        Assert.True(state.Stats.MachineRunsStarted > 0);
        Assert.Contains("GLOBAL INVENTORY", lines);
        Assert.Contains($"Money: ${state.Money}", lines);
        string expectedProfitLine = state.Stats.MachineValueAdded > 0
            ? $"Lifetime processing profit: +${state.Stats.MachineValueAdded}"
            : "Lifetime processing profit: $0";
        Assert.Contains(expectedProfitLine, lines);
    }

    [Fact]
    public void AutoPlay_WhileViewingActivityLogScreen_AddsEntriesOverTime()
    {
        ContentRegistry registry = BuildMachineMarketRegistry();
        TurnipTestState state = new(registry);
        DebugUiState uiState = new();
        uiState.ShowActivityLogScreen();
        SetMoney(state, 250);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings { TargetMoney = 300, MaxDays = 120, MaxSteps = 500, MaxNoActionSteps = 8, DesiredSeedCount = 12 });

        for (int step = 0; step < 50 && state.ActivityLog.Count == 0; step++)
            controller.RunStep();

        Assert.Equal(GameScreen.ActivityLog, uiState.CurrentScreen);
        Assert.NotEmpty(state.ActivityLog);
    }

    [Fact]
    public void AutoPlay_PausedStep_DoesNotAddActivityEntries()
    {
        ContentRegistry registry = BuildMachineMarketRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 250);
        AutoPlayTestController controller = new(state, registry);

        controller.RunStep(isPaused: true);

        Assert.Empty(state.ActivityLog);
    }

    [Fact]
    public void AutoPlay_CanStartFermenterUsingSharedStorageInputs()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry);
        AddStorageItem(state, "grape", 3);

        string message = controller.RunStep();

        Assert.Equal("Auto: started fermenter.", message);
        Assert.True(state.Fermenter.IsWorking);
        Assert.Equal(0, state.GetStorageItemCount("grape"));
    }

    [Fact]
    public void AutoPlay_DoesNotSellWineReservedForAgingBarrel()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "wine", 1);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.Equal("Auto: started aging barrel.", message);
        Assert.True(state.AgingBarrel.IsWorking);
        Assert.Equal(0, state.GetItemCount("wine"));
    }

    [Fact]
    public void AutoPlay_WineOpportunity_StillHonorsSeedSafetyBeforeOpportunityActions()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 500);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));
        AutoPlayTestController controller = new(state, registry, new AutoPlaySettings { DesiredSeedCount = 12 });

        string message = controller.RunStep();

        Assert.Contains("Auto: buying seeds first", message, StringComparison.Ordinal);
        Assert.Equal(0, state.Stats.MarketPurchases);
        Assert.False(state.Fermenter.IsWorking);
    }

    [Fact]
    public void AutoPlay_WineOpportunity_CanGrowProduceAndSellWineDuringNormalPlay()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 250);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings { TargetMoney = 400, MaxDays = 120, MaxSteps = 2000, MaxNoActionSteps = 8, DesiredSeedCount = 12 });

        for (int step = 0; step < 500 && controller.Stats.GetSoldItemCount("wine") == 0; step++)
            controller.RunStep();

        Assert.True(controller.Stats.GetPlantedSeedCount("grape_seed") > 0);
        Assert.True(state.Stats.GetProcessedOutputCount("wine") > 0 || controller.Stats.GetSoldItemCount("wine") > 0);
        Assert.True(controller.Stats.GetSoldItemCount("wine") > 0);
    }

    [Fact]
    public void AutoPlay_CanSellProcessedGoodsFromSharedStorage()
    {
        ContentRegistry registry = BuildMachineRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry);
        AddStorageItem(state, "aged_wine", 1);

        string message = controller.RunStep();

        Assert.StartsWith("Auto: sold processed goods.", message, StringComparison.Ordinal);
        Assert.Equal(260, state.Money);
        Assert.Equal(0, state.GetStorageItemCount("aged_wine"));
    }

    [Fact]
    public void AutoPlay_RealContentModules_SeedPreferenceOrder_IsCurrentDeterministicOrder()
    {
        ContentRegistry registry = new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry);

        Assert.Equal(["grape_seed", "hops_seed", "potato_seed", "turnip_seed"], controller.GetAutoplaySeedPreferenceOrder());
    }

    private static ContentRegistry BuildRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed", BuyPrice = 10, SellPrice = 1 },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop", BuyPrice = 0, SellPrice = 30 },
                new ItemDefinition { Id = "potato_seed", Name = "Potato Seeds", Type = "seed", BuyPrice = 12, SellPrice = 1 },
                new ItemDefinition { Id = "potato", Name = "Potato", Type = "crop", BuyPrice = 0, SellPrice = 35 }
            ],
            [
                new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 },
                new CropDefinition { Id = "potato", SeedItemId = "potato_seed", HarvestItemId = "potato", GrowthDays = 5 }
            ],
            [],
            [],
            [],
            TestContentFactory.DefaultRestorationSites());
    }

    private static ContentRegistry BuildMachineRegistry()
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
            [],
            TestContentFactory.DefaultRestorationSites());
    }

    private static ContentRegistry BuildMachineMarketRegistry(params MarketOfferDefinition[] marketOffers)
    {
        MarketOfferDefinition[] offers = marketOffers.Length == 0
            ?
            [
                CreateMarketOffer("buy_grapes", "grape", 3, 160),
                CreateMarketOffer("buy_hops", "hops", 3, 120),
                CreateMarketOffer("buy_wine", "wine", 1, 190)
            ]
            : marketOffers;

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
            offers,
            TestContentFactory.DefaultRestorationSites());
    }

    private static MarketOfferDefinition CreateMarketOffer(string id, string itemId, int quantity, int price)
    {
        return new MarketOfferDefinition
        {
            Id = id,
            ItemId = itemId,
            Quantity = quantity,
            Price = price,
            Category = "raw_input"
        };
    }

    private static ContentRegistry BuildExpensiveSelectedSeedRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "apple_seed", Name = "Apple Seeds", Type = "seed", BuyPrice = 20, SellPrice = 0 },
                new ItemDefinition { Id = "apple", Name = "Apple", Type = "crop", BuyPrice = 0, SellPrice = 45 },
                new ItemDefinition { Id = "bean_seed", Name = "Bean Seeds", Type = "seed", BuyPrice = 10, SellPrice = 0 },
                new ItemDefinition { Id = "bean", Name = "Bean", Type = "crop", BuyPrice = 0, SellPrice = 25 }
            ],
            [
                new CropDefinition { Id = "apple", SeedItemId = "apple_seed", HarvestItemId = "apple", GrowthDays = 4 },
                new CropDefinition { Id = "bean", SeedItemId = "bean_seed", HarvestItemId = "bean", GrowthDays = 3 }
            ],
            [],
            [],
            [],
            TestContentFactory.DefaultRestorationSites());
    }

    private static ContentRegistry BuildStalledRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "stalled_seed", Name = "Stalled Seeds", Type = "seed", BuyPrice = 0, SellPrice = 0 },
                new ItemDefinition { Id = "stalled_crop", Name = "Stalled Crop", Type = "crop", BuyPrice = 0, SellPrice = 0 }
            ],
            [
                new CropDefinition { Id = "stalled_crop", SeedItemId = "stalled_seed", HarvestItemId = "stalled_crop", GrowthDays = 1 }
            ],
            [],
            [],
            [],
            TestContentFactory.DefaultRestorationSites());
    }

    private static void SetMoney(TurnipTestState state, int amount)
    {
        typeof(TurnipTestState)
            .GetProperty("Money", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(state, amount);
    }

    private static void AddInventoryItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }

    private static void AddStorageItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddStorageItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }

    private static MarketOpportunity CreateWineBoom(int daysRemaining)
    {
        return new MarketOpportunity
        {
            Id = "wine_boom",
            ItemId = "wine",
            SellPriceMultiplier = 1.5,
            DaysRemaining = daysRemaining,
            DisplayName = "Wine Boom",
            Message = "Wine sells for 150% for 30 days."
        };
    }

    private static string FormatCounts(IReadOnlyDictionary<string, int> countsByItemId)
    {
        return countsByItemId.Count == 0
            ? "none"
            : string.Join(",", countsByItemId.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).Select(entry => $"{entry.Key}:{entry.Value}"));
    }
}
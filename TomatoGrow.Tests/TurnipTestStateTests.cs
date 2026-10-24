using System;
using System.Reflection;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class TurnipTestStateTests
{
    [Fact]
    public void FarmGrid_HasMultiplePlots()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.Equal(3, state.FarmGrid.Width);
        Assert.Equal(3, state.FarmGrid.Height);
        Assert.Equal(9, state.FarmGrid.GetAllPlots().Count());
    }

    [Fact]
    public void NewlyCreatedPlots_StartDry()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.All(state.FarmGrid.GetAllPlots(), plot => Assert.False(plot.IsWatered));
    }

    [Fact]
    public void FarmStartsAtExpansionLevel0With3x3Grid()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.Equal(0, state.FarmExpansionLevel);
        Assert.Equal(3, state.FarmGrid.Width);
        Assert.Equal(3, state.FarmGrid.Height);
    }

    [Fact]
    public void ExpandingFarmWithEnoughMoney_Subtracts150AndChangesFarmTo4x3()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 150);

        string message = state.ExpandFarm();

        Assert.Equal("Expanded farm to 4x3 for 150.", message);
        Assert.Equal(0, state.Money);
        Assert.Equal(1, state.FarmExpansionLevel);
        Assert.Equal(4, state.FarmGrid.Width);
        Assert.Equal(3, state.FarmGrid.Height);
        Assert.Equal(12, state.FarmGrid.GetAllPlots().Count());
    }

    [Fact]
    public void ExpandingFarmAgainWithEnoughMoney_Subtracts300AndChangesFarmTo5x3()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 950);

        state.ExpandFarm();
        state.ClearCleanupSite();
        string message = state.ExpandFarm();

        Assert.Equal("Expanded farm to 5x3 for 300.", message);
        Assert.Equal(0, state.Money);
        Assert.Equal(2, state.FarmExpansionLevel);
        Assert.Equal(5, state.FarmGrid.Width);
        Assert.Equal(3, state.FarmGrid.Height);
        Assert.Equal(15, state.FarmGrid.GetAllPlots().Count());
    }

    [Fact]
    public void ExpandingFarm_PreservesExistingPlantedCropsAndWaterState()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(2, 1, "turnip_seed");
        state.WaterPlot(2, 1);
        SetMoney(state, 150);

        state.ExpandFarm();

        Assert.Equal("turnip", state.FarmGrid.GetPlot(2, 1).PlantedCrop!.CropDefinitionId);
        Assert.True(state.FarmGrid.GetPlot(2, 1).IsWatered);
    }

    [Fact]
    public void ExpandingFarm_AddsNewEmptyDryPlots()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 150);

        state.ExpandFarm();

        Assert.True(state.FarmGrid.GetPlot(3, 0).IsEmpty);
        Assert.False(state.FarmGrid.GetPlot(3, 0).IsWatered);
        Assert.True(state.FarmGrid.GetPlot(3, 1).IsEmpty);
        Assert.False(state.FarmGrid.GetPlot(3, 1).IsWatered);
        Assert.True(state.FarmGrid.GetPlot(3, 2).IsEmpty);
        Assert.False(state.FarmGrid.GetPlot(3, 2).IsWatered);
    }

    [Fact]
    public void ExpandingFarmWithInsufficientMoney_LeavesMoneyAndGridSizeUnchanged()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 149);

        string message = state.ExpandFarm();

        Assert.Equal("Need 150 to expand farm.", message);
        Assert.Equal(149, state.Money);
        Assert.Equal(3, state.FarmGrid.Width);
        Assert.Equal(3, state.FarmGrid.Height);
    }

    [Fact]
    public void ExpandingFarmAtMaxSize_ReportsMaxedAndLeavesStateUnchanged()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 950);
        state.ExpandFarm();
        state.ClearCleanupSite();
        state.ExpandFarm();

        string message = state.ExpandFarm();

        Assert.Equal("Farm is already fully expanded.", message);
        Assert.Equal(5, state.FarmGrid.Width);
        Assert.Equal(3, state.FarmGrid.Height);
        Assert.Equal(0, state.Money);
    }

    [Fact]
    public void CleanupSite_StartsUncleared()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.NotNull(state.CleanupSite);
        Assert.False(state.CleanupSite.IsCleared);
        Assert.Equal(TurnipTestState.OldJunkPileName, state.CleanupSite.Name);
        Assert.Equal(TurnipTestState.OldJunkPileCleanupCost, state.CleanupSite.Cost);
    }

    [Fact]
    public void ClearingCleanupSite_WithEnoughMoney_SubtractsCostMarksClearedAndLogsActivity()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 500);

        string message = state.ClearCleanupSite();

        Assert.Equal("Cleared Old Junk Pile for $500.", message);
        Assert.True(state.CleanupSite.IsCleared);
        Assert.Equal(0, state.Money);
        ActivityLogEntry entry = state.ActivityLog[^1];
        Assert.Equal(ActivityLogCategory.Restoration, entry.Category);
        Assert.Equal("Cleared Old Junk Pile for $500.", entry.Message);
    }

    [Fact]
    public void ClearingCleanupSite_WithoutEnoughMoney_DoesNotChangeState()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 499);

        string message = state.ClearCleanupSite();

        Assert.Equal("Need $500 to clear Old Junk Pile.", message);
        Assert.False(state.CleanupSite.IsCleared);
        Assert.Equal(499, state.Money);
        Assert.Empty(state.ActivityLog);
    }

    [Fact]
    public void ClearingCleanupSite_AlreadyCleared_DoesNotChargeAgain()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 600);
        state.ClearCleanupSite();

        string message = state.ClearCleanupSite();

        Assert.Equal("Old Junk Pile already cleared.", message);
        Assert.Equal(100, state.Money);
        Assert.Single(state.ActivityLog);
    }

    [Fact]
    public void FinalExpansion_IsBlockedUntilCleanupIsCleared()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 450);
        state.ExpandFarm();

        string message = state.ExpandFarm();

        Assert.Equal("Clear Old Junk Pile before expanding further.", message);
        Assert.Equal(4, state.FarmGrid.Width);
        Assert.Equal(300, state.Money);
    }

    [Fact]
    public void EarlierExpansion_StillWorksBeforeCleanup()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 150);

        string message = state.ExpandFarm();

        Assert.Equal("Expanded farm to 4x3 for 150.", message);
        Assert.Equal(4, state.FarmGrid.Width);
    }

    [Fact]
    public void CleanupState_RoundTripsThroughSaveLoad_AndAllowsFinalExpansion()
    {
        TurnipTestState state = new(BuildRoundTripRegistry());
        SetMoney(state, 950);
        state.ExpandFarm();
        state.ClearCleanupSite();
        SaveGameFileService service = new();
        string json = service.Serialize(state.CreateSaveGame());
        SaveGameState saveGame = service.Deserialize(json);
        TurnipTestState loadedState = new(BuildRoundTripRegistry());

        loadedState.ApplySaveGame(saveGame);
        string message = loadedState.ExpandFarm();

        Assert.True(loadedState.CleanupSite.IsCleared);
        Assert.Equal("Expanded farm to 5x3 for 300.", message);
    }

    [Fact]
    public void CleanupStats_RoundTripThroughSaveLoad()
    {
        TurnipTestState state = new(BuildRoundTripRegistry());
        SetMoney(state, 500);
        state.ClearCleanupSite();
        SaveGameFileService service = new();
        string json = service.Serialize(state.CreateSaveGame());
        SaveGameState saveGame = service.Deserialize(json);
        TurnipTestState loadedState = new(BuildRoundTripRegistry());

        loadedState.ApplySaveGame(saveGame);

        Assert.Equal(1, loadedState.Stats.CleanupSitesCleared);
        Assert.Equal(500, loadedState.Stats.MoneySpentOnCleanup);
    }

    [Fact]
    public void LegacyCleanupField_ClearedState_MapsToOldJunkPileWhenRestorationContentExists()
    {
        TurnipTestState state = new(BuildRoundTripRegistry());
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.ClearedRestorationSiteIds.Clear();
        saveGame.CleanupSite = new CleanupSiteSaveState
        {
            Id = TurnipTestState.OldJunkPileId,
            IsCleared = true
        };

        TurnipTestState loadedState = new(BuildRoundTripRegistry());

        loadedState.ApplySaveGame(saveGame);

        Assert.True(loadedState.CleanupSite.IsCleared);
        Assert.Contains(TurnipTestState.OldJunkPileId, loadedState.ClearedRestorationSiteIds);
    }

    [Fact]
    public void Bots_StartLockedAfterResetOrNewState()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.False(state.IsPlanterBotUnlocked);
        Assert.False(state.IsWateringBotUnlocked);
        Assert.False(state.IsHarvesterBotUnlocked);

        SetMoney(state, 500);
        state.UnlockPlanterBot();
        state.UnlockWateringBot();
        state.UnlockHarvesterBot();
        state.Reset();

        Assert.False(state.IsPlanterBotUnlocked);
        Assert.False(state.IsWateringBotUnlocked);
        Assert.False(state.IsHarvesterBotUnlocked);
        Assert.Equal(0, state.Money);
    }

    [Fact]
    public void UnlockingPlanterBotWithEnoughMoney_Subtracts60AndUnlocksIt()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 60);

        string message = state.UnlockPlanterBot();

        Assert.Equal("Unlocked Planter Bot.", message);
        Assert.True(state.IsPlanterBotUnlocked);
        Assert.Equal(0, state.Money);
    }

    [Fact]
    public void UnlockingWateringBotWithEnoughMoney_Subtracts90AndUnlocksIt()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 90);

        string message = state.UnlockWateringBot();

        Assert.Equal("Unlocked Watering Bot.", message);
        Assert.True(state.IsWateringBotUnlocked);
        Assert.Equal(0, state.Money);
    }

    [Fact]
    public void UnlockingHarvesterBotWithEnoughMoney_Subtracts120AndUnlocksIt()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 120);

        string message = state.UnlockHarvesterBot();

        Assert.Equal("Unlocked Harvester Bot.", message);
        Assert.True(state.IsHarvesterBotUnlocked);
        Assert.Equal(0, state.Money);
    }

    [Fact]
    public void AttemptingToUnlockWithoutEnoughMoney_LeavesBotLockedAndMoneyUnchanged()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 59);

        string message = state.UnlockPlanterBot();

        Assert.Equal("Need 60 to unlock Planter Bot.", message);
        Assert.False(state.IsPlanterBotUnlocked);
        Assert.Equal(59, state.Money);
    }

    [Fact]
    public void AttemptingToUnlockAlreadyUnlockedBot_DoesNotSubtractMoneyAgain()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 120);
        state.UnlockPlanterBot();

        string message = state.UnlockPlanterBot();

        Assert.Equal("Planter Bot already unlocked.", message);
        Assert.Equal(60, state.Money);
    }

    [Fact]
    public void WateringPlot_MarksItWatered()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.WaterPlot(1, 1);

        Assert.Equal("Watered plot 1,1.", message);
        Assert.True(state.FarmGrid.GetPlot(1, 1).IsWatered);
    }

    [Fact]
    public void AdvancingDay_ClearsWateredState()
    {
        TurnipTestState state = new(BuildRegistry());
        state.WaterPlot(0, 0);
        state.WaterPlot(2, 1);

        state.AdvanceDay();

        Assert.False(state.FarmGrid.GetPlot(0, 0).IsWatered);
        Assert.False(state.FarmGrid.GetPlot(2, 1).IsWatered);
    }

    [Fact]
    public void PlantedCropInDryPlot_DoesNotGrowOnDayAdvance()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        state.AdvanceDay();

        Assert.Equal(0, state.FarmGrid.GetPlot(0, 0).PlantedCrop!.DaysGrown);
    }

    [Fact]
    public void PlantedCropInWateredPlot_GrowsExactlyOneDay()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.WaterPlot(0, 0);

        state.AdvanceDay();

        Assert.Equal(1, state.FarmGrid.GetPlot(0, 0).PlantedCrop!.DaysGrown);
    }

    [Fact]
    public void MultiplePlots_GrowIndependentlyByWateredState()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "potato_seed");
        state.WaterPlot(0, 0);

        state.AdvanceDay();

        Assert.Equal(1, state.FarmGrid.GetPlot(0, 0).PlantedCrop!.DaysGrown);
        Assert.Equal(0, state.FarmGrid.GetPlot(1, 0).PlantedCrop!.DaysGrown);
    }

    [Fact]
    public void WateringBot_WatersDryPlantedPlotInRegion()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockWateringBot(state);
        state.PlantSeedAtPlot(1, 1, "turnip_seed");

        string message = state.RunWateringBot();

        Assert.Equal("Watering Bot watered 1 plots.", message);
        Assert.True(state.FarmGrid.GetPlot(1, 1).IsWatered);
    }

    [Fact]
    public void EnabledWateringBot_RunsDuringDayAdvance()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockWateringBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.ToggleWateringBotEnabled();

        state.AdvanceDay();

        Assert.Equal("Watering Bot watered 1 plots.", state.LastBotActionMessage);
    }

    [Fact]
    public void DisabledWateringBot_DoesNotRunDuringDayAdvance()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        state.AdvanceDay();

        Assert.Equal("Watering Bot skipped (OFF).", state.LastBotActionMessage);
    }

    [Fact]
    public void WateringBot_DoesNotWaterEmptyPlots()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockWateringBot(state);

        string message = state.RunWateringBot();

        Assert.Equal("Watering Bot found no dry planted plots.", message);
        Assert.All(state.FarmGrid.GetAllPlots(), plot => Assert.False(plot.IsWatered));
    }

    [Fact]
    public void WateringBot_SkipsPlotsOutsideAssignedRegion()
    {
        TurnipTestState state = new(BuildRegistry(), new FarmRegion(0, 0, 1, 1));
        UnlockWateringBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 1, "potato_seed");

        string message = state.RunWateringBot();

        Assert.Equal("Watering Bot watered 1 plots.", message);
        Assert.True(state.FarmGrid.GetPlot(0, 0).IsWatered);
        Assert.False(state.FarmGrid.GetPlot(1, 1).IsWatered);
    }

    [Fact]
    public void WateringBot_SkipsAlreadyWateredPlots()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockWateringBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.WaterPlot(0, 0);

        string message = state.RunWateringBot();

        Assert.Equal("Watering Bot found no dry planted plots.", message);
        Assert.True(state.FarmGrid.GetPlot(0, 0).IsWatered);
    }

    [Fact]
    public void ManualBotRun_LeavesWateredStateVisibleUntilDayAdvance()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockWateringBot(state);
        state.PlantSeedAtPlot(1, 1, "turnip_seed");

        state.RunWateringBot();

        Assert.True(state.FarmGrid.GetPlot(1, 1).IsWatered);

        state.AdvanceDay();

        Assert.False(state.FarmGrid.GetPlot(1, 1).IsWatered);
    }

    [Fact]
    public void BotWateredCrop_GrowsWhenDayAdvances()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockWateringBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.ToggleWateringBotEnabled();

        state.AdvanceDay();

        Assert.Equal(1, state.FarmGrid.GetPlot(0, 0).PlantedCrop!.DaysGrown);
    }

    [Fact]
    public void DryCrop_DoesNotGrowWhenBotDisabled()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        state.AdvanceDay();

        Assert.Equal(0, state.FarmGrid.GetPlot(0, 0).PlantedCrop!.DaysGrown);
    }

    [Fact]
    public void WateringBot_WorksForTurnipAndPotatoWithoutCropSpecificLogic()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockWateringBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "potato_seed");

        string message = state.RunWateringBot();

        Assert.Equal("Watering Bot watered 2 plots.", message);
        Assert.True(state.FarmGrid.GetPlot(0, 0).IsWatered);
        Assert.True(state.FarmGrid.GetPlot(1, 0).IsWatered);
    }

    [Fact]
    public void WateringBot_CanWaterExpandedPlotsWhenUsingDefaultFullFarmRegion()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 150);
        state.ExpandFarm();
        UnlockWateringBot(state);
        state.PlantSeedAtPlot(3, 2, "turnip_seed");

        string message = state.RunWateringBot();

        Assert.Equal("Watering Bot watered 1 plots.", message);
        Assert.True(state.FarmGrid.GetPlot(3, 2).IsWatered);
    }

    [Fact]
    public void PlanterBot_PlantsAssignedSeedInEmptyPlotsInsideRegion()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockPlanterBot(state);

        string message = state.RunPlanterBot();

        Assert.Equal("Planter Bot planted 3 turnip_seed.", message);
        Assert.Equal("turnip", state.FarmGrid.GetPlot(0, 0).PlantedCrop!.CropDefinitionId);
        Assert.Equal("turnip", state.FarmGrid.GetPlot(1, 0).PlantedCrop!.CropDefinitionId);
        Assert.Equal("turnip", state.FarmGrid.GetPlot(2, 0).PlantedCrop!.CropDefinitionId);
    }

    [Fact]
    public void PlanterBot_ConsumesOneSeedPerPlantedPlot()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockPlanterBot(state);

        state.RunPlanterBot();

        Assert.Equal(0, state.GetItemCount("turnip_seed"));
    }

    [Fact]
    public void PlanterBot_StopsWhenOutOfSeeds()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockPlanterBot(state);

        state.RunPlanterBot();

        Assert.Equal(3, state.FarmGrid.GetAllPlots().Count(plot => !plot.IsEmpty));
        Assert.Equal(6, state.FarmGrid.GetAllPlots().Count(plot => plot.IsEmpty));
    }

    [Fact]
    public void PlanterBot_SkipsAlreadyPlantedPlots()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockPlanterBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        string message = state.RunPlanterBot();

        Assert.Equal("Planter Bot planted 2 turnip_seed.", message);
        Assert.Equal("turnip", state.FarmGrid.GetPlot(0, 0).PlantedCrop!.CropDefinitionId);
        Assert.Equal(0, state.GetItemCount("turnip_seed"));
    }

    [Fact]
    public void PlanterBot_SkipsPlotsOutsideAssignedRegion()
    {
        TurnipTestState limitedState = new(
            BuildRegistry(),
            planterBotRegion: new FarmRegion(0, 0, 1, 2));
        UnlockPlanterBot(limitedState);
        limitedState.PlanterBot.AssignedSeedItemId = "potato_seed";

        string message = limitedState.RunPlanterBot();

        Assert.Equal("Planter Bot planted 2 potato_seed.", message);
        Assert.Equal("potato", limitedState.FarmGrid.GetPlot(0, 0).PlantedCrop!.CropDefinitionId);
        Assert.Equal("potato", limitedState.FarmGrid.GetPlot(0, 1).PlantedCrop!.CropDefinitionId);
        Assert.Null(limitedState.FarmGrid.GetPlot(1, 0).PlantedCrop);
    }

    [Fact]
    public void ManualPlanting_WorksInNewlyExpandedPlots()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 150);
        state.ExpandFarm();

        string message = state.PlantSeedAtPlot(3, 2, "turnip_seed");

        Assert.Equal("Planted turnip at 3,2.", message);
        Assert.Equal("turnip", state.FarmGrid.GetPlot(3, 2).PlantedCrop!.CropDefinitionId);
    }

    [Fact]
    public void PlanterBot_CanPlantInExpandedPlotsWhenUsingDefaultFullFarmRegion()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 150);
        state.ExpandFarm();
        SetMoney(state, 10);
        state.BuySelectedSeed();
        UnlockPlanterBot(state);

        string message = state.RunPlanterBot();

        Assert.Equal("Planter Bot planted 4 turnip_seed.", message);
        Assert.Equal("turnip", state.FarmGrid.GetPlot(3, 0).PlantedCrop!.CropDefinitionId);
    }

    [Fact]
    public void PlanterBot_WorksForTurnipAndPotatoWithoutCropSpecificLogic()
    {
        TurnipTestState turnipState = new(BuildRegistry());
        TurnipTestState potatoState = new(BuildRegistry());
        UnlockPlanterBot(turnipState);
        UnlockPlanterBot(potatoState);
        potatoState.SelectNextSeed();

        string turnipMessage = turnipState.RunPlanterBot();
        string potatoMessage = potatoState.RunPlanterBot();

        Assert.Equal("Planter Bot planted 3 turnip_seed.", turnipMessage);
        Assert.Equal("Planter Bot planted 3 potato_seed.", potatoMessage);
        Assert.Equal("turnip", turnipState.FarmGrid.GetPlot(0, 0).PlantedCrop!.CropDefinitionId);
        Assert.Equal("potato", potatoState.FarmGrid.GetPlot(0, 0).PlantedCrop!.CropDefinitionId);
    }

    [Fact]
    public void PlanterBot_ReportsNonPlantableSeedCleanly()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockPlanterBot(state);
        state.PlanterBot.AssignedSeedItemId = "turnip";

        string message = state.RunPlanterBot();

        Assert.Equal("Planter Bot seed is not plantable.", message);
        Assert.All(state.FarmGrid.GetAllPlots(), plot => Assert.True(plot.IsEmpty));
    }

    [Fact]
    public void PlanterBot_FindsNoEmptyPlotsWhenRegionIsFull()
    {
        TurnipTestState state = new(
            BuildRegistry(),
            planterBotRegion: new FarmRegion(0, 0, 3, 1));
        UnlockPlanterBot(state);

        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "potato_seed");
        state.PlantSeedAtPlot(2, 0, "turnip_seed");

        string message = state.RunPlanterBot();

        Assert.Equal("Planter Bot found no empty plots.", message);
    }

    [Fact]
    public void PlantingSelectedSeed_AffectsOnlySelectedPlot()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.PlantSeedAtPlot(1, 1, "turnip_seed");

        Assert.Equal("Planted turnip at 1,1.", message);
        Assert.NotNull(state.FarmGrid.GetPlot(1, 1).PlantedCrop);
        Assert.Null(state.FarmGrid.GetPlot(0, 0).PlantedCrop);
        Assert.Null(state.FarmGrid.GetPlot(2, 2).PlantedCrop);
    }

    [Fact]
    public void DifferentPlots_CanContainDifferentCrops()
    {
        TurnipTestState state = new(BuildRegistry());

        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "potato_seed");

        Assert.Equal("turnip", state.FarmGrid.GetPlot(0, 0).PlantedCrop!.CropDefinitionId);
        Assert.Equal("potato", state.FarmGrid.GetPlot(1, 0).PlantedCrop!.CropDefinitionId);
    }

    [Fact]
    public void Harvesting_WorksAfterEnoughWateredDays()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(2, 2, "turnip_seed");
        state.MoveSelectedPlot(2, 2);

        for (int index = 0; index < 4; index++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        string message = state.HarvestCrop();

        Assert.Equal("Harvested Turnip from 2,2.", message);
        Assert.Equal(1, state.GetItemCount("turnip"));
        Assert.Null(state.FarmGrid.GetPlot(2, 2).PlantedCrop);
    }

    [Fact]
    public void Harvesting_IsNotHardcodedToSpecificCrop()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(1, 1, "potato_seed");
        state.MoveSelectedPlot(1, 1);

        for (int index = 0; index < 5; index++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        string message = state.HarvestCrop();

        Assert.Equal("Harvested Potato from 1,1.", message);
        Assert.Equal(1, state.GetItemCount("potato"));
    }

    [Fact]
    public void ManualHarvest_AffectsOnlySelectedPlot()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "turnip_seed");
        state.MoveSelectedPlot(1, 0);

        for (int index = 0; index < 4; index++)
        {
            state.WaterPlot(0, 0);
            state.WaterPlot(1, 0);
            state.AdvanceDay();
        }

        string message = state.HarvestCrop();

        Assert.Equal("Harvested Turnip from 1,0.", message);
        Assert.NotNull(state.FarmGrid.GetPlot(0, 0).PlantedCrop);
        Assert.Null(state.FarmGrid.GetPlot(1, 0).PlantedCrop);
        Assert.Equal(1, state.GetItemCount("turnip"));
    }

    [Fact]
    public void ManualHarvest_DoesNotHarvestOtherReadyPlots()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "potato_seed");
        state.MoveSelectedPlot(0, 0);

        for (int index = 0; index < 4; index++)
        {
            state.WaterPlot(0, 0);
            state.WaterPlot(1, 0);
            state.AdvanceDay();
        }

        state.WaterPlot(1, 0);
        state.AdvanceDay();

        state.HarvestCrop();

        Assert.True(state.FarmGrid.GetPlot(0, 0).IsEmpty);
        Assert.NotNull(state.FarmGrid.GetPlot(1, 0).PlantedCrop);
        Assert.Equal(1, state.GetItemCount("turnip"));
        Assert.Equal(0, state.GetItemCount("potato"));
    }

    [Fact]
    public void HarvesterBot_HarvestsReadyCropInsideRegion()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockHarvesterBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        for (int index = 0; index < 4; index++)
        {
            state.WaterPlot(0, 0);
            state.AdvanceDay();
        }

        string message = state.RunHarvesterBot();

        Assert.Equal("Harvester Bot harvested 1 crops.", message);
        Assert.Null(state.FarmGrid.GetPlot(0, 0).PlantedCrop);
    }

    [Fact]
    public void HarvesterBot_AddsCorrectJsonDefinedHarvestItemToInventory()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockHarvesterBot(state);
        state.PlantSeedAtPlot(1, 1, "potato_seed");

        for (int index = 0; index < 5; index++)
        {
            state.WaterPlot(1, 1);
            state.AdvanceDay();
        }

        state.RunHarvesterBot();

        Assert.Equal(1, state.GetItemCount("potato"));
    }

    [Fact]
    public void HarvesterBot_ClearsHarvestedPlot()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockHarvesterBot(state);
        state.PlantSeedAtPlot(2, 2, "turnip_seed");

        for (int index = 0; index < 4; index++)
        {
            state.WaterPlot(2, 2);
            state.AdvanceDay();
        }

        state.RunHarvesterBot();

        Assert.True(state.FarmGrid.GetPlot(2, 2).IsEmpty);
    }

    [Fact]
    public void HarvesterBot_SkipsImmatureCrops()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockHarvesterBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.WaterPlot(0, 0);
        state.AdvanceDay();

        string message = state.RunHarvesterBot();

        Assert.Equal("Harvester Bot found no ready crops. Skipped 1 immature crops.", message);
        Assert.NotNull(state.FarmGrid.GetPlot(0, 0).PlantedCrop);
        Assert.Equal(0, state.GetItemCount("turnip"));
    }

    [Fact]
    public void HarvesterBot_SkipsEmptyPlots()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockHarvesterBot(state);

        string message = state.RunHarvesterBot();

        Assert.Equal("Harvester Bot found no ready crops.", message);
        Assert.All(state.FarmGrid.GetAllPlots(), plot => Assert.True(plot.IsEmpty));
    }

    [Fact]
    public void HarvesterBot_SkipsReadyCropsOutsideAssignedRegion()
    {
        TurnipTestState state = new(
            BuildRegistry(),
            harvesterBotRegion: new FarmRegion(0, 0, 1, 1));
        UnlockHarvesterBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 1, "potato_seed");

        for (int index = 0; index < 4; index++)
        {
            state.WaterPlot(0, 0);
            state.WaterPlot(1, 1);
            state.AdvanceDay();
        }

        state.WaterPlot(1, 1);
        state.AdvanceDay();

        string message = state.RunHarvesterBot();

        Assert.Equal("Harvester Bot harvested 1 crops.", message);
        Assert.Equal(1, state.GetItemCount("turnip"));
        Assert.Equal(0, state.GetItemCount("potato"));
        Assert.Null(state.FarmGrid.GetPlot(0, 0).PlantedCrop);
        Assert.NotNull(state.FarmGrid.GetPlot(1, 1).PlantedCrop);
    }

    [Fact]
    public void HarvesterBot_ReportsCorrectHarvestedCount()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockHarvesterBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "potato_seed");

        for (int index = 0; index < 4; index++)
        {
            state.WaterPlot(0, 0);
            state.WaterPlot(1, 0);
            state.AdvanceDay();
        }

        state.WaterPlot(1, 0);
        state.AdvanceDay();

        string message = state.RunHarvesterBot();

        Assert.Equal("Harvester Bot harvested 2 crops.", message);
    }

    [Fact]
    public void HarvesterBot_WorksForTurnipAndPotatoWithoutCropSpecificLogic()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockHarvesterBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "potato_seed");

        for (int index = 0; index < 4; index++)
        {
            state.WaterPlot(0, 0);
            state.WaterPlot(1, 0);
            state.AdvanceDay();
        }

        state.WaterPlot(1, 0);
        state.AdvanceDay();
        state.RunHarvesterBot();

        Assert.Equal(1, state.GetItemCount("turnip"));
        Assert.Equal(1, state.GetItemCount("potato"));
    }

    [Fact]
    public void HarvesterBot_CanHarvestExpandedPlotsWhenUsingDefaultFullFarmRegion()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 150);
        state.ExpandFarm();
        UnlockHarvesterBot(state);
        state.PlantSeedAtPlot(3, 2, "turnip_seed");

        for (int index = 0; index < 4; index++)
        {
            state.WaterPlot(3, 2);
            state.AdvanceDay();
        }

        string message = state.RunHarvesterBot();

        Assert.Equal("Harvester Bot harvested 1 crops.", message);
        Assert.True(state.FarmGrid.GetPlot(3, 2).IsEmpty);
        Assert.Equal(1, state.GetItemCount("turnip"));
    }

    [Fact]
    public void SellingAfterHarvesterBotHarvest_WorksAndUsesJsonSellPrices()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockHarvesterBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "potato_seed");

        for (int index = 0; index < 4; index++)
        {
            state.WaterPlot(0, 0);
            state.WaterPlot(1, 0);
            state.AdvanceDay();
        }

        state.WaterPlot(1, 0);
        state.AdvanceDay();
        state.RunHarvesterBot();

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 2 items for 65.", message);
        Assert.Equal(65, state.Money);
    }

    [Fact]
    public void FarmCycle_PlantsAvailableSeedsIntoEmptyPlots_AndAdvancesTheDay()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockAllBots(state);

        string message = state.RunFarmCycle();

        Assert.Contains("planted 3", message);
        Assert.Contains("watered 3", message);
        Assert.Contains("advanced to day 2", message);
        Assert.Equal(2, state.DayNumber);
        Assert.Equal(3, state.FarmGrid.GetAllPlots().Count(plot => !plot.IsEmpty));
    }

    [Fact]
    public void FarmCycle_WatersPlantedPlotsSoTheyGrowOneDay()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockAllBots(state);

        state.RunFarmCycle();

        Assert.All(
            state.FarmGrid.GetAllPlots().Where(plot => !plot.IsEmpty),
            plot => Assert.Equal(1, plot.PlantedCrop!.DaysGrown));
    }

    [Fact]
    public void FarmCycle_HarvestsReadyCrops_AndSellsThem()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockAllBots(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        string lastMessage = string.Empty;
        for (int index = 0; index < 4; index++)
            lastMessage = state.RunFarmCycle();

        Assert.Contains("harvested 3", lastMessage);
        Assert.Contains("sold 3 for 90", lastMessage);
        Assert.Equal(90, state.Money);
        Assert.Equal(0, state.GetItemCount("turnip"));
    }

    [Fact]
    public void FarmCycle_WorksForPotatoThroughGenericRegistryLogic()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockAllBots(state);
        state.SelectNextSeed();
        state.PlantSeedAtPlot(0, 0, "potato_seed");

        string lastMessage = string.Empty;
        for (int index = 0; index < 5; index++)
            lastMessage = state.RunFarmCycle();

        Assert.Contains("sold 3 for 105", lastMessage);
        Assert.Equal(105, state.Money);
    }

    [Fact]
    public void FarmCycle_WorksOnExpandedGrid()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 150);
        state.ExpandFarm();
        SetMoney(state, 10);
        state.BuySelectedSeed();
        UnlockAllBots(state);

        string message = state.RunFarmCycle();

        Assert.Contains("planted 4", message);
        Assert.Contains("watered 4", message);
        Assert.Equal("turnip", state.FarmGrid.GetPlot(3, 0).PlantedCrop!.CropDefinitionId);
    }

    [Fact]
    public void FarmCycle_DoesNotHarvestReadyCropsIfHarvesterBotIsLocked()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockPlanterBot(state);
        UnlockWateringBot(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        string lastMessage = string.Empty;
        for (int index = 0; index < 4; index++)
            lastMessage = state.RunFarmCycle();

        Assert.Contains("harvester locked", lastMessage);
        Assert.Contains("nothing to sell", lastMessage);
        Assert.NotNull(state.FarmGrid.GetPlot(0, 0).PlantedCrop);
        Assert.Equal(0, state.GetItemCount("turnip"));
        Assert.Equal(0, state.Money);
    }

    [Fact]
    public void FarmCycle_HarvestsReadyCropsIfHarvesterBotIsUnlocked()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockAllBots(state);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        string lastMessage = string.Empty;
        for (int index = 0; index < 4; index++)
            lastMessage = state.RunFarmCycle();

        Assert.Contains("harvested 3", lastMessage);
        Assert.Contains("sold 3 for 90", lastMessage);
        Assert.Equal(90, state.Money);
    }

    [Fact]
    public void FarmCycle_DoesNotSellSeeds()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockAllBots(state);

        string message = state.RunFarmCycle();

        Assert.Contains("nothing to sell", message);
        Assert.Equal(0, state.Money);
        Assert.Equal(0, state.GetItemCount("turnip"));
        Assert.Equal(0, state.GetItemCount("potato"));
    }

    [Fact]
    public void FarmCycle_HandlesNoSeedsGracefully()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockAllBots(state);
        state.RunFarmCycle();

        string message = state.RunFarmCycle();

        Assert.Contains("no seeds", message);
        Assert.Contains("nothing to sell", message);
    }

    [Fact]
    public void FarmCycle_HandlesNoReadyCropsGracefully()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockAllBots(state);

        string message = state.RunFarmCycle();

        Assert.Contains("no ready crops", message);
    }

    [Fact]
    public void LockedPlanterBot_CannotRun()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.RunPlanterBot();

        Assert.Equal("Planter Bot is locked.", message);
    }

    [Fact]
    public void LockedWateringBot_CannotRun()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.RunWateringBot();

        Assert.Equal("Watering Bot is locked.", message);
    }

    [Fact]
    public void LockedHarvesterBot_CannotRun()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.RunHarvesterBot();

        Assert.Equal("Harvester Bot is locked.", message);
    }

    [Fact]
    public void ManualPlantingWateringAndHarvesting_StillWorkWhileBotsAreLocked()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.MoveSelectedPlot(0, 0);

        for (int index = 0; index < 4; index++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        string message = state.HarvestCrop();

        Assert.Equal("Harvested Turnip from 0,0.", message);
        Assert.Equal(1, state.GetItemCount("turnip"));
    }

    [Fact]
    public void FarmCycle_SkipsLockedBotsButStillAdvancesTheDay()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.RunFarmCycle();

        Assert.Equal(2, state.DayNumber);
        Assert.StartsWith("Cycle:", message);
        Assert.Contains("planter locked", message);
        Assert.Contains("watering locked", message);
        Assert.Contains("harvester locked", message);
    }

    [Fact]
    public void FarmCycle_RunsUnlockedBots()
    {
        TurnipTestState state = new(BuildRegistry());
        UnlockAllBots(state);

        string message = state.RunFarmCycle();

        Assert.Contains("planted 3", message);
        Assert.Contains("watered 3", message);
    }

    [Fact]
    public void FarmCycle_SummaryFormatting_StartsWithCycleAndHasNoExtraComma()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.RunFarmCycle();

        Assert.StartsWith("Cycle:", message);
        Assert.DoesNotContain("Cycle:,", message);
    }

    [Fact]
    public void SellingOneTurnip_IncreasesMoneyByJsonSellPrice()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.MoveSelectedPlot(0, 0);

        for (int index = 0; index < 4; index++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        state.HarvestCrop();
        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 30.", message);
        Assert.Equal(30, state.Money);
        Assert.Equal(0, state.GetItemCount("turnip"));
    }

    [Fact]
    public void SellingOnePotato_IncreasesMoneyByJsonSellPrice()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "potato_seed");
        state.MoveSelectedPlot(0, 0);

        for (int index = 0; index < 5; index++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        state.HarvestCrop();
        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 35.", message);
        Assert.Equal(35, state.Money);
        Assert.Equal(0, state.GetItemCount("potato"));
    }

    [Fact]
    public void SellingMultipleCropTypes_AddsCombinedValue()
    {
        TurnipTestState state = new(BuildRegistry());

        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.PlantSeedAtPlot(1, 0, "potato_seed");

        for (int index = 0; index < 4; index++)
        {
            state.WaterPlot(0, 0);
            state.WaterPlot(1, 0);
            state.AdvanceDay();
        }

        state.WaterPlot(1, 0);
        state.AdvanceDay();

        state.HarvestPlot(0, 0);
        state.HarvestPlot(1, 0);

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 2 items for 65.", message);
        Assert.Equal(65, state.Money);
    }

    [Fact]
    public void Selling_RemovesSoldItemsFromInventory()
    {
        TurnipTestState state = new(BuildRegistry());
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.MoveSelectedPlot(0, 0);

        for (int index = 0; index < 4; index++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        state.HarvestCrop();
        state.SellSelectedCrop();

        Assert.Equal(0, state.GetItemCount("turnip"));
    }

    [Fact]
    public void Selling_DoesNotSellSeeds()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.SellSelectedCrop();

        Assert.Equal("No harvested crops to sell.", message);
        Assert.Equal(3, state.GetItemCount("turnip_seed"));
        Assert.Equal(3, state.GetItemCount("potato_seed"));
        Assert.Equal(0, state.Money);
    }

    [Fact]
    public void SharedStorage_StartsEmpty_AndResetClearsStoredItems()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.Empty(state.SharedStorage);

        AddStorageItem(state, "turnip", 2);
        state.Reset();

        Assert.Empty(state.SharedStorage);
        Assert.Equal(0, state.GetStorageItemCount("turnip"));
    }

    [Fact]
    public void DepositAllSellablesToStorage_MovesSellableItemsAndKeepsSeedsInPlayerInventory()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "turnip", 2);
        AddInventoryItem(state, "potato", 1);

        string message = state.DepositAllSellablesToStorage();

        Assert.Equal("Stored 3 items in shared storage.", message);
        Assert.Equal(0, state.GetItemCount("turnip"));
        Assert.Equal(0, state.GetItemCount("potato"));
        Assert.Equal(2, state.GetStorageItemCount("turnip"));
        Assert.Equal(1, state.GetStorageItemCount("potato"));
        Assert.Equal(3, state.GetItemCount("turnip_seed"));
        Assert.Equal(3, state.GetItemCount("potato_seed"));
    }

    [Fact]
    public void WithdrawAllFromStorage_MovesStoredItemsBackToPlayerInventory()
    {
        TurnipTestState state = new(BuildRegistry());
        AddStorageItem(state, "turnip", 2);
        AddStorageItem(state, "potato", 1);

        string message = state.WithdrawAllFromStorage();

        Assert.Equal("Withdrew 3 items from shared storage.", message);
        Assert.Equal(2, state.GetItemCount("turnip"));
        Assert.Equal(1, state.GetItemCount("potato"));
        Assert.Empty(state.SharedStorage);
    }

    [Fact]
    public void Selling_CanSellItemsFromSharedStorage()
    {
        TurnipTestState state = new(BuildRegistry());
        AddStorageItem(state, "turnip", 2);

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 2 items for 60.", message);
        Assert.Equal(60, state.Money);
        Assert.Equal(0, state.GetStorageItemCount("turnip"));
    }

    [Fact]
    public void SellingWithNoHarvestedCrops_LeavesMoneyUnchangedAndReportsUsefulMessage()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.SellSelectedCrop();

        Assert.Equal("No harvested crops to sell.", message);
        Assert.Equal(0, state.Money);
    }

    [Fact]
    public void BuyingSelectedTurnipSeed_SubtractsJsonBuyPriceAndAddsOneSeed()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 10);

        string message = state.BuySelectedSeed();

        Assert.Equal("Bought 1 Turnip Seeds for 10.", message);
        Assert.Equal(0, state.Money);
        Assert.Equal(4, state.GetItemCount("turnip_seed"));
    }

    [Fact]
    public void BuyingSelectedPotatoSeed_SubtractsJsonBuyPriceAndAddsOneSeed()
    {
        TurnipTestState state = new(BuildRegistry());
        state.SelectNextSeed();
        SetMoney(state, 12);

        string message = state.BuySelectedSeed();

        Assert.Equal("Bought 1 Potato Seeds for 12.", message);
        Assert.Equal(0, state.Money);
        Assert.Equal(4, state.GetItemCount("potato_seed"));
    }

    [Fact]
    public void BuyingWithInsufficientMoney_LeavesMoneyAndInventoryUnchanged()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 9);

        string message = state.BuySelectedSeed();

        Assert.Equal("Need 10 to buy Turnip Seeds.", message);
        Assert.Equal(9, state.Money);
        Assert.Equal(3, state.GetItemCount("turnip_seed"));
    }

    [Fact]
    public void BuyingSeedWithZeroBuyPrice_IsRejectedCleanly()
    {
        TurnipTestState state = new(BuildFreeSeedRegistry());
        SetMoney(state, 50);

        string message = state.BuySelectedSeed();

        Assert.Equal("Free Seeds cannot be bought.", message);
        Assert.Equal(50, state.Money);
        Assert.Equal(3, state.GetItemCount("free_seed"));
    }

    [Fact]
    public void BuyingSelectedSeed_WorksThroughGenericRegistryLogic()
    {
        TurnipTestState state = new(BuildSingleSeedRegistry("beet_seed", "Beet Seeds", 7, "beet", "Beet", 22));
        SetMoney(state, 7);

        string message = state.BuySelectedSeed();

        Assert.Equal("Bought 1 Beet Seeds for 7.", message);
        Assert.Equal(0, state.Money);
        Assert.Equal(4, state.GetItemCount("beet_seed"));
    }

    [Fact]
    public void BuyingSelectedSeed_DoesNotChangeSelectedSeed()
    {
        TurnipTestState state = new(BuildRegistry());
        state.SelectNextSeed();
        SetMoney(state, 12);

        state.BuySelectedSeed();

        Assert.Equal("potato_seed", state.SelectedSeedItemId);
    }

    [Fact]
    public void BuyingAsManySelectedSeedsAsPossible_UsesSelectedSeedBuyPrice()
    {
        TurnipTestState state = new(BuildRegistry());
        state.SelectNextSeed();
        SetMoney(state, 25);

        string message = state.BuySelectedSeed(true);

        Assert.Equal("Bought 2 Potato Seeds for 24.", message);
        Assert.Equal(1, state.Money);
        Assert.Equal(5, state.GetItemCount("potato_seed"));
    }

    [Fact]
    public void InvalidSeedItem_CannotBePlanted()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.PlantSeedItem("bad_seed");

        Assert.Equal("No producer uses starter item 'bad_seed'.", message);
        Assert.Null(state.GetSelectedPlot().PlantedCrop);
    }

    [Fact]
    public void FullProgressionScenario_CanAutomateAndExpandFarm()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        state.Reset();

        Assert.Equal(3, state.FarmGrid.Width);
        Assert.Equal(3, state.FarmGrid.Height);
        Assert.False(state.IsPlanterBotUnlocked);
        Assert.False(state.IsWateringBotUnlocked);
        Assert.False(state.IsHarvesterBotUnlocked);
        Assert.Equal(0, state.Money);
        Assert.True(CountSeeds(state) > 0);

        GameplayScenarioResult result = RunFullProgressionScenario(state, registry, maxIterations: 100);

        Console.WriteLine(
            $"Scenario result: days={result.DaysSimulated}, moneyEarned={result.TotalMoneyEarned}, seedsBought={result.SeedsBought}, botsUnlocked={result.BotsUnlocked}, expansions={result.ExpansionsCompleted}, harvested={result.CropsHarvested}, sold={result.CropsSold}, iterations={result.IterationsUsed}");

        Assert.True(result.EarnedMoneyAtSomePoint);
        Assert.True(result.SeedsBought > 0);
        Assert.True(result.BotsUnlocked >= 1);
        Assert.True(state.IsPlanterBotUnlocked);
        Assert.True(state.IsWateringBotUnlocked);
        Assert.True(state.IsHarvesterBotUnlocked);
        Assert.True(result.ExpansionsCompleted >= 1);
        Assert.True(result.UsedBotActionSuccessfully);
        Assert.True(result.HarvesterBotHarvests > 0);
        Assert.True(result.ExpandedPlotWasUsed);
        Assert.True(result.IterationsUsed < 100);
        Assert.Equal(4, state.FarmGrid.Width);
        Assert.Equal(3, state.FarmGrid.Height);
    }

    private static GameplayScenarioResult RunFullProgressionScenario(TurnipTestState state, ContentRegistry registry, int maxIterations)
    {
        int seedsBought = 0;
        int botsUnlocked = 0;
        int expansionsCompleted = 0;
        int cropsHarvested = 0;
        int cropsSold = 0;
        int harvesterBotHarvests = 0;
        int totalMoneyEarned = 0;
        bool earnedMoneyAtSomePoint = false;
        bool usedBotActionSuccessfully = false;
        bool expandedPlotWasUsed = false;
        int iterationsUsed = 0;
        bool wasExpanded = false;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            iterationsUsed = iteration + 1;
            int selectedSeedBuyPrice = registry.GetItem(state.SelectedSeedItemId).BuyPrice;

            if (CountSeeds(state) < 6 && selectedSeedBuyPrice > 0 && state.Money >= selectedSeedBuyPrice)
            {
                int selectedSeedCountBeforeBuy = state.GetItemCount(state.SelectedSeedItemId);
                state.BuySelectedSeed();
                int selectedSeedCountAfterBuy = state.GetItemCount(state.SelectedSeedItemId);
                seedsBought += Math.Max(0, selectedSeedCountAfterBuy - selectedSeedCountBeforeBuy);
            }

            if (!wasExpanded && state.Money >= state.NextFarmExpansionCost)
            {
                string expansionMessage = state.ExpandFarm();
                if (expansionMessage.StartsWith("Expanded farm", StringComparison.Ordinal))
                {
                    expansionsCompleted++;
                    wasExpanded = true;
                }
            }
            else if (!state.IsPlanterBotUnlocked && state.Money >= TurnipTestState.PlanterBotUnlockCost)
            {
                string unlockMessage = state.UnlockPlanterBot();
                if (unlockMessage.StartsWith("Unlocked", StringComparison.Ordinal))
                    botsUnlocked++;
            }
            else if (!state.IsWateringBotUnlocked && state.Money >= TurnipTestState.WateringBotUnlockCost)
            {
                string unlockMessage = state.UnlockWateringBot();
                if (unlockMessage.StartsWith("Unlocked", StringComparison.Ordinal))
                    botsUnlocked++;
            }
            else if (!state.IsHarvesterBotUnlocked && state.Money >= TurnipTestState.HarvesterBotUnlockCost)
            {
                string unlockMessage = state.UnlockHarvesterBot();
                if (unlockMessage.StartsWith("Unlocked", StringComparison.Ordinal))
                    botsUnlocked++;
            }

            if (state.IsPlanterBotUnlocked)
            {
                int plantedPlotsBeforeBot = CountPlantedPlots(state);
                state.RunPlanterBot();
                int plantedByBot = CountPlantedPlots(state) - plantedPlotsBeforeBot;
                if (plantedByBot > 0)
                    usedBotActionSuccessfully = true;
            }
            else
            {
                PlantEmptyPlotsManually(state);
            }

            if (!expandedPlotWasUsed)
                expandedPlotWasUsed = HasUsedExpandedPlot(state);

            if (state.IsWateringBotUnlocked)
            {
                int dryPlotsBeforeBot = CountDryPlantedPlots(state);
                state.RunWateringBot();
                int wateredByBot = dryPlotsBeforeBot - CountDryPlantedPlots(state);
                if (wateredByBot > 0)
                    usedBotActionSuccessfully = true;
            }
            else
            {
                WaterDryPlotsManually(state);
            }

            AssertValidState(state, registry, maxIterations);

            state.AdvanceDay();

            if (state.IsHarvesterBotUnlocked)
            {
                int harvestedItemsBeforeBot = CountSellableCrops(state, registry);
                state.RunHarvesterBot();
                int harvestedByBot = CountSellableCrops(state, registry) - harvestedItemsBeforeBot;
                if (harvestedByBot > 0)
                {
                    cropsHarvested += harvestedByBot;
                    harvesterBotHarvests += harvestedByBot;
                    usedBotActionSuccessfully = true;
                }
            }
            else
            {
                cropsHarvested += HarvestReadyPlotsManually(state);
            }

            int sellableCropCountBeforeSell = CountSellableCrops(state, registry);
            int moneyBeforeSell = state.Money;
            state.SellSelectedCrop();
            int soldThisTurn = sellableCropCountBeforeSell - CountSellableCrops(state, registry);
            int moneyGainedThisTurn = state.Money - moneyBeforeSell;

            if (soldThisTurn > 0)
            {
                cropsSold += soldThisTurn;
                totalMoneyEarned += moneyGainedThisTurn;
                earnedMoneyAtSomePoint = earnedMoneyAtSomePoint || moneyGainedThisTurn > 0;
            }

            AssertValidState(state, registry, maxIterations);

            bool reachedSuccessState = wasExpanded
                && state.IsPlanterBotUnlocked
                && state.IsWateringBotUnlocked
                && state.IsHarvesterBotUnlocked
                && seedsBought > 0
                && usedBotActionSuccessfully
                && harvesterBotHarvests > 0
                && expandedPlotWasUsed;

            if (reachedSuccessState)
            {
                return new GameplayScenarioResult(
                    state.DayNumber - 1,
                    totalMoneyEarned,
                    seedsBought,
                    botsUnlocked,
                    expansionsCompleted,
                    cropsHarvested,
                    cropsSold,
                    harvesterBotHarvests,
                    usedBotActionSuccessfully,
                    expandedPlotWasUsed,
                    earnedMoneyAtSomePoint,
                    iterationsUsed);
            }
        }

        return new GameplayScenarioResult(
            state.DayNumber - 1,
            totalMoneyEarned,
            seedsBought,
            botsUnlocked,
            expansionsCompleted,
            cropsHarvested,
            cropsSold,
            harvesterBotHarvests,
            usedBotActionSuccessfully,
            expandedPlotWasUsed,
            earnedMoneyAtSomePoint,
            iterationsUsed);
    }

    [Fact]
    public void RunPlanterBot_ProducesReplaySequenceForAffectedTiles()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, TurnipTestState.PlanterBotUnlockCost);
        state.UnlockPlanterBot();

        string message = state.RunPlanterBot();
        BotActivityReplay replay = state.ConsumePendingBotActivityReplay();

        Assert.StartsWith("Planter Bot planted", message, StringComparison.Ordinal);
        Assert.NotNull(replay);
        Assert.Equal(3, replay.Steps.Count);
        Assert.All(replay.Steps, step => Assert.Equal(BotRole.Starter, step.Role));
        Assert.All(replay.Steps, step => Assert.True(step.Before.IsEmpty));
        Assert.All(replay.Steps, step => Assert.False(step.After.IsEmpty));
        Assert.Equal((0, 0), (replay.Steps[0].TileX, replay.Steps[0].TileY));
        Assert.Equal("Planter Bot planting tile 0,0", replay.Steps[0].StatusLine);
        Assert.Null(state.PendingBotActivityReplay);
    }

    [Fact]
    public void ConsumingPendingBotReplay_DoesNotChangeGameplayResults()
    {
        TurnipTestState baselineState = new(BuildRegistry());
        TurnipTestState replayState = new(BuildRegistry());
        SetMoney(baselineState, 500);
        SetMoney(replayState, 500);

        baselineState.UnlockPlanterBot();
        baselineState.UnlockWateringBot();
        baselineState.UnlockHarvesterBot();
        replayState.UnlockPlanterBot();
        replayState.UnlockWateringBot();
        replayState.UnlockHarvesterBot();

        string baselineMessage = baselineState.RunFarmCycle();
        string replayMessage = replayState.RunFarmCycle();
        _ = replayState.ConsumePendingBotActivityReplay();

        Assert.Equal(baselineMessage, replayMessage);
        Assert.Equal(baselineState.DayNumber, replayState.DayNumber);
        Assert.Equal(baselineState.Money, replayState.Money);
        Assert.Equal(baselineState.Inventory.OrderBy(entry => entry.Key), replayState.Inventory.OrderBy(entry => entry.Key));

        foreach (FarmPlot plot in baselineState.FarmGrid.GetAllPlots())
        {
            FarmPlot replayPlot = replayState.FarmGrid.GetPlot(plot.X, plot.Y);
            Assert.Equal(plot.IsEmpty, replayPlot.IsEmpty);
            Assert.Equal(plot.IsWatered, replayPlot.IsWatered);
            Assert.Equal(plot.PlantedCrop?.CropDefinitionId, replayPlot.PlantedCrop?.CropDefinitionId);
            Assert.Equal(plot.PlantedCrop?.DaysGrown ?? 0, replayPlot.PlantedCrop?.DaysGrown ?? 0);
        }
    }

    private static void PlantEmptyPlotsManually(TurnipTestState state)
    {
        while (state.GetItemCount(state.SelectedSeedItemId) > 0 && TryFindEmptyPlot(state, out int emptyPlotX, out int emptyPlotY))
        {
            SelectPlot(state, emptyPlotX, emptyPlotY);
            state.PlantSelectedSeed();
        }
    }

    private static void WaterDryPlotsManually(TurnipTestState state)
    {
        while (TryFindDryPlantedPlot(state, out int dryPlotX, out int dryPlotY))
        {
            SelectPlot(state, dryPlotX, dryPlotY);
            state.WaterSelectedPlot();
        }
    }

    private static int HarvestReadyPlotsManually(TurnipTestState state)
    {
        int harvestedCount = 0;

        while (TryFindReadyPlot(state, out int readyPlotX, out int readyPlotY))
        {
            SelectPlot(state, readyPlotX, readyPlotY);
            string harvestMessage = state.HarvestCrop();
            if (harvestMessage.StartsWith("Harvested", StringComparison.Ordinal))
                harvestedCount++;
        }

        return harvestedCount;
    }

    private static int CountSeeds(TurnipTestState state)
    {
        return state.SeedItemIds.Sum(state.GetItemCount);
    }

    private static int CountSellableCrops(TurnipTestState state, ContentRegistry registry)
    {
        return registry.Items
            .Where(item => !registry.IsStarterItem(item.Id) && item.SellPrice > 0)
            .Sum(item => state.GetItemCount(item.Id));
    }

    private static int CountPlantedPlots(TurnipTestState state)
    {
        return state.FarmGrid.GetAllPlots().Count(plot => !plot.IsEmpty);
    }

    private static int CountDryPlantedPlots(TurnipTestState state)
    {
        return state.FarmGrid.GetAllPlots().Count(plot => !plot.IsEmpty && !plot.IsWatered);
    }

    private static void SelectPlot(TurnipTestState state, int x, int y)
    {
        state.MoveSelectedPlot(x - state.SelectedPlotX, y - state.SelectedPlotY);
    }

    private static bool TryFindEmptyPlot(TurnipTestState state, out int plotX, out int plotY)
    {
        foreach (FarmPlot plot in state.FarmGrid.GetAllPlots())
        {
            if (!plot.IsEmpty)
                continue;

            plotX = plot.X;
            plotY = plot.Y;
            return true;
        }

        plotX = -1;
        plotY = -1;
        return false;
    }

    private static bool TryFindDryPlantedPlot(TurnipTestState state, out int plotX, out int plotY)
    {
        foreach (FarmPlot plot in state.FarmGrid.GetAllPlots())
        {
            if (plot.IsEmpty || plot.IsWatered)
                continue;

            plotX = plot.X;
            plotY = plot.Y;
            return true;
        }

        plotX = -1;
        plotY = -1;
        return false;
    }

    private static bool TryFindReadyPlot(TurnipTestState state, out int plotX, out int plotY)
    {
        foreach (FarmPlot plot in state.FarmGrid.GetAllPlots())
        {
            if (!state.IsPlotHarvestable(plot.X, plot.Y))
                continue;

            plotX = plot.X;
            plotY = plot.Y;
            return true;
        }

        plotX = -1;
        plotY = -1;
        return false;
    }

    private static bool HasUsedExpandedPlot(TurnipTestState state)
    {
        return state.FarmGrid.GetAllPlots().Any(plot => plot.X >= TurnipTestState.InitialFarmWidth && !plot.IsEmpty);
    }

    private static void AssertValidState(TurnipTestState state, ContentRegistry registry, int maxIterations)
    {
        Assert.True(state.Money >= 0);
        Assert.InRange(state.SelectedPlotX, 0, state.FarmGrid.Width - 1);
        Assert.InRange(state.SelectedPlotY, 0, state.FarmGrid.Height - 1);

        foreach (ItemDefinition item in registry.Items)
            Assert.True(state.GetItemCount(item.Id) >= 0);

        foreach (FarmPlot plot in state.FarmGrid.GetAllPlots())
        {
            if (plot.PlantedCrop is null)
                continue;

            CropDefinition cropDefinition = registry.GetCrop(plot.PlantedCrop.CropDefinitionId);
            Assert.True(plot.PlantedCrop.DaysGrown <= cropDefinition.GrowthDays + maxIterations);
        }
    }

    private sealed record GameplayScenarioResult(
        int DaysSimulated,
        int TotalMoneyEarned,
        int SeedsBought,
        int BotsUnlocked,
        int ExpansionsCompleted,
        int CropsHarvested,
        int CropsSold,
        int HarvesterBotHarvests,
        bool UsedBotActionSuccessfully,
        bool ExpandedPlotWasUsed,
        bool EarnedMoneyAtSomePoint,
        int IterationsUsed);

    [Fact]
    public void NewGame_StartsWithEmptyActivityLog()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.Empty(state.ActivityLog);
    }

    [Fact]
    public void ActivityLog_IsBoundedToMaxEntries()
    {
        TurnipTestState state = new(BuildRegistry());
        MethodInfo addActivity = typeof(TurnipTestState)
            .GetMethod("AddActivity", BindingFlags.Instance | BindingFlags.NonPublic)!;

        for (int index = 0; index < TurnipTestState.ActivityLogMaxEntries + 5; index++)
            addActivity.Invoke(state, [ActivityLogCategory.System, $"Entry {index}", null, null, null]);

        Assert.Equal(TurnipTestState.ActivityLogMaxEntries, state.ActivityLog.Count);
        Assert.Equal("Entry 5", state.ActivityLog[0].Message);
        Assert.Equal($"Entry {TurnipTestState.ActivityLogMaxEntries + 4}", state.ActivityLog[^1].Message);
    }

    [Fact]
    public void Reset_ClearsActivityLog()
    {
        TurnipTestState state = new(BuildActivityLogRegistry());
        SetMoney(state, 160);
        state.BuyMarketOffer("buy_grapes");

        state.Reset();

        Assert.Empty(state.ActivityLog);
    }

    [Fact]
    public void ActivityLog_EntriesIncludeDayCategoryAndDeterministicOrder()
    {
        TurnipTestState state = new(BuildActivityLogRegistry());
        SetMoney(state, 320);

        state.BuyMarketOffer("buy_grapes");
        state.StartFermenterFromInventory();

        Assert.Collection(
            state.ActivityLog,
            entry =>
            {
                Assert.Equal(1, entry.Day);
                Assert.Equal(ActivityLogCategory.Market, entry.Category);
                Assert.Equal("Bought 3 Grapes for $160.", entry.Message);
            },
            entry =>
            {
                Assert.Equal(1, entry.Day);
                Assert.Equal(ActivityLogCategory.Machine, entry.Category);
                Assert.Equal("Started Fermenter: 3 Grapes -> 1 Wine, ready in 2 days.", entry.Message);
            });
    }

    [Fact]
    public void BuyingMarketOffers_AddsMarketActivityEntries()
    {
        TurnipTestState grapeState = new(BuildActivityLogRegistry());
        SetMoney(grapeState, 160);
        grapeState.BuyMarketOffer("buy_grapes");

        TurnipTestState hopsState = new(BuildActivityLogRegistry());
        SetMoney(hopsState, 145);
        hopsState.BuyMarketOffer("buy_hops");

        Assert.Equal("Bought 3 Grapes for $160.", Assert.Single(grapeState.ActivityLog).Message);
        Assert.Equal(ActivityLogCategory.Market, grapeState.ActivityLog[0].Category);
        Assert.Equal("Bought 3 Hops for $145.", Assert.Single(hopsState.ActivityLog).Message);
    }

    [Fact]
    public void FailedMarketPurchase_DoesNotAddActivityEntry()
    {
        TurnipTestState state = new(BuildActivityLogRegistry());
        SetMoney(state, 100);

        state.BuyMarketOffer("buy_grapes");

        Assert.Empty(state.ActivityLog);
    }

    [Fact]
    public void SellingGoods_AddsSaleActivityEntry_AndFailedSellDoesNot()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "turnip", 1);

        state.SellSelectedCrop();

        ActivityLogEntry entry = Assert.Single(state.ActivityLog);
        Assert.Equal(ActivityLogCategory.Sale, entry.Category);
        Assert.Equal("Sold 1 Turnip for $30.", entry.Message);

        state.SellSelectedCrop();

        Assert.Single(state.ActivityLog);
    }

    [Fact]
    public void StorageActions_AddEntries_AndNoOpDoesNotSpam()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "turnip", 2);

        state.DepositAllSellablesToStorage();
        state.DepositAllSellablesToStorage();
        state.WithdrawAllFromStorage();
        int entryCountAfterSuccess = state.ActivityLog.Count;
        state.WithdrawAllFromStorage();

        Assert.Equal(2, entryCountAfterSuccess);
        Assert.Equal(entryCountAfterSuccess, state.ActivityLog.Count);
        Assert.Equal("Stored 2 items in shared storage.", state.ActivityLog[0].Message);
        Assert.Equal("Withdrew 2 items from shared storage.", state.ActivityLog[1].Message);
    }

    [Fact]
    public void UnlockingBot_AddsActivityEntry_AndFailedUnlockDoesNot()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 60);

        state.UnlockPlanterBot();

        ActivityLogEntry entry = Assert.Single(state.ActivityLog);
        Assert.Equal(ActivityLogCategory.Bot, entry.Category);
        Assert.Equal("Unlocked Planter Bot for $60.", entry.Message);

        state.Reset();
        SetMoney(state, 59);
        state.UnlockPlanterBot();
        Assert.Empty(state.ActivityLog);
    }

    [Fact]
    public void ExpandingFarm_AddsActivityEntry_AndFailedExpansionDoesNot()
    {
        TurnipTestState successState = new(BuildRegistry());
        SetMoney(successState, 150);
        successState.ExpandFarm();

        Assert.Equal("Expanded farm to 4x3 for $150.", Assert.Single(successState.ActivityLog).Message);

        TurnipTestState failedState = new(BuildRegistry());
        SetMoney(failedState, 149);
        failedState.ExpandFarm();

        Assert.Empty(failedState.ActivityLog);
    }

    private static ContentRegistry BuildActivityLogRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed", BuyPrice = 10, SellPrice = 1 },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop", BuyPrice = 0, SellPrice = 30 },
                new ItemDefinition { Id = "grape_seed", Name = "Grape Seeds", Type = "seed", BuyPrice = 15, SellPrice = 1 },
                new ItemDefinition { Id = "grape", Name = "Grapes", Type = "crop", BuyPrice = 0, SellPrice = 45 },
                new ItemDefinition { Id = "hops_seed", Name = "Hops Seeds", Type = "seed", BuyPrice = 14, SellPrice = 1 },
                new ItemDefinition { Id = "hops", Name = "Hops", Type = "crop", BuyPrice = 0, SellPrice = 40 },
                new ItemDefinition { Id = "wine", Name = "Wine", Type = "processed_good", BuyPrice = 0, SellPrice = 160 },
                new ItemDefinition { Id = "beer", Name = "Beer", Type = "processed_good", BuyPrice = 0, SellPrice = 140 }
            ],
            [
                new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 },
                new CropDefinition { Id = "grape", SeedItemId = "grape_seed", HarvestItemId = "grape", GrowthDays = 4 },
                new CropDefinition { Id = "hops", SeedItemId = "hops_seed", HarvestItemId = "hops", GrowthDays = 4 }
            ],
            [
                new MachineDefinition { Id = "fermenter", Name = "Fermenter", Type = "processor_machine" }
            ],
            [
                new ProcessingRecipeDefinition { Id = "grape_to_wine", MachineId = "fermenter", InputItemId = "grape", InputQuantity = 3, OutputItemId = "wine", OutputQuantity = 1, ProcessingDays = 2 },
                new ProcessingRecipeDefinition { Id = "hops_to_beer", MachineId = "fermenter", InputItemId = "hops", InputQuantity = 3, OutputItemId = "beer", OutputQuantity = 1, ProcessingDays = 2 }
            ],
            [
                new MarketOfferDefinition { Id = "buy_grapes", ItemId = "grape", Quantity = 3, Price = 160, Category = "raw_input" },
                new MarketOfferDefinition { Id = "buy_hops", ItemId = "hops", Quantity = 3, Price = 145, Category = "raw_input" }
            ],
            TestContentFactory.DefaultRestorationSites());
    }

    private static ContentRegistry BuildRoundTripRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed", BuyPrice = 10, SellPrice = 1 },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop", BuyPrice = 0, SellPrice = 30 },
                new ItemDefinition { Id = "wine", Name = "Wine", Type = "processed_good", BuyPrice = 0, SellPrice = 160 },
                new ItemDefinition { Id = "aged_wine", Name = "Aged Wine", Type = "processed_good", BuyPrice = 0, SellPrice = 260 }
            ],
            [
                new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 }
            ],
            [
                new MachineDefinition { Id = "fermenter", Name = "Fermenter", Type = "processor_machine" },
                new MachineDefinition { Id = "aging_barrel", Name = "Aging Barrel", Type = "processor_machine" }
            ],
            [
                new ProcessingRecipeDefinition { Id = "wine_to_aged_wine", MachineId = "aging_barrel", InputItemId = "wine", InputQuantity = 1, OutputItemId = "aged_wine", OutputQuantity = 1, ProcessingDays = 5 }
            ],
            [],
            TestContentFactory.DefaultRestorationSites());
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

    private static ContentRegistry BuildFreeSeedRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "free_seed", Name = "Free Seeds", Type = "seed", BuyPrice = 0, SellPrice = 0 },
                new ItemDefinition { Id = "free_crop", Name = "Free Crop", Type = "crop", BuyPrice = 0, SellPrice = 5 }
            ],
            [
                new CropDefinition { Id = "free_crop", SeedItemId = "free_seed", HarvestItemId = "free_crop", GrowthDays = 1 }
            ],
            [],
            [],
            [],
            TestContentFactory.DefaultRestorationSites());
    }

    private static ContentRegistry BuildSingleSeedRegistry(string seedId, string seedName, int buyPrice, string cropId, string cropName, int sellPrice)
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = seedId, Name = seedName, Type = "seed", BuyPrice = buyPrice, SellPrice = 0 },
                new ItemDefinition { Id = cropId, Name = cropName, Type = "crop", BuyPrice = 0, SellPrice = sellPrice }
            ],
            [
                new CropDefinition { Id = cropId, SeedItemId = seedId, HarvestItemId = cropId, GrowthDays = 2 }
            ],
            [],
            [],
            [],
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

    private static void UnlockAllBots(TurnipTestState state)
    {
        SetMoney(
            state,
            TurnipTestState.PlanterBotUnlockCost
            + TurnipTestState.WateringBotUnlockCost
            + TurnipTestState.HarvesterBotUnlockCost);
        state.UnlockPlanterBot();
        state.UnlockWateringBot();
        state.UnlockHarvesterBot();
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
}
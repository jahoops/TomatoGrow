using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class GameStateValidatorTests
{
    [Fact]
    public void ValidateCurrentState_FreshResetState_IsValid()
    {
        TurnipTestState state = CreateState();

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateCurrentState_AfterPlanting_IsValid()
    {
        TurnipTestState state = CreateState();
        state.PlantSelectedSeed();

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCurrentState_AfterWateringAndAdvanceDay_IsValid()
    {
        TurnipTestState state = CreateState();
        state.PlantSelectedSeed();
        state.WaterSelectedPlot();
        state.AdvanceDay();

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCurrentState_AfterHarvestAndSell_IsValid()
    {
        TurnipTestState state = CreateState();

        PlantAndGrowTurnip(state);
        state.HarvestCrop();
        state.SellSelectedCrop();

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCurrentState_AfterStorageDepositAndWithdraw_IsValid()
    {
        TurnipTestState state = CreateState();
        AddInventoryItem(state, "turnip", 2);

        state.DepositAllSellablesToStorage();
        state.WithdrawAllFromStorage();

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCurrentState_AfterMachineStartProcessAndCollect_IsValid()
    {
        TurnipTestState state = CreateState();
        AddInventoryItem(state, "grape", 3);

        state.StartFermenterFromInventory();
        while (!state.Fermenter.HasOutputReady)
            state.AdvanceDay();

        state.CollectFermenterOutput();

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCurrentState_AfterMarketPurchase_IsValid()
    {
        TurnipTestState state = CreateState();
        SetPrivateAutoProperty(state, nameof(TurnipTestState.Money), 500);

        state.BuyMarketOffer("buy_grapes");

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCurrentState_AfterCleanup_IsValid()
    {
        TurnipTestState state = CreateState();
        SetPrivateAutoProperty(state, nameof(TurnipTestState.Money), 500);
        state.ClearCleanupSite();

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCurrentState_AfterSaveLoadRoundTrip_IsValid()
    {
        TurnipTestState state = CreateState();
        state.PlantSelectedSeed();
        state.WaterSelectedPlot();
        state.AdvanceDay();
        AddInventoryItem(state, "grape", 3);
        state.StartFermenterFromInventory();

        SaveGameFileService service = new();
        string json = service.Serialize(state.CreateSaveGame());
        SaveGameState saveGame = service.Deserialize(json);
        TurnipTestState restoredState = CreateState();
        restoredState.ApplySaveGame(saveGame);

        GameStateValidationResult result = restoredState.ValidateCurrentState();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCurrentState_AfterAutoplayProgression_IsValid()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings
            {
                TargetMoney = 250,
                MaxDays = 365,
                MaxSteps = 5000,
                MaxNoActionSteps = 8,
                DesiredSeedCount = 8
            });

        controller.Enable();
        while (controller.IsEnabled)
            controller.RunStep();

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid);
        Assert.NotEqual(AutoPlayStopReason.InvalidState, controller.Stats.StopReason);
    }

    [Fact]
    public void ValidateCurrentState_UnusuallyMatureCrop_ReportsWarningOnly()
    {
        TurnipTestState state = CreateState();
        state.FarmGrid.GetPlot(0, 0).Restore("turnip", 1000, false);

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, warning => warning.Contains("unusually high DaysGrown", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateCurrentState_NegativeMoney_IsInvalid()
    {
        TurnipTestState state = CreateState();
        SetPrivateAutoProperty(state, nameof(TurnipTestState.Money), -5);

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "Money cannot be negative");
    }

    [Fact]
    public void ValidateCurrentState_NegativeCleanupStat_IsInvalid()
    {
        TurnipTestState state = CreateState();
        SetPrivateAutoProperty(state.Stats, nameof(GameStats.MoneySpentOnCleanup), -1);

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "MoneySpentOnCleanup");
    }

    [Fact]
    public void ValidateCurrentState_FinalExpansionWithoutCleanup_IsInvalid()
    {
        TurnipTestState state = CreateState();
        state.FarmGrid.Resize(5, 3);

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "requires Old Junk Pile to be cleared");
    }

    [Fact]
    public void ValidateCurrentState_NegativeInventoryCount_IsInvalid()
    {
        TurnipTestState state = CreateState();
        GetPrivateDictionary(state, "_inventory")["turnip"] = -1;

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "player inventory contains negative count");
    }

    [Fact]
    public void ValidateCurrentState_MissingInventoryItemId_IsInvalid()
    {
        TurnipTestState state = CreateState();
        GetPrivateDictionary(state, "_inventory")["ghost_item"] = 1;

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "player inventory references missing item id 'ghost_item'");
    }

    [Fact]
    public void ValidateCurrentState_MissingStorageItemId_IsInvalid()
    {
        TurnipTestState state = CreateState();
        GetPrivateDictionary(state, "_sharedStorage")["ghost_item"] = 1;

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "storage references missing item id 'ghost_item'");
    }

    [Fact]
    public void ValidateCurrentState_MissingPlantedCropId_IsInvalid()
    {
        TurnipTestState state = CreateState();
        state.FarmGrid.GetPlot(0, 0).Restore("ghost_crop", 1, false);

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "references missing crop id 'ghost_crop'");
    }

    [Fact]
    public void ValidateCurrentState_SelectedPlotOutsideGrid_IsInvalid()
    {
        TurnipTestState state = CreateState();
        SetPrivateAutoProperty(state, nameof(TurnipTestState.SelectedPlotX), 99);

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "Selected plot is outside the farm grid");
    }

    [Fact]
    public void ValidateCurrentState_SelectedSeedMissingItemId_IsInvalid()
    {
        TurnipTestState state = CreateState();
        SetPrivateAutoProperty(state, nameof(TurnipTestState.SelectedSeedItemId), "ghost_seed");

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "Selected seed references missing item id 'ghost_seed'");
    }

    [Fact]
    public void ValidateCurrentState_SelectedSeedWithNonSeedType_IsInvalid()
    {
        TurnipTestState state = CreateState();
        SetPrivateAutoProperty(state, nameof(TurnipTestState.SelectedSeedItemId), "turnip");

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "Selected seed item 'turnip' is not a valid starter item");
    }

    [Fact]
    public void ValidateCurrentState_MachineMissingMachineId_IsInvalid()
    {
        TurnipTestState state = CreateState();
        GetPrivateMachineList(state)[0] = new ProcessorMachineInstance("ghost_machine");

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "references missing machine id 'ghost_machine'");
    }

    [Fact]
    public void ValidateCurrentState_MachineMissingRecipeId_IsInvalid()
    {
        TurnipTestState state = CreateState();
        state.Fermenter.Restore("ghost_recipe", "grape", 3, "wine", 1, 1, false);

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "references missing recipe id 'ghost_recipe'");
    }

    [Fact]
    public void ValidateCurrentState_MachineRecipeMismatch_IsInvalid()
    {
        TurnipTestState state = CreateState();
        state.Fermenter.Restore("wine_to_aged_wine", "wine", 1, "aged_wine", 1, 1, false);

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "uses recipe 'wine_to_aged_wine' for machine 'aging_barrel'");
    }

    [Fact]
    public void ValidateCurrentState_MachineNegativeDaysRemaining_IsInvalid()
    {
        TurnipTestState state = CreateState();
        state.Fermenter.Restore("grape_to_wine", "grape", 3, "wine", 1, 1, false);
        SetPrivateAutoProperty(state.Fermenter, nameof(ProcessorMachineInstance.DaysRemaining), -1);

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "has negative DaysRemaining");
    }

    [Fact]
    public void ValidateCurrentState_OversizedActivityLog_IsInvalid()
    {
        TurnipTestState state = CreateState();
        List<ActivityLogEntry> activityLog = GetPrivateActivityLog(state);
        for (int index = 0; index < TurnipTestState.ActivityLogMaxEntries + 1; index++)
            activityLog.Add(new ActivityLogEntry(1, ActivityLogCategory.System, $"Entry {index}"));

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "Activity log exceeds max length");
    }

    [Fact]
    public void ValidateCurrentState_BlankActivityLogMessage_IsInvalid()
    {
        TurnipTestState state = CreateState();
        GetPrivateActivityLog(state).Add(new ActivityLogEntry(1, ActivityLogCategory.System, ""));

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "has blank message");
    }

    [Fact]
    public void ValidateCurrentState_ProcessedOutputWithMissingItemId_IsInvalid()
    {
        TurnipTestState state = CreateState();
        GetProcessedOutputs(state.Stats)["ghost_output"] = 1;

        GameStateValidationResult result = state.ValidateCurrentState();

        AssertInvalid(result, "Processed output total references missing item id 'ghost_output'");
    }

    [Fact]
    public void AutoPlay_InvalidStateInDebugBuild_StopsWithInvariantMessage()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry);
        controller.Enable();
        SetPrivateAutoProperty(state, nameof(TurnipTestState.Money), -1);

        string message = controller.RunStep();

        Assert.Equal("Autoplay stopped: invalid state - Money cannot be negative: -1.", message);
        Assert.False(controller.IsEnabled);
        Assert.Equal(AutoPlayStopReason.InvalidState, controller.Stats.StopReason);
        Assert.False(controller.LastValidationResult.IsValid);
        Assert.Contains(controller.LastValidationResult.Errors, error => error.Contains("Money cannot be negative", StringComparison.Ordinal));
    }

    private static void AssertInvalid(GameStateValidationResult result, string expectedMessageFragment)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains(expectedMessageFragment, StringComparison.Ordinal));
    }

    private static void PlantAndGrowTurnip(TurnipTestState state)
    {
        state.PlantSelectedSeed();
        for (int day = 0; day < 4; day++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }
    }

    private static TurnipTestState CreateState()
    {
        return new(BuildRegistry());
    }

    private static ContentRegistry BuildRegistry()
    {
        return new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));
    }

    private static void AddInventoryItem(TurnipTestState state, string itemId, int quantity)
    {
        Dictionary<string, int> inventory = GetPrivateDictionary(state, "_inventory");
        inventory[itemId] = state.GetItemCount(itemId) + quantity;
    }

    private static Dictionary<string, int> GetPrivateDictionary(TurnipTestState state, string fieldName)
    {
        return (Dictionary<string, int>)typeof(TurnipTestState)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(state)!;
    }

    private static List<ProcessorMachineInstance> GetPrivateMachineList(TurnipTestState state)
    {
        return (List<ProcessorMachineInstance>)typeof(TurnipTestState)
            .GetField("_processorMachines", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(state)!;
    }

    private static List<ActivityLogEntry> GetPrivateActivityLog(TurnipTestState state)
    {
        return (List<ActivityLogEntry>)typeof(TurnipTestState)
            .GetField("_activityLog", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(state)!;
    }

    private static Dictionary<string, int> GetProcessedOutputs(GameStats stats)
    {
        return (Dictionary<string, int>)typeof(GameStats)
            .GetField("_processedOutputsByItemId", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(stats)!;
    }

    private static void SetPrivateAutoProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        typeof(TTarget)
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }
}

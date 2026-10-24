using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed partial class SaveGameTests
{
    [Fact]
    public void SaveGameFileService_SerializesAndDeserializes_WithVersionField()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = CreateRoundTripState(registry);
        SaveGameFileService service = new();

        string json = service.Serialize(state.CreateSaveGame());
        SaveGameState saveGame = service.Deserialize(json);

        Assert.Contains("\"Version\": 1", json, StringComparison.Ordinal);
        Assert.Equal(SaveGameState.CurrentVersion, saveGame.Version);
        Assert.NotEmpty(saveGame.Plots);
        Assert.NotEmpty(saveGame.Machines);
    }

    [Fact]
    public void SaveGameFileService_Deserialize_AllowsMissingOptionalFields_AndIgnoresUnknownFields()
    {
        SaveGameFileService service = new();

        SaveGameState saveGame = service.Deserialize(
            """
            {
              "Version": 1,
              "DayNumber": 7,
              "Money": 345,
              "FarmWidth": 3,
              "FarmHeight": 3,
              "UnknownField": "ignored"
            }
            """);

        Assert.Equal(1, saveGame.Version);
        Assert.Equal(7, saveGame.DayNumber);
        Assert.Equal(345, saveGame.Money);
        Assert.Empty(saveGame.Plots);
        Assert.Empty(saveGame.Inventory);
        Assert.NotNull(saveGame.Bots);
        Assert.NotNull(saveGame.Stats);
        Assert.Empty(saveGame.ActivityLog);
    }

    [Fact]
    public void SaveGame_RoundTrips_CoreStatePlotsInventoryStorageBotsStatsMachinesAndActivityLog()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState originalState = CreateRoundTripState(registry);

        TurnipTestState loadedState = RoundTripState(originalState, registry);

        Assert.Equal(originalState.DayNumber, loadedState.DayNumber);
        Assert.Equal(originalState.Money, loadedState.Money);
        Assert.Equal(originalState.FarmGrid.Width, loadedState.FarmGrid.Width);
        Assert.Equal(originalState.FarmGrid.Height, loadedState.FarmGrid.Height);
        Assert.Equal(originalState.SelectedPlotX, loadedState.SelectedPlotX);
        Assert.Equal(originalState.SelectedPlotY, loadedState.SelectedPlotY);
        Assert.Equal(originalState.SelectedSeedItemId, loadedState.SelectedSeedItemId);
        Assert.Equal(originalState.GetItemCount("turnip_seed"), loadedState.GetItemCount("turnip_seed"));
        Assert.Equal(originalState.GetItemCount("grape_seed"), loadedState.GetItemCount("grape_seed"));
        Assert.Equal(originalState.GetItemCount("turnip"), loadedState.GetItemCount("turnip"));
        Assert.Equal(originalState.GetStorageItemCount("beer"), loadedState.GetStorageItemCount("beer"));
        Assert.Equal(originalState.IsPlanterBotUnlocked, loadedState.IsPlanterBotUnlocked);
        Assert.Equal(originalState.IsWateringBotUnlocked, loadedState.IsWateringBotUnlocked);
        Assert.Equal(originalState.IsHarvesterBotUnlocked, loadedState.IsHarvesterBotUnlocked);
        Assert.Equal(originalState.PlanterBot.AssignedSeedItemId, loadedState.PlanterBot.AssignedSeedItemId);
        Assert.Equal(originalState.WateringBot.Enabled, loadedState.WateringBot.Enabled);
        Assert.Equal(originalState.Fermenter.CurrentRecipeId, loadedState.Fermenter.CurrentRecipeId);
        Assert.Equal(originalState.Fermenter.HasOutputReady, loadedState.Fermenter.HasOutputReady);
        Assert.Equal(originalState.AgingBarrel.CurrentRecipeId, loadedState.AgingBarrel.CurrentRecipeId);
        Assert.Equal(originalState.AgingBarrel.DaysRemaining, loadedState.AgingBarrel.DaysRemaining);
        Assert.Equal(originalState.Stats.MachineRunsStarted, loadedState.Stats.MachineRunsStarted);
        Assert.Equal(originalState.Stats.MachineOutputsCollected, loadedState.Stats.MachineOutputsCollected);
        Assert.Equal(originalState.Stats.MachineValueAdded, loadedState.Stats.MachineValueAdded);
        Assert.Equal(originalState.Stats.MarketPurchases, loadedState.Stats.MarketPurchases);
        Assert.Equal(originalState.Stats.MarketMoneySpent, loadedState.Stats.MarketMoneySpent);
        Assert.Equal(originalState.Stats.GetProcessedOutputCount("wine"), loadedState.Stats.GetProcessedOutputCount("wine"));
        Assert.Equal(originalState.ActivityLog.Count, loadedState.ActivityLog.Count);
        Assert.Equal(originalState.ActivityLog.Select(entry => entry.Message), loadedState.ActivityLog.Select(entry => entry.Message));
        Assert.Equal(originalState.ActivityLog.Select(entry => entry.Category), loadedState.ActivityLog.Select(entry => entry.Category));

        FarmPlot originalWorkingPlot = originalState.FarmGrid.GetPlot(1, 1);
        FarmPlot loadedWorkingPlot = loadedState.FarmGrid.GetPlot(1, 1);
        Assert.NotNull(loadedWorkingPlot.PlantedCrop);
        Assert.Equal(originalWorkingPlot.PlantedCrop!.CropDefinitionId, loadedWorkingPlot.PlantedCrop!.CropDefinitionId);
        Assert.Equal(originalWorkingPlot.PlantedCrop.DaysGrown, loadedWorkingPlot.PlantedCrop.DaysGrown);
        Assert.Equal(originalWorkingPlot.IsWatered, loadedWorkingPlot.IsWatered);
    }

    [Fact]
    public void LoadedWorkingMachine_CanContinueProcessing_AfterDayAdvance()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "wine", 1);
        state.StartProcessorMachineFromInventory("aging_barrel");

        TurnipTestState loadedState = RoundTripState(state, registry);

        int daysRemainingBefore = loadedState.AgingBarrel.DaysRemaining;
        while (!loadedState.AgingBarrel.HasOutputReady && loadedState.AgingBarrel.DaysRemaining > 0)
            loadedState.AdvanceDay();

        Assert.True(daysRemainingBefore > 0);
        Assert.True(loadedState.AgingBarrel.HasOutputReady);
        Assert.Equal(0, loadedState.AgingBarrel.DaysRemaining);
    }

    [Fact]
    public void LoadedOutputReadyMachine_CanCollectOutput_AndLoadedStateCanContinueGameplay()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "hops", 3);
        state.StartProcessorMachineFromInventory("fermenter");
        state.AdvanceDay();
        state.AdvanceDay();

        TurnipTestState loadedState = RoundTripState(state, registry);
        int activityCountBefore = loadedState.ActivityLog.Count;
        int moneyBeforeSell = loadedState.Money;

        string collectMessage = loadedState.CollectProcessorMachineOutput("fermenter");
        Assert.Equal(1, loadedState.GetItemCount("beer"));

        string plantMessage = loadedState.PlantSeedAtPlot(0, 0, "turnip_seed");
        for (int day = 0; day < 4; day++)
        {
            loadedState.WaterPlot(0, 0);
            loadedState.AdvanceDay();
        }

        loadedState.MoveSelectedPlot(0, 0);
        string harvestMessage = loadedState.HarvestCrop();
        string sellMessage = loadedState.SellSelectedCrop();

        Assert.Equal("Collected 1 Beer from fermenter.", collectMessage);
        Assert.StartsWith("Planted turnip at 0,0.", plantMessage, StringComparison.Ordinal);
        Assert.StartsWith("Harvested Turnip", harvestMessage, StringComparison.Ordinal);
        Assert.StartsWith("Sold", sellMessage, StringComparison.Ordinal);
        Assert.True(loadedState.Money > moneyBeforeSell);
        Assert.True(loadedState.ActivityLog.Count > activityCountBefore);
        Assert.Equal(ActivityLogCategory.Sale, loadedState.ActivityLog[^1].Category);
    }

    [Fact]
    public void GameSaveController_SaveAndLoad_UseFilePath_AndResetUiPauseAndAutoplay()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = CreateRoundTripState(registry);
        DebugUiState uiState = new();
        AutoPlayTestController autoPlayController = new(state, registry);
        string savePath = Path.Combine(Path.GetTempPath(), $"tomatogrow-save-{Guid.NewGuid():N}.json");
        GameSaveController controller = new(state, uiState, autoPlayController, savePath);

        try
        {
            Assert.Equal(savePath, controller.Status.SavePath);

            string saveMessage = controller.SaveGame();
            Assert.Equal("Game saved.", saveMessage);
            Assert.True(File.Exists(savePath));
            Assert.True(controller.Status.LastSaveSucceeded);
            Assert.Equal("Game saved.", controller.Status.LastSaveMessage);
            Assert.NotNull(controller.Status.LastSaveTimestampUtc);

            uiState.ShowMarketScreen();
            uiState.TogglePause();
            autoPlayController.Enable();
            state.Reset();

            string loadMessage = controller.LoadGame();

            Assert.Equal("Game loaded.", loadMessage);
            Assert.Equal(GameScreen.Farm, uiState.CurrentScreen);
            Assert.False(uiState.IsPaused);
            Assert.False(autoPlayController.IsEnabled);
            Assert.Equal(4, state.DayNumber);
            Assert.True(state.IsWateringBotUnlocked);
            Assert.True(state.ActivityLog.Count > 0);
            Assert.True(controller.Status.LastLoadSucceeded);
            Assert.Equal("Game loaded.", controller.Status.LastLoadMessage);
            Assert.NotNull(controller.Status.LastLoadTimestampUtc);
        }
        finally
        {
            if (File.Exists(savePath))
                File.Delete(savePath);
        }
    }

    [Fact]
    public void GameSaveController_LoadWithoutFile_ReturnsNoSaveFileFound()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        GameSaveController controller = new(
            state,
            new DebugUiState(),
            new AutoPlayTestController(state, registry),
            Path.Combine(Path.GetTempPath(), $"tomatogrow-missing-{Guid.NewGuid():N}.json"));

        Assert.Equal("No save file found.", controller.LoadGame());
        Assert.False(controller.Status.LastLoadSucceeded);
        Assert.Equal("No save file found.", controller.Status.LastLoadMessage);
    }

    [Fact]
    public void InvalidSave_DoesNotPartiallyApplyState_AndReturnsClearLoadFailureMessage()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = CreateRoundTripState(registry);
        DebugUiState uiState = new();
        AutoPlayTestController autoPlayController = new(state, registry);
        string savePath = Path.Combine(Path.GetTempPath(), $"tomatogrow-invalid-save-{Guid.NewGuid():N}.json");
        GameSaveController controller = new(state, uiState, autoPlayController, savePath);
        SaveGameFileService service = new();

        try
        {
            SaveGameState invalidSave = state.CreateSaveGame();
            invalidSave.Inventory.Add(new ItemStackSaveState { ItemId = "ghost_grape", Quantity = 1 });
            invalidSave.DayNumber = 99;
            invalidSave.Money = 1;
            invalidSave.SelectedSeedItemId = "ghost_seed";
            service.SaveGameToFile(savePath, invalidSave);

            int originalDay = state.DayNumber;
            int originalMoney = state.Money;
            string originalSelectedSeed = state.SelectedSeedItemId;
            int originalActivityCount = state.ActivityLog.Count;

            LoadGameResult result = controller.LoadGameDetailed();

            Assert.False(result.Success);
            Assert.Contains("Load failed:", result.Message, StringComparison.Ordinal);
            Assert.Contains("Missing item id in player inventory: 'ghost_grape'.", result.Message, StringComparison.Ordinal);
            Assert.Contains("Missing item id in selected seed: 'ghost_seed'.", result.Message, StringComparison.Ordinal);
            Assert.False(controller.Status.LastLoadSucceeded);
            Assert.Equal(result.Message, controller.Status.LastLoadMessage);
            Assert.Equal(originalDay, state.DayNumber);
            Assert.Equal(originalMoney, state.Money);
            Assert.Equal(originalSelectedSeed, state.SelectedSeedItemId);
            Assert.Equal(originalActivityCount, state.ActivityLog.Count);
        }
        finally
        {
            if (File.Exists(savePath))
                File.Delete(savePath);
        }
    }

    private static TurnipTestState CreateRoundTripState(ContentRegistry registry)
    {
        TurnipTestState state = new(registry);
        SetMoney(state, 2000);
        SetSelectedSeed(state, "grape_seed");
        state.MoveSelectedPlot(1, 1);
        state.PlantSelectedSeed();
        state.WaterSelectedPlot();
        state.AdvanceDay();
        state.UnlockPlanterBot();
        state.UnlockWateringBot();
        state.UnlockHarvesterBot();
        state.ToggleWateringBotEnabled();
        AddInventoryItem(state, "turnip", 2);
        AddStorageItem(state, "beer", 1);
        state.BuyMarketOffer("buy_grapes");
        state.StartFermenterFromInventory();
        state.AdvanceDay();
        state.AdvanceDay();
        state.CollectFermenterOutput();
        AddInventoryItem(state, "wine", 1);
        state.StartProcessorMachineFromInventory("aging_barrel");
        state.MoveSelectedPlot(2, 1);
        SetSelectedSeed(state, "turnip_seed");
        return state;
    }

    private static TurnipTestState RoundTripState(TurnipTestState state, ContentRegistry registry)
    {
        SaveGameFileService service = new();
        string json = service.Serialize(state.CreateSaveGame());
        SaveGameState saveGame = service.Deserialize(json);
        TurnipTestState loadedState = new(registry);
        loadedState.ApplySaveGame(saveGame);
        return loadedState;
    }

    private static ContentRegistry BuildRegistry()
    {
        return new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));
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

    private static void SetMoney(TurnipTestState state, int amount)
    {
        typeof(TurnipTestState)
            .GetProperty("Money", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(state, amount);
    }

    private static void SetSelectedSeed(TurnipTestState state, string seedItemId)
    {
        int safety = 0;
        while (!string.Equals(state.SelectedSeedItemId, seedItemId, StringComparison.OrdinalIgnoreCase) && safety < 20)
        {
            state.SelectNextSeed();
            safety++;
        }

        Assert.Equal(seedItemId, state.SelectedSeedItemId);
    }
}

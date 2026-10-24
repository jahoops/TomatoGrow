using System;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed partial class SaveGameTests
{
    [Fact]
    public void ValidSave_StillPassesValidation_AndLoadsNormally()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = CreateRoundTripState(registry);

        SaveGameState saveGame = state.CreateSaveGame();
        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);
        TurnipTestState loadedState = new(registry);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);

        loadedState.ApplySaveGame(saveGame);

        Assert.Equal(state.DayNumber, loadedState.DayNumber);
        Assert.Equal(state.Money, loadedState.Money);
    }

    [Fact]
    public void MissingPlayerInventoryItemId_FailsValidation()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.Inventory.Add(new ItemStackSaveState { ItemId = "ghost_grape", Quantity = 2 });

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Missing item id in player inventory: 'ghost_grape'.", validation.Errors);
    }

    [Fact]
    public void MissingStorageItemId_FailsValidation()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.SharedStorage.Add(new ItemStackSaveState { ItemId = "ghost_wine", Quantity = 1 });

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Missing item id in storage: 'ghost_wine'.", validation.Errors);
    }

    [Fact]
    public void MissingPlantedCropId_FailsValidation()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.Plots.Add(new FarmPlotSaveState { X = 2, Y = 1, CropDefinitionId = "ghost_crop", DaysGrown = 2, IsWatered = true });

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Missing crop id in farm plot (2,1): 'ghost_crop'.", validation.Errors);
    }

    [Fact]
    public void MissingSelectedSeedId_FailsValidation()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.SelectedSeedItemId = "ghost_seed";

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Missing item id in selected seed: 'ghost_seed'.", validation.Errors);
    }

    [Fact]
    public void MissingMachineId_FailsValidation()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.Machines.Add(new ProcessorMachineSaveState { MachineId = "ghost_machine" });

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Missing machine id: 'ghost_machine'.", validation.Errors);
    }

    [Fact]
    public void MissingCurrentRecipeId_FailsValidation()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.Machines[0] = new ProcessorMachineSaveState
        {
            MachineId = "fermenter",
            CurrentRecipeId = "ghost_recipe",
            InputItemId = "grape",
            InputQuantity = 3,
            OutputItemId = "wine",
            OutputQuantity = 1,
            DaysRemaining = 1,
            HasOutputReady = false
        };

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Missing processing recipe id: 'ghost_recipe'.", validation.Errors);
    }

    [Fact]
    public void RecipeMachineMismatch_FailsValidation()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.Machines[0] = new ProcessorMachineSaveState
        {
            MachineId = "fermenter",
            CurrentRecipeId = "wine_to_aged_wine",
            InputItemId = "wine",
            InputQuantity = 1,
            OutputItemId = "aged_wine",
            OutputQuantity = 1,
            DaysRemaining = 1,
            HasOutputReady = false
        };

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Machine 'fermenter' references recipe 'wine_to_aged_wine' for machine 'aging_barrel'.", validation.Errors);
    }

    [Fact]
    public void MissingProcessedOutputItemId_FailsValidation()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.Stats.ProcessedOutputs.Add(new ItemStackSaveState { ItemId = "ghost_beer", Quantity = 4 });

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Missing processed output item id: 'ghost_beer'.", validation.Errors);
    }

    [Fact]
    public void FinalExpansionWithoutCleanup_FailsValidation()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.FarmWidth = 5;
        saveGame.CleanupSite = new CleanupSiteSaveState { Id = TurnipTestState.OldJunkPileId, IsCleared = false };

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Farm width 5 requires Old Junk Pile to be cleared.", validation.Errors);
    }

    [Fact]
    public void NegativeCleanupStats_FailValidation()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.Stats.MoneySpentOnCleanup = -1;

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("MoneySpentOnCleanup cannot be negative: -1.", validation.Errors);
    }

    [Fact]
    public void BotActivityReplay_IsNotSavedOrLoaded()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveGameFileService fileService = new();
        SetMoney(state, TurnipTestState.PlanterBotUnlockCost);
        state.UnlockPlanterBot();
        state.RunPlanterBot();

        Assert.NotNull(state.PendingBotActivityReplay);

        SaveGameState saveGame = state.CreateSaveGame();
        string json = fileService.Serialize(saveGame);
        TurnipTestState loadedState = new(registry);

        Assert.DoesNotContain("PendingBotActivityReplay", json, StringComparison.Ordinal);
        Assert.DoesNotContain("BotActivity", json, StringComparison.Ordinal);

        loadedState.ApplySaveGame(saveGame);

        Assert.Null(loadedState.PendingBotActivityReplay);
    }
}

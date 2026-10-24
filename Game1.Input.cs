using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public partial class Game1
{
    protected override void Update(GameTime gameTime)
    {
        _debugUiState.AdvanceBotActivityPlayback(gameTime.ElapsedGameTime, _autoPlayController.IsEnabled);

        KeyboardState keyboard = Keyboard.GetState();
        bool isShiftDown = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        if (TryHandleLoadConfirmationInput(keyboard))
        {
            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        if (IsNewKeyPress(keyboard, Keys.Space))
            TogglePause();

        if (IsNewKeyPress(keyboard, Keys.Escape))
        {
            if (_debugUiState.HandleEscape())
                Exit();
            else
                LogAction("Returned to Farm screen.");
        }

        if (IsNewKeyPress(keyboard, Keys.F1))
            ToggleDebugDetails();

        if (IsNewKeyPress(keyboard, Keys.F2))
            ShowStatsScreen();

        if (IsNewKeyPress(keyboard, Keys.F3))
            ShowMachinesScreen();

        if (IsNewKeyPress(keyboard, Keys.F4))
            ShowHelpScreen();

        if (IsNewKeyPress(keyboard, Keys.I))
            ShowInventoryScreen();

        if (IsNewKeyPress(keyboard, Keys.F8))
            ShowStorageScreen();

        if (IsNewKeyPress(keyboard, Keys.F9))
        {
            if (isShiftDown)
                ShowProductionReportScreen();
            else
                ShowComputerScreen();
        }

        if (IsNewKeyPress(keyboard, Keys.F10))
        {
            if (isShiftDown)
                ShowProgressionReportScreen();
            else
                ShowMarketScreen();
        }

        if (IsNewKeyPress(keyboard, Keys.F11))
            ShowActivityLogScreen();

        if (IsNewKeyPress(keyboard, Keys.F12))
        {
            if (isShiftDown)
                RequestLoadConfirmation();
            else
                SaveGame();
        }

        if (IsNewKeyPress(keyboard, Keys.F5))
            ToggleAutoPlay();

        if (IsNewKeyPress(keyboard, Keys.F6))
            RunSingleAutoPlayStep();

        if (IsNewKeyPress(keyboard, Keys.F7))
            ResetAutoPlayCounters();

        TryHandleMarketPurchaseHotkeys(keyboard);

        if (IsNewKeyPress(keyboard, Keys.Tab))
            TryRunFarmAction(SelectNextSeed);

        if (IsNewKeyPress(keyboard, Keys.Left))
            TryRunFarmAction(() => MovePlotCursor(-1, 0));

        if (IsNewKeyPress(keyboard, Keys.Right))
            TryRunFarmAction(() => MovePlotCursor(1, 0));

        if (IsNewKeyPress(keyboard, Keys.Up))
        {
            if (_debugUiState.CurrentScreen == GameScreen.Machines)
                TryRunMachineAction(SelectPreviousMachineRecipe);
            else
                TryRunFarmAction(() => MovePlotCursor(0, -1));
        }

        if (IsNewKeyPress(keyboard, Keys.Down))
        {
            if (_debugUiState.CurrentScreen == GameScreen.Machines)
                TryRunMachineAction(SelectNextMachineRecipe);
            else
                TryRunFarmAction(() => MovePlotCursor(0, 1));
        }

        if (IsNewKeyPress(keyboard, Keys.P))
            TryRunFarmGameplayAction(PlantSelectedSeed);

        if (IsNewKeyPress(keyboard, Keys.E))
            TryRunFarmGameplayAction(ExpandFarm);

        if (IsNewKeyPress(keyboard, Keys.J))
            TryRunFarmGameplayAction(ClearCleanupSite);

        if (IsNewKeyPress(keyboard, Keys.Y))
            TryRunFarmGameplayAction(() => BuySelectedSeed(isShiftDown));

        if (IsNewKeyPress(keyboard, Keys.D1))
            TryRunFarmGameplayAction(UnlockPlanterBot);

        if (IsNewKeyPress(keyboard, Keys.D2))
            TryRunFarmGameplayAction(UnlockWateringBot);

        if (IsNewKeyPress(keyboard, Keys.D3))
            TryRunFarmGameplayAction(UnlockHarvesterBot);

        if (IsNewKeyPress(keyboard, Keys.W))
            TryRunFarmGameplayAction(WaterSelectedPlot);

        if (IsNewKeyPress(keyboard, Keys.B))
            TryRunFarmGameplayAction(RunWateringBotNow);

        if (IsNewKeyPress(keyboard, Keys.PageUp))
            TryRunMachineAction(SelectPreviousProcessorMachine);

        if (IsNewKeyPress(keyboard, Keys.PageDown))
            TryRunMachineAction(SelectNextProcessorMachine);

        if (IsNewKeyPress(keyboard, Keys.M))
            TryRunMachineGameplayAction(StartSelectedProcessorMachine);

        if (IsNewKeyPress(keyboard, Keys.O))
            TryRunMachineGameplayAction(CollectSelectedProcessorMachineOutput);

        if (IsNewKeyPress(keyboard, Keys.V))
            TryRunFarmGameplayAction(RunPlanterBotNow);

        if (IsNewKeyPress(keyboard, Keys.X))
            TryRunFarmGameplayAction(RunHarvesterBotNow);

        if (IsNewKeyPress(keyboard, Keys.G))
            TryRunFarmGameplayAction(RunFarmCycleNow);

        if (IsNewKeyPress(keyboard, Keys.T))
            TryRunFarmGameplayAction(ToggleWateringBot);

        if (IsNewKeyPress(keyboard, Keys.N))
            TryRunFarmGameplayAction(AdvanceDay);

        if (IsNewKeyPress(keyboard, Keys.H))
            TryRunFarmGameplayAction(HarvestSelectedPlot);

        if (IsNewKeyPress(keyboard, Keys.S))
        {
            if (_debugUiState.CurrentScreen == GameScreen.Storage)
                TryRunStorageGameplayAction(SellSelectedCrop);
            else
                TryRunFarmGameplayAction(SellSelectedCrop);
        }

        if (IsNewKeyPress(keyboard, Keys.C))
            TryRunStorageGameplayAction(DepositSellablesToStorage);

        if (IsNewKeyPress(keyboard, Keys.U))
            TryRunStorageGameplayAction(WithdrawAllFromStorage);

        if (IsNewKeyPress(keyboard, Keys.R))
        {
            if (_debugUiState.CurrentScreen == GameScreen.ProgressionReport)
                RunProgressionSimulationReport();
            else
                ResetGame();
        }

        UpdateAutoPlay(gameTime);

        _previousKeyboard = keyboard;

        base.Update(gameTime);
    }

    private bool IsNewKeyPress(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    }

    private void MovePlotCursor(int xOffset, int yOffset)
    {
        LogAction(_turnipTestState.MoveSelectedPlot(xOffset, yOffset));
    }

    private void AdvanceDay()
    {
        LogGameplayAction(_turnipTestState.AdvanceDay());
    }

    private void ResetGame()
    {
        _autoPlayController.Disable(AutoPlayStopReason.None, string.Empty);
        _autoPlayController.ResetStats();
        _autoPlayElapsedSeconds = 0;
        _turnipTestState.Reset();
        _debugUiState.Reset();
        LogAction("Reset Turnip Test state.");
    }

    private void TogglePause()
    {
        _debugUiState.TogglePause();
        LogAction(_debugUiState.IsPaused ? "Game paused." : "Game resumed.");
    }

    private void ToggleDebugDetails()
    {
        _debugUiState.ToggleDebugDetails();
        LogAction($"Debug details {(_debugUiState.ShowDebugDetails ? "ON" : "OFF")}.");
    }

    private void ShowStatsScreen()
    {
        _debugUiState.ShowStatsScreen();
        LogAction("Stats screen ON.");
    }

    private void ShowInventoryScreen()
    {
        _debugUiState.ShowInventoryScreen();
        LogAction("Inventory screen ON.");
    }

    private void ShowMachinesScreen()
    {
        _debugUiState.ShowMachinesScreen();
        LogAction("Machines screen ON.");
    }

    private void ShowStorageScreen()
    {
        _debugUiState.ShowStorageScreen();
        LogAction("Storage screen ON.");
    }

    private void ShowComputerScreen()
    {
        _debugUiState.ShowComputerScreen();
        LogAction("Computer screen ON.");
    }

    private void ShowProductionReportScreen()
    {
        _debugUiState.ShowProductionReportScreen();
        LogAction("Production Report screen ON.");
    }

    private void ShowProgressionReportScreen()
    {
        _debugUiState.ShowProgressionReportScreen();
        LogAction("Progression Report screen ON. Press R to run simulation.");
    }

    private void ShowActivityLogScreen()
    {
        _debugUiState.ShowActivityLogScreen();
        LogAction("Activity Log screen ON.");
    }

    private void RunProgressionSimulationReport()
    {
        ProgressionSimulationSettings balanceDefaults = _turnipTestState.BalanceConfig.CreateProgressionSimulationSettings();
        ProgressionSimulationSettings settings = new()
        {
            EnableMarketOpportunity = balanceDefaults.EnableMarketOpportunity,
            MarketOpportunityId = balanceDefaults.MarketOpportunityId,
            MarketOpportunityItemId = balanceDefaults.MarketOpportunityItemId,
            MarketOpportunityDisplayName = balanceDefaults.MarketOpportunityDisplayName,
            MarketOpportunityMessage = balanceDefaults.MarketOpportunityMessage,
            MarketOpportunityDurationDays = balanceDefaults.MarketOpportunityDurationDays,
            MarketOpportunitySellPriceMultiplier = balanceDefaults.MarketOpportunitySellPriceMultiplier,
            MarketOpportunity = balanceDefaults.MarketOpportunity,
            TargetMoney = _autoPlayController.Settings.TargetMoney,
            MaxDays = _autoPlayController.Settings.MaxDays,
            MaxSteps = Math.Max(_turnipTestState.BalanceConfig.ProgressionSimulationDefaultMaxSteps, _autoPlayController.Settings.MaxSteps),
            MaxNoActionSteps = _autoPlayController.Settings.MaxNoActionSteps,
            DesiredSeedCount = _autoPlayController.Settings.DesiredSeedCount
        };

        _progressionSimulationResult = _progressionSimulationRunner.Run(settings);
        LogAction($"Progression report updated: {_progressionSimulationResult.StopReason} day {_progressionSimulationResult.DaysSimulated} money ${_progressionSimulationResult.FinalMoney}.");
    }

    private void ShowMarketScreen()
    {
        _debugUiState.ShowMarketScreen();
        LogAction("Supply Market screen ON.");
    }

    private void ShowHelpScreen()
    {
        _debugUiState.ShowHelpScreen();
        LogAction("Help screen ON.");
    }

    private void ToggleAutoPlay()
    {
        _autoPlayElapsedSeconds = 0;
        bool isEnabled = _autoPlayController.ToggleEnabled();
        LogAction(isEnabled ? "Auto Play ON." : "Auto Play OFF.");
    }

    private void SaveGame()
    {
        LogAction(_gameSaveController.SaveGame());
    }

    private void RequestLoadConfirmation()
    {
        _debugUiState.RequestLoadConfirmation();
        LogAction(DebugUiTextFormatter.GetLoadConfirmationPrompt(_gameSaveController.Status.SavePath));
    }

    private void LoadGame()
    {
        _autoPlayElapsedSeconds = 0;
        LogAction(_gameSaveController.LoadGame());
    }

    private void ConfirmLoad()
    {
        _debugUiState.CancelLoadConfirmation();
        _autoPlayElapsedSeconds = 0;
        LoadGameResult result = _gameSaveController.LoadGameDetailed();
        if (!result.Success)
            _debugUiState.ReturnToFarm();

        LogAction(result.Message);
    }

    private void CancelLoadConfirmation()
    {
        _debugUiState.CancelLoadConfirmation();
        LogAction(DebugUiState.LoadConfirmationCanceledMessage);
    }

    private void RunSingleAutoPlayStep()
    {
        if (TryHandlePausedGameplayBlock())
            return;

        _autoPlayElapsedSeconds = 0;
        LogGameplayAction(_autoPlayController.RunStep(), isAutoPlayStep: true);
    }

    private void ResetAutoPlayCounters()
    {
        _autoPlayController.ResetStats();
        _autoPlayElapsedSeconds = 0;
        LogAction("Auto Play counters reset.");
    }

    private void UpdateAutoPlay(GameTime gameTime)
    {
        if (!_autoPlayController.IsEnabled || _debugUiState.IsPaused || _debugUiState.IsPlayingBotActivity)
            return;

        _autoPlayElapsedSeconds += gameTime.ElapsedGameTime.TotalSeconds;
        if (_autoPlayElapsedSeconds < _autoPlayController.Settings.StepIntervalSeconds)
            return;

        _autoPlayElapsedSeconds = 0;
        LogGameplayAction(_autoPlayController.RunStep(), isAutoPlayStep: true);
    }

    private void SelectNextSeed()
    {
        LogAction(_turnipTestState.SelectNextSeed());
    }

    private void PlantSelectedSeed()
    {
        LogAction(_turnipTestState.StartSelectedTileProducer());
    }

    private void ExpandFarm()
    {
        LogAction(_turnipTestState.ExpandFarm());
    }

    private void ClearCleanupSite()
    {
        LogAction(_turnipTestState.ClearCleanupSite());
    }

    private void BuySelectedSeed(bool buyAsManyAsPossible)
    {
        LogAction(_turnipTestState.BuySelectedSeed(buyAsManyAsPossible));
    }

    private void UnlockPlanterBot()
    {
        LogAction(_turnipTestState.UnlockPlanterBot());
    }

    private void UnlockWateringBot()
    {
        LogAction(_turnipTestState.UnlockWateringBot());
    }

    private void UnlockHarvesterBot()
    {
        LogAction(_turnipTestState.UnlockHarvesterBot());
    }

    private void HarvestSelectedPlot()
    {
        LogAction(_turnipTestState.CollectSelectedTile());
    }

    private void WaterSelectedPlot()
    {
        LogAction(_turnipTestState.MaintainSelectedTile());
    }

    private void RunWateringBotNow()
    {
        LogGameplayAction(_turnipTestState.RunWateringBot());
    }

    private void StartSelectedProcessorMachine()
    {
        string selectedMachineId = _turnipTestState.SelectedProcessorMachine.MachineId;
        string selectedRecipeId = GetSelectedMachineRecipeId(selectedMachineId);
        LogAction(_turnipTestState.StartSelectedProcessorMachineFromInventory(selectedRecipeId, requireSpecificRecipe: true));
    }

    private void CollectSelectedProcessorMachineOutput()
    {
        LogAction(_turnipTestState.CollectSelectedProcessorMachineOutput());
    }

    private void SelectNextProcessorMachine()
    {
        LogAction(_turnipTestState.SelectNextProcessorMachine());
    }

    private void SelectPreviousProcessorMachine()
    {
        LogAction(_turnipTestState.SelectPreviousProcessorMachine());
    }

    private void SelectNextMachineRecipe()
    {
        string machineId = _turnipTestState.SelectedProcessorMachine.MachineId;
        IReadOnlyList<ProcessingRecipeDefinition> recipes = _contentRegistry.GetProcessingRecipesForMachine(machineId);
        if (recipes.Count == 0)
        {
            LogAction("No recipes loaded for selected machine.");
            return;
        }

        int recipeIndex = _debugUiState.SelectNextRecipe(machineId, recipes.Count);
        LogAction($"Selected recipe: {recipes[recipeIndex].InputQuantity} {_contentRegistry.GetItem(recipes[recipeIndex].InputItemId).Name} -> {recipes[recipeIndex].OutputQuantity} {_contentRegistry.GetItem(recipes[recipeIndex].OutputItemId).Name}");
    }

    private void SelectPreviousMachineRecipe()
    {
        string machineId = _turnipTestState.SelectedProcessorMachine.MachineId;
        IReadOnlyList<ProcessingRecipeDefinition> recipes = _contentRegistry.GetProcessingRecipesForMachine(machineId);
        if (recipes.Count == 0)
        {
            LogAction("No recipes loaded for selected machine.");
            return;
        }

        int recipeIndex = _debugUiState.SelectPreviousRecipe(machineId, recipes.Count);
        LogAction($"Selected recipe: {recipes[recipeIndex].InputQuantity} {_contentRegistry.GetItem(recipes[recipeIndex].InputItemId).Name} -> {recipes[recipeIndex].OutputQuantity} {_contentRegistry.GetItem(recipes[recipeIndex].OutputItemId).Name}");
    }

    private string GetSelectedMachineRecipeId(string machineId)
    {
        IReadOnlyList<ProcessingRecipeDefinition> recipes = _contentRegistry.GetProcessingRecipesForMachine(machineId);
        if (recipes.Count == 0)
            return null;

        int recipeIndex = _debugUiState.GetSelectedRecipeIndex(machineId, recipes.Count);
        return recipes[recipeIndex].Id;
    }

    private void ToggleWateringBot()
    {
        LogAction(_turnipTestState.ToggleWateringBotEnabled());
    }

    private void RunPlanterBotNow()
    {
        LogGameplayAction(_turnipTestState.RunPlanterBot());
    }

    private void RunHarvesterBotNow()
    {
        LogGameplayAction(_turnipTestState.RunHarvesterBot());
    }

    private void RunFarmCycleNow()
    {
        LogGameplayAction(_turnipTestState.RunFarmCycle());
    }

    private void SellSelectedCrop()
    {
        LogAction(_turnipTestState.SellSelectedCrop());
    }

    private void DepositSellablesToStorage()
    {
        LogAction(_turnipTestState.DepositAllSellablesToStorage());
    }

    private void WithdrawAllFromStorage()
    {
        LogAction(_turnipTestState.WithdrawAllFromStorage());
    }

    private void TryHandleMarketPurchaseHotkeys(KeyboardState keyboard)
    {
        if (_debugUiState.CurrentScreen != GameScreen.Market)
            return;

        Keys[] purchaseKeys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9];
        for (int index = 0; index < purchaseKeys.Length; index++)
        {
            if (!IsNewKeyPress(keyboard, purchaseKeys[index]))
                continue;

            if (index >= _turnipTestState.MarketOffers.Count)
                return;

            TryRunMarketGameplayAction(() => BuyMarketOffer(index));
            return;
        }
    }

    private void BuyMarketOffer(int offerIndex)
    {
        LogAction(_turnipTestState.BuyMarketOffer(_turnipTestState.MarketOffers[offerIndex].Id));
    }

    private void LogAction(string message)
    {
        _lastActionMessage = message;
        Console.WriteLine(message);
    }

    private void LogGameplayAction(string message, bool isAutoPlayStep = false)
    {
        LogAction(message);
        BotActivityReplay replay = _turnipTestState.ConsumePendingBotActivityReplay();
        if (replay?.HasSteps == true)
            _debugUiState.StartBotActivityReplay(replay, isAutoPlayStep);
    }

    private void TryRunFarmAction(Action action)
    {
        _debugUiState.TryRunFarmAction(action);
    }

    private void TryRunFarmGameplayAction(Action action)
    {
        if (TryHandlePausedGameplayBlock())
            return;

        _debugUiState.TryRunFarmAction(action);
    }

    private void TryRunMachineAction(Action action)
    {
        _debugUiState.TryRunMachineAction(action);
    }

    private void TryRunMachineGameplayAction(Action action)
    {
        if (TryHandlePausedGameplayBlock())
            return;

        _debugUiState.TryRunMachineAction(action);
    }

    private void TryRunStorageAction(Action action)
    {
        _debugUiState.TryRunStorageAction(action);
    }

    private void TryRunMarketGameplayAction(Action action)
    {
        if (TryHandlePausedGameplayBlock())
            return;

        _debugUiState.TryRunMarketAction(action);
    }

    private void TryRunStorageGameplayAction(Action action)
    {
        if (TryHandlePausedGameplayBlock())
            return;

        _debugUiState.TryRunStorageAction(action);
    }

    private bool TryHandlePausedGameplayBlock()
    {
        if (!_debugUiState.IsPaused)
            return false;

        LogAction(DebugUiState.PausedBlockedActionMessage);
        return true;
    }

    private bool TryHandleLoadConfirmationInput(KeyboardState keyboard)
    {
        if (!_debugUiState.IsLoadConfirmationPending)
            return false;

        if (IsNewKeyPress(keyboard, Keys.Y))
        {
            ConfirmLoad();
            return true;
        }

        if (IsNewKeyPress(keyboard, Keys.N) || IsNewKeyPress(keyboard, Keys.Escape))
        {
            CancelLoadConfirmation();
            return true;
        }

        if (HasMeaningfulNewKeyPress(keyboard))
            LogAction(DebugUiTextFormatter.GetLoadConfirmationPrompt(_gameSaveController.Status.SavePath));

        return true;
    }

    private bool HasMeaningfulNewKeyPress(KeyboardState keyboard)
    {
        return keyboard.GetPressedKeys().Any(key =>
            key is not Keys.LeftShift
                and not Keys.RightShift
                && !_previousKeyboard.IsKeyDown(key));
    }
}

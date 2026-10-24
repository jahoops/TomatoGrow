using System;
using System.Collections.Generic;

namespace TomatoGrow;

public enum GameScreen
{
    Farm,
    Inventory,
    Storage,
    Market,
    Computer,
    ProgressionReport,
    ProductionReport,
    ActivityLog,
    Machines,
    Stats,
    Help
}

public sealed class DebugUiState
{
    private readonly Dictionary<string, int> _selectedRecipeIndexByMachineId = new(StringComparer.OrdinalIgnoreCase);
    private readonly BotActivityPlaybackState _botActivityPlayback = new();

    public const string PausedBlockedActionMessage = "Game is paused. Press Space to resume.";
    public const string LoadConfirmationCanceledMessage = "Load canceled.";

    public bool ShowDebugDetails { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsLoadConfirmationPending { get; private set; }
    public GameScreen CurrentScreen { get; private set; } = GameScreen.Farm;
    public bool ShowsFarmVisuals => CurrentScreen == GameScreen.Farm;
    public bool AllowsFarmActions => CurrentScreen == GameScreen.Farm;
    public bool AllowsMachineActions => CurrentScreen is GameScreen.Farm or GameScreen.Machines;
    public bool AllowsStorageActions => CurrentScreen is GameScreen.Farm or GameScreen.Storage;
    public bool AllowsMarketActions => CurrentScreen == GameScreen.Market;
    public bool IsPlayingBotActivity => _botActivityPlayback.IsPlayingBotActivity;
    public BotRole? ActiveBotRole => _botActivityPlayback.ActiveBotRole;
    public int ActiveTileX => _botActivityPlayback.ActiveTileX;
    public int ActiveTileY => _botActivityPlayback.ActiveTileY;
    public int ActivityStepIndex => _botActivityPlayback.ActivityStepIndex;
    public double StepTimerSeconds => _botActivityPlayback.StepTimerSeconds;
    public string ActiveBotActivityStatusLine => _botActivityPlayback.ActiveStatusLine;

    public void ToggleDebugDetails()
    {
        ShowDebugDetails = !ShowDebugDetails;
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    public void ShowInventoryScreen()
    {
        CurrentScreen = GameScreen.Inventory;
    }

    public void ShowMachinesScreen()
    {
        CurrentScreen = GameScreen.Machines;
    }

    public void ShowStorageScreen()
    {
        CurrentScreen = GameScreen.Storage;
    }

    public void ShowMarketScreen()
    {
        CurrentScreen = GameScreen.Market;
    }

    public void ShowComputerScreen()
    {
        CurrentScreen = GameScreen.Computer;
    }

    public void ShowProgressionReportScreen()
    {
        CurrentScreen = GameScreen.ProgressionReport;
    }

    public void ShowProductionReportScreen()
    {
        CurrentScreen = GameScreen.ProductionReport;
    }

    public void ShowActivityLogScreen()
    {
        CurrentScreen = GameScreen.ActivityLog;
    }

    public void ShowStatsScreen()
    {
        CurrentScreen = GameScreen.Stats;
    }

    public void ShowHelpScreen()
    {
        CurrentScreen = GameScreen.Help;
    }

    public void ReturnToFarm()
    {
        CurrentScreen = GameScreen.Farm;
    }

    public bool HandleEscape()
    {
        if (IsLoadConfirmationPending)
        {
            CancelLoadConfirmation();
            return false;
        }

        if (CurrentScreen == GameScreen.Farm)
            return true;

        ReturnToFarm();
        return false;
    }

    public bool TryRunFarmAction(System.Action action)
    {
        if (IsLoadConfirmationPending || !AllowsFarmActions)
            return false;

        action();
        return true;
    }

    public bool TryRunMachineAction(System.Action action)
    {
        if (IsLoadConfirmationPending || !AllowsMachineActions)
            return false;

        action();
        return true;
    }

    public bool TryRunStorageAction(System.Action action)
    {
        if (IsLoadConfirmationPending || !AllowsStorageActions)
            return false;

        action();
        return true;
    }

    public bool TryRunUnpausedAction(System.Action action)
    {
        if (IsLoadConfirmationPending || IsPaused)
            return false;

        action();
        return true;
    }

    public bool TryRunMarketAction(System.Action action)
    {
        if (IsLoadConfirmationPending || !AllowsMarketActions)
            return false;

        action();
        return true;
    }

    public void RequestLoadConfirmation()
    {
        IsLoadConfirmationPending = true;
    }

    public void CancelLoadConfirmation()
    {
        IsLoadConfirmationPending = false;
    }

    public void StartBotActivityReplay(BotActivityReplay replay, bool isFastMode)
    {
        _botActivityPlayback.Start(replay, isFastMode);
    }

    public void AdvanceBotActivityPlayback(TimeSpan elapsedTime, bool preferFastMode)
    {
        _botActivityPlayback.Advance(elapsedTime, preferFastMode);
    }

    public bool TryGetBotActivityTileOverride(int tileX, int tileY, out FarmPlotVisualSnapshot snapshot)
    {
        return _botActivityPlayback.TryGetTileVisualOverride(tileX, tileY, out snapshot);
    }

    public int GetSelectedRecipeIndex(string machineId, int recipeCount)
    {
        if (string.IsNullOrWhiteSpace(machineId) || recipeCount <= 0)
            return 0;

        return _selectedRecipeIndexByMachineId.TryGetValue(machineId, out int selectedIndex)
            ? Math.Clamp(selectedIndex, 0, recipeCount - 1)
            : 0;
    }

    public int SelectNextRecipe(string machineId, int recipeCount)
    {
        if (string.IsNullOrWhiteSpace(machineId) || recipeCount <= 0)
            return 0;

        int nextIndex = (GetSelectedRecipeIndex(machineId, recipeCount) + 1) % recipeCount;
        _selectedRecipeIndexByMachineId[machineId] = nextIndex;
        return nextIndex;
    }

    public int SelectPreviousRecipe(string machineId, int recipeCount)
    {
        if (string.IsNullOrWhiteSpace(machineId) || recipeCount <= 0)
            return 0;

        int nextIndex = (GetSelectedRecipeIndex(machineId, recipeCount) - 1 + recipeCount) % recipeCount;
        _selectedRecipeIndexByMachineId[machineId] = nextIndex;
        return nextIndex;
    }

    public void Reset()
    {
        ShowDebugDetails = false;
        IsPaused = false;
        IsLoadConfirmationPending = false;
        CurrentScreen = GameScreen.Farm;
        _selectedRecipeIndexByMachineId.Clear();
        _botActivityPlayback.Clear();
    }
}
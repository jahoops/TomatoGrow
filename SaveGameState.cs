using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TomatoGrow;

public sealed class SaveGameState
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public int DayNumber { get; set; } = 1;
    public int Money { get; set; }
    public int FarmWidth { get; set; } = TurnipTestState.InitialFarmWidth;
    public int FarmHeight { get; set; } = TurnipTestState.InitialFarmHeight;
    public int SelectedPlotX { get; set; }
    public int SelectedPlotY { get; set; }
    public string SelectedSeedItemId { get; set; }
    public int SelectedProcessorMachineIndex { get; set; }
    public List<FarmPlotSaveState> Plots { get; set; } = [];
    public List<ItemStackSaveState> Inventory { get; set; } = [];
    public List<ItemStackSaveState> SharedStorage { get; set; } = [];
    public BotSaveState Bots { get; set; } = new();
    public List<ProcessorMachineSaveState> Machines { get; set; } = [];
    public GameStatsSaveState Stats { get; set; } = new();
    public List<ActivityLogEntrySaveState> ActivityLog { get; set; } = [];
    public MarketOpportunitySaveState MarketOpportunity { get; set; }
    public int LastGeneratedMarketOpportunityDay { get; set; }
    public int GeneratedMarketOpportunityCount { get; set; }
    public CleanupSiteSaveState CleanupSite { get; set; }
    public List<string> ClearedRestorationSiteIds { get; set; } = [];
}

public sealed class CleanupSiteSaveState
{
    public string Id { get; set; }
    public bool IsCleared { get; set; }
}

public sealed class MarketOpportunitySaveState
{
    public string Id { get; set; }
    public string ItemId { get; set; }
    public double SellPriceMultiplier { get; set; }
    public int DaysRemaining { get; set; }
    public string DisplayName { get; set; }
    public string Message { get; set; }
}

public sealed class FarmPlotSaveState
{
    public int X { get; set; }
    public int Y { get; set; }
    public string CropDefinitionId { get; set; }
    public int DaysGrown { get; set; }
    public bool IsWatered { get; set; }
}

public sealed class ItemStackSaveState
{
    public string ItemId { get; set; }
    public int Quantity { get; set; }
}

public sealed class BotSaveState
{
    public bool IsPlanterUnlocked { get; set; }
    public bool IsWateringUnlocked { get; set; }
    public bool IsHarvesterUnlocked { get; set; }
    public bool IsWateringEnabled { get; set; }
    public string PlanterAssignedSeedItemId { get; set; }
    public FarmRegionSaveState PlanterRegion { get; set; } = new();
    public FarmRegionSaveState WateringRegion { get; set; } = new();
    public FarmRegionSaveState HarvesterRegion { get; set; } = new();
}

public sealed class FarmRegionSaveState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class ProcessorMachineSaveState
{
    public string MachineId { get; set; }
    public string CurrentRecipeId { get; set; }
    public string InputItemId { get; set; }
    public int InputQuantity { get; set; }
    public string OutputItemId { get; set; }
    public int OutputQuantity { get; set; }
    public int DaysRemaining { get; set; }
    public bool HasOutputReady { get; set; }
}

public sealed class GameStatsSaveState
{
    public int DaysAdvanced { get; set; }
    public int MoneyEarnedFromSelling { get; set; }
    public int MoneySpentOnSeeds { get; set; }
    public int MoneySpentOnBotUnlocks { get; set; }
    public int MoneySpentOnExpansion { get; set; }
    public int SeedsBought { get; set; }
    public int CropsPlantedManually { get; set; }
    public int CropsPlantedByPlanterBot { get; set; }
    public int PlotsWateredManually { get; set; }
    public int PlotsWateredByWateringBot { get; set; }
    public int CropsHarvestedManually { get; set; }
    public int CropsHarvestedByHarvesterBot { get; set; }
    public int CropsSold { get; set; }
    public int FarmExpansionsPurchased { get; set; }
    public int BotsUnlocked { get; set; }
    public int FarmCyclesRun { get; set; }
    public int AutoPlayStepsRun { get; set; }
    public int MachineRunsStarted { get; set; }
    public int MachineOutputsCollected { get; set; }
    public int MachineInputValueConsumed { get; set; }
    public int MachineOutputValueCreated { get; set; }
    public int MachineValueAdded { get; set; }
    public int ProcessedGoodsProduced { get; set; }
    public int MarketPurchases { get; set; }
    public int MarketMoneySpent { get; set; }
    public int MarketItemsBought { get; set; }
    public int CleanupSitesCleared { get; set; }
    public int MoneySpentOnCleanup { get; set; }
    public List<ItemStackSaveState> ProcessedOutputs { get; set; } = [];
}

public sealed class ActivityLogEntrySaveState
{
    public int Day { get; set; }
    public string Category { get; set; }
    public string Message { get; set; }
    public int? ValueAmount { get; set; }
    public string ItemId { get; set; }
    public int? Quantity { get; set; }
}

public sealed class SaveLoadValidationResult
{
    public SaveLoadValidationResult(IEnumerable<string> errors = null)
    {
        Errors = (errors ?? [])
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; }
}

public sealed class LoadGameResult
{
    public LoadGameResult(bool success, string message, IEnumerable<string> errors = null)
    {
        Success = success;
        Message = message ?? string.Empty;
        Errors = (errors ?? [])
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .ToArray();
    }

    public bool Success { get; }
    public string Message { get; }
    public IReadOnlyList<string> Errors { get; }
}

public static class SaveLoadMessages
{
    public const string SaveSucceeded = "Game saved.";
    public const string LoadSucceeded = "Game loaded.";
    public const string NoSaveFileFound = "No save file found.";

    public static string FormatSaveFailed(string errorMessage)
    {
        return $"Save failed: {errorMessage}";
    }

    public static string FormatLoadFailed(string errorMessage)
    {
        return $"Load failed: {errorMessage}";
    }

    public static string FormatValidationFailure(IEnumerable<string> errors)
    {
        return FormatLoadFailed(string.Join("; ", errors ?? []));
    }
}

public sealed class SaveLoadStatus
{
    public SaveLoadStatus(string savePath)
    {
        SavePath = savePath ?? string.Empty;
    }

    public string SavePath { get; }
    public string LastSaveMessage { get; private set; } = string.Empty;
    public bool? LastSaveSucceeded { get; private set; }
    public DateTimeOffset? LastSaveTimestampUtc { get; private set; }
    public string LastLoadMessage { get; private set; } = string.Empty;
    public bool? LastLoadSucceeded { get; private set; }
    public DateTimeOffset? LastLoadTimestampUtc { get; private set; }

    public void RecordSave(bool succeeded, string message)
    {
        LastSaveSucceeded = succeeded;
        LastSaveMessage = message ?? string.Empty;
        LastSaveTimestampUtc = DateTimeOffset.UtcNow;
    }

    public void RecordLoad(bool succeeded, string message)
    {
        LastLoadSucceeded = succeeded;
        LastLoadMessage = message ?? string.Empty;
        LastLoadTimestampUtc = DateTimeOffset.UtcNow;
    }
}

public sealed class SaveGameFileService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string Serialize(SaveGameState saveGame)
    {
        if (saveGame is null)
            throw new ArgumentNullException(nameof(saveGame));

        return JsonSerializer.Serialize(saveGame, SerializerOptions);
    }

    public SaveGameState Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Save file was empty.");

        return JsonSerializer.Deserialize<SaveGameState>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Save file could not be deserialized.");
    }

    public void SaveGameToFile(string path, SaveGameState saveGame)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Save path is required.", nameof(path));

        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, Serialize(saveGame));
    }

    public SaveGameState LoadGameFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Save path is required.", nameof(path));

        return Deserialize(File.ReadAllText(path));
    }

    public bool SaveFileExists(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }
}

public sealed class GameSaveController
{
    private readonly TurnipTestState _state;
    private readonly DebugUiState _debugUiState;
    private readonly AutoPlayTestController _autoPlayController;
    private readonly SaveGameFileService _fileService;

    public GameSaveController(
        TurnipTestState state,
        DebugUiState debugUiState,
        AutoPlayTestController autoPlayController,
        string saveFilePath,
        SaveGameFileService fileService = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _debugUiState = debugUiState ?? throw new ArgumentNullException(nameof(debugUiState));
        _autoPlayController = autoPlayController ?? throw new ArgumentNullException(nameof(autoPlayController));
        SaveFilePath = string.IsNullOrWhiteSpace(saveFilePath)
            ? throw new ArgumentException("Save path is required.", nameof(saveFilePath))
            : saveFilePath;
        Status = new SaveLoadStatus(SaveFilePath);
        _fileService = fileService ?? new SaveGameFileService();
    }

    public string SaveFilePath { get; }
    public SaveLoadStatus Status { get; }

    public string SaveGame()
    {
        try
        {
            _fileService.SaveGameToFile(SaveFilePath, _state.CreateSaveGame());
            Status.RecordSave(true, SaveLoadMessages.SaveSucceeded);
            return Status.LastSaveMessage;
        }
        catch (Exception ex)
        {
            Status.RecordSave(false, SaveLoadMessages.FormatSaveFailed(ex.Message));
            return Status.LastSaveMessage;
        }
    }

    public string LoadGame()
    {
        return LoadGameDetailed().Message;
    }

    public LoadGameResult LoadGameDetailed()
    {
        if (!_fileService.SaveFileExists(SaveFilePath))
        {
            Status.RecordLoad(false, SaveLoadMessages.NoSaveFileFound);
            return new LoadGameResult(false, Status.LastLoadMessage);
        }

        try
        {
            SaveGameState saveGame = _fileService.LoadGameFromFile(SaveFilePath);
            SaveLoadValidationResult validation = _state.ValidateSaveGame(saveGame);
            if (!validation.IsValid)
            {
                string validationMessage = SaveLoadMessages.FormatValidationFailure(validation.Errors);
                Status.RecordLoad(false, validationMessage);
                return new LoadGameResult(false, validationMessage, validation.Errors);
            }

            _state.ApplySaveGame(saveGame);
            _autoPlayController.Disable(AutoPlayStopReason.None, string.Empty);
            _autoPlayController.ResetStats();
            _debugUiState.Reset();
            Status.RecordLoad(true, SaveLoadMessages.LoadSucceeded);
            return new LoadGameResult(true, Status.LastLoadMessage);
        }
        catch (Exception ex)
        {
            Status.RecordLoad(false, SaveLoadMessages.FormatLoadFailed(ex.Message));
            return new LoadGameResult(false, Status.LastLoadMessage, [ex.Message]);
        }
    }
}

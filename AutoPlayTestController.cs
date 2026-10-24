using System;
using System.Collections.Generic;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public enum AutoPlayStopReason
{
    None,
    TargetMoneyReached,
    MaxDaysReached,
    MaxStepsReached,
    InvalidState,
    NoUsefulActionPossible,
    StoppedByUser
}

public sealed class AutoPlaySettings
{
    public int TargetMoney { get; init; } = GameBalanceConfig.Default.AutoPlayTargetMoney;
    public int MaxDays { get; init; } = GameBalanceConfig.Default.AutoPlayMaxDays;
    public int MaxSteps { get; init; } = GameBalanceConfig.Default.AutoPlayMaxSteps;
    public int MaxNoActionSteps { get; init; } = GameBalanceConfig.Default.AutoPlayNoActionLimit;
    public int DesiredSeedCount { get; init; }
    public double StepIntervalSeconds { get; init; } = 0.2;

    public static AutoPlaySettings CreateDefault(GameBalanceConfig config = null)
    {
        GameBalanceConfig effectiveConfig = config ?? GameBalanceConfig.Default;
        return new AutoPlaySettings
        {
            TargetMoney = effectiveConfig.AutoPlayTargetMoney,
            MaxDays = effectiveConfig.AutoPlayMaxDays,
            MaxSteps = effectiveConfig.AutoPlayMaxSteps,
            MaxNoActionSteps = effectiveConfig.AutoPlayNoActionLimit
        };
    }
}

public sealed class AutoPlayStats
{
    private readonly Dictionary<string, int> _boughtSeedsByItemId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _plantedSeedsByItemId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _harvestedItemsByItemId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _soldItemsByItemId = new(StringComparer.OrdinalIgnoreCase);

    public int StepsRun { get; private set; }
    public int DaysSimulated { get; private set; }
    public int SeedsBought { get; private set; }
    public int CropsHarvested { get; private set; }
    public int CropsSold { get; private set; }
    public int BotsUnlocked { get; private set; }
    public int ExpansionsCompleted { get; private set; }
    public int PlanterBotUses { get; private set; }
    public int WateringBotUses { get; private set; }
    public int HarvesterBotUses { get; private set; }
    public int ManualPlantActions { get; private set; }
    public int ManualWaterActions { get; private set; }
    public int ManualHarvestActions { get; private set; }
    public int ConsecutiveNoActionSteps { get; private set; }
    public string LastAutoAction { get; private set; } = "Auto: idle.";
    public AutoPlayStopReason StopReason { get; private set; }
    public string StopReasonText { get; private set; } = string.Empty;
    public IReadOnlyDictionary<string, int> BoughtSeedsByItemId => _boughtSeedsByItemId;
    public IReadOnlyDictionary<string, int> PlantedSeedsByItemId => _plantedSeedsByItemId;
    public IReadOnlyDictionary<string, int> HarvestedItemsByItemId => _harvestedItemsByItemId;
    public IReadOnlyDictionary<string, int> SoldItemsByItemId => _soldItemsByItemId;

    public void RecordStep()
    {
        StepsRun++;
    }

    public void RecordDayAdvanced()
    {
        DaysSimulated++;
    }

    public void RecordSeedsBought(int count)
    {
        SeedsBought += Math.Max(0, count);
    }

    public void RecordSeedsBought(string seedItemId, int count)
    {
        RecordSeedsBought(count);
        RecordItemCount(_boughtSeedsByItemId, seedItemId, count);
    }

    public void RecordCropsHarvested(int count)
    {
        CropsHarvested += Math.Max(0, count);
    }

    public void RecordHarvestedItems(IReadOnlyDictionary<string, int> harvestedItemCounts)
    {
        if (harvestedItemCounts is null)
            return;

        foreach ((string itemId, int count) in harvestedItemCounts)
            RecordItemCount(_harvestedItemsByItemId, itemId, count);
    }

    public void RecordCropsSold(int count)
    {
        CropsSold += Math.Max(0, count);
    }

    public void RecordSoldItems(IReadOnlyDictionary<string, int> soldItemCounts)
    {
        if (soldItemCounts is null)
            return;

        foreach ((string itemId, int count) in soldItemCounts)
            RecordItemCount(_soldItemsByItemId, itemId, count);
    }

    public void RecordBotUnlocked()
    {
        BotsUnlocked++;
    }

    public void RecordExpansion()
    {
        ExpansionsCompleted++;
    }

    public void RecordPlanterBotUse()
    {
        PlanterBotUses++;
    }

    public void RecordWateringBotUse()
    {
        WateringBotUses++;
    }

    public void RecordHarvesterBotUse()
    {
        HarvesterBotUses++;
    }

    public void RecordManualPlant()
    {
        ManualPlantActions++;
    }

    public void RecordPlantedSeeds(string seedItemId, int count)
    {
        RecordItemCount(_plantedSeedsByItemId, seedItemId, count);
    }

    public void RecordManualWater()
    {
        ManualWaterActions++;
    }

    public void RecordManualHarvest()
    {
        ManualHarvestActions++;
    }

    public void RecordAction(string message)
    {
        ConsecutiveNoActionSteps = 0;
        LastAutoAction = message;
    }

    public void RecordNoAction(string message)
    {
        ConsecutiveNoActionSteps++;
        LastAutoAction = message;
    }

    public void SetStopReason(AutoPlayStopReason stopReason, string stopReasonText)
    {
        StopReason = stopReason;
        StopReasonText = stopReasonText;
    }

    public void ClearStopReason()
    {
        StopReason = AutoPlayStopReason.None;
        StopReasonText = string.Empty;
    }

    public void Reset()
    {
        StepsRun = 0;
        DaysSimulated = 0;
        SeedsBought = 0;
        CropsHarvested = 0;
        CropsSold = 0;
        BotsUnlocked = 0;
        ExpansionsCompleted = 0;
        PlanterBotUses = 0;
        WateringBotUses = 0;
        HarvesterBotUses = 0;
        ManualPlantActions = 0;
        ManualWaterActions = 0;
        ManualHarvestActions = 0;
        ConsecutiveNoActionSteps = 0;
        LastAutoAction = "Auto: idle.";
        StopReason = AutoPlayStopReason.None;
        StopReasonText = string.Empty;
        _boughtSeedsByItemId.Clear();
        _plantedSeedsByItemId.Clear();
        _harvestedItemsByItemId.Clear();
        _soldItemsByItemId.Clear();
    }

    public int GetBoughtSeedCount(string seedItemId) => GetItemCount(_boughtSeedsByItemId, seedItemId);
    public int GetPlantedSeedCount(string seedItemId) => GetItemCount(_plantedSeedsByItemId, seedItemId);
    public int GetHarvestedItemCount(string itemId) => GetItemCount(_harvestedItemsByItemId, itemId);
    public int GetSoldItemCount(string itemId) => GetItemCount(_soldItemsByItemId, itemId);

    private static int GetItemCount(IReadOnlyDictionary<string, int> countsByItemId, string itemId)
    {
        return countsByItemId.TryGetValue(itemId, out int count) ? count : 0;
    }

    private static void RecordItemCount(IDictionary<string, int> countsByItemId, string itemId, int count)
    {
        if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            return;

        countsByItemId[itemId] = GetItemCount((IReadOnlyDictionary<string, int>)countsByItemId, itemId) + count;
    }
}

public sealed partial class AutoPlayTestController
{
    private readonly TurnipTestState _state;
    private readonly ContentRegistry _registry;
    private readonly HashSet<string> _marketOffersPurchasedThisRun = new(StringComparer.OrdinalIgnoreCase);
    private int _seedPreferenceCursor;
    private int _marketPurchasesThisCycle;

    public AutoPlayTestController(TurnipTestState state, ContentRegistry registry, AutoPlaySettings settings = null)
    {
        _state = state;
        _registry = registry;
        Settings = settings ?? AutoPlaySettings.CreateDefault(state.BalanceConfig);
        Stats = new AutoPlayStats();
    }

    public AutoPlaySettings Settings { get; }
    public AutoPlayStats Stats { get; }
    public GameStateValidationResult LastValidationResult { get; private set; } = GameStateValidationResult.Valid();
    public bool IsEnabled { get; private set; }
    public int CurrentSeedCount => CountSeeds();
    public int RequiredSeedSafetyCount => Math.Max(Settings.DesiredSeedCount, _state.FarmGrid.Width * _state.FarmGrid.Height);
    public bool IsSeedSafetySatisfied => CurrentSeedCount >= RequiredSeedSafetyCount;

    public IReadOnlyList<string> GetProcessorInputItemIds()
    {
        return _registry.ProcessingRecipes
            .Select(recipe => recipe.InputItemId)
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<string> GetSeedItemIdsForProcessorInputs()
    {
        return _registry.ProcessingRecipes
            .Select(recipe => recipe.InputItemId)
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .Select(inputItemId => _registry.TileProducers.FirstOrDefault(tileProducer => string.Equals(tileProducer.OutputItemId, inputItemId, StringComparison.OrdinalIgnoreCase))?.StarterItemId)
            .Where(seedItemId => !string.IsNullOrWhiteSpace(seedItemId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<string> GetStarterItemIdsForProcessorInputs()
    {
        return GetSeedItemIdsForProcessorInputs();
    }

    public string GetActiveOpportunityItemId()
    {
        return _state.ActiveMarketOpportunity?.IsActive == true
            ? _state.ActiveMarketOpportunity.ItemId
            : string.Empty;
    }

    public IReadOnlyList<string> GetRecipeIdsProducingItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return [];

        return _registry.ProcessingRecipes
            .Where(recipe => string.Equals(recipe.OutputItemId, itemId, StringComparison.OrdinalIgnoreCase))
            .Select(recipe => recipe.Id)
            .Where(recipeId => !string.IsNullOrWhiteSpace(recipeId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<string> GetSeedItemIdsSupportingOpportunity()
    {
        return GetSeedItemIdsSupportingProducedItem(GetActiveOpportunityItemId());
    }

    public IReadOnlyList<string> GetStarterItemIdsSupportingOpportunity()
    {
        return GetSeedItemIdsSupportingOpportunity();
    }

    public IReadOnlyList<string> GetAutoplaySeedPreferenceOrder()
    {
        HashSet<string> opportunitySeedIds = GetSeedItemIdsSupportingOpportunity().ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> processorSeedIds = GetSeedItemIdsForProcessorInputs().ToHashSet(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> opportunitySeeds = _state.SeedItemIds.Where(seedItemId => opportunitySeedIds.Contains(seedItemId));
        IEnumerable<string> processorSeeds = _state.SeedItemIds.Where(seedItemId => processorSeedIds.Contains(seedItemId) && !opportunitySeedIds.Contains(seedItemId));
        IEnumerable<string> otherSeeds = _state.SeedItemIds.Where(seedItemId => !processorSeedIds.Contains(seedItemId) && !opportunitySeedIds.Contains(seedItemId));
        return opportunitySeeds.Concat(processorSeeds).Concat(otherSeeds).ToArray();
    }

    private IReadOnlyList<string> GetSeedItemIdsSupportingProducedItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return [];

        HashSet<string> supportingSeedIds = _registry.ProcessingRecipes
            .Where(recipe => string.Equals(recipe.OutputItemId, itemId, StringComparison.OrdinalIgnoreCase))
            .Select(recipe => _registry.TileProducers.FirstOrDefault(tileProducer => string.Equals(tileProducer.OutputItemId, recipe.InputItemId, StringComparison.OrdinalIgnoreCase))?.StarterItemId)
            .Where(seedItemId => !string.IsNullOrWhiteSpace(seedItemId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _state.SeedItemIds
            .Where(seedItemId => supportingSeedIds.Contains(seedItemId))
            .ToArray();
    }

    public void Enable()
    {
        IsEnabled = true;
        Stats.ClearStopReason();
        LastValidationResult = GameStateValidationResult.Valid();
    }

    public void Disable(AutoPlayStopReason stopReason = AutoPlayStopReason.StoppedByUser, string stopReasonText = "Stopped manually.")
    {
        IsEnabled = false;
        if (stopReason != AutoPlayStopReason.None)
            Stats.SetStopReason(stopReason, stopReasonText);
    }

    public bool ToggleEnabled()
    {
        if (IsEnabled)
        {
            Disable();
            return false;
        }

        Enable();
        return true;
    }

    public void ResetStats()
    {
        Stats.Reset();
        _seedPreferenceCursor = 0;
        _marketOffersPurchasedThisRun.Clear();
        LastValidationResult = GameStateValidationResult.Valid();
    }

    public string RunStep(bool isPaused = false)
    {
        if (isPaused)
            return "Auto: paused.";

        if (TryStopForInvalidState(out string invalidStateMessage))
            return invalidStateMessage;

        if (ShouldStopBeforeStep(out string stopMessage))
            return stopMessage;

        Stats.RecordStep();
        _state.Stats.RecordAutoPlayStep();
        _marketPurchasesThisCycle = 0;

        // The order is intentional: collect or start machine work first, keep a safe
        // seed buffer before spending on progression, then keep the farm loop moving.
        if (TryCollectMachineOutput(out string collectMachineMessage))
            return FinalizeAction(collectMachineMessage);

        if (TryStartMachine(out string startMachineMessage))
            return FinalizeAction(startMachineMessage);

        if (TryBuyMarketInputForIdleMachine(out string marketBuyMessage))
            return FinalizeAction(marketBuyMessage);

        bool seedSafetySatisfiedAtStepStart = IsSeedSafetySatisfied;

        if (!seedSafetySatisfiedAtStepStart && TryBuySeeds(out string buyMessage))
            return FinalizeAction(buyMessage);

        if (seedSafetySatisfiedAtStepStart && TryClearCleanupSite(out string cleanupMessage))
            return FinalizeAction(cleanupMessage);

        if (seedSafetySatisfiedAtStepStart && TryExpandFarm(out string expansionMessage))
            return FinalizeAction(expansionMessage);

        if (seedSafetySatisfiedAtStepStart && TryUnlockBots(out string unlockMessage))
            return FinalizeAction(unlockMessage);

        if (TryHarvest(out string harvestMessage))
            return FinalizeAction(harvestMessage);

        if (TrySell(out string sellMessage))
            return FinalizeAction(sellMessage);

        if (TryPlant(out string plantMessage))
            return FinalizeAction(plantMessage);

        if (TryWater(out string waterMessage))
            return FinalizeAction(waterMessage);

        if (HasMeaningfulDayAdvanceOpportunity())
            return FinalizeAction(AdvanceDay());

        string noActionMessage = seedSafetySatisfiedAtStepStart
            ? "Auto: no useful action possible."
            : $"Auto: skipped spending, seeds below safety target {CurrentSeedCount}/{RequiredSeedSafetyCount}.";
        Stats.RecordNoAction(noActionMessage);

        if (Stats.ConsecutiveNoActionSteps >= Settings.MaxNoActionSteps)
        {
            Disable(AutoPlayStopReason.NoUsefulActionPossible, "No useful action possible.");
            return $"Auto: stopped, {Stats.StopReasonText.ToLowerInvariant()}";
        }

        return noActionMessage;
    }

    private string FinalizeAction(string message)
    {
        Stats.RecordAction(message);

        if (TryStopForInvalidState(out string invalidStateMessage))
            return invalidStateMessage;

        if (ShouldStopAfterAction())
            return $"Auto: stopped, {Stats.StopReasonText.ToLowerInvariant()}";

        return message;
    }

    private bool ShouldStopBeforeStep(out string stopMessage)
    {
        if (_state.Money >= Settings.TargetMoney)
        {
            Disable(AutoPlayStopReason.TargetMoneyReached, $"Reached ${Settings.TargetMoney}.");
            stopMessage = $"Auto: stopped, reached ${Settings.TargetMoney}.";
            return true;
        }

        if ((
                _state.DayNumber - 1) >= Settings.MaxDays)
        {
            Disable(AutoPlayStopReason.MaxDaysReached, $"Reached day limit {Settings.MaxDays}.");
            stopMessage = $"Auto: stopped, reached day limit {Settings.MaxDays}.";
            return true;
        }

        if (Stats.StepsRun >= Settings.MaxSteps)
        {
            Disable(AutoPlayStopReason.MaxStepsReached, $"Reached step limit {Settings.MaxSteps}.");
            stopMessage = $"Auto: stopped, reached step limit {Settings.MaxSteps}.";
            return true;
        }

        stopMessage = string.Empty;
        return false;
    }

    private bool ShouldStopAfterAction()
    {
        if (_state.Money >= Settings.TargetMoney)
        {
            Disable(AutoPlayStopReason.TargetMoneyReached, $"Reached ${Settings.TargetMoney}.");
            return true;
        }

        if ((_state.DayNumber - 1) >= Settings.MaxDays)
        {
            Disable(AutoPlayStopReason.MaxDaysReached, $"Reached day limit {Settings.MaxDays}.");
            return true;
        }

        if (Stats.StepsRun >= Settings.MaxSteps)
        {
            Disable(AutoPlayStopReason.MaxStepsReached, $"Reached step limit {Settings.MaxSteps}.");
            return true;
        }

        return false;
    }

    private bool TryStopForInvalidState(out string stopMessage)
    {
#if DEBUG
        LastValidationResult = _state.ValidateCurrentState();
        if (LastValidationResult.IsValid)
        {
            stopMessage = string.Empty;
            return false;
        }

        string firstError = LastValidationResult.Errors[0];
        Disable(AutoPlayStopReason.InvalidState, $"Invalid state - {firstError}");
        stopMessage = $"Autoplay stopped: invalid state - {firstError}";
        return true;
#else
        stopMessage = string.Empty;
        return false;
#endif
    }

}
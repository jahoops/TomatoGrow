using System;
using System.Collections.Generic;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public sealed class ProgressionSimulationSettings
{
    public int TargetMoney { get; init; } = GameBalanceConfig.Default.ProgressionSimulationDefaultTargetMoney;
    public int MaxDays { get; init; } = GameBalanceConfig.Default.ProgressionSimulationDefaultMaxDays;
    public int MaxSteps { get; init; } = GameBalanceConfig.Default.ProgressionSimulationDefaultMaxSteps;
    public int MaxNoActionSteps { get; init; } = GameBalanceConfig.Default.AutoPlayNoActionLimit;
    public int DesiredSeedCount { get; init; }
    public bool EnableMarketOpportunity { get; init; }
    public string MarketOpportunityId { get; init; } = "simulation_opportunity";
    public string MarketOpportunityItemId { get; init; } = string.Empty;
    public string MarketOpportunityDisplayName { get; init; } = string.Empty;
    public string MarketOpportunityMessage { get; init; } = string.Empty;
    public double MarketOpportunitySellPriceMultiplier { get; init; } = GameBalanceConfig.Default.MarketOpportunityDefaultMultiplier;
    public int MarketOpportunityDurationDays { get; init; } = GameBalanceConfig.Default.MarketOpportunityDefaultDurationDays;
    public MarketOpportunity MarketOpportunity { get; init; }

    public static ProgressionSimulationSettings CreateDefault(GameBalanceConfig config = null)
    {
        GameBalanceConfig effectiveConfig = config ?? GameBalanceConfig.Default;
        return new ProgressionSimulationSettings
        {
            TargetMoney = effectiveConfig.ProgressionSimulationDefaultTargetMoney,
            MaxDays = effectiveConfig.ProgressionSimulationDefaultMaxDays,
            MaxSteps = effectiveConfig.ProgressionSimulationDefaultMaxSteps,
            MaxNoActionSteps = effectiveConfig.AutoPlayNoActionLimit,
            MarketOpportunityDurationDays = effectiveConfig.MarketOpportunityDefaultDurationDays,
            MarketOpportunitySellPriceMultiplier = effectiveConfig.MarketOpportunityDefaultMultiplier
        };
    }
}

public enum ProgressionMilestoneType
{
    FirstCropPlanted,
    FirstCropHarvested,
    FirstCropSold,
    FirstSeedBought,
    PlanterBotUnlocked,
    WateringBotUnlocked,
    HarvesterBotUnlocked,
    FirstFarmExpansion,
    OldJunkPileCleared,
    FinalFarmExpansion,
    FirstMachineRunStarted,
    FirstMachineOutputCollected,
    FirstProcessedGoodSold,
    FirstAgedGoodProduced,
    FirstAgedGoodSold,
    FirstMarketPurchase,
    FirstMarketOpportunitySale
}

public readonly record struct ProgressionMilestone(ProgressionMilestoneType Type, int Day, string Description);

public sealed class ProgressionSimulationResult
{
    private readonly Dictionary<ProgressionMilestoneType, ProgressionMilestone> _milestones;

    public ProgressionSimulationResult(
        ProgressionSimulationSettings settings,
        IReadOnlyDictionary<ProgressionMilestoneType, ProgressionMilestone> milestones,
        IReadOnlyList<string> invariantErrors)
    {
        Settings = settings ?? new ProgressionSimulationSettings();
        _milestones = new Dictionary<ProgressionMilestoneType, ProgressionMilestone>(milestones ?? new Dictionary<ProgressionMilestoneType, ProgressionMilestone>());
        InvariantErrors = (invariantErrors ?? [])
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public ProgressionSimulationSettings Settings { get; }
    public int DaysSimulated { get; init; }
    public int StepsRun { get; init; }
    public int FinalMoney { get; init; }
    public int CurrentSellableValue { get; init; }
    public int PotentialMoney { get; init; }
    public AutoPlayStopReason StopReason { get; init; }
    public string StopReasonText { get; init; } = string.Empty;
    public IReadOnlyList<string> InvariantErrors { get; }
    public bool InvariantStatusOk => InvariantErrors.Count == 0;
    public bool Stalled => StopReason == AutoPlayStopReason.NoUsefulActionPossible;
    public bool SeedSafetyTriggered { get; init; }
    public bool StalledWhileBelowSeedSafety { get; init; }
    public int FinalSeedCount { get; init; }
    public int RequiredSeedSafetyCount { get; init; }
    public int CropsPlanted { get; init; }
    public int CropsHarvested { get; init; }
    public int CropsSold { get; init; }
    public int SeedsBought { get; init; }
    public int FarmExpansionsPurchased { get; init; }
    public int CleanupSitesCleared { get; init; }
    public int BotsUnlocked { get; init; }
    public int MachineRunsStarted { get; init; }
    public int MachineOutputsCollected { get; init; }
    public int ProcessedGoodsProduced { get; init; }
    public int MachineValueAdded { get; init; }
    public int MarketPurchases { get; init; }
    public int MarketMoneySpent { get; init; }
    public int MarketItemsBought { get; init; }
    public int ActivityLogEntriesCount { get; init; }
    public int MarketOpportunitiesGenerated { get; init; }
    public int? FirstGeneratedMarketOpportunityDay { get; init; }
    public int? FirstBoostedSaleDay { get; init; }
    public string ActiveMarketOpportunityAtEnd { get; init; } = string.Empty;
    public int MoneyEarnedFromSales { get; init; }
    public int MoneySpentOnSeeds { get; init; }
    public int MoneySpentOnBots { get; init; }
    public int MoneySpentOnExpansion { get; init; }
    public int MoneySpentOnCleanup { get; init; }
    public int MoneySpentOnMarket { get; init; }
    public int NetMoneyChange { get; init; }
    public int ManualPlantActions { get; init; }
    public int ManualWaterActions { get; init; }
    public int ManualHarvestActions { get; init; }
    public int PlanterBotActions { get; init; }
    public int WateringBotActions { get; init; }
    public int HarvesterBotActions { get; init; }
    public int BotActionsSaved { get; init; }
    public double AverageNetMoneyPerDay { get; init; }
    public double AverageMachineValueAddedPerRun { get; init; }

    public IReadOnlyList<ProgressionMilestone> Milestones => _milestones.Values
        .OrderBy(milestone => milestone.Day)
        .ThenBy(milestone => (int)milestone.Type)
        .ToArray();

    public int? GetMilestoneDay(ProgressionMilestoneType type)
    {
        return _milestones.TryGetValue(type, out ProgressionMilestone milestone) ? milestone.Day : null;
    }

    public bool HasMilestone(ProgressionMilestoneType type)
    {
        return _milestones.ContainsKey(type);
    }

    public string GetSeedSafetyStatusText()
    {
        if (StalledWhileBelowSeedSafety)
            return "Potential dead end detected";

        if (SeedSafetyTriggered)
            return "OK - safety buffer engaged";

        return "No issue detected";
    }

    public string GetLoopHealthStatusText()
    {
        if (!InvariantStatusOk)
            return "Invariant failure detected";

        if (Stalled)
            return "Stalled before target";

        return "No issue detected";
    }
}

public sealed class ProgressionSimulationRunner
{
    private readonly ContentRegistry _registry;
    private readonly GameBalanceConfig _balanceConfig;
    private readonly string[] _sellableItemIds;
    private readonly string[] _processedGoodItemIds;
    private readonly string[] _agedGoodItemIds;

    public ProgressionSimulationRunner(ContentRegistry registry, GameBalanceConfig balanceConfig = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _balanceConfig = balanceConfig ?? GameBalanceConfig.Default;
        _sellableItemIds = _registry.Items
            .Where(item => !_registry.IsStarterItem(item.Id) && item.SellPrice > 0)
            .Select(item => item.Id)
            .ToArray();
        _processedGoodItemIds = _registry.Items
            .Where(item => string.Equals(item.Type, "processed_good", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Id)
            .ToArray();
        _agedGoodItemIds = _registry.Items
            .Where(item => string.Equals(item.Type, "processed_good", StringComparison.OrdinalIgnoreCase))
            .Where(item => item.Id.Contains("aged", StringComparison.OrdinalIgnoreCase) || item.Name.Contains("aged", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Id)
            .ToArray();
    }

    public ProgressionSimulationResult Run(ProgressionSimulationSettings settings = null)
    {
        ProgressionSimulationSettings effectiveSettings = settings ?? ProgressionSimulationSettings.CreateDefault(_balanceConfig);
        TurnipTestState state = new(_registry, balanceConfig: _balanceConfig);
        ConfigureOpportunity(state, effectiveSettings);

        AutoPlayTestController controller = new(
            state,
            _registry,
            new AutoPlaySettings
            {
                TargetMoney = effectiveSettings.TargetMoney,
                MaxDays = effectiveSettings.MaxDays,
                MaxSteps = effectiveSettings.MaxSteps,
                MaxNoActionSteps = effectiveSettings.MaxNoActionSteps,
                DesiredSeedCount = effectiveSettings.DesiredSeedCount
            });

        Dictionary<ProgressionMilestoneType, ProgressionMilestone> milestones = [];
        bool seedSafetyTriggered = false;
        bool stalledWhileBelowSeedSafety = false;
        GameStateValidationResult lastValidationResult = state.ValidateCurrentState();

        if (!lastValidationResult.IsValid)
        {
            controller.Disable(AutoPlayStopReason.InvalidState, $"Invalid state - {lastValidationResult.Errors[0]}");
            return CreateResult(effectiveSettings, state, controller, milestones, seedSafetyTriggered, stalledWhileBelowSeedSafety, lastValidationResult.Errors);
        }

        controller.Enable();

        while (controller.IsEnabled)
        {
            SimulationSnapshot before = CaptureSnapshot(state);
            string stepMessage = controller.RunStep();
            SimulationSnapshot after = CaptureSnapshot(state);

            ObserveMilestones(milestones, before, after, state.ActiveMarketOpportunity?.ItemId);

            if (!seedSafetyTriggered && (stepMessage.Contains("buying seeds first", StringComparison.OrdinalIgnoreCase)
                || stepMessage.Contains("seeds below safety target", StringComparison.OrdinalIgnoreCase)))
            {
                seedSafetyTriggered = true;
            }

            lastValidationResult = state.ValidateCurrentState();
            if (!lastValidationResult.IsValid)
            {
                controller.Disable(AutoPlayStopReason.InvalidState, $"Invalid state - {lastValidationResult.Errors[0]}");
                break;
            }
        }

        if (controller.Stats.StopReason == AutoPlayStopReason.NoUsefulActionPossible && !controller.IsSeedSafetySatisfied)
            stalledWhileBelowSeedSafety = true;

        IReadOnlyList<string> invariantErrors = lastValidationResult.IsValid ? [] : lastValidationResult.Errors;
        return CreateResult(effectiveSettings, state, controller, milestones, seedSafetyTriggered, stalledWhileBelowSeedSafety, invariantErrors);
    }

    private ProgressionSimulationResult CreateResult(
        ProgressionSimulationSettings settings,
        TurnipTestState state,
        AutoPlayTestController controller,
        IReadOnlyDictionary<ProgressionMilestoneType, ProgressionMilestone> milestones,
        bool seedSafetyTriggered,
        bool stalledWhileBelowSeedSafety,
        IReadOnlyList<string> invariantErrors)
    {
        GameStats stats = state.Stats;
        int currentSellableValue = GetCurrentSellableValue(state);
        int potentialMoney = state.Money + currentSellableValue;

        return new ProgressionSimulationResult(settings, milestones, invariantErrors)
        {
            DaysSimulated = controller.Stats.DaysSimulated,
            StepsRun = controller.Stats.StepsRun,
            FinalMoney = state.Money,
            CurrentSellableValue = currentSellableValue,
            PotentialMoney = potentialMoney,
            StopReason = controller.Stats.StopReason,
            StopReasonText = controller.Stats.StopReasonText,
            SeedSafetyTriggered = seedSafetyTriggered,
            StalledWhileBelowSeedSafety = stalledWhileBelowSeedSafety,
            FinalSeedCount = controller.CurrentSeedCount,
            RequiredSeedSafetyCount = controller.RequiredSeedSafetyCount,
            CropsPlanted = stats.TotalCropsPlanted,
            CropsHarvested = stats.TotalCropsHarvested,
            CropsSold = stats.CropsSold,
            SeedsBought = stats.SeedsBought,
            FarmExpansionsPurchased = stats.FarmExpansionsPurchased,
            CleanupSitesCleared = stats.CleanupSitesCleared,
            BotsUnlocked = stats.BotsUnlocked,
            MachineRunsStarted = stats.MachineRunsStarted,
            MachineOutputsCollected = stats.MachineOutputsCollected,
            ProcessedGoodsProduced = stats.ProcessedGoodsProduced,
            MachineValueAdded = stats.MachineValueAdded,
            MarketPurchases = stats.MarketPurchases,
            MarketMoneySpent = stats.MarketMoneySpent,
            MarketItemsBought = stats.MarketItemsBought,
            ActivityLogEntriesCount = state.ActivityLog.Count,
            MarketOpportunitiesGenerated = state.GeneratedMarketOpportunityCount,
            FirstGeneratedMarketOpportunityDay = state.ActivityLog
                .Where(entry => entry.Category == ActivityLogCategory.Market && entry.Message.Contains(" started:", StringComparison.Ordinal))
                .Select(entry => (int?)entry.Day)
                .FirstOrDefault(),
            FirstBoostedSaleDay = milestones.TryGetValue(ProgressionMilestoneType.FirstMarketOpportunitySale, out ProgressionMilestone firstOpportunitySale)
                ? firstOpportunitySale.Day
                : null,
            ActiveMarketOpportunityAtEnd = state.ActiveMarketOpportunity?.IsActive == true ? state.ActiveMarketOpportunity.DisplayName : string.Empty,
            MoneyEarnedFromSales = stats.MoneyEarnedFromSelling,
            MoneySpentOnSeeds = stats.MoneySpentOnSeeds,
            MoneySpentOnBots = stats.MoneySpentOnBotUnlocks,
            MoneySpentOnExpansion = stats.MoneySpentOnExpansion,
            MoneySpentOnCleanup = stats.MoneySpentOnCleanup,
            MoneySpentOnMarket = stats.MarketMoneySpent,
            NetMoneyChange = state.Money,
            ManualPlantActions = stats.CropsPlantedManually,
            ManualWaterActions = stats.PlotsWateredManually,
            ManualHarvestActions = stats.CropsHarvestedManually,
            PlanterBotActions = stats.CropsPlantedByPlanterBot,
            WateringBotActions = stats.PlotsWateredByWateringBot,
            HarvesterBotActions = stats.CropsHarvestedByHarvesterBot,
            BotActionsSaved = stats.BotActionsSaved,
            AverageNetMoneyPerDay = controller.Stats.DaysSimulated > 0 ? (double)state.Money / controller.Stats.DaysSimulated : state.Money,
            AverageMachineValueAddedPerRun = stats.MachineRunsStarted > 0 ? (double)stats.MachineValueAdded / stats.MachineRunsStarted : 0d
        };
    }

    private void ConfigureOpportunity(TurnipTestState state, ProgressionSimulationSettings settings)
    {
        if (!settings.EnableMarketOpportunity)
            return;

        MarketOpportunity opportunity = settings.MarketOpportunity?.Clone();
        if (opportunity is null && !string.IsNullOrWhiteSpace(settings.MarketOpportunityItemId))
        {
            ItemDefinition item = _registry.GetItem(settings.MarketOpportunityItemId);
            int durationDays = Math.Max(1, settings.MarketOpportunityDurationDays);
            double multiplier = settings.MarketOpportunitySellPriceMultiplier > 0
                ? settings.MarketOpportunitySellPriceMultiplier
                : _balanceConfig.MarketOpportunityDefaultMultiplier;
            int multiplierPercent = (int)Math.Round(multiplier * 100, MidpointRounding.AwayFromZero);
            opportunity = new MarketOpportunity
            {
                Id = string.IsNullOrWhiteSpace(settings.MarketOpportunityId) ? settings.MarketOpportunityItemId : settings.MarketOpportunityId,
                ItemId = settings.MarketOpportunityItemId,
                SellPriceMultiplier = multiplier,
                DaysRemaining = durationDays,
                DisplayName = string.IsNullOrWhiteSpace(settings.MarketOpportunityDisplayName) ? $"{item.Name} Boom" : settings.MarketOpportunityDisplayName,
                Message = string.IsNullOrWhiteSpace(settings.MarketOpportunityMessage)
                    ? $"{item.Name} sells for {multiplierPercent}% for {durationDays} days."
                    : settings.MarketOpportunityMessage
            };
        }

        if (opportunity is not null)
            state.SetActiveMarketOpportunity(opportunity);
    }

    private int GetCurrentSellableValue(TurnipTestState state)
    {
        return _sellableItemIds.Sum(itemId => state.GetAccessibleItemCount(itemId) * state.GetEffectiveSellPrice(itemId));
    }

    private void ObserveMilestones(
        IDictionary<ProgressionMilestoneType, ProgressionMilestone> milestones,
        SimulationSnapshot before,
        SimulationSnapshot after,
        string activeOpportunityItemId)
    {
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstSeedBought, after.DayNumber, "First seed bought", after.SeedsBought > before.SeedsBought);
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstCropPlanted, after.DayNumber, "First crop planted", after.CropsPlanted > before.CropsPlanted);
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstCropHarvested, after.DayNumber, "First crop harvested", after.CropsHarvested > before.CropsHarvested);
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstCropSold, after.DayNumber, "First crop sold", after.CropsSold > before.CropsSold);
        TryAddMilestone(milestones, ProgressionMilestoneType.PlanterBotUnlocked, after.DayNumber, "Planter Bot unlocked", !before.IsPlanterBotUnlocked && after.IsPlanterBotUnlocked);
        TryAddMilestone(milestones, ProgressionMilestoneType.WateringBotUnlocked, after.DayNumber, "Watering Bot unlocked", !before.IsWateringBotUnlocked && after.IsWateringBotUnlocked);
        TryAddMilestone(milestones, ProgressionMilestoneType.HarvesterBotUnlocked, after.DayNumber, "Harvester Bot unlocked", !before.IsHarvesterBotUnlocked && after.IsHarvesterBotUnlocked);
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstFarmExpansion, after.DayNumber, $"First farm expansion to {after.FarmWidth}x{after.FarmHeight}", after.FarmExpansionsPurchased > before.FarmExpansionsPurchased);
        TryAddMilestone(milestones, ProgressionMilestoneType.OldJunkPileCleared, after.DayNumber, "Cleared Old Junk Pile", !before.OldJunkPileCleared && after.OldJunkPileCleared);
        TryAddMilestone(milestones, ProgressionMilestoneType.FinalFarmExpansion, after.DayNumber, $"Expanded to {after.FarmWidth}x{after.FarmHeight}", before.FarmWidth < 5 && after.FarmWidth >= 5 && after.FarmHeight == TurnipTestState.InitialFarmHeight);
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstMachineRunStarted, after.DayNumber, "First machine run started", after.MachineRunsStarted > before.MachineRunsStarted);
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstMachineOutputCollected, after.DayNumber, "First machine output collected", after.MachineOutputsCollected > before.MachineOutputsCollected);
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstProcessedGoodSold, after.DayNumber, "First processed good sold", after.CropsSold > before.CropsSold && after.ProcessedGoodsAccessible < before.ProcessedGoodsAccessible);
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstAgedGoodProduced, after.DayNumber, "First aged good produced", after.AgedGoodsProduced > before.AgedGoodsProduced);
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstAgedGoodSold, after.DayNumber, "First aged good sold", after.CropsSold > before.CropsSold && after.AgedGoodsAccessible < before.AgedGoodsAccessible);
        TryAddMilestone(milestones, ProgressionMilestoneType.FirstMarketPurchase, after.DayNumber, "First market purchase", after.MarketPurchases > before.MarketPurchases);
        TryAddMilestone(
            milestones,
            ProgressionMilestoneType.FirstMarketOpportunitySale,
            after.DayNumber,
            "First market opportunity sale",
            !string.IsNullOrWhiteSpace(activeOpportunityItemId)
            && after.CropsSold > before.CropsSold
            && after.OpportunityItemAccessible < before.OpportunityItemAccessible);
    }

    private static void TryAddMilestone(
        IDictionary<ProgressionMilestoneType, ProgressionMilestone> milestones,
        ProgressionMilestoneType type,
        int dayNumber,
        string description,
        bool shouldAdd)
    {
        if (!shouldAdd || milestones.ContainsKey(type))
            return;

        milestones[type] = new ProgressionMilestone(type, Math.Max(1, dayNumber), description);
    }

    private SimulationSnapshot CaptureSnapshot(TurnipTestState state)
    {
        int processedGoodsAccessible = _processedGoodItemIds.Sum(state.GetAccessibleItemCount);
        int agedGoodsAccessible = _agedGoodItemIds.Sum(state.GetAccessibleItemCount);
        int processedGoodsProduced = _processedGoodItemIds.Sum(state.Stats.GetProcessedOutputCount);
        int agedGoodsProduced = _agedGoodItemIds.Sum(state.Stats.GetProcessedOutputCount);
        int opportunityItemAccessible = 0;

        if (state.ActiveMarketOpportunity is not null && !string.IsNullOrWhiteSpace(state.ActiveMarketOpportunity.ItemId))
            opportunityItemAccessible = state.GetAccessibleItemCount(state.ActiveMarketOpportunity.ItemId);

        return new SimulationSnapshot(
            state.DayNumber,
            state.FarmGrid.Width,
            state.FarmGrid.Height,
            state.Stats.SeedsBought,
            state.Stats.TotalCropsPlanted,
            state.Stats.TotalCropsHarvested,
            state.Stats.CropsSold,
            state.Stats.FarmExpansionsPurchased,
            state.Stats.MachineRunsStarted,
            state.Stats.MachineOutputsCollected,
            state.Stats.MarketPurchases,
            processedGoodsAccessible,
            agedGoodsAccessible,
            processedGoodsProduced,
            agedGoodsProduced,
            opportunityItemAccessible,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.ClearedRestorationSiteIds.Contains(TurnipTestState.OldJunkPileId, StringComparer.OrdinalIgnoreCase));
    }

    private readonly record struct SimulationSnapshot(
        int DayNumber,
        int FarmWidth,
        int FarmHeight,
        int SeedsBought,
        int CropsPlanted,
        int CropsHarvested,
        int CropsSold,
        int FarmExpansionsPurchased,
        int MachineRunsStarted,
        int MachineOutputsCollected,
        int MarketPurchases,
        int ProcessedGoodsAccessible,
        int AgedGoodsAccessible,
        int ProcessedGoodsProduced,
        int AgedGoodsProduced,
        int OpportunityItemAccessible,
        bool IsPlanterBotUnlocked,
        bool IsWateringBotUnlocked,
        bool IsHarvesterBotUnlocked,
        bool OldJunkPileCleared);
}
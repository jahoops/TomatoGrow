using System;
using System.Collections.Generic;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public sealed class GameStateValidationResult
{
    public GameStateValidationResult(IEnumerable<string> errors = null, IEnumerable<string> warnings = null)
    {
        Errors = (errors ?? [])
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Warnings = (warnings ?? [])
            .Where(warning => !string.IsNullOrWhiteSpace(warning))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }

    public static GameStateValidationResult Valid() => new();
}

public sealed class GameStateValidator
{
    private const int MaxExtraCropDaysWarning = 100;

    public GameStateValidationResult Validate(TurnipTestState state, ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(registry);

        List<string> errors = [];
        List<string> warnings = [];

        ValidateMoney(state, errors);
        ValidateDay(state, errors);
        ValidateFarmGrid(state, registry, errors, warnings);
        ValidateInventory(state.Inventory, registry, "player inventory", errors, warnings);
        ValidateInventory(state.SharedStorage, registry, "storage", errors, warnings);
        ValidateSelectedSeed(state, registry, errors);
        ValidateBots(state, registry, errors, warnings);
        ValidateMachines(state, registry, errors, warnings);
        ValidateStats(state.Stats, registry, errors, warnings);
        ValidateCleanupState(state, registry, errors);
        ValidateMarketOpportunity(state.ActiveMarketOpportunity, registry, errors);
        ValidateMarketOpportunityGenerator(state, errors);
        ValidateActivityLog(state, registry, errors);

        return new GameStateValidationResult(errors, warnings);
    }

    private static void ValidateMoney(TurnipTestState state, ICollection<string> errors)
    {
        if (state.Money < 0)
            errors.Add($"Money cannot be negative: {state.Money}.");
    }

    private static void ValidateDay(TurnipTestState state, ICollection<string> errors)
    {
        if (state.DayNumber < 1)
            errors.Add($"Day number must be at least 1: {state.DayNumber}.");
    }

    private static void ValidateFarmGrid(
        TurnipTestState state,
        ContentRegistry registry,
        ICollection<string> errors,
        ICollection<string> warnings)
    {
        FarmGrid farmGrid = state.FarmGrid;
        if (farmGrid.Width <= 0)
            errors.Add($"Farm width must be positive: {farmGrid.Width}.");

        if (farmGrid.Height <= 0)
            errors.Add($"Farm height must be positive: {farmGrid.Height}.");

        if (!IsInsideGrid(state.SelectedPlotX, state.SelectedPlotY, farmGrid))
            errors.Add($"Selected plot is outside the farm grid: ({state.SelectedPlotX},{state.SelectedPlotY}) in {farmGrid.Width}x{farmGrid.Height}.");

        List<FarmPlot> plots = farmGrid.GetAllPlots().ToList();
        if (plots.Count != farmGrid.Width * farmGrid.Height)
            errors.Add($"Farm grid returned {plots.Count} plots, expected {farmGrid.Width * farmGrid.Height}.");

        for (int y = 0; y < farmGrid.Height; y++)
        {
            for (int x = 0; x < farmGrid.Width; x++)
            {
                FarmPlot plot;
                try
                {
                    plot = farmGrid.GetPlot(x, y);
                }
                catch (Exception ex)
                {
                    errors.Add($"Farm grid is missing plot ({x},{y}): {ex.Message}");
                    continue;
                }

                if (plot is null)
                {
                    errors.Add($"Farm grid returned null for plot ({x},{y}).");
                    continue;
                }

                if (plot.X != x || plot.Y != y)
                    errors.Add($"Farm plot coordinates mismatch at ({x},{y}): plot reports ({plot.X},{plot.Y}).");

                if (plot.PlantedCrop is null)
                    continue;

                if (string.IsNullOrWhiteSpace(plot.PlantedCrop.CropDefinitionId) || !registry.TileProducersById.ContainsKey(plot.PlantedCrop.CropDefinitionId))
                {
                    errors.Add($"Farm plot ({x},{y}) references missing crop id '{plot.PlantedCrop.CropDefinitionId}'.");
                    continue;
                }

                TileProducerDefinition tileProducer = registry.GetTileProducer(plot.PlantedCrop.CropDefinitionId);
                if (plot.PlantedCrop.DaysGrown < 0)
                    errors.Add($"Farm plot ({x},{y}) has negative DaysGrown: {plot.PlantedCrop.DaysGrown}.");

                if (plot.PlantedCrop.DaysGrown > tileProducer.ProductionDays + MaxExtraCropDaysWarning)
                {
                    warnings.Add(
                        $"Farm plot ({x},{y}) has unusually high DaysGrown {plot.PlantedCrop.DaysGrown} for producer '{tileProducer.Id}' with production days {tileProducer.ProductionDays}.");
                }
            }
        }
    }

    private static void ValidateInventory(
        IReadOnlyDictionary<string, int> countsByItemId,
        ContentRegistry registry,
        string scopeName,
        ICollection<string> errors,
        ICollection<string> warnings)
    {
        foreach ((string itemId, int count) in countsByItemId)
        {
            if (count < 0)
                errors.Add($"{scopeName} contains negative count for '{itemId}': {count}.");

            if (string.IsNullOrWhiteSpace(itemId) || !registry.ItemsById.ContainsKey(itemId))
                errors.Add($"{scopeName} references missing item id '{itemId}'.");

            if (count == 0)
                warnings.Add($"{scopeName} contains zero-count entry for '{itemId}'.");
        }
    }

    private static void ValidateSelectedSeed(TurnipTestState state, ContentRegistry registry, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(state.SelectedSeedItemId))
        {
            errors.Add("Selected seed item id must be present.");
            return;
        }

        if (!registry.ItemsById.TryGetValue(state.SelectedSeedItemId, out ItemDefinition seedItem))
        {
            errors.Add($"Selected seed references missing item id '{state.SelectedSeedItemId}'.");
            return;
        }

        if (!registry.IsStarterItem(state.SelectedSeedItemId))
            errors.Add($"Selected seed item '{state.SelectedSeedItemId}' is not a valid starter item.");
    }

    private static void ValidateBots(
        TurnipTestState state,
        ContentRegistry registry,
        ICollection<string> errors,
        ICollection<string> warnings)
    {
        ValidatePlanterSeed(state.PlanterBot.AssignedSeedItemId, registry, errors);
        ValidateRegion("Planter Bot", state.PlanterBot.Region, state.FarmGrid, errors, warnings);
        ValidateRegion("Watering Bot", state.WateringBot.Region, state.FarmGrid, errors, warnings);
        ValidateRegion("Harvester Bot", state.HarvesterBot.Region, state.FarmGrid, errors, warnings);

        ValidateNonNegativeStat(state.Stats.BotsUnlocked, nameof(state.Stats.BotsUnlocked), errors);
        ValidateNonNegativeStat(state.Stats.CropsPlantedByPlanterBot, nameof(state.Stats.CropsPlantedByPlanterBot), errors);
        ValidateNonNegativeStat(state.Stats.PlotsWateredByWateringBot, nameof(state.Stats.PlotsWateredByWateringBot), errors);
        ValidateNonNegativeStat(state.Stats.CropsHarvestedByHarvesterBot, nameof(state.Stats.CropsHarvestedByHarvesterBot), errors);
    }

    private static void ValidateMachines(
        TurnipTestState state,
        ContentRegistry registry,
        ICollection<string> errors,
        ICollection<string> warnings)
    {
        bool hasMachineContent = registry.Machines.Count > 0 || registry.ProcessingRecipes.Count > 0;

        for (int index = 0; index < state.ProcessorMachines.Count; index++)
        {
            ProcessorMachineInstance machine = state.ProcessorMachines[index];
            string machineLabel = $"Machine[{index}]";

            if (machine is null)
            {
                errors.Add($"{machineLabel} is null.");
                continue;
            }

            bool hasRecipe = !string.IsNullOrWhiteSpace(machine.CurrentRecipeId);
            bool hasRunPayload = !string.IsNullOrWhiteSpace(machine.InputItemId)
                || !string.IsNullOrWhiteSpace(machine.OutputItemId)
                || machine.InputQuantity > 0
                || machine.OutputQuantity > 0;
            bool isIdle = !hasRecipe && !machine.HasOutputReady && machine.DaysRemaining == 0 && !hasRunPayload;

            if (string.IsNullOrWhiteSpace(machine.MachineId) || !registry.MachinesById.ContainsKey(machine.MachineId))
            {
                if (!hasMachineContent && isIdle)
                {
                    warnings.Add($"{machineLabel} machine id '{machine.MachineId}' is not defined because no machine content is loaded.");
                    continue;
                }

                errors.Add($"{machineLabel} references missing machine id '{machine.MachineId}'.");
                continue;
            }

            ProcessingRecipeDefinition recipe = null;
            if (hasRecipe)
            {
                if (!registry.ProcessingRecipesById.TryGetValue(machine.CurrentRecipeId, out recipe))
                {
                    errors.Add($"{machineLabel} references missing recipe id '{machine.CurrentRecipeId}'.");
                }
                else if (!string.Equals(recipe.MachineId, machine.MachineId, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"{machineLabel} machine '{machine.MachineId}' uses recipe '{machine.CurrentRecipeId}' for machine '{recipe.MachineId}'.");
                }
            }

            if (machine.DaysRemaining < 0)
                errors.Add($"{machineLabel} has negative DaysRemaining: {machine.DaysRemaining}.");

            if (!string.IsNullOrWhiteSpace(machine.InputItemId) && !registry.ItemsById.ContainsKey(machine.InputItemId))
                errors.Add($"{machineLabel} references missing input item id '{machine.InputItemId}'.");

            if (!string.IsNullOrWhiteSpace(machine.OutputItemId) && !registry.ItemsById.ContainsKey(machine.OutputItemId))
                errors.Add($"{machineLabel} references missing output item id '{machine.OutputItemId}'.");

            if (machine.InputQuantity < 0)
                errors.Add($"{machineLabel} has negative input quantity: {machine.InputQuantity}.");

            if (machine.OutputQuantity < 0)
                errors.Add($"{machineLabel} has negative output quantity: {machine.OutputQuantity}.");

            if (!hasRecipe)
            {
                if (machine.HasOutputReady || machine.DaysRemaining != 0 || hasRunPayload)
                    errors.Add($"{machineLabel} idle state is inconsistent for machine '{machine.MachineId}'.");

                continue;
            }

            if (machine.HasOutputReady)
            {
                if (machine.DaysRemaining != 0)
                    errors.Add($"{machineLabel} output-ready state must have DaysRemaining == 0.");

                if (string.IsNullOrWhiteSpace(machine.OutputItemId) || machine.OutputQuantity <= 0)
                    errors.Add($"{machineLabel} output-ready state must have output item id and positive output quantity.");
            }
            else
            {
                if (machine.DaysRemaining <= 0)
                    errors.Add($"{machineLabel} working state must have DaysRemaining > 0.");
            }

            if (recipe is not null)
            {
                if (string.IsNullOrWhiteSpace(machine.InputItemId) || machine.InputQuantity <= 0)
                    errors.Add($"{machineLabel} recipe-backed state must have input item id and positive input quantity.");

                if (string.IsNullOrWhiteSpace(machine.OutputItemId) || machine.OutputQuantity <= 0)
                    errors.Add($"{machineLabel} recipe-backed state must have output item id and positive output quantity.");
            }
        }
    }

    private static void ValidateStats(GameStats stats, ContentRegistry registry, ICollection<string> errors, ICollection<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(stats);

        ValidateNonNegativeStat(stats.DaysAdvanced, nameof(stats.DaysAdvanced), errors);
        ValidateNonNegativeStat(stats.MoneyEarnedFromSelling, nameof(stats.MoneyEarnedFromSelling), errors);
        ValidateNonNegativeStat(stats.MoneySpentOnSeeds, nameof(stats.MoneySpentOnSeeds), errors);
        ValidateNonNegativeStat(stats.MoneySpentOnBotUnlocks, nameof(stats.MoneySpentOnBotUnlocks), errors);
        ValidateNonNegativeStat(stats.MoneySpentOnExpansion, nameof(stats.MoneySpentOnExpansion), errors);
        ValidateNonNegativeStat(stats.SeedsBought, nameof(stats.SeedsBought), errors);
        ValidateNonNegativeStat(stats.CropsPlantedManually, nameof(stats.CropsPlantedManually), errors);
        ValidateNonNegativeStat(stats.CropsPlantedByPlanterBot, nameof(stats.CropsPlantedByPlanterBot), errors);
        ValidateNonNegativeStat(stats.PlotsWateredManually, nameof(stats.PlotsWateredManually), errors);
        ValidateNonNegativeStat(stats.PlotsWateredByWateringBot, nameof(stats.PlotsWateredByWateringBot), errors);
        ValidateNonNegativeStat(stats.CropsHarvestedManually, nameof(stats.CropsHarvestedManually), errors);
        ValidateNonNegativeStat(stats.CropsHarvestedByHarvesterBot, nameof(stats.CropsHarvestedByHarvesterBot), errors);
        ValidateNonNegativeStat(stats.CropsSold, nameof(stats.CropsSold), errors);
        ValidateNonNegativeStat(stats.FarmExpansionsPurchased, nameof(stats.FarmExpansionsPurchased), errors);
        ValidateNonNegativeStat(stats.BotsUnlocked, nameof(stats.BotsUnlocked), errors);
        ValidateNonNegativeStat(stats.FarmCyclesRun, nameof(stats.FarmCyclesRun), errors);
        ValidateNonNegativeStat(stats.AutoPlayStepsRun, nameof(stats.AutoPlayStepsRun), errors);
        ValidateNonNegativeStat(stats.MachineRunsStarted, nameof(stats.MachineRunsStarted), errors);
        ValidateNonNegativeStat(stats.MachineOutputsCollected, nameof(stats.MachineOutputsCollected), errors);
        ValidateNonNegativeStat(stats.MachineInputValueConsumed, nameof(stats.MachineInputValueConsumed), errors);
        ValidateNonNegativeStat(stats.MachineOutputValueCreated, nameof(stats.MachineOutputValueCreated), errors);
        ValidateNonNegativeStat(stats.ProcessedGoodsProduced, nameof(stats.ProcessedGoodsProduced), errors);
        ValidateNonNegativeStat(stats.MarketPurchases, nameof(stats.MarketPurchases), errors);
        ValidateNonNegativeStat(stats.MarketMoneySpent, nameof(stats.MarketMoneySpent), errors);
        ValidateNonNegativeStat(stats.MarketItemsBought, nameof(stats.MarketItemsBought), errors);
        ValidateNonNegativeStat(stats.CleanupSitesCleared, nameof(stats.CleanupSitesCleared), errors);
        ValidateNonNegativeStat(stats.MoneySpentOnCleanup, nameof(stats.MoneySpentOnCleanup), errors);

        if (stats.MachineValueAdded < 0)
            warnings.Add($"MachineValueAdded is negative: {stats.MachineValueAdded}.");

        foreach ((string itemId, int count) in stats.ProcessedOutputsByItemId)
        {
            if (count < 0)
                errors.Add($"Processed output total has negative count for '{itemId}': {count}.");

            if (string.IsNullOrWhiteSpace(itemId) || !registry.ItemsById.ContainsKey(itemId))
                errors.Add($"Processed output total references missing item id '{itemId}'.");
        }
    }

    private static void ValidateActivityLog(TurnipTestState state, ContentRegistry registry, ICollection<string> errors)
    {
        if (state.ActivityLog.Count > state.BalanceConfig.ActivityLogMaxEntries)
            errors.Add($"Activity log exceeds max length {state.BalanceConfig.ActivityLogMaxEntries}: {state.ActivityLog.Count}.");

        for (int index = 0; index < state.ActivityLog.Count; index++)
        {
            ActivityLogEntry entry = state.ActivityLog[index];
            if (entry.Day < 1 || entry.Day > state.DayNumber)
                errors.Add($"Activity log entry {index} has invalid day {entry.Day} for current day {state.DayNumber}.");

            if (!Enum.IsDefined(entry.Category))
                errors.Add($"Activity log entry {index} has invalid category '{entry.Category}'.");

            if (string.IsNullOrWhiteSpace(entry.Message))
                errors.Add($"Activity log entry {index} has blank message.");

            if (!string.IsNullOrWhiteSpace(entry.ItemId) && !registry.ItemsById.ContainsKey(entry.ItemId))
                errors.Add($"Activity log entry {index} references missing item id '{entry.ItemId}'.");
        }
    }

    private static void ValidateMarketOpportunity(MarketOpportunity marketOpportunity, ContentRegistry registry, ICollection<string> errors)
    {
        if (marketOpportunity is null)
            return;

        if (string.IsNullOrWhiteSpace(marketOpportunity.ItemId) || !registry.ItemsById.ContainsKey(marketOpportunity.ItemId))
            errors.Add($"Market opportunity references missing item id '{marketOpportunity.ItemId}'.");

        if (marketOpportunity.SellPriceMultiplier <= 0)
            errors.Add($"Market opportunity multiplier must be greater than 0: {marketOpportunity.SellPriceMultiplier}.");

        if (marketOpportunity.DaysRemaining < 0)
            errors.Add($"Market opportunity days remaining cannot be negative: {marketOpportunity.DaysRemaining}.");
    }

    private static void ValidateMarketOpportunityGenerator(TurnipTestState state, ICollection<string> errors)
    {
        if (state.LastGeneratedMarketOpportunityDay < 0)
            errors.Add($"Last generated market opportunity day cannot be negative: {state.LastGeneratedMarketOpportunityDay}.");

        if (state.LastGeneratedMarketOpportunityDay > state.DayNumber)
            errors.Add($"Last generated market opportunity day {state.LastGeneratedMarketOpportunityDay} cannot be after current day {state.DayNumber}.");

        if (state.GeneratedMarketOpportunityCount < 0)
            errors.Add($"Generated market opportunity count cannot be negative: {state.GeneratedMarketOpportunityCount}.");

        if (state.BalanceConfig.MarketOpportunityGenerationIntervalDays <= 0)
            errors.Add($"Market opportunity generation interval must be greater than 0: {state.BalanceConfig.MarketOpportunityGenerationIntervalDays}.");

        if (state.BalanceConfig.MarketOpportunityGeneratedDurationDays <= 0)
            errors.Add($"Market opportunity generated duration must be greater than 0: {state.BalanceConfig.MarketOpportunityGeneratedDurationDays}.");

        if (state.BalanceConfig.MarketOpportunityGeneratedMultiplier <= 0)
            errors.Add($"Market opportunity generated multiplier must be greater than 0: {state.BalanceConfig.MarketOpportunityGeneratedMultiplier}.");
    }

    private static void ValidateCleanupState(TurnipTestState state, ContentRegistry registry, ICollection<string> errors)
    {
        foreach (string clearedSiteId in state.ClearedRestorationSiteIds)
        {
            if (!registry.RestorationSitesById.ContainsKey(clearedSiteId))
                errors.Add($"Cleanup site id is invalid: '{clearedSiteId}'.");
        }

        RestorationSiteDefinition requiredSite = registry.RestorationSites
            .FirstOrDefault(site =>
                string.Equals(site.RewardType, "unlock_expansion_level", StringComparison.OrdinalIgnoreCase)
                && site.RewardValue == TurnipTestState.MaxFarmExpansionLevel);

        if (state.FarmGrid.Width > TurnipTestState.InitialFarmWidth + 1 && requiredSite is null)
        {
            errors.Add($"Farm width {state.FarmGrid.Width} requires a restoration site that unlocks expansion level {TurnipTestState.MaxFarmExpansionLevel}.");
            return;
        }

        if (state.FarmGrid.Width > TurnipTestState.InitialFarmWidth + 1 && requiredSite is not null && !state.ClearedRestorationSiteIds.Contains(requiredSite.Id, StringComparer.OrdinalIgnoreCase))
            errors.Add($"Farm width {state.FarmGrid.Width} requires {requiredSite.Name} to be cleared.");
    }

    private static void ValidatePlanterSeed(string seedItemId, ContentRegistry registry, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(seedItemId))
            return;

        if (!registry.ItemsById.TryGetValue(seedItemId, out ItemDefinition seedItem))
        {
            errors.Add($"Planter Bot assigned seed references missing item id '{seedItemId}'.");
            return;
        }

        if (!registry.IsStarterItem(seedItemId))
            errors.Add($"Planter Bot assigned seed '{seedItemId}' is not a valid starter item.");
    }

    private static void ValidateRegion(
        string regionName,
        FarmRegion region,
        FarmGrid farmGrid,
        ICollection<string> errors,
        ICollection<string> warnings)
    {
        if (region is null)
        {
            errors.Add($"{regionName} region is missing.");
            return;
        }

        if (region.Width <= 0 || region.Height <= 0)
        {
            errors.Add($"{regionName} region must have positive size: {region.Width}x{region.Height}.");
            return;
        }

        int intersectionWidth = Math.Min(region.X + region.Width, farmGrid.Width) - Math.Max(region.X, 0);
        int intersectionHeight = Math.Min(region.Y + region.Height, farmGrid.Height) - Math.Max(region.Y, 0);
        if (intersectionWidth <= 0 || intersectionHeight <= 0)
        {
            errors.Add($"{regionName} region does not intersect the farm grid: ({region.X},{region.Y}) {region.Width}x{region.Height}.");
            return;
        }

        if (region.X < 0 || region.Y < 0 || region.X + region.Width > farmGrid.Width || region.Y + region.Height > farmGrid.Height)
        {
            warnings.Add($"{regionName} region extends beyond the farm grid: ({region.X},{region.Y}) {region.Width}x{region.Height} on {farmGrid.Width}x{farmGrid.Height}.");
        }
    }

    private static void ValidateNonNegativeStat(int value, string statName, ICollection<string> errors)
    {
        if (value < 0)
            errors.Add($"Stat '{statName}' cannot be negative: {value}.");
    }

    private static bool IsInsideGrid(int x, int y, FarmGrid farmGrid)
    {
        return x >= 0 && x < farmGrid.Width && y >= 0 && y < farmGrid.Height;
    }
}

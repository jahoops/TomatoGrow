using System;
using System.Collections.Generic;
using System.Linq;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public sealed partial class AutoPlayTestController
{
    private bool TryExpandFarm(out string message)
    {
        if (!IsSeedSafetySatisfied)
        {
            message = "Auto: skipped expansion, seeds below safety target.";
            return false;
        }

        if (_state.IsFarmFullyExpanded || _state.Money < _state.NextFarmExpansionCost)
        {
            message = string.Empty;
            return false;
        }

        int widthBefore = _state.FarmGrid.Width;
        string result = _state.ExpandFarm();
        if (_state.FarmGrid.Width <= widthBefore)
        {
            message = string.Empty;
            return false;
        }

        Stats.RecordExpansion();
        message = "Auto: seed safety OK, considering expansion. " + result;
        return true;
    }

    private bool TryClearCleanupSite(out string message)
    {
        CleanupSiteState cleanupSite = _state.CleanupSite;

        if (!IsSeedSafetySatisfied)
        {
            message = "Auto: skipped cleanup, seeds below safety target.";
            return false;
        }

        if (!_state.IsFinalFarmExpansionBlockedByCleanup || cleanupSite is null || cleanupSite.IsCleared || _state.Money < cleanupSite.Cost)
        {
            message = string.Empty;
            return false;
        }

        string result = _state.ClearCleanupSite();
        if (!result.StartsWith("Cleared", StringComparison.Ordinal))
        {
            message = string.Empty;
            return false;
        }

        message = $"Auto: cleared {cleanupSite.Name} to unlock expansion. {result}";
        return true;
    }

    private bool TryUnlockBots(out string message)
    {
        if (!IsSeedSafetySatisfied)
        {
            message = "Auto: skipped bot unlock, seeds below safety target.";
            return false;
        }

        if (TryUnlockBot(_state.IsPlanterBotUnlocked, _state.BalanceConfig.PlanterBotCost, _state.UnlockPlanterBot, out message))
            return true;

        if (TryUnlockBot(_state.IsWateringBotUnlocked, _state.BalanceConfig.WateringBotCost, _state.UnlockWateringBot, out message))
            return true;

        if (TryUnlockBot(_state.IsHarvesterBotUnlocked, _state.BalanceConfig.HarvesterBotCost, _state.UnlockHarvesterBot, out message))
            return true;

        message = string.Empty;
        return false;
    }

    private bool TryUnlockBot(bool isUnlocked, int unlockCost, Func<string> unlockAction, out string message)
    {
        if (isUnlocked || _state.Money < unlockCost)
        {
            message = string.Empty;
            return false;
        }

        string result = unlockAction();
        if (!result.StartsWith("Unlocked", StringComparison.Ordinal))
        {
            message = string.Empty;
            return false;
        }

        Stats.RecordBotUnlocked();
        message = "Auto: " + result;
        return true;
    }

    private bool TryBuySeeds(out string message)
    {
        int requiredSeedCount = RequiredSeedSafetyCount;
        int totalSeedsBefore = CountSeeds();
        if (totalSeedsBefore >= requiredSeedCount)
        {
            message = string.Empty;
            return false;
        }

        if (!TryGetSeedItemToBuy(out string seedItemId))
        {
            message = $"Auto: buying seeds first, need {requiredSeedCount} total.";
            return false;
        }

        int totalSeedsBeforeBuy = CountSeeds();
        string result = _state.BuySeedItem(seedItemId);
        int boughtCount = CountSeeds() - totalSeedsBeforeBuy;

        if (boughtCount <= 0)
        {
            message = string.Empty;
            return false;
        }

        Stats.RecordSeedsBought(seedItemId, boughtCount);
        string seedName = _registry.GetItem(seedItemId).Name;
        string prefix = GetSeedPriorityMessage(seedItemId);
        message = $"{prefix}Auto: buying seeds first, need {requiredSeedCount} total. {result}";
        return true;
    }

    private bool TryHarvest(out string message)
    {
        if (_state.IsHarvesterBotUnlocked)
        {
            IReadOnlyDictionary<string, int> sellableBefore = GetSellableInventoryCounts();
            string result = _state.RunHarvesterBot();
            IReadOnlyDictionary<string, int> harvestedItemCounts = GetInventoryIncreases(sellableBefore, GetSellableInventoryCounts());
            int harvestedCount = harvestedItemCounts.Values.Sum();
            if (harvestedCount <= 0)
            {
                message = string.Empty;
                return false;
            }

            Stats.RecordHarvesterBotUse();
            Stats.RecordCropsHarvested(harvestedCount);
            Stats.RecordHarvestedItems(harvestedItemCounts);
            message = $"Auto: ran Harvester Bot, harvested {harvestedCount}.";
            return true;
        }

        if (!TryFindReadyPlot(out int plotX, out int plotY))
        {
            message = string.Empty;
            return false;
        }

        IReadOnlyDictionary<string, int> manualHarvestBefore = GetSellableInventoryCounts();
        SelectPlot(plotX, plotY);
        string harvestResult = _state.CollectSelectedTile();
        IReadOnlyDictionary<string, int> harvestedManualItemCounts = GetInventoryIncreases(manualHarvestBefore, GetSellableInventoryCounts());
        int harvestedCountFromInventory = harvestedManualItemCounts.Values.Sum();
        if (!harvestResult.StartsWith("Harvested", StringComparison.Ordinal))
        {
            message = string.Empty;
            return false;
        }

        Stats.RecordManualHarvest();
        Stats.RecordCropsHarvested(Math.Max(1, harvestedCountFromInventory));
        Stats.RecordHarvestedItems(harvestedManualItemCounts);
        message = $"Auto: manually harvested at {plotX},{plotY}.";
        return true;
    }

    private bool TrySell(out string message)
    {
        IReadOnlyDictionary<string, int> sellableBefore = GetSellableInventoryCounts();
        int sellableCountBefore = sellableBefore.Values.Sum();
        if (sellableCountBefore <= 0)
        {
            message = string.Empty;
            return false;
        }

        IReadOnlyDictionary<string, int> reservedInputs = GetReservedMachineInputQuantities();
        string result = _state.SellInventoryExceptReserved(reservedInputs);
        IReadOnlyDictionary<string, int> sellableAfter = GetSellableInventoryCounts();
        IReadOnlyDictionary<string, int> soldItemCounts = GetInventoryReductions(sellableBefore, sellableAfter);
        int soldCount = soldItemCounts.Values.Sum();
        if (soldCount <= 0)
        {
            message = string.Empty;
            return false;
        }

        Stats.RecordCropsSold(soldCount);
        Stats.RecordSoldItems(soldItemCounts);
        bool soldProcessedGoods = HasProcessedGoods(soldItemCounts);
        bool soldOpportunityItem = !string.IsNullOrWhiteSpace(GetActiveOpportunityItemId())
            && soldItemCounts.ContainsKey(GetActiveOpportunityItemId());
        message = soldOpportunityItem && soldProcessedGoods
            ? $"Auto: sold processed goods. {GetActiveOpportunityDisplayName()}: {result}"
            : soldOpportunityItem
                ? $"Auto: sold {_registry.GetItem(GetActiveOpportunityItemId()).Name} during {GetActiveOpportunityDisplayName()}. {result}"
                : soldProcessedGoods
                    ? "Auto: sold processed goods. " + result
                    : "Auto: " + result;
        return true;
    }

    private bool TryPlant(out string message)
    {
        if (!TryEnsurePlantableSeedSelected(out string selectedSeedItemId) || CountSeeds() <= 0 || !TryFindEmptyPlot(out int plotX, out int plotY))
        {
            message = string.Empty;
            return false;
        }

        if (_state.IsPlanterBotUnlocked)
        {
            int plantedBefore = CountPlantedPlots();
            string result = _state.RunPlanterBot();
            int plantedCount = CountPlantedPlots() - plantedBefore;
            if (plantedCount <= 0)
            {
                message = string.Empty;
                return false;
            }

            Stats.RecordPlanterBotUse();
            Stats.RecordPlantedSeeds(selectedSeedItemId, plantedCount);
            string prefix = GetSeedPriorityMessage(selectedSeedItemId);
            message = $"{prefix}Auto: ran Planter Bot, planted {plantedCount}.";
            return true;
        }

        SelectPlot(plotX, plotY);
        string plantResult = _state.StartSelectedTileProducer();
        if (!plantResult.StartsWith("Planted", StringComparison.Ordinal))
        {
            message = string.Empty;
            return false;
        }

        Stats.RecordManualPlant();
        Stats.RecordPlantedSeeds(selectedSeedItemId, 1);
        string plantMessagePrefix = GetSeedPriorityMessage(selectedSeedItemId);
        message = $"{plantMessagePrefix}Auto: manually planted at {plotX},{plotY}.";
        return true;
    }

    private bool TryWater(out string message)
    {
        if (!TryFindDryPlantedPlot(out int plotX, out int plotY))
        {
            message = string.Empty;
            return false;
        }

        if (_state.IsWateringBotUnlocked)
        {
            int dryBefore = CountDryPlantedPlots();
            string result = _state.RunWateringBot();
            int wateredCount = dryBefore - CountDryPlantedPlots();
            if (wateredCount <= 0)
            {
                message = string.Empty;
                return false;
            }

            Stats.RecordWateringBotUse();
            message = $"Auto: ran Watering Bot, watered {wateredCount}.";
            return true;
        }

        SelectPlot(plotX, plotY);
        string waterResult = _state.MaintainSelectedTile();
        if (!waterResult.StartsWith("Watered", StringComparison.Ordinal))
        {
            message = string.Empty;
            return false;
        }

        Stats.RecordManualWater();
        message = $"Auto: manually watered at {plotX},{plotY}.";
        return true;
    }

    private string AdvanceDay()
    {
        string result = _state.AdvanceDay();
        Stats.RecordDayAdvanced();
        return "Auto: " + result;
    }

    private bool TryCollectMachineOutput(out string message)
    {
        ProcessorMachineInstance machine = GetProcessorMachinesByStartPriority()
            .FirstOrDefault(candidate => candidate.HasOutputReady);

        if (machine is null)
        {
            message = string.Empty;
            return false;
        }

        string outputName = string.IsNullOrWhiteSpace(machine.OutputItemId)
            ? "output"
            : _registry.GetItem(machine.OutputItemId).Name;
        string result = _state.CollectProcessorMachineOutput(machine.MachineId);
        if (!result.StartsWith("Collected", StringComparison.Ordinal))
        {
            message = string.Empty;
            return false;
        }

        message = $"Auto: collected {outputName}.";
        return true;
    }

    private bool TryStartMachine(out string message)
    {
        ProcessorMachineInstance machine = GetProcessorMachinesByStartPriority()
            .FirstOrDefault(candidate => !candidate.IsWorking && !candidate.HasOutputReady && CanStartAnyMachineRecipe(candidate.MachineId));

        if (machine is null)
        {
            message = string.Empty;
            return false;
        }

        ProcessingRecipeDefinition recipe = ChooseMachineRecipeForAutoplay(machine.MachineId);
        if (recipe is null)
        {
            message = string.Empty;
            return false;
        }

        string machineActionName = GetMachineActionName(machine.MachineId);
        string result = _state.StartProcessorMachineFromInventory(machine.MachineId, recipe.Id);
        if (!result.StartsWith($"Started {machineActionName}", StringComparison.Ordinal))
        {
            message = string.Empty;
            return false;
        }

        message = string.Equals(recipe.OutputItemId, GetActiveOpportunityItemId(), StringComparison.OrdinalIgnoreCase)
            ? $"Auto: started {machineActionName} for {GetActiveOpportunityDisplayName()}."
            : $"Auto: started {machineActionName}.";
        return true;
    }

    private bool HasMeaningfulDayAdvanceOpportunity()
    {
        return _state.FarmGrid.GetAllPlots().Any(plot => !plot.IsEmpty)
               || CountSellableCrops() > 0
               || _state.ProcessorMachines.Any(machine => machine.IsWorking);
    }

    private bool CanStartAnyMachineRecipe(string machineId)
    {
        return ChooseMachineRecipeForAutoplay(machineId) is not null;
    }

    private IReadOnlyDictionary<string, int> GetReservedMachineInputQuantities()
    {
        Dictionary<string, int> reserved = new(StringComparer.OrdinalIgnoreCase);
        foreach (ProcessorMachineInstance machine in _state.ProcessorMachines.Where(candidate => _registry.MachinesById.ContainsKey(candidate.MachineId)))
        {
            foreach (ProcessingRecipeDefinition recipe in _registry.GetProcessingRecipesForMachine(machine.MachineId))
            {
                if (!ShouldReserveItemForLaterProcessing(recipe.InputItemId))
                    continue;

                if (_state.GetAccessibleItemCount(recipe.InputItemId) < recipe.InputQuantity)
                    continue;

                reserved[recipe.InputItemId] = Math.Max(
                    reserved.TryGetValue(recipe.InputItemId, out int currentReserved) ? currentReserved : 0,
                    recipe.InputQuantity);
            }
        }

        return reserved;
    }

    private IEnumerable<ProcessorMachineInstance> GetProcessorMachinesByStartPriority()
    {
        string activeOpportunityItemId = GetActiveOpportunityItemId();
        HashSet<string> outputItemIds = _registry.ProcessingRecipesById.Values
            .Select(recipe => recipe.OutputItemId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _state.ProcessorMachines
            .Where(machine => _registry.MachinesById.ContainsKey(machine.MachineId))
            .OrderByDescending(machine => !string.IsNullOrWhiteSpace(activeOpportunityItemId)
                && _registry.GetProcessingRecipesForMachine(machine.MachineId)
                    .Any(recipe => string.Equals(recipe.OutputItemId, activeOpportunityItemId, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(machine => _registry.GetProcessingRecipesForMachine(machine.MachineId)
                .Any(recipe => outputItemIds.Contains(recipe.InputItemId)))
            .ThenBy(machine => _registry.GetMachine(machine.MachineId).Name, StringComparer.OrdinalIgnoreCase);
    }

    private string GetMachineActionName(string machineId)
    {
        return _registry.GetMachine(machineId).Name.ToLowerInvariant();
    }

    private bool TryGetSeedItemToBuy(out string seedItemId)
    {
        foreach (string preferredSeedItemId in EnumerateSeedPreferenceFromCursor())
        {
            if (!CanAffordSeed(preferredSeedItemId))
                continue;

            seedItemId = preferredSeedItemId;
            AdvanceSeedPreferenceCursor(preferredSeedItemId);
            return true;
        }

        ItemDefinition cheapestAffordableSeed = _registry.Items
            .Where(item => _registry.IsStarterItem(item.Id))
            .Where(item => item.BuyPrice > 0 && item.BuyPrice <= _state.Money)
            .OrderBy(item => item.BuyPrice)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (cheapestAffordableSeed is not null)
        {
            seedItemId = cheapestAffordableSeed.Id;
            AdvanceSeedPreferenceCursor(seedItemId);
            return true;
        }

        seedItemId = string.Empty;
        return false;
    }

    private bool TryEnsurePlantableSeedSelected(out string selectedSeedItemId)
    {
        foreach (string preferredSeedItemId in EnumerateSeedPreferenceFromCursor())
        {
            if (!_registry.TryGetTileProducerByStarterItemId(preferredSeedItemId, out _))
                continue;

            if (_state.GetItemCount(preferredSeedItemId) <= 0)
                continue;

            SelectSeedItem(preferredSeedItemId);
            AdvanceSeedPreferenceCursor(preferredSeedItemId);
            selectedSeedItemId = preferredSeedItemId;
            return true;
        }

        selectedSeedItemId = string.Empty;
        return false;
    }

    private bool CanAffordSeed(string seedItemId)
    {
        ItemDefinition selectedSeed = _registry.GetItem(seedItemId);
        return _registry.IsStarterItem(selectedSeed.Id)
               && selectedSeed.BuyPrice > 0
               && _state.Money >= selectedSeed.BuyPrice;
    }

    private IEnumerable<string> EnumerateSeedPreferenceFromCursor()
    {
        IReadOnlyList<string> seedPreferenceOrder = GetAutoplaySeedPreferenceOrder();
        if (seedPreferenceOrder.Count == 0)
            yield break;

        for (int offset = 0; offset < seedPreferenceOrder.Count; offset++)
            yield return seedPreferenceOrder[(_seedPreferenceCursor + offset) % seedPreferenceOrder.Count];
    }

    private void AdvanceSeedPreferenceCursor(string seedItemId)
    {
        IReadOnlyList<string> seedPreferenceOrder = GetAutoplaySeedPreferenceOrder();
        int selectedIndex = seedPreferenceOrder
            .Select((candidateSeedItemId, index) => (candidateSeedItemId, index))
            .FirstOrDefault(entry => string.Equals(entry.candidateSeedItemId, seedItemId, StringComparison.OrdinalIgnoreCase))
            .index;

        if (seedPreferenceOrder.Count == 0)
            return;

        _seedPreferenceCursor = (selectedIndex + 1) % seedPreferenceOrder.Count;
    }

    private void SelectSeedItem(string seedItemId)
    {
        if (string.Equals(_state.SelectedSeedItemId, seedItemId, StringComparison.OrdinalIgnoreCase))
            return;

        for (int index = 0; index < _state.SeedItemIds.Count; index++)
        {
            _state.SelectNextSeed();
            if (string.Equals(_state.SelectedSeedItemId, seedItemId, StringComparison.OrdinalIgnoreCase))
                return;
        }
    }

    private bool IsProcessorInputSeed(string seedItemId)
    {
        return GetSeedItemIdsForProcessorInputs().Contains(seedItemId, StringComparer.OrdinalIgnoreCase);
    }

    private ProcessingRecipeDefinition ChooseMachineRecipeForAutoplay(string machineId)
    {
        string activeOpportunityItemId = GetActiveOpportunityItemId();

        return _registry.GetProcessingRecipesForMachine(machineId)
            .Select((recipe, index) => new { recipe, index })
            .Where(entry => _state.GetAccessibleItemCount(entry.recipe.InputItemId) >= entry.recipe.InputQuantity)
            .Where(entry => IsRecipeEligibleForAutoplay(entry.recipe))
            .OrderByDescending(entry => !string.IsNullOrWhiteSpace(activeOpportunityItemId)
                && string.Equals(entry.recipe.OutputItemId, activeOpportunityItemId, StringComparison.OrdinalIgnoreCase))
            .ThenBy(entry => entry.index)
            .Select(entry => entry.recipe)
            .FirstOrDefault();
    }

    private bool IsRecipeEligibleForAutoplay(ProcessingRecipeDefinition recipe)
    {
        string activeOpportunityItemId = GetActiveOpportunityItemId();
        if (string.IsNullOrWhiteSpace(activeOpportunityItemId))
            return true;

        return !string.Equals(recipe.InputItemId, activeOpportunityItemId, StringComparison.OrdinalIgnoreCase)
               || string.Equals(recipe.OutputItemId, activeOpportunityItemId, StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldReserveItemForLaterProcessing(string itemId)
    {
        string activeOpportunityItemId = GetActiveOpportunityItemId();
        if (string.IsNullOrWhiteSpace(activeOpportunityItemId))
            return true;

        return !string.Equals(itemId, activeOpportunityItemId, StringComparison.OrdinalIgnoreCase);
    }

    private string GetSeedPriorityMessage(string seedItemId)
    {
        if (GetSeedItemIdsSupportingOpportunity().Contains(seedItemId, StringComparer.OrdinalIgnoreCase))
            return $"Auto: prioritizing {_registry.GetItem(seedItemId).Name} for {GetActiveOpportunityDisplayName()}. ";

        return IsProcessorInputSeed(seedItemId)
            ? $"Auto: selected {_registry.GetItem(seedItemId).Name} for processing. "
            : string.Empty;
    }

    private string GetActiveOpportunityDisplayName()
    {
        return _state.ActiveMarketOpportunity?.DisplayName ?? "market opportunity";
    }

    private bool HasProcessedGoods(IReadOnlyDictionary<string, int> itemCounts)
    {
        return itemCounts.Any(entry => string.Equals(_registry.GetItem(entry.Key).Type, "processed_good", StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyDictionary<string, int> GetSellableInventoryCounts()
    {
        Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);
        foreach (ItemDefinition item in _registry.Items)
        {
            if (_registry.IsStarterItem(item.Id) || item.SellPrice <= 0)
                continue;

            int count = _state.GetAccessibleItemCount(item.Id);
            if (count <= 0)
                continue;

            counts[item.Id] = count;
        }

        return counts;
    }

    private static IReadOnlyDictionary<string, int> GetInventoryIncreases(
        IReadOnlyDictionary<string, int> before,
        IReadOnlyDictionary<string, int> after)
    {
        return GetInventoryDelta(before, after, countDelta => countDelta > 0);
    }

    private static IReadOnlyDictionary<string, int> GetInventoryReductions(
        IReadOnlyDictionary<string, int> before,
        IReadOnlyDictionary<string, int> after)
    {
        return GetInventoryDelta(before, after, countDelta => countDelta < 0)
            .ToDictionary(entry => entry.Key, entry => Math.Abs(entry.Value), StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, int> GetInventoryDelta(
        IReadOnlyDictionary<string, int> before,
        IReadOnlyDictionary<string, int> after,
        Func<int, bool> includeDelta)
    {
        HashSet<string> itemIds = before.Keys.Concat(after.Keys).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> delta = new(StringComparer.OrdinalIgnoreCase);

        foreach (string itemId in itemIds)
        {
            int beforeCount = before.TryGetValue(itemId, out int beforeValue) ? beforeValue : 0;
            int afterCount = after.TryGetValue(itemId, out int afterValue) ? afterValue : 0;
            int countDelta = afterCount - beforeCount;
            if (!includeDelta(countDelta))
                continue;

            delta[itemId] = countDelta;
        }

        return delta;
    }

    private int CountSeeds()
    {
        return _state.SeedItemIds.Sum(_state.GetItemCount);
    }

    private int CountSellableCrops()
    {
        return _registry.Items
            .Where(item => !_registry.IsStarterItem(item.Id) && item.SellPrice > 0)
            .Sum(item => _state.GetAccessibleItemCount(item.Id));
    }

    private int CountPlantedPlots()
    {
        return _state.FarmGrid.GetAllPlots().Count(plot => !plot.IsEmpty);
    }

    private int CountDryPlantedPlots()
    {
        return _state.FarmGrid.GetAllPlots().Count(plot => !plot.IsEmpty && !plot.IsWatered);
    }

    private void SelectPlot(int plotX, int plotY)
    {
        _state.MoveSelectedPlot(plotX - _state.SelectedPlotX, plotY - _state.SelectedPlotY);
    }

    private bool TryFindEmptyPlot(out int plotX, out int plotY)
    {
        foreach (FarmPlot plot in _state.FarmGrid.GetAllPlots())
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

    private bool TryFindDryPlantedPlot(out int plotX, out int plotY)
    {
        foreach (FarmPlot plot in _state.FarmGrid.GetAllPlots())
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

    private bool TryFindReadyPlot(out int plotX, out int plotY)
    {
        foreach (FarmPlot plot in _state.FarmGrid.GetAllPlots())
        {
            if (!_state.IsPlotHarvestable(plot.X, plot.Y))
                continue;

            plotX = plot.X;
            plotY = plot.Y;
            return true;
        }

        plotX = -1;
        plotY = -1;
        return false;
    }

    private bool TryBuyMarketInputForIdleMachine(out string message)
    {
        if (_marketPurchasesThisCycle > 0)
        {
            message = string.Empty;
            return false;
        }

        MarketPurchaseCandidate? candidate = GetBestMarketPurchaseCandidate();
        if (candidate is null)
        {
            message = string.Empty;
            return false;
        }

        string result = _state.BuyMarketOffer(candidate.Value.Offer.Id);
        if (!result.StartsWith("Bought", StringComparison.Ordinal))
        {
            message = string.Empty;
            return false;
        }

        _marketPurchasesThisCycle++;
        _marketOffersPurchasedThisRun.Add(candidate.Value.Offer.Id);
        string itemName = _registry.GetItem(candidate.Value.Offer.ItemId).Name;
        string machineName = _registry.GetMachine(candidate.Value.Recipe.MachineId).Name;
        string machineActionName = GetMachineActionName(candidate.Value.Recipe.MachineId);
        string startResult = _state.StartProcessorMachineFromInventory(candidate.Value.Recipe.MachineId);
        if (startResult.StartsWith($"Started {machineActionName}", StringComparison.Ordinal))
        {
            message = $"Auto: bought {candidate.Value.Offer.Quantity} {itemName} from market for processing. Auto: started {machineActionName} with market {itemName}.";
            return true;
        }

        message = $"Auto: bought {candidate.Value.Offer.Quantity} {itemName} from market for {machineName}.";
        return true;
    }

    private MarketPurchaseCandidate? GetBestMarketPurchaseCandidate()
    {
        if (!GetIdleProcessorMachines().Any())
            return null;

        int protectedSeedMoney = GetProtectedSeedMoney();
        MarketPurchaseCandidate? bestCandidate = null;

        foreach (MarketOfferDefinition marketOffer in _state.MarketOffers)
        {
            if (_marketOffersPurchasedThisRun.Contains(marketOffer.Id))
                continue;

            if (_state.Money < marketOffer.Price)
                continue;

            if ((_state.Money - marketOffer.Price) < protectedSeedMoney)
                continue;

            foreach (ProcessingRecipeDefinition recipe in _registry.ProcessingRecipes.Where(candidate =>
                         string.Equals(candidate.InputItemId, marketOffer.ItemId, StringComparison.OrdinalIgnoreCase)
                         && candidate.InputQuantity > 0))
            {
                if (!IsRecipeEligibleForAutoplay(recipe))
                    continue;

                if (!HasIdleMachineForRecipe(recipe.MachineId))
                    continue;

                if (_state.GetAccessibleItemCount(recipe.InputItemId) >= recipe.InputQuantity)
                    continue;

                DebugUiTextFormatter.MarketOfferProcessingEconomics? economics = DebugUiTextFormatter.CalculateMarketOfferProcessingEconomics(
                    _registry,
                    marketOffer,
                    recipe,
                    _state.GetEffectiveSellPrice);
                if (economics is null || economics.Value.ProfitIfBought < 0)
                    continue;

                MarketPurchaseCandidate candidate = new(marketOffer, recipe, economics.Value.ProfitIfBought, economics.Value.OutputValue);
                if (bestCandidate is null
                    || candidate.ProfitIfBought > bestCandidate.Value.ProfitIfBought
                    || (candidate.ProfitIfBought == bestCandidate.Value.ProfitIfBought
                        && string.Equals(candidate.Recipe.OutputItemId, GetActiveOpportunityItemId(), StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(bestCandidate.Value.Recipe.OutputItemId, GetActiveOpportunityItemId(), StringComparison.OrdinalIgnoreCase))
                    || (candidate.ProfitIfBought == bestCandidate.Value.ProfitIfBought
                        && candidate.OutputValue > bestCandidate.Value.OutputValue)
                    || (candidate.ProfitIfBought == bestCandidate.Value.ProfitIfBought
                        && candidate.OutputValue == bestCandidate.Value.OutputValue
                        && string.Compare(candidate.Offer.Id, bestCandidate.Value.Offer.Id, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    bestCandidate = candidate;
                }
            }
        }

        return bestCandidate;
    }

    private IEnumerable<ProcessorMachineInstance> GetIdleProcessorMachines()
    {
        return GetProcessorMachinesByStartPriority()
            .Where(machine => !machine.IsWorking && !machine.HasOutputReady);
    }

    private bool HasIdleMachineForRecipe(string machineId)
    {
        return GetIdleProcessorMachines().Any(machine => string.Equals(machine.MachineId, machineId, StringComparison.OrdinalIgnoreCase));
    }

    private int GetProtectedSeedMoney()
    {
        int missingSeeds = Math.Max(0, RequiredSeedSafetyCount - CurrentSeedCount);
        if (missingSeeds <= 0)
            return 0;

        int cheapestSeedPrice = _registry.GetStarterItems()
            .Where(item => item.BuyPrice > 0)
            .Select(item => item.BuyPrice)
            .DefaultIfEmpty(0)
            .Min();

        return missingSeeds * cheapestSeedPrice;
    }

    private readonly record struct MarketPurchaseCandidate(
        MarketOfferDefinition Offer,
        ProcessingRecipeDefinition Recipe,
        int ProfitIfBought,
        int OutputValue);
}

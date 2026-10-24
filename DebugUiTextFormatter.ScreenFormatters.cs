using System;
using System.Collections.Generic;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public static partial class DebugUiTextFormatter
{
    public static string[] GetInventoryScreenLines(ContentRegistry registry, Func<string, int> getCount, bool includeDebugDetails)
    {
        return GetInventoryScreenLines(registry, getCount, _ => 0, includeDebugDetails);
    }

    public static string[] GetInventoryScreenLines(
        ContentRegistry registry,
        Func<string, int> getPlayerCount,
        Func<string, int> getStorageCount,
        bool includeDebugDetails,
        bool isAutoPlayRunning = false,
        bool isPaused = false,
        Func<string, int> getSellPrice = null,
        ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        List<string> lines =
        [
            "PLAYER INVENTORY"
        ];

        lines.AddRange(GetInventoryLines(registry, getPlayerCount, includeDebugDetails, getSellPrice));
        lines.Add(string.Empty);
        lines.Add($"SHARED {theme.ToUpperLabel(theme.StorageName)} SUMMARY");
        lines.Add($"Items: {GetInventoryItemCount(registry, getStorageCount)} | Value: ${GetEstimatedSellValue(registry, getStorageCount, getSellPrice)}");
        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "F8: storage | F9: computer | F4: help"));
        return lines.ToArray();
    }

    public static string[] GetStorageScreenLines(ContentRegistry registry, Func<string, int> getCount, bool includeDebugDetails, bool isAutoPlayRunning = false, bool isPaused = false, Func<string, int> getSellPrice = null, ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        List<string> lines =
        [
            $"SHARED {theme.ToUpperLabel(theme.StorageName)}",
            .. GetStorageLines(registry, getCount, includeDebugDetails, getSellPrice),
        ];

        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "C: deposit | U: withdraw | S: sell", "F9: computer | F4: help"));
        return lines.ToArray();
    }

    public static string[] GetMarketScreenLines(
        ContentRegistry registry,
        IReadOnlyList<MarketOfferDefinition> marketOffers,
        int money,
        bool includeDebugDetails,
        bool isAutoPlayRunning = false,
        bool isPaused = false,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null)
    {
        List<string> lines =
        [
            "SUPPLY MARKET",
            $"Money: ${money}",
            "Buy outside inputs. Home-grown inputs keep better margins."
        ];

        string opportunityLine = FormatMarketOpportunityLine(registry, activeMarketOpportunity);
        if (!string.IsNullOrWhiteSpace(opportunityLine))
        {
            lines.Add("Market Opportunity:");
            lines.Add(opportunityLine);
        }

        lines.Add(string.Empty);
        lines.Add("OFFERS");

        if (marketOffers.Count == 0)
        {
            lines.Add("No market offers available.");
        }
        else
        {
            for (int index = 0; index < Math.Min(9, marketOffers.Count); index++)
                lines.AddRange(GetMarketOfferDisplayLines(registry, marketOffers[index], index + 1, money, includeDebugDetails, getSellPrice));
        }

        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "1-9: buy offer | I: inventory | F8: storage", "F9: computer | F4: help"));
        return lines.ToArray();
    }

    public static string[] GetComputerScreenLines(
        ContentRegistry registry,
        FarmGrid farmGrid,
        IReadOnlyList<ProcessorMachineInstance> machines,
        IReadOnlyList<ActivityLogEntry> activityLog,
        SaveLoadStatus saveLoadStatus,
        GameStats stats,
        int dayNumber,
        int money,
        string selectedSeedItemId,
        bool isPlanterBotUnlocked,
        bool isWateringBotUnlocked,
        bool isHarvesterBotUnlocked,
        bool isFarmFullyExpanded,
        int nextFarmExpansionCost,
        bool isFinalFarmExpansionBlockedByCleanup,
        Func<string, int> getPlayerCount,
        Func<string, int> getStorageCount,
        Func<string, int> getAccessibleCount,
        Func<FarmPlot, bool> isPlotReady,
        bool includeDebugDetails = false,
        bool isAutoPlayRunning = false,
        bool isPaused = false,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null,
        IReadOnlyList<CleanupSiteState> cleanupSites = null,
        GameBalanceConfig balanceConfig = null,
        ThemeVocabulary themeVocabulary = null,
        int daysUntilNextOpportunity = 0,
        string nextCandidateItemId = null,
        IReadOnlyList<ProductionArea> productionAreas = null)
    {
        GameBalanceConfig effectiveConfig = balanceConfig ?? GameBalanceConfig.Default;
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        List<string> lines =
        [
            .. GetGlobalInventoryLines(registry, getPlayerCount, getStorageCount, includeDebugDetails, getSellPrice),
            string.Empty,
            .. GetGlobalValueSummaryLines(registry, money, getPlayerCount, getStorageCount, getSellPrice),
            string.Empty,
            .. GetComputerCleanupLines(cleanupSites, money),
            string.Empty,
            .. GetComputerMarketSummaryLines(registry, activeMarketOpportunity, getSellPrice, daysUntilNextOpportunity, nextCandidateItemId),
            string.Empty,
            .. GetProductionChainSummaryLines(registry),
            string.Empty,
            .. GetComputerMachineSummaryLines(registry, machines, stats),
            string.Empty,
            .. GetComputerBotSummaryLines(money, isPlanterBotUnlocked, isWateringBotUnlocked, isHarvesterBotUnlocked, stats, effectiveConfig, theme),
            string.Empty,
            .. GetComputerFarmSummaryLines(farmGrid, dayNumber, isFarmFullyExpanded, nextFarmExpansionCost, isPlotReady, theme),
            string.Empty,
            .. GetComputerProductionAreaLines(productionAreas),
            string.Empty,
            .. GetHintSectionLines(
                registry,
                farmGrid,
                machines,
                money,
                selectedSeedItemId,
                isPlanterBotUnlocked,
                isWateringBotUnlocked,
                isHarvesterBotUnlocked,
                isFarmFullyExpanded,
                nextFarmExpansionCost,
                isFinalFarmExpansionBlockedByCleanup,
                cleanupSites?.FirstOrDefault(site => !site.IsCleared) ?? cleanupSites?.FirstOrDefault(),
                getPlayerCount,
                getStorageCount,
                getAccessibleCount,
                isPlotReady,
                activeMarketOpportunity,
                getSellPrice,
                isPaused,
                isLoadConfirmationPending: false,
                maxHints: 3,
                balanceConfig: effectiveConfig,
                themeVocabulary: theme)
        ];

        if (saveLoadStatus is not null)
        {
            lines.Add(string.Empty);
            lines.AddRange(GetSaveStatusLines(saveLoadStatus, includeHeader: true));
        }

        string[] recentActivityLines = GetRecentActivityLines(activityLog, 3);
        if (recentActivityLines.Length > 0)
        {
            lines.Add(string.Empty);
            lines.Add("RECENT ACTIVITY");
            lines.AddRange(recentActivityLines);
        }

        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "Live farm overview | Shift+F9: production | I: inventory", "F8: storage | F10: market | F11: ledger | F4: help"));

        return lines.ToArray();
    }

    public static string[] GetComputerProductionAreaLines(IReadOnlyList<ProductionArea> productionAreas)
    {
        IReadOnlyList<ProductionArea> areas = productionAreas ?? [];
        if (areas.Count == 0)
            return ["PRODUCTION AREAS", "No production areas available."];

        List<string> lines = ["PRODUCTION AREAS"];
        lines.AddRange(areas
            .OrderBy(area => area.DisplayOrder)
            .ThenBy(area => area.Id, StringComparer.OrdinalIgnoreCase)
            .Select(area => $"{area.Name}: {(area.IsUnlocked ? "unlocked" : "locked")}, {area.TileCount} tiles"));
        return lines.ToArray();
    }

    public static string[] GetProductionReportScreenLines(
        ContentRegistry registry,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null,
        bool isAutoPlayRunning = false,
        bool isPaused = false)
    {
        List<string> lines =
        [
            .. GetProductionChainLines(registry, activeMarketOpportunity, getSellPrice, indentDetails: true)
        ];

        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "Detailed production routes | F9: computer | F10: market", "F11: ledger | F4: help"));
        return lines.ToArray();
    }

    public static string[] GetActivityLogScreenLines(
        IReadOnlyList<ActivityLogEntry> activityLog,
        bool isAutoPlayRunning = false,
        bool isPaused = false,
        ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        List<string> lines =
        [
            theme.ToUpperLabel(theme.ActivityLogName)
        ];

        string[] recentActivityLines = GetRecentActivityLines(activityLog, 12);
        if (recentActivityLines.Length == 0)
            lines.Add("No recent activity.");
        else
            lines.AddRange(recentActivityLines);

        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "Live updates while open | F9: computer | F4: help"));
        return lines.ToArray();
    }

    public static string[] GetRecentActivityLines(IReadOnlyList<ActivityLogEntry> activityLog, int count)
    {
        if (activityLog is null || count <= 0 || activityLog.Count == 0)
            return [];

        return activityLog
            .Reverse()
            .Take(count)
            .Select(FormatActivityLogEntry)
            .ToArray();
    }

    public static string[] GetGlobalInventoryLines(
        ContentRegistry registry,
        Func<string, int> getPlayerCount,
        Func<string, int> getStorageCount,
        bool includeDebugDetails = false,
        Func<string, int> getSellPrice = null)
    {
        List<string> lines = ["GLOBAL INVENTORY"];

        foreach (ItemDefinition item in registry.Items
                     .Where(item => getPlayerCount(item.Id) + getStorageCount(item.Id) > 0)
                     .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            int playerCount = getPlayerCount(item.Id);
            int storageCount = getStorageCount(item.Id);
            int totalCount = playerCount + storageCount;
            string line = $"{item.Name} x{totalCount} | Player {playerCount} / Storage {storageCount}";

            if (IsSellableInventoryItem(registry, item))
            {
                int displaySellPrice = GetDisplaySellPrice(item, getSellPrice);
                line += $" | ${displaySellPrice} each | ${totalCount * displaySellPrice} total";
            }

            if (includeDebugDetails)
                line += $" | id: {item.Id}";

            lines.Add(line);
        }

        if (lines.Count == 1)
            lines.Add("No items in player inventory or shared storage.");

        return lines.ToArray();
    }

    public static string[] GetProgressionReportScreenLines(
        ProgressionSimulationResult result,
        bool isAutoPlayRunning = false,
        bool isPaused = false,
        ThemeVocabulary themeVocabulary = null)
    {
        List<string> lines =
        [
            .. ProgressionReportFormatter.Format(result, themeVocabulary)
        ];

        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "R: run simulation | Shift+F10: progression", "F9: computer | F10: market | F4: help"));
        return lines.ToArray();
    }

    public static string[] GetGlobalValueSummaryLines(
        ContentRegistry registry,
        int money,
        Func<string, int> getPlayerCount,
        Func<string, int> getStorageCount,
        Func<string, int> getSellPrice = null)
    {
        int playerSellableValue = GetEstimatedSellValue(registry, getPlayerCount, getSellPrice);
        int storageSellableValue = GetEstimatedSellValue(registry, getStorageCount, getSellPrice);
        int totalSellableValue = playerSellableValue + storageSellableValue;

        return
        [
            "VALUES",
            $"Money: ${money}",
            $"Player sellable value: ${playerSellableValue}",
            $"Storage sellable value: ${storageSellableValue}",
            $"Total sellable value: ${totalSellableValue}",
            $"Potential money: ${money + totalSellableValue}"
        ];
    }

    public static string[] GetComputerMarketSummaryLines(
        ContentRegistry registry,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null,
        int daysUntilNextOpportunity = 0,
        string nextCandidateItemId = null)
    {
        List<string> lines =
        [
            "MARKET OPPORTUNITY",
            $"Offers available: {registry.MarketOffers.Count}"
        ];

        MarketOpportunitySummary? summary = GetMarketOpportunitySummary(registry, activeMarketOpportunity, getSellPrice, daysUntilNextOpportunity, nextCandidateItemId);
        if (summary is not null && !string.IsNullOrWhiteSpace(summary.Value.DisplayName))
        {
            lines.Add(summary.Value.DisplayName);
            lines.Add($"{summary.Value.ItemName}: ${summary.Value.BaseSellPrice} -> ${summary.Value.EffectiveSellPrice}");
            lines.Add($"Bonus: +{summary.Value.BonusPercent}%");
            lines.Add($"Days left: {summary.Value.DaysRemaining}");
            if (!string.IsNullOrWhiteSpace(summary.Value.NextChainSummary))
                lines.Add($"Next candidate chain: {summary.Value.NextChainSummary}");
        }
        else
        {
            string nextDayLabel = summary?.DaysUntilNextOpportunity == 1 ? "day" : "days";
            lines.Add($"Next opportunity in {summary?.DaysUntilNextOpportunity ?? 0} {nextDayLabel}");
            if (!string.IsNullOrWhiteSpace(summary?.NextCandidateItemName))
                lines.Add($"Next boosted item: {summary.Value.NextCandidateItemName}");
        }

        MarketOfferProcessingSummary? bestOpportunity = GetBestMarketProcessingOpportunity(registry, getSellPrice);
        if (bestOpportunity is null)
        {
            lines.Add("Best market use: no processing route");
            return lines.ToArray();
        }

        MarketOfferProcessingSummary bestSummary = bestOpportunity.Value;
        lines.Add($"Best market use: {FormatMarketProcessingUse(registry, bestSummary.ProcessingEconomics)}, profit if bought: {FormatSignedCurrency(bestSummary.ProfitIfBought)}");
        return lines.ToArray();
    }

    public static string[] GetComputerMachineSummaryLines(ContentRegistry registry, IReadOnlyList<ProcessorMachineInstance> machines, GameStats stats)
    {
        List<string> lines = ["MACHINES"];

        for (int index = 0; index < machines.Count; index++)
        {
            ProcessorMachineInstance machine = machines[index];
            if (index > 0)
                lines.Add(string.Empty);

            if (machine.HasOutputReady && !string.IsNullOrWhiteSpace(machine.CurrentRecipeId))
            {
                ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe(machine.CurrentRecipeId);
                int valueAdded = GetValueAdded(registry, recipe);
                lines.Add($"{GetMachineName(registry, machine.MachineId)}: Output ready");
                lines.Add($"{recipe.InputQuantity} {registry.GetItem(recipe.InputItemId).Name} -> {recipe.OutputQuantity} {registry.GetItem(recipe.OutputItemId).Name}");
                lines.Add($"Output ready: {machine.OutputQuantity} {registry.GetItem(machine.OutputItemId).Name}");
                lines.Add($"Current value added: {FormatSignedCurrency(valueAdded)}");
                continue;
            }

            if (machine.IsWorking && !string.IsNullOrWhiteSpace(machine.CurrentRecipeId))
            {
                ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe(machine.CurrentRecipeId);
                int valueAdded = GetValueAdded(registry, recipe);
                lines.Add($"{GetMachineName(registry, machine.MachineId)}: Working");
                lines.Add($"{recipe.InputQuantity} {registry.GetItem(recipe.InputItemId).Name} -> {recipe.OutputQuantity} {registry.GetItem(recipe.OutputItemId).Name}");
                lines.Add($"Days remaining: {machine.DaysRemaining}");
                lines.Add($"Current value added: {FormatSignedCurrency(valueAdded)}");
                continue;
            }

            lines.Add($"{GetMachineName(registry, machine.MachineId)}: Idle");
            lines.Add("No active recipe.");
            lines.Add("Current value added: $0");
        }

        lines.Add($"Lifetime processing profit: {FormatSignedCurrency(stats.MachineValueAdded)}");
        return lines.ToArray();
    }

    public static string[] GetComputerBotSummaryLines(
        int money,
        bool isPlanterBotUnlocked,
        bool isWateringBotUnlocked,
        bool isHarvesterBotUnlocked,
        GameStats stats,
        GameBalanceConfig balanceConfig = null,
        ThemeVocabulary themeVocabulary = null)
    {
        GameBalanceConfig effectiveConfig = balanceConfig ?? GameBalanceConfig.Default;
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        return
        [
            "BOTS",
            FormatComputerBotLine(BotRoleFormatter.GetBotShortDisplayName(BotRole.Starter, theme), isPlanterBotUnlocked, BotRoleFormatter.GetBotCost(BotRole.Starter, effectiveConfig), money),
            FormatComputerBotLine(BotRoleFormatter.GetBotShortDisplayName(BotRole.Maintainer, theme), isWateringBotUnlocked, BotRoleFormatter.GetBotCost(BotRole.Maintainer, effectiveConfig), money),
            FormatComputerBotLine(BotRoleFormatter.GetBotShortDisplayName(BotRole.Collector, theme), isHarvesterBotUnlocked, BotRoleFormatter.GetBotCost(BotRole.Collector, effectiveConfig), money),
            $"{ThemeStatLabelFormatter.GetStarterBotActionLabel(theme)}: {stats.CropsPlantedByPlanterBot}",
            $"{ThemeStatLabelFormatter.GetMaintainerBotActionLabel(theme)}: {stats.PlotsWateredByWateringBot}",
            $"{ThemeStatLabelFormatter.GetCollectorBotActionLabel(theme)}: {stats.CropsHarvestedByHarvesterBot}"
        ];
    }

    public static string[] GetComputerFarmSummaryLines(
        FarmGrid farmGrid,
        int dayNumber,
        bool isFarmFullyExpanded,
        int nextFarmExpansionCost,
        Func<FarmPlot, bool> isPlotReady,
        ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        bool useLegacyFarmingLabels = string.Equals(theme.ThemeId, ThemeVocabulary.FarmingDefault.ThemeId, StringComparison.OrdinalIgnoreCase);
        int plantedCount = farmGrid.GetAllPlots().Count(plot => !plot.IsEmpty);
        int wateredCount = farmGrid.GetAllPlots().Count(plot => !plot.IsEmpty && plot.IsWatered);
        int readyCount = farmGrid.GetAllPlots().Count(isPlotReady);
        int emptyCount = farmGrid.GetAllPlots().Count(plot => plot.IsEmpty);

        return
        [
            theme.ToUpperLabel(theme.WorldName),
            $"Day {dayNumber}",
            $"Size: {farmGrid.Width}x{farmGrid.Height}",
            useLegacyFarmingLabels ? $"Planted: {plantedCount}" : $"{theme.ProducerPluralName}: {plantedCount}",
            useLegacyFarmingLabels ? $"Watered: {wateredCount}" : $"{theme.MaintainActionName}ed: {wateredCount}",
            $"Ready: {readyCount}",
            useLegacyFarmingLabels ? $"Empty: {emptyCount}" : $"Empty {theme.TilePluralName}: {emptyCount}",
            isFarmFullyExpanded ? "Next expansion: maxed" : $"Next expansion: ${nextFarmExpansionCost}"
        ];
    }

    public static string[] GetHintSectionLines(
        ContentRegistry registry,
        FarmGrid farmGrid,
        IReadOnlyList<ProcessorMachineInstance> machines,
        int money,
        string selectedSeedItemId,
        bool isPlanterBotUnlocked,
        bool isWateringBotUnlocked,
        bool isHarvesterBotUnlocked,
        bool isFarmFullyExpanded,
        int nextFarmExpansionCost,
        bool isFinalFarmExpansionBlockedByCleanup,
        CleanupSiteState cleanupSite,
        Func<string, int> getPlayerCount,
        Func<string, int> getAccessibleCount,
        Func<string, int> getStorageCount,
        Func<FarmPlot, bool> isPlotReady,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null,
        bool isPaused = false,
        bool isLoadConfirmationPending = false,
        int maxHints = 3,
        GameBalanceConfig balanceConfig = null,
        ThemeVocabulary themeVocabulary = null)
    {
        GameBalanceConfig effectiveConfig = balanceConfig ?? GameBalanceConfig.Default;
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        List<string> lines = ["HINTS", .. GetNextHintLines(
            registry,
            farmGrid,
            machines,
            money,
            selectedSeedItemId,
            isPlanterBotUnlocked,
            isWateringBotUnlocked,
            isHarvesterBotUnlocked,
            isFarmFullyExpanded,
            nextFarmExpansionCost,
            isFinalFarmExpansionBlockedByCleanup,
            cleanupSite,
            getPlayerCount,
            getStorageCount,
            getAccessibleCount,
            isPlotReady,
            activeMarketOpportunity,
            getSellPrice,
            isPaused,
            isLoadConfirmationPending,
            maxHints,
            effectiveConfig,
            theme)];
        return lines.ToArray();
    }

    public static string[] GetNextHintLines(
        ContentRegistry registry,
        FarmGrid farmGrid,
        IReadOnlyList<ProcessorMachineInstance> machines,
        int money,
        string selectedSeedItemId,
        bool isPlanterBotUnlocked,
        bool isWateringBotUnlocked,
        bool isHarvesterBotUnlocked,
        bool isFarmFullyExpanded,
        int nextFarmExpansionCost,
        bool isFinalFarmExpansionBlockedByCleanup,
        CleanupSiteState cleanupSite,
        Func<string, int> getPlayerCount,
        Func<string, int> getStorageCount,
        Func<string, int> getAccessibleCount,
        Func<FarmPlot, bool> isPlotReady,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null,
        bool isPaused = false,
        bool isLoadConfirmationPending = false,
        int maxHints = 3,
        GameBalanceConfig balanceConfig = null,
        ThemeVocabulary themeVocabulary = null)
    {
        GameBalanceConfig effectiveConfig = balanceConfig ?? GameBalanceConfig.Default;
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        List<string> hints = [];

        if (isLoadConfirmationPending)
            AddHint(hints, "Confirm load with Y or cancel with N/Esc.");

        if (isPaused)
            AddHint(hints, "Paused. Press Space to resume.");

        IReadOnlyList<FarmPlot> plots = farmGrid.GetAllPlots().ToArray();
        bool hasAnyPlayerSeeds = registry.TileProducers
            .Select(tileProducer => tileProducer.StarterItemId)
            .Where(seedItemId => !string.IsNullOrWhiteSpace(seedItemId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Any(seedItemId => getPlayerCount(seedItemId) > 0);
        bool hasEmptyPlots = plots.Any(plot => plot.IsEmpty);
        bool hasDryPlantedCrops = plots.Any(plot => !plot.IsEmpty && !plot.IsWatered);
        bool hasReadyCrops = plots.Any(isPlotReady);

        if (!hasAnyPlayerSeeds)
        {
            bool canAffordSelectedSeed = CanAffordSelectedSeed(registry, selectedSeedItemId, money);
            AddHint(hints, canAffordSelectedSeed ? $"Buy {theme.StarterItemPluralName.ToLowerInvariant()} with Y." : "Sell crops or use the market to recover money.");
        }

        if (hasEmptyPlots && hasAnyPlayerSeeds)
            AddHint(hints, isPlanterBotUnlocked ? $"Plant {theme.StarterItemPluralName.ToLowerInvariant()} with P or run {theme.StarterBotName} with V." : $"Plant {theme.StarterItemPluralName.ToLowerInvariant()} with P.");

        if (hasDryPlantedCrops)
            AddHint(hints, isWateringBotUnlocked ? $"{theme.MaintainActionName} {theme.ProducerPluralName.ToLowerInvariant()} with W or run {theme.MaintainerBotName} with B." : $"{theme.MaintainActionName} {theme.ProducerPluralName.ToLowerInvariant()} with W.");

        if (hasReadyCrops)
            AddHint(hints, isHarvesterBotUnlocked ? $"{theme.CollectActionName} selected {theme.ProducerName.ToLowerInvariant()} with H or run {theme.CollectorBotName} with X." : $"{theme.CollectActionName} selected {theme.ProducerName.ToLowerInvariant()} with H.");

        if (HasSellableGoods(registry, getPlayerCount))
            AddHint(hints, "Sell goods with S.");

        if (machines.Any(machine => machine.HasOutputReady))
            AddHint(hints, "Collect machine output with O.");

        bool hasIdleMachineWithInput = machines.Any(machine =>
            !machine.IsWorking
            && !machine.HasOutputReady
            && registry.GetProcessingRecipesForMachine(machine.MachineId)
                .Any(recipe => getAccessibleCount(recipe.InputItemId) >= recipe.InputQuantity));
        if (hasIdleMachineWithInput)
            AddHint(hints, "Start processor with M.");

        if (!isPlanterBotUnlocked && money >= effectiveConfig.PlanterBotCost)
            AddHint(hints, $"Unlock {theme.StarterBotName} with 1.");
        else if (!isWateringBotUnlocked && money >= effectiveConfig.WateringBotCost)
            AddHint(hints, $"Unlock {theme.MaintainerBotName} with 2.");
        else if (!isHarvesterBotUnlocked && money >= effectiveConfig.HarvesterBotCost)
            AddHint(hints, $"Unlock {theme.CollectorBotName} with 3.");

        if (!isFarmFullyExpanded && !isFinalFarmExpansionBlockedByCleanup && money >= nextFarmExpansionCost)
            AddHint(hints, "Expand farm with E.");

        if (isFinalFarmExpansionBlockedByCleanup && cleanupSite is not null && !cleanupSite.IsCleared)
            AddHint(hints, $"Clear {cleanupSite.Name} with J to {GetCleanupUnlockText(cleanupSite)}.");

        if (activeMarketOpportunity?.IsActive == true)
            AddHint(hints, GetMarketOpportunityHint(registry, activeMarketOpportunity));

        if (HasSellableGoods(registry, getStorageCount))
            AddHint(hints, "Storage has sellable goods; sell with S or inspect Storage.");

        if (hints.Count == 0)
            hints.Add($"Run {theme.WorldName.ToLowerInvariant()} cycle with G or continue improving production.");

        return hints.Take(Math.Max(1, maxHints)).ToArray();
    }

    private static void AddHint(ICollection<string> hints, string hint)
    {
        if (string.IsNullOrWhiteSpace(hint) || hints.Contains(hint))
            return;

        hints.Add(hint);
    }

    private static bool HasSellableGoods(ContentRegistry registry, Func<string, int> getCount)
    {
        return registry.Items.Any(item =>
            !registry.IsStarterItem(item.Id)
            && item.SellPrice > 0
            && getCount(item.Id) > 0);
    }

    private static string GetMarketOpportunityHint(ContentRegistry registry, MarketOpportunity activeMarketOpportunity)
    {
        ItemDefinition item = registry.GetItem(activeMarketOpportunity.ItemId);
        string opportunityName = string.IsNullOrWhiteSpace(activeMarketOpportunity.DisplayName)
            ? item.Name
            : activeMarketOpportunity.DisplayName;
        return $"{opportunityName} active: produce/sell {item.Name} before it ends.";
    }

    private static string GetCleanupUnlockText(CleanupSiteState cleanupSite)
    {
        string rewardText = cleanupSite.RewardDescription?.Trim() ?? "unlock the next expansion";
        if (rewardText.StartsWith("Unlocks ", StringComparison.OrdinalIgnoreCase))
            rewardText = rewardText[8..];

        rewardText = rewardText.Replace(" farm expansion", " expansion", StringComparison.OrdinalIgnoreCase);
        rewardText = rewardText.Replace("farm expansion", "expansion", StringComparison.OrdinalIgnoreCase);
        rewardText = rewardText.Trim().TrimEnd('.');
        return $"unlock {rewardText}";
    }

    public static string[] GetMachineScreenLines(
        ContentRegistry registry,
        IReadOnlyList<ProcessorMachineInstance> machines,
        string selectedMachineId,
        string selectedRecipeId,
        GameStats stats,
        Func<string, int> getCount,
        bool includeDebugDetails = false,
        bool isAutoPlayRunning = false,
        bool isPaused = false,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null)
    {
        ProcessorMachineInstance selectedMachine = machines.FirstOrDefault(machine => string.Equals(machine.MachineId, selectedMachineId, StringComparison.OrdinalIgnoreCase));
        ProcessingRecipeDefinition selectedRecipe = registry.GetProcessingRecipesForMachine(selectedMachineId)
            .FirstOrDefault(recipe => string.Equals(recipe.Id, selectedRecipeId, StringComparison.OrdinalIgnoreCase))
            ?? registry.GetProcessingRecipesForMachine(selectedMachineId).FirstOrDefault();
        string selectedRecipeLabel = selectedRecipe is null
            ? "none"
            : $"{selectedRecipe.InputQuantity} {registry.GetItem(selectedRecipe.InputItemId).Name} -> {selectedRecipe.OutputQuantity} {registry.GetItem(selectedRecipe.OutputItemId).Name}";
        List<string> lines =
        [
            $"Selected Machine: {GetMachineName(registry, selectedMachineId)}",
            selectedMachine is not null && selectedMachine.HasOutputReady
                ? "Status: Output Ready"
                : selectedMachine is not null && selectedMachine.IsWorking
                    ? "Status: Working"
                    : "Status: Idle",
            $"Selected recipe: {selectedRecipeLabel}",
            string.Empty,
            "Machine Details:"
        ];

        if (selectedMachine is not null)
        {
            lines.InsertRange(4,
            [
                "MACHINE CONTROLS",
                .. GetVisualCommandLines(GetMachineVisualCommands(registry, selectedMachine, selectedRecipeId, getCount, activeMarketOpportunity), includeGroupHeaders: false),
                "[ ESC ] Return to farm",
                string.Empty
            ]);
        }

        if (selectedMachine is not null)
            lines.AddRange(GetMachineStatusLines(registry, selectedMachine, getCount, includeDebugDetails, activeMarketOpportunity, getSellPrice));

        lines.Add(string.Empty);
        lines.Add("Recipes:");
        lines.AddRange(GetMachineRecipeLines(registry, selectedMachineId, selectedRecipeId, getCount, activeMarketOpportunity, getSellPrice));
        lines.Add(string.Empty);
        lines.Add("All Machines:");

        for (int index = 0; index < machines.Count; index++)
        {
            if (index > 0)
                lines.Add(string.Empty);

            lines.AddRange(GetCompactMachineStatusLines(registry, machines[index]));
        }

        lines.Add(string.Empty);
        lines.AddRange(GetMachineProductionTotalsLines(stats));
        lines.AddRange(GetProcessedOutputLines(registry, stats));
        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "PGUP/PGDN machine | UP/DOWN recipe | M start | O collect", "F9: computer | F4: help"));

        return lines.ToArray();
    }

    public static string[] GetMachineScreenLines(
        ContentRegistry registry,
        IReadOnlyList<ProcessorMachineInstance> machines,
        string selectedMachineId,
        GameStats stats,
        Func<string, int> getCount,
        bool includeDebugDetails = false,
        bool isAutoPlayRunning = false,
        bool isPaused = false)
    {
        string selectedRecipeId = registry.GetProcessingRecipesForMachine(selectedMachineId).FirstOrDefault()?.Id;
        return GetMachineScreenLines(registry, machines, selectedMachineId, selectedRecipeId, stats, getCount, includeDebugDetails, isAutoPlayRunning, isPaused);
    }

    public static string[] GetMachineScreenLines(
        ContentRegistry registry,
        ProcessorMachineInstance machine,
        GameStats stats,
        Func<string, int> getCount,
        bool includeDebugDetails = false,
        bool isAutoPlayRunning = false,
        bool isPaused = false)
    {
        List<string> lines =
        [
            "CURRENT RUN",
            .. GetMachineStatusLines(registry, machine, getCount, includeDebugDetails),
            string.Empty,
            .. GetMachineProductionTotalsLines(stats),
            .. GetProcessedOutputLines(registry, stats)
        ];

        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "M: start selected | O: collect selected", "F9: computer | F4: help"));

        return lines.ToArray();
    }

    public static string[] GetMachineProductionTotalsLines(GameStats stats)
    {
        List<string> lines =
        [
            "PRODUCTION TOTALS",
            $"Runs: {stats.MachineRunsStarted}",
            $"Outputs: {stats.MachineOutputsCollected}",
            $"Goods produced: {stats.ProcessedGoodsProduced}",
            $"Raw value consumed: ${stats.MachineInputValueConsumed}",
            $"Output value created: ${stats.MachineOutputValueCreated}",
            $"Processing profit: {FormatSignedCurrency(stats.MachineValueAdded)}"
        ];

        if (stats.MachineRunsStarted > 0)
            lines.Add($"Avg profit per run: {FormatSignedCurrency(stats.MachineValueAdded / stats.MachineRunsStarted)}");

        if (stats.ProcessedGoodsProduced > 0)
            lines.Add($"Avg profit per output: {FormatSignedCurrency(stats.MachineValueAdded / stats.ProcessedGoodsProduced)}");

        return lines.ToArray();
    }

    public static string[] GetProcessedOutputLines(ContentRegistry registry, GameStats stats)
    {
        if (stats.ProcessedOutputsByItemId.Count == 0)
            return [];

        List<string> lines = [string.Empty, "PRODUCED"];
        lines.AddRange(stats.ProcessedOutputsByItemId
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => $"{registry.GetItem(entry.Key).Name}: {entry.Value}"));
        return lines.ToArray();
    }

    public static string[] GetStatsScreenLines(
        ContentRegistry registry,
        GameStats stats,
        int farmWidth,
        int farmHeight,
        bool includeDebugDetails,
        bool isAutoPlayRunning = false,
        bool isPaused = false,
        ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        List<string> lines =
        [
            "PRODUCTION",
            $"{ThemeStatLabelFormatter.GetStarterItemsBoughtLabel(theme)}: {stats.SeedsBought}",
            $"{ThemeStatLabelFormatter.GetProducersStartedLabel(theme)}: {stats.TotalCropsPlanted}",
            $"{ThemeStatLabelFormatter.GetProducersCollectedLabel(theme)}: {stats.TotalCropsHarvested}",
            $"Sold: {stats.CropsSold}",
            $"Earned from sales: ${stats.MoneyEarnedFromSelling}",
            "SPENDING",
            $"{ThemeStatLabelFormatter.GetMoneySpentOnStarterItemsLabel(theme)}: ${stats.MoneySpentOnSeeds}",
            $"Spent on bots: ${stats.MoneySpentOnBotUnlocks}",
            $"Spent on expansion: ${stats.MoneySpentOnExpansion}",
            $"Spent on market: ${stats.MarketMoneySpent}",
            $"Total spent: ${stats.TotalMoneySpent}",
            "MARKET",
            $"Purchases: {stats.MarketPurchases}",
            $"Items bought: {stats.MarketItemsBought}",
            $"Market spend: ${stats.MarketMoneySpent}",
            "PROCESSING",
            $"Runs: {stats.MachineRunsStarted}",
            $"Outputs collected: {stats.MachineOutputsCollected}",
            $"Processed goods: {stats.ProcessedGoodsProduced}",
            $"Raw {stats.MachineInputValueConsumed} | Out {stats.MachineOutputValueCreated} | Add {FormatSignedValue(stats.MachineValueAdded)}",
            "AUTOMATION",
            $"{ThemeStatLabelFormatter.GetManualStartedLabel(theme)}: {stats.CropsPlantedManually}",
            $"{ThemeStatLabelFormatter.GetManualMaintainedLabel(theme)}: {stats.PlotsWateredManually}",
            $"{ThemeStatLabelFormatter.GetManualCollectedLabel(theme)}: {stats.CropsHarvestedManually}",
            $"{ThemeStatLabelFormatter.GetStarterBotActionLabel(theme)}: {stats.CropsPlantedByPlanterBot}",
            $"{ThemeStatLabelFormatter.GetMaintainerBotActionLabel(theme)}: {stats.PlotsWateredByWateringBot}",
            $"{ThemeStatLabelFormatter.GetCollectorBotActionLabel(theme)}: {stats.CropsHarvestedByHarvesterBot}",
            $"Cycles {stats.FarmCyclesRun} | Auto {stats.AutoPlayStepsRun} | Saved {stats.BotActionsSaved}",
            "PROGRESS",
            $"Days advanced: {stats.DaysAdvanced}",
            $"Bots unlocked: {stats.BotsUnlocked}",
            $"Expansions purchased: {stats.FarmExpansionsPurchased}",
            $"{theme.WorldName} size: {farmWidth}x{farmHeight}"
        ];

        if (includeDebugDetails)
        {
            if (stats.ProcessedOutputsByItemId.Count > 0)
            {
                string outputCounts = string.Join(" ", stats.ProcessedOutputsByItemId
                    .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(entry => $"{registry.GetItem(entry.Key).Name.ToUpperInvariant()} {entry.Value}"));
                lines.Add(outputCounts);
            }

            lines.Add($"BOT ACTIONS SAVED {stats.BotActionsSaved}");
        }

        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "I: inventory | F3: machines | F9: computer", "F4: help"));
        return lines.ToArray();
    }

    public static string[] GetHelpScreenLines(bool isAutoPlayRunning = false, bool isPaused = false, ThemeVocabulary themeVocabulary = null)
    {
        return GetHelpScreenLines(null, null, null, isAutoPlayRunning, isPaused, themeVocabulary);
    }

    public static string[] GetHelpScreenLines(SaveLoadStatus saveLoadStatus, bool isAutoPlayRunning = false, bool isPaused = false, ThemeVocabulary themeVocabulary = null)
    {
        return GetHelpScreenLines(null, null, saveLoadStatus, isAutoPlayRunning, isPaused, themeVocabulary);
    }

    public static string[] GetHelpScreenLines(
        ContentRegistry registry,
        TurnipTestState state,
        SaveLoadStatus saveLoadStatus,
        bool isAutoPlayRunning = false,
        bool isPaused = false,
        ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = state?.ThemeVocabulary ?? themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        List<string> lines =
        [
            "NAVIGATION",
            "I Inventory | F2 Stats | F3 Machines | F8 Storage | F9 Computer | Shift+F9 Production",
            "F10 Market | Shift+F10 Progression | F11 Ledger | F4 Help",
            $"ESC on sub-screens: return to {theme.WorldName}",
            $"ESC on {theme.WorldName}: quit",
            string.Empty,
            "PAUSE",
            "Space pause or resume",
            "When paused, navigation works but gameplay actions are frozen.",
            string.Empty,
            theme.ToUpperLabel(theme.ThemeName),
            $"Arrows select | Tab {theme.StarterItemName.ToLowerInvariant()} | P plant | W {theme.MaintainActionName.ToLowerInvariant()} | H {theme.CollectActionName.ToLowerInvariant()}",
            $"N next day | S sell | Y buy {theme.StarterItemName.ToLowerInvariant()} | Shift+Y buy max | E expand | J cleanup",
            string.Empty,
            "BOTS",
            $"{BotRoleFormatter.GetBotUnlockKey(BotRole.Starter)} {BotRoleFormatter.GetBotUnlockKey(BotRole.Maintainer)} {BotRoleFormatter.GetBotUnlockKey(BotRole.Collector)} unlock | {BotRoleFormatter.GetBotRunKey(BotRole.Starter)} {BotRoleFormatter.GetBotShortDisplayName(BotRole.Starter, theme).ToLowerInvariant()} | {BotRoleFormatter.GetBotRunKey(BotRole.Maintainer)} {BotRoleFormatter.GetBotShortDisplayName(BotRole.Maintainer, theme).ToLowerInvariant()} | {BotRoleFormatter.GetBotRunKey(BotRole.Collector)} {BotRoleFormatter.GetBotShortDisplayName(BotRole.Collector, theme).ToLowerInvariant()} | G cycle",
            string.Empty,
            "STORAGE",
            "C deposit | U withdraw | S sell from storage screen",
            string.Empty,
            "MACHINES",
            "F3 Machines",
            "PGUP/PGDN select machine | UP/DOWN select recipe",
            "M start selected recipe | O collect output",
            string.Empty,
            "MARKET",
            "F10 open market | 1-9 buy listed offer",
            "Market opportunities are positive bonuses; ignoring them has no penalty.",
            string.Empty,
            "PRODUCTION",
            "Shift+F9 open production report",
            string.Empty,
            "PROGRESSION",
            "Shift+F10 open progression report | R run report on that screen",
            string.Empty,
            "LEDGER",
            "F11 open activity log",
            string.Empty,
            "SAVE / LOAD",
            "F12 save | Shift+F12 request load",
            "Y confirm load | N/Esc cancel load",
            $"Path: {FormatSavePathForDisplay(saveLoadStatus?.SavePath)}",
            $"Load returns to {theme.WorldName}, unpaused, Auto Play off.",
            string.Empty,
            "AUTOPLAY",
            "F5 toggle | F6 single step | F7 reset counters",
            "Autoplay remains enabled while paused but does not step.",
            "Farm continues updating while viewing sub-screens",
            string.Empty,
            .. GetThemePackHelpSectionLines(state, registry),
            .. GetThemePackHelpSectionSpacerLines(state, registry),
            "DEBUG / RESET / QUIT",
            $"F1 debug details | R reset game | ESC quits on {theme.WorldName}"
        ];

        lines.AddRange(GetScreenFooterLines(isPaused, isAutoPlayRunning, "All controls grouped above"));
        return lines.ToArray();
    }

    private static string[] GetThemePackHelpSectionLines(TurnipTestState state, ContentRegistry registry)
    {
        if (state is null || registry is null)
            return [];

        return ThemePackRuntimeDiagnostics.GetThemePackSummaryLines(state, registry);
    }

    private static string[] GetThemePackHelpSectionSpacerLines(TurnipTestState state, ContentRegistry registry)
    {
        if (state is null || registry is null)
            return [];

        return [string.Empty];
    }

    public static string[] GetComputerCleanupLines(IReadOnlyList<CleanupSiteState> cleanupSites, int money)
    {
        if (cleanupSites is null || cleanupSites.Count == 0)
            return ["RESTORATION", "No cleanup sites."];

        List<string> lines = ["RESTORATION"];

        foreach (CleanupSiteState cleanupSite in cleanupSites)
        {
            lines.Add($"{cleanupSite.Name}: {(cleanupSite.IsCleared ? "Cleared" : "Uncleared")}");
            lines.Add($"Cost: ${cleanupSite.Cost}");
            lines.Add($"Reward: {cleanupSite.RewardDescription}");
            lines.Add($"Affordable: {(money >= cleanupSite.Cost ? "yes" : "no")}");

            if (!ReferenceEquals(cleanupSite, cleanupSites[^1]))
                lines.Add(string.Empty);
        }

        return lines.ToArray();
    }

    public static string[] GetSaveStatusLines(SaveLoadStatus saveLoadStatus, bool includeHeader = false)
    {
        if (saveLoadStatus is null)
            return [];

        List<string> lines = [];
        if (includeHeader)
            lines.Add("SAVE STATUS");

        lines.Add($"Path: {FormatSavePathForDisplay(saveLoadStatus.SavePath)}");
        lines.Add($"Last save: {FormatStatusLine(saveLoadStatus.LastSaveSucceeded, saveLoadStatus.LastSaveMessage)}");
        lines.Add($"Last load: {FormatStatusLine(saveLoadStatus.LastLoadSucceeded, saveLoadStatus.LastLoadMessage)}");
        return lines.ToArray();
    }

    public static string GetLoadConfirmationPrompt(string savePath)
    {
        return $"Load {FormatSavePathForDisplay(savePath)}? Press Y to confirm or N/Esc to cancel.";
    }

    public static string[] GetLoadConfirmationLines(string savePath)
    {
        return
        [
            $"Load {FormatSavePathForDisplay(savePath)}?",
            "Y confirm | N cancel | Esc cancel"
        ];
    }
}

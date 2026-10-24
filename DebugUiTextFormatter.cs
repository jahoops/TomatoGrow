using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public static partial class DebugUiTextFormatter
{
    public enum TileDisplayState
    {
        Empty,
        GrowingDry,
        GrowingMaintained,
        Ready
    }

    public readonly record struct DashboardSummary(
        int DayNumber,
        int Money,
        string FarmSize,
        string SelectedSeedName,
        int SelectedSeedCount,
        int SellableCropCount,
        int EstimatedSellValue,
        string CleanupStatusLine,
        string MarketStatusLine);

    public readonly record struct ProcessingRecipeEconomics(
        int InputValue,
        int OutputValue,
        int ValueAdded,
        int ProcessingDays);

    public readonly record struct MarketOfferDirectEconomics(
        int DirectValue,
        int DirectResaleProfit);

    public readonly record struct MarketOfferProcessingEconomics(
        string RecipeId,
        string InputItemId,
        int InputQuantity,
        string OutputItemId,
        int OutputQuantity,
        int BatchCount,
        int TotalOutputQuantity,
        int OutputValue,
        int ProfitIfBought,
        int ValueAddedIfGrown,
        int ProcessingDays);

    public readonly record struct ProductionChainStep(
        string RecipeId,
        string MachineName,
        string InputItemId,
        string InputItemName,
        int InputQuantity,
        string OutputItemId,
        string OutputItemName,
        int OutputQuantity,
        int ProcessingDays,
        int InputValue,
        int OutputValue,
        int ValueAdded,
        string OpportunityName);

    public readonly record struct ProductionChain(
        string RootItemId,
        string RootItemName,
        string Summary,
        IReadOnlyList<ProductionChainStep> Steps);

    public readonly record struct MarketOpportunitySummary(
        string DisplayName,
        string ItemName,
        int BaseSellPrice,
        int EffectiveSellPrice,
        int BonusPercent,
        int DaysRemaining,
        string NextChainSummary,
        int DaysUntilNextOpportunity,
        string NextCandidateItemName);

    public enum VisualCommandThemeRole
    {
        Normal,
        Primary,
        Disabled,
        Ready,
        MarketBonus
    }

    public enum VisualCommandGroup
    {
        Primary,
        Market,
        Also
    }

    public readonly record struct VisualCommand(
        string KeyLabel,
        string ActionLabel,
        bool IsEnabled,
        int Priority = 0,
        VisualCommandGroup Group = VisualCommandGroup.Also,
        bool IsPrimary = false,
        string StatusLabel = null,
        string ReasonDisabled = null,
        VisualCommandThemeRole ThemeRole = VisualCommandThemeRole.Normal);

    public readonly record struct FarmMachineCard(
        string MachineId,
        string MachineName,
        string StatusLabel,
        string[] DetailLines,
        VisualCommand? ActionCommand,
        VisualCommandThemeRole ThemeRole,
        bool IsSelected);

    public static string GetBotStatusText(string botName, bool isUnlocked, int unlockCost, int money, string unlockedDetail = "UNLOCKED")
    {
        if (isUnlocked)
            return $"{botName}: {unlockedDetail}";

        if (money >= unlockCost)
            return $"{botName}: LOCKED COST {unlockCost} AFFORDABLE";

        return $"{botName}: LOCKED COST {unlockCost} NEED {unlockCost - money} MORE";
    }

    public static string GetScreenTitle(GameScreen screen, ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;

        return screen switch
        {
            GameScreen.Farm => $"{theme.ToUpperLabel(theme.WorldName)} GRID",
            GameScreen.Inventory => "INVENTORY",
            GameScreen.Storage => theme.ToUpperLabel(theme.StorageName),
            GameScreen.Market => theme.ToUpperLabel(theme.MarketName),
            GameScreen.Computer => theme.ToUpperLabel(theme.ComputerName),
            GameScreen.ProgressionReport => "PROGRESSION REPORT",
            GameScreen.ProductionReport => theme.ToUpperLabel(theme.ProductionReportName),
            GameScreen.ActivityLog => theme.ToUpperLabel(theme.ActivityLogName),
            GameScreen.Machines => "MACHINES",
            GameScreen.Stats => "STATS",
            GameScreen.Help => "HELP / CONTROLS",
            _ => throw new ArgumentOutOfRangeException(nameof(screen), screen, "Unsupported screen title.")
        };
    }

    public static Color GetTileFillColor(FarmPlot plot, InterfacePack interfacePack = null)
    {
        InterfacePack pack = interfacePack ?? InterfacePack.FarmingDefault;

        if (plot.IsEmpty)
            return plot.IsWatered ? pack.MaintainedEmptyTileFillColor : pack.EmptyTileFillColor;

        return plot.IsWatered ? pack.MaintainedTileFillColor : pack.UnmaintainedTileFillColor;
    }

    public static Color GetTileReadyTextColor(InterfacePack interfacePack = null)
    {
        return (interfacePack ?? InterfacePack.FarmingDefault).ReadyTextColor;
    }

    public static Color GetTileSelectedBorderColor(InterfacePack interfacePack = null)
    {
        return (interfacePack ?? InterfacePack.FarmingDefault).SelectedBorderColor;
    }

    public static Color GetTileNormalBorderColor(InterfacePack interfacePack = null)
    {
        return (interfacePack ?? InterfacePack.FarmingDefault).NormalBorderColor;
    }

    public static TileDisplayState GetTileDisplayState(FarmPlot plot, bool isReady)
    {
        if (plot.IsEmpty)
            return TileDisplayState.Empty;

        if (isReady)
            return TileDisplayState.Ready;

        return plot.IsWatered ? TileDisplayState.GrowingMaintained : TileDisplayState.GrowingDry;
    }

    public static float GetTileProgressFraction(FarmPlot plot, ContentRegistry registry)
    {
        if (plot.PlantedCrop is null)
            return 0f;

        TileProducerDefinition tileProducer = registry.GetTileProducer(plot.PlantedCrop.CropDefinitionId);
        if (tileProducer.ProductionDays <= 0)
            return 1f;

        return Math.Clamp((float)plot.PlantedCrop.DaysGrown / tileProducer.ProductionDays, 0f, 1f);
    }

    public static string GetTilePrimarySymbol(FarmPlot plot, ContentRegistry registry, InterfacePack interfacePack = null)
    {
        InterfacePack pack = interfacePack ?? InterfacePack.FarmingDefault;

        if (plot.IsEmpty)
            return pack.EmptyTileSymbol;

        if (plot.PlantedCrop is null)
            return pack.OccupiedTileSymbol;

        string outputName = registry.GetItem(registry.GetTileProducer(plot.PlantedCrop.CropDefinitionId).OutputItemId).Name;
        char firstLetter = outputName.FirstOrDefault(character => char.IsLetterOrDigit(character));
        return firstLetter == default ? pack.OccupiedTileSymbol : char.ToUpperInvariant(firstLetter).ToString();
    }

    public static Rectangle GetTileProgressBarRect(Rectangle tileBounds, float progressFraction)
    {
        int horizontalPadding = Math.Max(4, tileBounds.Width / 12);
        int barHeight = Math.Max(8, tileBounds.Height / 9);
        int availableWidth = Math.Max(0, tileBounds.Width - (horizontalPadding * 2));
        int filledWidth = (int)Math.Round(availableWidth * Math.Clamp(progressFraction, 0f, 1f));
        return new Rectangle(tileBounds.X + horizontalPadding, tileBounds.Bottom - barHeight - 6, filledWidth, barHeight);
    }

    public static string GetTileStateLabel(FarmPlot plot, InterfacePack interfacePack = null)
    {
        InterfacePack pack = interfacePack ?? InterfacePack.FarmingDefault;

        if (plot.IsEmpty)
            return pack.EmptyShortLabel;

        return plot.IsWatered ? pack.MaintainedShortLabel : pack.UnmaintainedShortLabel;
    }

    public static string GetTileStatusLine(FarmPlot plot, bool isReady, InterfacePack interfacePack = null)
    {
        InterfacePack pack = interfacePack ?? InterfacePack.FarmingDefault;
        if (isReady)
            return pack.ReadyShortLabel;

        return plot.IsWatered ? pack.MaintainedShortLabel : pack.UnmaintainedShortLabel;
    }

    public static string GetTileSymbol(FarmPlot plot, bool isReady, InterfacePack interfacePack = null)
    {
        InterfacePack pack = interfacePack ?? InterfacePack.FarmingDefault;

        if (plot.IsEmpty)
            return pack.EmptyTileSymbol;

        return isReady ? pack.ReadyTileSymbol : pack.OccupiedTileSymbol;
    }

    public static string GetTileSelectionMarker(InterfacePack interfacePack = null)
    {
        return (interfacePack ?? InterfacePack.FarmingDefault).SelectedTileMarker;
    }

    public static DashboardSummary GetDashboardSummary(
        int dayNumber,
        int money,
        int farmWidth,
        int farmHeight,
        ContentRegistry registry,
        string selectedSeedItemId,
        Func<string, int> getCount,
        Func<string, int> getSellPrice = null,
        MarketOpportunity activeMarketOpportunity = null,
        int daysUntilNextOpportunity = 0)
    {
        return new DashboardSummary(
            dayNumber,
            money,
            $"{farmWidth}X{farmHeight}",
            registry.GetItem(selectedSeedItemId).Name,
            GetSelectedSeedCount(selectedSeedItemId, getCount),
            GetTotalSellableCropCount(registry, getCount),
            GetEstimatedSellValue(registry, getCount, getSellPrice),
            string.Empty,
            FormatDashboardMarketLine(registry, activeMarketOpportunity, daysUntilNextOpportunity));
    }

    public static string[] GetSelectedPlotSummaryLines(FarmPlot plot, ContentRegistry registry, bool isReady, bool includeDebugDetails = false, ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        string[] lines = GetSelectedTileDetailLines(plot, registry, isReady, theme);

        if (!includeDebugDetails)
            return lines;

        if (plot.PlantedCrop is null)
            return lines;

        TileProducerDefinition tileProducer = registry.GetTileProducer(plot.PlantedCrop.CropDefinitionId);
        return
        [
            .. lines,
            $"CROP ID: {tileProducer.Id.ToUpperInvariant()}",
            $"HARVEST ID: {tileProducer.OutputItemId.ToUpperInvariant()}"
        ];
    }

    public static string[] GetSelectedTileDetailLines(FarmPlot plot, ContentRegistry registry, bool isReady, ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        if (plot.PlantedCrop is null)
        {
            return
            [
                $"SELECTED {theme.ToUpperLabel(theme.TileName)} {plot.X},{plot.Y}",
                $"EMPTY {theme.ToUpperLabel(theme.TileName)}",
                "PROGRESS: --",
                $"{theme.ToUpperLabel(theme.MaintainActionName)}: {(plot.IsWatered ? theme.ToUpperLabel(theme.MaintainedStateName) : theme.ToUpperLabel(theme.UnmaintainedStateName))}",
                "OUTPUT: NONE",
                $"P: PLANT {theme.ToUpperLabel(theme.StarterItemName)}"
            ];
        }

        TileProducerDefinition tileProducer = registry.GetTileProducer(plot.PlantedCrop.CropDefinitionId);
        string producerName = registry.GetItem(tileProducer.OutputItemId).Name.ToUpperInvariant();
        string actionLine = isReady
            ? $"H: {theme.ToUpperLabel(theme.CollectActionName)} WHEN READY"
            : plot.IsWatered
                ? $"G: RUN {theme.ToUpperLabel(theme.WorldName)} CYCLE"
                : $"W: {theme.ToUpperLabel(theme.MaintainActionName)}";

        return
        [
            $"SELECTED {theme.ToUpperLabel(theme.TileName)} {plot.X},{plot.Y}",
            producerName,
            $"PROGRESS: {plot.PlantedCrop.DaysGrown} / {tileProducer.ProductionDays}",
            $"{theme.ToUpperLabel(theme.MaintainActionName)}: {(plot.IsWatered ? theme.ToUpperLabel(theme.MaintainedStateName) : theme.ToUpperLabel(theme.UnmaintainedStateName))}",
            $"OUTPUT: {registry.GetItem(tileProducer.OutputItemId).Name.ToUpperInvariant()}",
            actionLine
        ];
    }

    public static string[] GetCompactTileVisualLines(FarmPlot plot, ContentRegistry registry, bool isReady, InterfacePack interfacePack = null)
    {
        InterfacePack pack = interfacePack ?? InterfacePack.FarmingDefault;
        string stateToken = isReady ? $"{pack.ReadyShortLabel}!" : GetTileStatusLine(plot, isReady, pack);
        string symbol = plot.IsEmpty ? GetTileSymbol(plot, isReady, pack) : GetTilePrimarySymbol(plot, registry, pack);

        if (plot.PlantedCrop is null)
            return [$"{symbol} {pack.EmptyShortLabel}"];

        TileProducerDefinition tileProducer = registry.GetTileProducer(plot.PlantedCrop.CropDefinitionId);
        return
        [
            $"{symbol} {stateToken}",
            $"{GetTileProgressBar(plot.PlantedCrop.DaysGrown, tileProducer.ProductionDays)} {plot.PlantedCrop.DaysGrown}/{tileProducer.ProductionDays}"
        ];
    }

    public static string GetTileProgressBar(int daysProduced, int requiredProductionDays)
    {
        if (requiredProductionDays <= 0)
            return "[-----]";

        int filled = Math.Clamp(daysProduced, 0, requiredProductionDays);
        return $"[{new string('#', filled)}{new string('-', Math.Max(0, requiredProductionDays - filled))}]";
    }

    public static string[] GetFarmControlLines(
        ContentRegistry registry,
        FarmPlot selectedPlot,
        string selectedStarterItemId,
        Func<int, int, bool> isTileReady,
        bool isPaused,
        InterfacePack interfacePack = null,
        ThemeVocabulary themeVocabulary = null,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null,
        int daysUntilNextOpportunity = 0)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        string marketLine = activeMarketOpportunity?.IsActive == true
            ? $"MARKET BONUS: {registry.GetItem(activeMarketOpportunity.ItemId).Name.ToUpperInvariant()} +{Math.Max(0, activeMarketOpportunity.GetMultiplierPercent() - 100)}% FOR {activeMarketOpportunity.DaysRemaining} DAYS"
            : $"MARKET: NEXT BONUS IN {Math.Max(0, daysUntilNextOpportunity)} {(daysUntilNextOpportunity == 1 ? "DAY" : "DAYS")}";
        string marketActionLine = GetFarmMarketActionLine(registry, activeMarketOpportunity);
        VisualCommand[] commands = GetFarmVisualCommands(registry, selectedPlot, selectedStarterItemId, isTileReady, isPaused, theme, activeMarketOpportunity);

        List<string> lines =
        [
            marketLine,
            marketActionLine,
            "ACTIONS",
            .. GetVisualCommandLines(commands, includeGroupHeaders: true),
            "MORE KEYS",
            FormatVisualCommand(new VisualCommand("TAB", $"Change {theme.StarterItemName}", true, Priority: 10, Group: VisualCommandGroup.Also)),
            FormatVisualCommand(new VisualCommand("F3", "Open machines", true, Priority: 9, Group: VisualCommandGroup.Also)),
            FormatVisualCommand(new VisualCommand("F10", "Market offers", true, Priority: 9, Group: VisualCommandGroup.Also))
        ];

        return lines.ToArray();
    }

    public static string[] GetFarmControlLines(
        ContentRegistry registry,
        ThemeVocabulary themeVocabulary = null,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null,
        int daysUntilNextOpportunity = 0)
    {
        return GetFarmControlLines(
            registry,
            selectedPlot: new FarmPlot(0, 0),
            selectedStarterItemId: registry.GetStarterItems().FirstOrDefault()?.Id,
            isTileReady: static (_, _) => false,
            isPaused: false,
            interfacePack: null,
            themeVocabulary,
            activeMarketOpportunity,
            getSellPrice,
            daysUntilNextOpportunity);
    }

    public static string[] GetFarmControlLines(ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        return
        [
            "MARKET: NEXT BONUS IN 0 DAYS",
            "F10 MARKET FOR OFFERS AND PROCESSING BONUSES",
            "LEGEND",
            $"EMPTY / {theme.ToUpperLabel(theme.UnmaintainedStateName)} = WAITING | {theme.ToUpperLabel(theme.MaintainedStateName)} = GROWING",
            $"READY STRIPE = {theme.ToUpperLabel(theme.CollectActionName)} NOW | BRIGHT BORDER = SELECTED",
            "ACTIONS",
            $"ARROWS TILE | TAB {theme.ToUpperLabel(theme.StarterItemName)} | P PLANT | W {theme.ToUpperLabel(theme.MaintainActionName)}",
            $"H {theme.ToUpperLabel(theme.CollectActionName)} | G DAY CYCLE | SPACE PAUSE",
            "PGUP/PGDN MACHINE | UP/DOWN RECIPE | M START | O COLLECT"
        ];
    }

    public static VisualCommand[] GetFarmVisualCommands(
        ContentRegistry registry,
        FarmPlot selectedPlot,
        string selectedStarterItemId,
        Func<int, int, bool> isTileReady,
        bool isPaused,
        ThemeVocabulary themeVocabulary = null,
        MarketOpportunity activeMarketOpportunity = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        bool isReady = selectedPlot is not null && isTileReady(selectedPlot.X, selectedPlot.Y);
        bool canPlant = !isPaused && selectedPlot is not null && selectedPlot.IsEmpty && !string.IsNullOrWhiteSpace(selectedStarterItemId);
        string starterName = string.IsNullOrWhiteSpace(selectedStarterItemId)
            ? theme.StarterItemName
            : registry.GetItem(selectedStarterItemId).Name;

        VisualCommand plantCommand = new(
            "P",
            $"Plant {starterName}",
            canPlant,
            Priority: canPlant ? 90 : 20,
            Group: canPlant ? VisualCommandGroup.Primary : VisualCommandGroup.Also,
            IsPrimary: canPlant,
            StatusLabel: canPlant ? "READY" : null,
            ReasonDisabled: isPaused ? "Paused" : selectedPlot?.IsEmpty == false ? $"{theme.TileName} occupied" : $"No {theme.StarterItemName.ToLowerInvariant()} selected",
            ThemeRole: canPlant ? VisualCommandThemeRole.Primary : VisualCommandThemeRole.Disabled);

        bool canMaintain = !isPaused && selectedPlot is not null && !selectedPlot.IsEmpty && !selectedPlot.IsWatered && !isReady;
        string maintainTarget = GetFarmTargetName(registry, selectedPlot, theme.ProducerName);
        VisualCommand maintainCommand = new(
            "W",
            canMaintain ? $"{theme.MaintainActionName} {maintainTarget}" : theme.MaintainActionName,
            canMaintain,
            Priority: canMaintain ? 100 : 15,
            Group: canMaintain ? VisualCommandGroup.Primary : VisualCommandGroup.Also,
            IsPrimary: canMaintain,
            StatusLabel: canMaintain ? "NEXT" : null,
            ReasonDisabled: isPaused ? "Paused" : selectedPlot is null || selectedPlot.IsEmpty ? $"No {theme.ProducerName.ToLowerInvariant()}" : isReady ? "Already ready" : selectedPlot.IsWatered ? $"Already {theme.MaintainedStateName.ToLowerInvariant()}" : null,
            ThemeRole: canMaintain ? VisualCommandThemeRole.Primary : VisualCommandThemeRole.Disabled);

        bool canCollect = !isPaused && selectedPlot is not null && isReady;
        VisualCommand collectCommand = new(
            "H",
            canCollect ? $"{theme.CollectActionName} {maintainTarget}" : theme.CollectActionName,
            canCollect,
            Priority: canCollect ? 110 : 10,
            Group: canCollect ? VisualCommandGroup.Primary : VisualCommandGroup.Also,
            IsPrimary: canCollect,
            StatusLabel: canCollect ? "READY" : null,
            ReasonDisabled: isPaused ? "Paused" : selectedPlot is null || selectedPlot.IsEmpty ? "Nothing planted" : "Not ready",
            ThemeRole: canCollect ? VisualCommandThemeRole.Ready : VisualCommandThemeRole.Disabled);

        List<VisualCommand> commands = [];
        VisualCommand? tileAction = GetFarmTileActionCommand(selectedPlot, isPaused, isReady, plantCommand, maintainCommand, collectCommand);
        if (tileAction is not null)
            commands.Add(tileAction.Value);

        commands.Add(new VisualCommand(
            "G",
            "Run farm cycle",
            !isPaused,
            Priority: 30,
            Group: VisualCommandGroup.Also,
            ReasonDisabled: isPaused ? "Paused" : null,
            ThemeRole: !isPaused ? VisualCommandThemeRole.Normal : VisualCommandThemeRole.Disabled));

        commands.Add(new VisualCommand(
            "TAB",
            $"Change {theme.StarterItemName}",
            true,
            Priority: 25,
            Group: VisualCommandGroup.Also,
            ThemeRole: VisualCommandThemeRole.Normal));

        string spaceAction = isPaused ? "Resume" : "Pause";
        commands.Add(new VisualCommand(
            "SPACE",
            spaceAction,
            true,
            Priority: isPaused ? 95 : 18,
            Group: isPaused ? VisualCommandGroup.Primary : VisualCommandGroup.Also,
            IsPrimary: isPaused,
            ThemeRole: isPaused ? VisualCommandThemeRole.Primary : VisualCommandThemeRole.Normal));

        if (activeMarketOpportunity?.IsActive == true)
        {
            string hint = GetFarmMarketCommandActionLabel(registry, activeMarketOpportunity);
            bool prioritizeMarket = HasActiveMarketRecipe(registry, activeMarketOpportunity.ItemId) && tileAction is null;
            commands.Add(new VisualCommand(
                "F3",
                hint,
                true,
                Priority: prioritizeMarket ? 120 : 40,
                Group: VisualCommandGroup.Market,
                IsPrimary: prioritizeMarket,
                StatusLabel: "MARKET BONUS",
                ThemeRole: VisualCommandThemeRole.MarketBonus));
        }

            return BuildVisibleFarmCommands(commands, maxSecondaryActions: 3);
    }

    public static VisualCommand[] GetMachineVisualCommands(
        ContentRegistry registry,
        ProcessorMachineInstance selectedMachine,
        string selectedRecipeId,
        Func<string, int> getCount,
        MarketOpportunity activeMarketOpportunity = null)
    {
        ProcessingRecipeDefinition selectedRecipe = registry.GetProcessingRecipesForMachine(selectedMachine.MachineId)
            .FirstOrDefault(recipe => string.Equals(recipe.Id, selectedRecipeId, StringComparison.OrdinalIgnoreCase))
            ?? registry.GetProcessingRecipesForMachine(selectedMachine.MachineId).FirstOrDefault();

        string recipeNavigationLabel = selectedRecipe is null
            ? "Recipe selection"
            : $"Recipe: {registry.GetItem(selectedRecipe.InputItemId).Name} -> {registry.GetItem(selectedRecipe.OutputItemId).Name}";

        List<VisualCommand> commands =
        [
            new("PGUP/PGDN", $"Machine: {GetMachineName(registry, selectedMachine.MachineId)}", true, Priority: 20, Group: VisualCommandGroup.Also),
            new("UP/DOWN", recipeNavigationLabel, true, Priority: 19, Group: VisualCommandGroup.Also)
        ];

        if (selectedMachine.HasOutputReady && !string.IsNullOrWhiteSpace(selectedMachine.OutputItemId))
        {
            string outputName = registry.GetItem(selectedMachine.OutputItemId).Name;
            commands.Add(new VisualCommand("O", $"Collect {outputName}", true, Priority: 110, Group: VisualCommandGroup.Primary, IsPrimary: true, StatusLabel: "READY", ThemeRole: VisualCommandThemeRole.Ready));
            commands.Add(new VisualCommand("M", "Start recipe", false, Priority: 15, Group: VisualCommandGroup.Also, ReasonDisabled: $"Collect {outputName} first", ThemeRole: VisualCommandThemeRole.Disabled));
            return OrderVisualCommands(commands).ToArray();
        }

        if (selectedMachine.IsWorking && !string.IsNullOrWhiteSpace(selectedMachine.OutputItemId))
        {
            string outputName = registry.GetItem(selectedMachine.OutputItemId).Name;
            commands.Add(new VisualCommand("M", $"Working on {outputName}", false, Priority: 30, Group: VisualCommandGroup.Primary, IsPrimary: true, ReasonDisabled: $"{selectedMachine.DaysRemaining} day{(selectedMachine.DaysRemaining == 1 ? string.Empty : "s")} left", ThemeRole: VisualCommandThemeRole.Disabled));
            commands.Add(new VisualCommand("O", "Collect output", false, Priority: 14, Group: VisualCommandGroup.Also, ReasonDisabled: "Not ready", ThemeRole: VisualCommandThemeRole.Disabled));
            return OrderVisualCommands(commands).ToArray();
        }

        if (selectedRecipe is null)
        {
            commands.Add(new VisualCommand("M", "No recipe loaded", false, Priority: 15, Group: VisualCommandGroup.Primary, IsPrimary: true, ReasonDisabled: "No recipe loaded", ThemeRole: VisualCommandThemeRole.Disabled));
            commands.Add(new VisualCommand("O", "Collect output", false, Priority: 14, Group: VisualCommandGroup.Also, ReasonDisabled: "Nothing ready", ThemeRole: VisualCommandThemeRole.Disabled));
            return OrderVisualCommands(commands).ToArray();
        }

        int availableInputCount = getCount(selectedRecipe.InputItemId);
        bool canStart = availableInputCount >= selectedRecipe.InputQuantity;
        bool hasMarketBonus = activeMarketOpportunity?.IsActive == true
            && string.Equals(activeMarketOpportunity.ItemId, selectedRecipe.OutputItemId, StringComparison.OrdinalIgnoreCase);
        string outputNameForSelectedRecipe = registry.GetItem(selectedRecipe.OutputItemId).Name;

        string startLabel = canStart
            ? $"Make {outputNameForSelectedRecipe}"
            : $"Need {selectedRecipe.InputQuantity} {registry.GetItem(selectedRecipe.InputItemId).Name}";

        commands.Add(new VisualCommand(
            "M",
            startLabel,
            canStart,
            Priority: hasMarketBonus ? 120 : canStart ? 100 : 24,
            Group: hasMarketBonus ? VisualCommandGroup.Market : VisualCommandGroup.Primary,
            IsPrimary: canStart || hasMarketBonus,
            StatusLabel: hasMarketBonus && canStart ? "MARKET BONUS" : null,
            ReasonDisabled: canStart ? null : $"Have {availableInputCount}",
            ThemeRole: hasMarketBonus ? VisualCommandThemeRole.MarketBonus : canStart ? VisualCommandThemeRole.Primary : VisualCommandThemeRole.Disabled));
        commands.Add(new VisualCommand("O", "Collect output", false, Priority: 14, Group: VisualCommandGroup.Also, ReasonDisabled: "Nothing ready", ThemeRole: VisualCommandThemeRole.Disabled));

        return OrderVisualCommands(commands).ToArray();
    }

    public static string FormatVisualCommand(VisualCommand command)
    {
        string keyLabel = FormatVisualKeyLabel(command.KeyLabel);
        string suffix = command.IsEnabled
            ? string.IsNullOrWhiteSpace(command.StatusLabel) ? string.Empty : $" - {command.StatusLabel}"
            : $" - {(string.IsNullOrWhiteSpace(command.ReasonDisabled) ? "Unavailable" : command.ReasonDisabled)}";
        return $"{keyLabel} {command.ActionLabel}{suffix}";
    }

    public static string FormatVisualKeyLabel(string keyLabel)
    {
        string safeKey = (keyLabel ?? string.Empty).ToUpperInvariant();
        return safeKey.Length switch
        {
            <= 1 => $"[  {safeKey}  ]",
            2 => $"[ {safeKey} ]",
            _ => $"[ {safeKey} ]"
        };
    }

    public static string[] GetVisualCommandLines(IEnumerable<VisualCommand> commands, bool includeGroupHeaders)
    {
        List<string> lines = [];
        VisualCommandGroup? currentGroup = null;

        foreach (VisualCommand command in OrderVisualCommands(commands))
        {
            if (includeGroupHeaders && currentGroup != command.Group)
            {
                currentGroup = command.Group;
                lines.Add(GetVisualCommandGroupLabel(command.Group));
            }

            lines.Add(FormatVisualCommand(command));
        }

        return lines.ToArray();
    }

    public static VisualCommand? GetPrimaryVisualCommand(IEnumerable<VisualCommand> commands)
    {
        return OrderVisualCommands(commands)
            .Select(command => (VisualCommand?)command)
            .FirstOrDefault();
    }

    public static VisualCommand[] GetSecondaryVisualCommands(IEnumerable<VisualCommand> commands, int maxCount)
    {
        return OrderVisualCommands(commands)
            .Skip(1)
            .Take(maxCount)
            .ToArray();
    }

    public static VisualCommand[] OrderVisualCommands(IEnumerable<VisualCommand> commands)
    {
        return commands
            .OrderByDescending(command => command.IsPrimary)
            .ThenBy(command => GetVisualCommandGroupOrder(command.Group))
            .ThenByDescending(command => command.Priority)
            .ThenBy(command => command.KeyLabel, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetVisualCommandGroupLabel(VisualCommandGroup group)
    {
        return group switch
        {
            VisualCommandGroup.Primary => "Primary:",
            VisualCommandGroup.Market => "Market:",
            _ => "Also:"
        };
    }

    private static int GetVisualCommandGroupOrder(VisualCommandGroup group)
    {
        return group switch
        {
            VisualCommandGroup.Primary => 0,
            VisualCommandGroup.Market => 1,
            _ => 2
        };
    }

    private static bool HasActiveMarketRecipe(ContentRegistry registry, string outputItemId)
    {
        return registry.ProcessingRecipesById.Values.Any(recipe => string.Equals(recipe.OutputItemId, outputItemId, StringComparison.OrdinalIgnoreCase));
    }

    private static VisualCommand? GetFarmTileActionCommand(
        FarmPlot selectedPlot,
        bool isPaused,
        bool isReady,
        VisualCommand plantCommand,
        VisualCommand maintainCommand,
        VisualCommand collectCommand)
    {
        if (selectedPlot is null)
            return null;

        if (selectedPlot.IsEmpty)
            return plantCommand;

        if (isReady)
            return collectCommand;

        if (!selectedPlot.IsWatered || isPaused)
            return maintainCommand;

        return null;
    }

    private static VisualCommand[] BuildVisibleFarmCommands(IEnumerable<VisualCommand> commands, int maxSecondaryActions)
    {
        VisualCommand[] orderedCommands = OrderVisualCommands(commands);
        VisualCommand? primary = orderedCommands
            .Where(command => command.IsPrimary || command.Group == VisualCommandGroup.Primary)
            .Select(command => (VisualCommand?)command)
            .FirstOrDefault();
        List<VisualCommand> visibleCommands = [];

        if (primary is not null)
            visibleCommands.Add(primary.Value);

        IEnumerable<VisualCommand> enabledSecondaryCommands = orderedCommands
            .Where(command => primary is null || !command.Equals(primary.Value))
            .Where(command => command.IsEnabled)
            .Where(command => command.Group == VisualCommandGroup.Market || command.Group == VisualCommandGroup.Also)
            .Take(maxSecondaryActions);

        visibleCommands.AddRange(enabledSecondaryCommands);
        return visibleCommands.ToArray();
    }

    public static string[] GetMachineRecipeLines(
        ContentRegistry registry,
        string machineId,
        string selectedRecipeId,
        Func<string, int> getCount,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null)
    {
        IReadOnlyList<ProcessingRecipeDefinition> recipes = registry.GetProcessingRecipesForMachine(machineId);
        if (recipes.Count == 0)
            return ["No recipes loaded."];

        string effectiveSelectedRecipeId = string.IsNullOrWhiteSpace(selectedRecipeId)
            ? recipes[0].Id
            : selectedRecipeId;

        return recipes.Select((recipe, index) => FormatMachineRecipeLine(
                registry,
                recipe,
            index + 1,
                string.Equals(recipe.Id, effectiveSelectedRecipeId, StringComparison.OrdinalIgnoreCase),
                getCount,
                activeMarketOpportunity,
                getSellPrice))
            .ToArray();
    }

    public static string FormatMachineRecipeLine(
        ContentRegistry registry,
        ProcessingRecipeDefinition recipe,
        int displayIndex,
        bool isSelected,
        Func<string, int> getCount,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null)
    {
        ProcessingRecipeEconomics economics = CalculateProcessingRecipeEconomics(registry, recipe, getSellPrice);
        int availableCount = getCount(recipe.InputItemId);
        bool hasInputs = availableCount >= recipe.InputQuantity;
        bool hasMarketBonus = activeMarketOpportunity?.IsActive == true
            && string.Equals(activeMarketOpportunity.ItemId, recipe.OutputItemId, StringComparison.OrdinalIgnoreCase);
        string outputValueSegment = hasMarketBonus
            ? $"Output ${economics.OutputValue} during {activeMarketOpportunity.DisplayName}"
            : $"Output ${economics.OutputValue}";
        string readinessSegment = hasInputs ? "Ready to start" : "Missing input";
        string marketBonusSegment = hasMarketBonus ? " | MARKET BONUS" : string.Empty;

        return $"{(isSelected ? ">" : " ")} {displayIndex}. {recipe.InputQuantity} {registry.GetItem(recipe.InputItemId).Name} -> {recipe.OutputQuantity} {registry.GetItem(recipe.OutputItemId).Name} | {economics.ProcessingDays} days | Need: {recipe.InputQuantity} {registry.GetItem(recipe.InputItemId).Name} | Have: {availableCount} | {readinessSegment} | {outputValueSegment}{marketBonusSegment}";
    }

    public static int GetSelectedSeedCount(string selectedSeedItemId, Func<string, int> getCount)
    {
        return getCount(selectedSeedItemId);
    }

    public static int GetSelectedSeedBuyPrice(ContentRegistry registry, string selectedSeedItemId)
    {
        return registry.GetItem(selectedSeedItemId).BuyPrice;
    }

    public static bool CanAffordSelectedSeed(ContentRegistry registry, string selectedSeedItemId, int money)
    {
        int buyPrice = GetSelectedSeedBuyPrice(registry, selectedSeedItemId);
        return buyPrice > 0 && money >= buyPrice;
    }

    public static int GetTotalSeedCount(ContentRegistry registry, Func<string, int> getCount)
    {
        return registry.GetStarterItems()
            .Sum(item => getCount(item.Id));
    }

    public static int GetTotalSellableCropCount(ContentRegistry registry, Func<string, int> getCount)
    {
        return registry.Items
            .Where(item => !registry.IsStarterItem(item.Id) && item.SellPrice > 0)
            .Sum(item => getCount(item.Id));
    }

    public static int GetEstimatedSellValue(ContentRegistry registry, Func<string, int> getCount, Func<string, int> getSellPrice = null)
    {
        return registry.Items
            .Where(item => !registry.IsStarterItem(item.Id) && item.SellPrice > 0)
            .Sum(item => getCount(item.Id) * GetDisplaySellPrice(item, getSellPrice));
    }

    public static string[] GetInventoryLines(ContentRegistry registry, Func<string, int> getCount, bool includeDebugDetails, Func<string, int> getSellPrice = null)
    {
        HashSet<string> machineRelatedItemIds = registry.ProcessingRecipes
            .SelectMany(recipe => new[] { recipe.InputItemId, recipe.OutputItemId })
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> lines = [];
        AddInventorySection(
            registry,
            lines,
            "Seeds:",
            registry.GetStarterItems(),
            getCount,
            includeDebugDetails,
            machineRelatedItemIds,
            getSellPrice);
        AddInventorySection(
            registry,
            lines,
            "Crops / Inputs:",
            registry.Items.Where(item => string.Equals(item.Type, "crop", StringComparison.OrdinalIgnoreCase)),
            getCount,
            includeDebugDetails,
            machineRelatedItemIds,
            getSellPrice);
        AddInventorySection(
            registry,
            lines,
            "Processed:",
            registry.Items.Where(item => string.Equals(item.Type, "processed_good", StringComparison.OrdinalIgnoreCase)),
            getCount,
            includeDebugDetails,
            machineRelatedItemIds,
            getSellPrice);
        AddInventorySection(
            registry,
            lines,
            "Other:",
            registry.Items.Where(item =>
                !registry.IsStarterItem(item.Id)
                && !string.Equals(item.Type, "crop", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(item.Type, "processed_good", StringComparison.OrdinalIgnoreCase)),
            getCount,
            includeDebugDetails,
            machineRelatedItemIds,
            getSellPrice);

        if (lines.Count == 0)
            return ["Inventory empty."];

        return lines.ToArray();
    }

    public static string[] GetStorageLines(ContentRegistry registry, Func<string, int> getCount, bool includeDebugDetails, Func<string, int> getSellPrice = null)
    {
        return GetInventoryLines(registry, getCount, includeDebugDetails, getSellPrice);
    }

    public static string[] GetMachineStatusLines(
        ContentRegistry registry,
        ProcessorMachineInstance machine,
        Func<string, int> getCount,
        bool includeDebugDetails = false,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null)
    {
        string machineName = registry.MachinesById.TryGetValue(machine.MachineId, out MachineDefinition machineDefinition)
            ? machineDefinition.Name
            : machine.MachineId;

        if (machine.HasOutputReady && !string.IsNullOrWhiteSpace(machine.OutputItemId))
        {
            ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe(machine.CurrentRecipeId);
            ProcessingRecipeEconomics economics = CalculateProcessingRecipeEconomics(registry, recipe, getSellPrice);
            string outputName = registry.GetItem(machine.OutputItemId).Name;
            List<string> readyLines =
            [
                $"Status: Output Ready",
                $"Current: {recipe.InputQuantity} {registry.GetItem(recipe.InputItemId).Name} -> {recipe.OutputQuantity} {outputName}",
                $"Output: {machine.OutputQuantity} {outputName}",
                $"Press O to collect.",
                GetMachineMarketStatusLine(activeMarketOpportunity, recipe, economics.OutputValue)
            ];

            readyLines.RemoveAll(string.IsNullOrWhiteSpace);

            if (includeDebugDetails)
                readyLines.Add($"Recipe id: {machine.CurrentRecipeId ?? "none"}");

            return readyLines.ToArray();
        }

        if (machine.IsWorking && !string.IsNullOrWhiteSpace(machine.InputItemId) && !string.IsNullOrWhiteSpace(machine.OutputItemId))
        {
            ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe(machine.CurrentRecipeId);
            ProcessingRecipeEconomics economics = CalculateProcessingRecipeEconomics(registry, recipe, getSellPrice);
            List<string> workingLines =
            [
                $"Status: Working",
                $"Current: {machine.InputQuantity} {registry.GetItem(machine.InputItemId).Name} -> {machine.OutputQuantity} {registry.GetItem(machine.OutputItemId).Name}",
                $"Days remaining: {machine.DaysRemaining}",
                $"Output: {registry.GetItem(machine.OutputItemId).Name}",
                GetMachineMarketStatusLine(activeMarketOpportunity, recipe, economics.OutputValue)
            ];

            workingLines.RemoveAll(string.IsNullOrWhiteSpace);

            if (includeDebugDetails)
                workingLines.Add($"Recipe id: {machine.CurrentRecipeId ?? "none"}");

            return workingLines.ToArray();
        }

        List<string> lines =
        [
            "Status: Idle",
            "Press M to start the selected recipe."
        ];
        string[] availableRecipeLines = GetAvailableMachineRecipeLines(registry, machine.MachineId, getCount);

        if (availableRecipeLines.Length > 0)
        {
            lines.Add($"Ready inputs: {string.Join(" | ", availableRecipeLines)}");
        }
        else
        {
            lines.Add(GetMachineNeedText(registry, machine.MachineId));
        }

        if (includeDebugDetails)
            lines.Add($"Machine id: {machine.MachineId}");

        return lines.ToArray();
    }
    public static string FormatCleanupStatusLine(CleanupSiteState cleanupSite)
    {
        if (cleanupSite is null)
            return string.Empty;

        return cleanupSite.IsCleared
            ? $"Cleanup: {cleanupSite.Name} cleared"
            : $"Cleanup: {cleanupSite.Name} uncleared, ${cleanupSite.Cost}";
    }

    public static string GetSimulationStatusLine(bool isPaused, bool isAutoPlayRunning)
    {
        if (isAutoPlayRunning)
            return isPaused ? "AUTOPLAY PAUSED - press Space to resume" : "AUTOPLAY RUNNING - press Space to pause";

        return isPaused ? "PAUSED - press Space to resume" : "LIVE - press Space to pause";
    }

    public static string[] GetCompactMachineStatusLines(ContentRegistry registry, IReadOnlyList<ProcessorMachineInstance> machines, string selectedMachineId)
    {
        List<string> lines = [$"Selected: {GetMachineName(registry, selectedMachineId)}"];
        foreach (ProcessorMachineInstance machine in machines)
            lines.AddRange(GetCompactMachineStatusLines(registry, machine));

        return lines.ToArray();
    }

    public static string[] GetCompactMachineStatusLines(ContentRegistry registry, ProcessorMachineInstance machine)
    {
        string machineName = registry.MachinesById.TryGetValue(machine.MachineId, out MachineDefinition machineDefinition)
            ? machineDefinition.Name
            : machine.MachineId;

        if (machine.HasOutputReady && !string.IsNullOrWhiteSpace(machine.OutputItemId))
            return [$"{machineName}: {registry.GetItem(machine.OutputItemId).Name} ready! Press F3 then O"];

        if (machine.IsWorking && !string.IsNullOrWhiteSpace(machine.CurrentRecipeId))
        {
            ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe(machine.CurrentRecipeId);
            string dayLabel = machine.DaysRemaining == 1 ? "day" : "days";
            return [$"{machineName}: Working on {registry.GetItem(recipe.OutputItemId).Name}, {machine.DaysRemaining} {dayLabel} left"]; 
        }

        return [$"{machineName}: Idle"]; 
    }

    public static FarmMachineCard[] GetFarmMachineCards(
        ContentRegistry registry,
        IReadOnlyList<ProcessorMachineInstance> machines,
        string selectedMachineId,
        Func<string, string> getSelectedRecipeId,
        Func<string, int> getCount,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null,
        int maxCards = 2)
    {
        if (machines is null || machines.Count == 0)
            return [];

        return machines
            .Take(Math.Max(1, maxCards))
            .Select(machine => BuildFarmMachineCard(
                registry,
                machine,
                string.Equals(machine.MachineId, selectedMachineId, StringComparison.OrdinalIgnoreCase)
                    ? getSelectedRecipeId?.Invoke(machine.MachineId)
                    : null,
                getCount,
                activeMarketOpportunity,
                getSellPrice,
                isSelected: string.Equals(machine.MachineId, selectedMachineId, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    public static string[] GetFarmMachinePanelLines(
        ContentRegistry registry,
        IReadOnlyList<ProcessorMachineInstance> machines,
        string selectedMachineId,
        Func<string, string> getSelectedRecipeId,
        Func<string, int> getCount,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null,
        int maxCards = 2)
    {
        FarmMachineCard[] cards = GetFarmMachineCards(
            registry,
            machines,
            selectedMachineId,
            getSelectedRecipeId,
            getCount,
            activeMarketOpportunity,
            getSellPrice,
            maxCards);

        if (cards.Length == 0)
            return ["No machines loaded.", "F3 Machines for details"];

        List<string> lines = [];
        foreach (FarmMachineCard card in cards)
        {
            if (lines.Count > 0)
                lines.Add(string.Empty);

            lines.Add($"[ M ] {card.MachineName}");
            lines.AddRange(GetMachineCardLines(
                registry,
                machines.First(machine => string.Equals(machine.MachineId, card.MachineId, StringComparison.OrdinalIgnoreCase)),
                card.IsSelected ? getSelectedRecipeId?.Invoke(card.MachineId) : null,
                getCount,
                activeMarketOpportunity,
                getSellPrice,
                card.IsSelected));
        }

        lines.Add(string.Empty);
        lines.Add(cards.Length < machines.Count
            ? $"F3 Machines for details (+{machines.Count - cards.Length} more)"
            : "F3 Machines for details");
        return lines.ToArray();
    }

    public static string[] GetMachineCardLines(
        ContentRegistry registry,
        ProcessorMachineInstance machine,
        string selectedRecipeId,
        Func<string, int> getCount,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null,
        bool isSelected = false)
    {
        FarmMachineCard card = BuildFarmMachineCard(registry, machine, selectedRecipeId, getCount, activeMarketOpportunity, getSellPrice, isSelected);
        List<string> lines = [card.StatusLabel, .. card.DetailLines];

        if (card.ActionCommand is not null)
            lines.Add(FormatVisualCommand(card.ActionCommand.Value));

        return lines.ToArray();
    }

    public static string GetMachineStatusLabel(ProcessorMachineInstance machine)
    {
        if (machine.HasOutputReady)
            return "READY!";

        if (machine.IsWorking)
            return "WORKING...";

        return "IDLE";
    }

    public static VisualCommand? GetMachineActionCommand(
        ContentRegistry registry,
        ProcessorMachineInstance machine,
        string selectedRecipeId,
        Func<string, int> getCount,
        MarketOpportunity activeMarketOpportunity = null)
    {
        if (machine.HasOutputReady && !string.IsNullOrWhiteSpace(machine.OutputItemId))
        {
            string outputName = registry.GetItem(machine.OutputItemId).Name;
            return new VisualCommand("O", $"Collect {outputName}", true, Priority: 110, Group: VisualCommandGroup.Primary, IsPrimary: true, StatusLabel: "READY", ThemeRole: VisualCommandThemeRole.Ready);
        }

        if (machine.IsWorking)
            return null;

        ProcessingRecipeDefinition selectedRecipe = ResolveFarmMachinePanelRecipe(registry, machine, selectedRecipeId, getCount, preferSelectedRecipe: true);
        if (selectedRecipe is null)
        {
            return new VisualCommand("M", "No recipe loaded", false, Priority: 15, Group: VisualCommandGroup.Also, ReasonDisabled: "No recipe loaded", ThemeRole: VisualCommandThemeRole.Disabled);
        }

        int availableInputCount = getCount(selectedRecipe.InputItemId);
        bool canStart = availableInputCount >= selectedRecipe.InputQuantity;
        bool hasMarketBonus = activeMarketOpportunity?.IsActive == true
            && string.Equals(activeMarketOpportunity.ItemId, selectedRecipe.OutputItemId, StringComparison.OrdinalIgnoreCase);
        string outputNameForSelectedRecipe = registry.GetItem(selectedRecipe.OutputItemId).Name;
        string inputName = registry.GetItem(selectedRecipe.InputItemId).Name;

        return new VisualCommand(
            "M",
            canStart ? $"Start {outputNameForSelectedRecipe}" : $"Need {selectedRecipe.InputQuantity} {inputName}",
            canStart,
            Priority: hasMarketBonus ? 120 : canStart ? 100 : 24,
            Group: hasMarketBonus ? VisualCommandGroup.Market : VisualCommandGroup.Primary,
            IsPrimary: canStart || hasMarketBonus,
            StatusLabel: hasMarketBonus && canStart ? "MARKET BONUS" : null,
            ReasonDisabled: canStart ? null : $"Have {availableInputCount}",
            ThemeRole: hasMarketBonus ? VisualCommandThemeRole.MarketBonus : canStart ? VisualCommandThemeRole.Primary : VisualCommandThemeRole.Disabled);
    }

    public static string[] GetCompactMachineStatusLines(
        ContentRegistry registry,
        IReadOnlyList<ProcessorMachineInstance> machines,
        string selectedMachineId,
        MarketOpportunity activeMarketOpportunity)
    {
        List<string> lines = [$"Selected: {GetMachineName(registry, selectedMachineId)}"];

        string opportunityHint = GetMachineOpportunityHintLine(registry, activeMarketOpportunity);
        if (!string.IsNullOrWhiteSpace(opportunityHint))
            lines.Add(opportunityHint);

        foreach (ProcessorMachineInstance machine in machines)
            lines.AddRange(GetCompactMachineStatusLines(registry, machine));

        return lines.ToArray();
    }

    private static string GetMachineMarketStatusLine(MarketOpportunity activeMarketOpportunity, ProcessingRecipeDefinition recipe, int outputValue)
    {
        if (activeMarketOpportunity?.IsActive != true || !string.Equals(activeMarketOpportunity.ItemId, recipe.OutputItemId, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return $"Market: {activeMarketOpportunity.DisplayName} active, output ${outputValue}";
    }

    private static string GetMachineOpportunityHintLine(ContentRegistry registry, MarketOpportunity activeMarketOpportunity)
    {
        if (activeMarketOpportunity?.IsActive != true)
            return string.Empty;

        ProcessingRecipeDefinition recipe = registry.ProcessingRecipesById.Values
            .OrderBy(candidate => registry.GetMachine(candidate.MachineId).Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(candidate => string.Equals(candidate.OutputItemId, activeMarketOpportunity.ItemId, StringComparison.OrdinalIgnoreCase));

        if (recipe is null)
            return string.Empty;

        return $"{activeMarketOpportunity.DisplayName}: use {registry.GetMachine(recipe.MachineId).Name} recipe {registry.GetItem(recipe.InputItemId).Name} -> {registry.GetItem(recipe.OutputItemId).Name}";
    }

    private static FarmMachineCard BuildFarmMachineCard(
        ContentRegistry registry,
        ProcessorMachineInstance machine,
        string selectedRecipeId,
        Func<string, int> getCount,
        MarketOpportunity activeMarketOpportunity,
        Func<string, int> getSellPrice,
        bool isSelected)
    {
        string machineName = GetMachineName(registry, machine.MachineId);
        string statusLabel = GetMachineStatusLabel(machine);
        VisualCommand? actionCommand = GetMachineActionCommand(registry, machine, selectedRecipeId, getCount, activeMarketOpportunity);

        if (machine.HasOutputReady && !string.IsNullOrWhiteSpace(machine.OutputItemId) && !string.IsNullOrWhiteSpace(machine.CurrentRecipeId))
        {
            ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe(machine.CurrentRecipeId);
            string outputName = registry.GetItem(machine.OutputItemId).Name;
            List<string> detailLines =
            [
                $"Output: {machine.OutputQuantity} {outputName}",
                $"{recipe.InputQuantity} {registry.GetItem(recipe.InputItemId).Name} -> {recipe.OutputQuantity} {outputName}"
            ];

            string marketLine = GetMachineCardMarketLine(registry, recipe, activeMarketOpportunity, getSellPrice, activeLabel: "MARKET BONUS active");
            if (!string.IsNullOrWhiteSpace(marketLine))
                detailLines.Add(marketLine);

            return new FarmMachineCard(machine.MachineId, machineName, statusLabel, detailLines.ToArray(), actionCommand, VisualCommandThemeRole.Ready, isSelected);
        }

        if (machine.IsWorking && !string.IsNullOrWhiteSpace(machine.InputItemId) && !string.IsNullOrWhiteSpace(machine.OutputItemId))
        {
            ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe(machine.CurrentRecipeId);
            string dayLabel = machine.DaysRemaining == 1 ? "day" : "days";
            List<string> detailLines =
            [
                $"{machine.InputQuantity} {registry.GetItem(machine.InputItemId).Name} -> {machine.OutputQuantity} {registry.GetItem(machine.OutputItemId).Name}",
                $"{machine.DaysRemaining} {dayLabel} left"
            ];

            string marketLine = GetMachineCardMarketLine(registry, recipe, activeMarketOpportunity, getSellPrice, activeLabel: "MARKET BONUS active");
            if (!string.IsNullOrWhiteSpace(marketLine))
                detailLines.Add(marketLine);

            return new FarmMachineCard(machine.MachineId, machineName, statusLabel, detailLines.ToArray(), actionCommand, VisualCommandThemeRole.Disabled, isSelected);
        }

        ProcessingRecipeDefinition selectedRecipe = ResolveFarmMachinePanelRecipe(registry, machine, selectedRecipeId, getCount, preferSelectedRecipe: isSelected);
        if (selectedRecipe is null)
            return new FarmMachineCard(machine.MachineId, machineName, statusLabel, ["No recipe loaded"], actionCommand, VisualCommandThemeRole.Disabled, isSelected);

        int availableInputCount = getCount(selectedRecipe.InputItemId);
        string recipeLabelPrefix = isSelected ? "Selected" : "Next";
        List<string> idleLines =
        [
            $"{recipeLabelPrefix}: {selectedRecipe.InputQuantity} {registry.GetItem(selectedRecipe.InputItemId).Name} -> {selectedRecipe.OutputQuantity} {registry.GetItem(selectedRecipe.OutputItemId).Name}",
            $"Have: {availableInputCount} {registry.GetItem(selectedRecipe.InputItemId).Name}"
        ];

        string idleMarketLine = GetMachineCardMarketLine(registry, selectedRecipe, activeMarketOpportunity, getSellPrice, activeLabel: null);
        if (!string.IsNullOrWhiteSpace(idleMarketLine))
            idleLines.Add(idleMarketLine);

        VisualCommandThemeRole themeRole = actionCommand?.ThemeRole ?? VisualCommandThemeRole.Normal;
        return new FarmMachineCard(machine.MachineId, machineName, statusLabel, idleLines.ToArray(), actionCommand, themeRole, isSelected);
    }

    private static ProcessingRecipeDefinition ResolveFarmMachinePanelRecipe(
        ContentRegistry registry,
        ProcessorMachineInstance machine,
        string selectedRecipeId,
        Func<string, int> getCount,
        bool preferSelectedRecipe)
    {
        if (!string.IsNullOrWhiteSpace(machine.CurrentRecipeId))
            return registry.GetProcessingRecipe(machine.CurrentRecipeId);

        IReadOnlyList<ProcessingRecipeDefinition> recipes = registry.GetProcessingRecipesForMachine(machine.MachineId);
        if (recipes.Count == 0)
            return null;

        if (preferSelectedRecipe && !string.IsNullOrWhiteSpace(selectedRecipeId))
        {
            ProcessingRecipeDefinition explicitlySelected = recipes.FirstOrDefault(recipe => string.Equals(recipe.Id, selectedRecipeId, StringComparison.OrdinalIgnoreCase));
            if (explicitlySelected is not null)
                return explicitlySelected;
        }

        return recipes.FirstOrDefault(recipe => getCount(recipe.InputItemId) >= recipe.InputQuantity)
            ?? recipes.FirstOrDefault();
    }

    private static string GetMachineCardMarketLine(
        ContentRegistry registry,
        ProcessingRecipeDefinition recipe,
        MarketOpportunity activeMarketOpportunity,
        Func<string, int> getSellPrice,
        string activeLabel)
    {
        if (activeMarketOpportunity?.IsActive != true || recipe is null || !string.Equals(activeMarketOpportunity.ItemId, recipe.OutputItemId, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(activeLabel))
            return activeLabel;

        int baseValue = recipe.OutputQuantity * registry.GetItem(recipe.OutputItemId).SellPrice;
        int boostedValue = recipe.OutputQuantity * GetDisplaySellPrice(registry.GetItem(recipe.OutputItemId), getSellPrice);
        return $"MARKET BONUS: {registry.GetItem(recipe.OutputItemId).Name} ${baseValue} -> ${boostedValue}";
    }

    private static string GetFarmTargetName(ContentRegistry registry, FarmPlot selectedPlot, string emptyFallback)
    {
        if (selectedPlot?.PlantedCrop is null)
            return emptyFallback;

        TileProducerDefinition tileProducer = registry.GetTileProducer(selectedPlot.PlantedCrop.CropDefinitionId);
        return registry.GetItem(tileProducer.OutputItemId).Name;
    }

    private static string GetFarmMarketActionLine(ContentRegistry registry, MarketOpportunity activeMarketOpportunity)
    {
        if (activeMarketOpportunity?.IsActive != true)
            return "F10 MARKET FOR OFFERS AND PROCESSING BONUSES";

        return $"PRESS F3 TO MAKE {registry.GetItem(activeMarketOpportunity.ItemId).Name.ToUpperInvariant()}";
    }

    private static string GetFarmMarketCommandActionLabel(ContentRegistry registry, MarketOpportunity activeMarketOpportunity)
    {
        if (activeMarketOpportunity?.IsActive != true)
            return "Machines";

        return $"Machines: make {registry.GetItem(activeMarketOpportunity.ItemId).Name}";
    }

    public static string[] GetAvailableMachineRecipeLines(ContentRegistry registry, string machineId, Func<string, int> getCount)
    {
        return registry
            .GetProcessingRecipesForMachine(machineId)
            .Where(recipe => getCount(recipe.InputItemId) >= recipe.InputQuantity)
            .Select(recipe => FormatRecipeEconomics(registry, recipe))
            .ToArray();
    }

    public static ProcessingRecipeEconomics CalculateProcessingRecipeEconomics(ContentRegistry registry, ProcessingRecipeDefinition recipe, Func<string, int> getSellPrice = null)
    {
        return new ProcessingRecipeEconomics(
            GetInputValue(registry, recipe, getSellPrice),
            GetOutputValue(registry, recipe, getSellPrice),
            GetValueAdded(registry, recipe, getSellPrice),
            recipe.ProcessingDays);
    }

    public static MarketOfferDirectEconomics CalculateMarketOfferDirectValue(ContentRegistry registry, MarketOfferDefinition marketOffer, Func<string, int> getSellPrice = null)
    {
        int directValue = marketOffer.Quantity * GetDisplaySellPrice(registry.GetItem(marketOffer.ItemId), getSellPrice);
        return new MarketOfferDirectEconomics(directValue, directValue - marketOffer.Price);
    }

    public static MarketOfferProcessingEconomics? FindBestProcessingUseForMarketOffer(ContentRegistry registry, MarketOfferDefinition marketOffer, Func<string, int> getSellPrice = null)
    {
        MarketOfferProcessingEconomics? bestEconomics = null;

        foreach (ProcessingRecipeDefinition recipe in registry.ProcessingRecipes.Where(candidate =>
                     string.Equals(candidate.InputItemId, marketOffer.ItemId, StringComparison.OrdinalIgnoreCase)
                     && candidate.InputQuantity > 0))
        {
            MarketOfferProcessingEconomics? economics = CalculateMarketOfferProcessingEconomics(registry, marketOffer, recipe, getSellPrice);
            if (economics is null)
                continue;

            if (bestEconomics is null
                || economics.Value.ProfitIfBought > bestEconomics.Value.ProfitIfBought
                || (economics.Value.ProfitIfBought == bestEconomics.Value.ProfitIfBought
                    && economics.Value.OutputValue > bestEconomics.Value.OutputValue))
            {
                bestEconomics = economics;
            }
        }

        return bestEconomics;
    }

    public static MarketOfferProcessingEconomics? CalculateMarketOfferProcessingEconomics(
        ContentRegistry registry,
        MarketOfferDefinition marketOffer,
        ProcessingRecipeDefinition recipe,
        Func<string, int> getSellPrice = null)
    {
        if (!string.Equals(recipe.InputItemId, marketOffer.ItemId, StringComparison.OrdinalIgnoreCase))
            return null;

        if (recipe.InputQuantity <= 0)
            return null;

        int batchCount = marketOffer.Quantity / recipe.InputQuantity;
        if (batchCount <= 0)
            return null;

        int totalOutputQuantity = batchCount * recipe.OutputQuantity;
        int outputValue = totalOutputQuantity * GetDisplaySellPrice(registry.GetItem(recipe.OutputItemId), getSellPrice);
        ProcessingRecipeEconomics recipeEconomics = CalculateProcessingRecipeEconomics(registry, recipe, getSellPrice);

        return new MarketOfferProcessingEconomics(
            recipe.Id,
            recipe.InputItemId,
            recipe.InputQuantity,
            recipe.OutputItemId,
            recipe.OutputQuantity,
            batchCount,
            totalOutputQuantity,
            outputValue,
            outputValue - marketOffer.Price,
            batchCount * recipeEconomics.ValueAdded,
            recipe.ProcessingDays);
    }

    public static string[] GetMarketOfferDisplayLines(
        ContentRegistry registry,
        MarketOfferDefinition marketOffer,
        int displayIndex,
        int money,
        bool includeDebugDetails,
        Func<string, int> getSellPrice = null)
    {
        ItemDefinition item = registry.GetItem(marketOffer.ItemId);
        MarketOfferDirectEconomics directEconomics = CalculateMarketOfferDirectValue(registry, marketOffer, getSellPrice);
        MarketOfferProcessingEconomics? bestProcessingUse = FindBestProcessingUseForMarketOffer(registry, marketOffer, getSellPrice);
        string affordability = money >= marketOffer.Price ? "Affordable" : "Need more money";

        List<string> lines =
        [
            $"{displayIndex}. {marketOffer.Quantity} {item.Name} - ${marketOffer.Price} - {affordability}",
            $"   Direct value: ${directEconomics.DirectValue}",
            $"   Direct resale: {FormatSignedCurrency(directEconomics.DirectResaleProfit)}"
        ];

        if (bestProcessingUse is not null)
        {
            MarketOfferProcessingEconomics economics = bestProcessingUse.Value;
            lines.Add($"   Best use: {FormatMarketProcessingUse(registry, economics)}");
            lines.Add($"   Output value: {FormatMarketOutputValueLine(registry, economics, getSellPrice)}");
            lines.Add($"   Profit if bought: {FormatSignedCurrency(economics.ProfitIfBought)}");
            lines.Add($"   Value added if grown: {FormatSignedCurrency(economics.ValueAddedIfGrown)}");
        }

        if (includeDebugDetails)
            lines.Add($"   Category: {marketOffer.Category} | id: {marketOffer.Id}");

        return lines.ToArray();
    }

    public static int GetInputValue(ContentRegistry registry, ProcessingRecipeDefinition recipe, Func<string, int> getSellPrice = null)
    {
        return recipe.InputQuantity * GetDisplaySellPrice(registry.GetItem(recipe.InputItemId), getSellPrice);
    }

    public static int GetOutputValue(ContentRegistry registry, ProcessingRecipeDefinition recipe, Func<string, int> getSellPrice = null)
    {
        return recipe.OutputQuantity * GetDisplaySellPrice(registry.GetItem(recipe.OutputItemId), getSellPrice);
    }

    public static int GetValueAdded(ContentRegistry registry, ProcessingRecipeDefinition recipe, Func<string, int> getSellPrice = null)
    {
        return GetOutputValue(registry, recipe, getSellPrice) - GetInputValue(registry, recipe, getSellPrice);
    }

    public static string FormatRecipeEconomics(ContentRegistry registry, ProcessingRecipeDefinition recipe, Func<string, int> getSellPrice = null)
    {
        ProcessingRecipeEconomics economics = CalculateProcessingRecipeEconomics(registry, recipe, getSellPrice);
        return $"{recipe.InputQuantity} {registry.GetItem(recipe.InputItemId).Name} -> {recipe.OutputQuantity} {registry.GetItem(recipe.OutputItemId).Name}, {economics.ProcessingDays} days, {FormatSignedCurrency(economics.ValueAdded)}";
    }

    public static IReadOnlyList<ProductionChain> BuildProductionChains(
        ContentRegistry registry,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null)
    {
        Dictionary<string, List<ProcessingRecipeDefinition>> recipesByInput = registry.ProcessingRecipes
            .GroupBy(recipe => recipe.InputItemId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(recipe => recipe.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        HashSet<string> outputItemIds = registry.ProcessingRecipes
            .Select(recipe => recipe.OutputItemId)
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<string> cropHarvestItemIds = registry.TileProducers
            .Select(tileProducer => tileProducer.OutputItemId)
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> rootInputIds = registry.ProcessingRecipes
            .Select(recipe => recipe.InputItemId)
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(itemId => !outputItemIds.Contains(itemId) || cropHarvestItemIds.Contains(itemId))
            .OrderBy(itemId => itemId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rootInputIds.Count == 0)
        {
            rootInputIds = registry.ProcessingRecipes
                .Select(recipe => recipe.InputItemId)
                .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(itemId => itemId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        List<ProductionChain> chains = [];

        foreach (string rootInputId in rootInputIds)
        {
            if (!recipesByInput.TryGetValue(rootInputId, out List<ProcessingRecipeDefinition> rootRecipes))
                continue;

            foreach (ProcessingRecipeDefinition rootRecipe in rootRecipes)
            {
                foreach (List<ProductionChainStep> steps in BuildProductionChainSteps(registry, recipesByInput, rootRecipe, activeMarketOpportunity, getSellPrice, []))
                {
                    string rootItemName = registry.GetItem(rootInputId).Name;
                    string summary = BuildProductionChainSummary(rootItemName, steps);
                    chains.Add(new ProductionChain(rootInputId, rootItemName, summary, steps));
                }
            }
        }

        return chains
            .OrderBy(chain => chain.RootItemId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(chain => chain.Summary, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string[] GetProductionChainLines(
        ContentRegistry registry,
        MarketOpportunity activeMarketOpportunity = null,
        Func<string, int> getSellPrice = null)
    {
        return GetProductionChainLines(registry, activeMarketOpportunity, getSellPrice, indentDetails: false);
    }

    public static string[] GetProductionChainSummaryLines(ContentRegistry registry, int maxChains = 4)
    {
        IReadOnlyList<ProductionChain> chains = BuildProductionChains(registry);
        if (chains.Count == 0)
            return ["PRODUCTION", "No processing chains loaded."];

        List<string> lines = ["PRODUCTION"];

        foreach (ProductionChain chain in chains.Take(maxChains))
            lines.Add(chain.Summary);

        if (chains.Count > maxChains)
            lines.Add($"{chains.Count - maxChains} more chains...");

        lines.Add("Open Production Report for details.");
        return lines.ToArray();
    }

    private static string[] GetProductionChainLines(
        ContentRegistry registry,
        MarketOpportunity activeMarketOpportunity,
        Func<string, int> getSellPrice,
        bool indentDetails)
    {
        IReadOnlyList<ProductionChain> chains = BuildProductionChains(registry, activeMarketOpportunity, getSellPrice);
        if (chains.Count == 0)
            return ["PRODUCTION CHAINS", "No processing chains loaded."];

        List<string> lines = ["PRODUCTION CHAINS"];

        for (int index = 0; index < chains.Count; index++)
        {
            ProductionChain chain = chains[index];
            lines.Add(chain.Summary);
            lines.AddRange(chain.Steps.Select(step => FormatProductionChainStep(step, indentDetails)));

            if (index < chains.Count - 1)
                lines.Add(string.Empty);
        }

        return lines.ToArray();
    }

    public static string FormatMarketOpportunityLine(ContentRegistry registry, MarketOpportunity activeMarketOpportunity)
    {
        if (activeMarketOpportunity is null || !activeMarketOpportunity.IsActive)
            return string.Empty;

        string itemName = registry.GetItem(activeMarketOpportunity.ItemId).Name;
        string dayLabel = activeMarketOpportunity.DaysRemaining == 1 ? "day left" : "days left";
        return $"{activeMarketOpportunity.DisplayName} - {itemName} sells for {activeMarketOpportunity.GetMultiplierPercent()}% - {activeMarketOpportunity.DaysRemaining} {dayLabel}";
    }

    public static MarketOpportunitySummary? GetMarketOpportunitySummary(
        ContentRegistry registry,
        MarketOpportunity activeMarketOpportunity,
        Func<string, int> getSellPrice,
        int daysUntilNextOpportunity = 0,
        string nextCandidateItemId = null)
    {
        if (activeMarketOpportunity?.IsActive != true)
        {
            string nextCandidateItemName = string.IsNullOrWhiteSpace(nextCandidateItemId)
                ? string.Empty
                : registry.GetItem(nextCandidateItemId).Name;
            return new MarketOpportunitySummary(
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                0,
                string.Empty,
                Math.Max(0, daysUntilNextOpportunity),
                nextCandidateItemName);
        }

        ItemDefinition item = registry.GetItem(activeMarketOpportunity.ItemId);
        int baseSellPrice = item.SellPrice;
        int effectiveSellPrice = getSellPrice is null ? baseSellPrice : getSellPrice(item.Id);
        int bonusPercent = Math.Max(0, activeMarketOpportunity.GetMultiplierPercent() - 100);
        string nextChainSummary = GetBestChainSummaryForItem(registry, item.Id);

        return new MarketOpportunitySummary(
            activeMarketOpportunity.DisplayName,
            item.Name,
            baseSellPrice,
            effectiveSellPrice,
            bonusPercent,
            activeMarketOpportunity.DaysRemaining,
            nextChainSummary,
            0,
            string.Empty);
    }

    private static IEnumerable<List<ProductionChainStep>> BuildProductionChainSteps(
        ContentRegistry registry,
        IReadOnlyDictionary<string, List<ProcessingRecipeDefinition>> recipesByInput,
        ProcessingRecipeDefinition recipe,
        MarketOpportunity activeMarketOpportunity,
        Func<string, int> getSellPrice,
        HashSet<string> visitedRecipeIds)
    {
        if (!visitedRecipeIds.Add(recipe.Id))
            yield break;

        ProductionChainStep currentStep = CreateProductionChainStep(registry, recipe, activeMarketOpportunity, getSellPrice);

        if (!recipesByInput.TryGetValue(recipe.OutputItemId, out List<ProcessingRecipeDefinition> nextRecipes))
        {
            yield return [currentStep];
            yield break;
        }

        bool yieldedChild = false;
        foreach (ProcessingRecipeDefinition nextRecipe in nextRecipes)
        {
            foreach (List<ProductionChainStep> childSteps in BuildProductionChainSteps(
                         registry,
                         recipesByInput,
                         nextRecipe,
                         activeMarketOpportunity,
                         getSellPrice,
                         new HashSet<string>(visitedRecipeIds, StringComparer.OrdinalIgnoreCase)))
            {
                yieldedChild = true;
                List<ProductionChainStep> steps = [currentStep, .. childSteps];
                yield return steps;
            }
        }

        if (!yieldedChild)
            yield return [currentStep];
    }

    private static ProductionChainStep CreateProductionChainStep(
        ContentRegistry registry,
        ProcessingRecipeDefinition recipe,
        MarketOpportunity activeMarketOpportunity,
        Func<string, int> getSellPrice)
    {
        ProcessingRecipeEconomics economics = CalculateProcessingRecipeEconomics(registry, recipe, getSellPrice);
        string opportunityName = activeMarketOpportunity is not null
            && activeMarketOpportunity.IsActive
            && string.Equals(activeMarketOpportunity.ItemId, recipe.OutputItemId, StringComparison.OrdinalIgnoreCase)
            && economics.OutputValue != GetOutputValue(registry, recipe)
                ? activeMarketOpportunity.DisplayName
                : string.Empty;

        return new ProductionChainStep(
            recipe.Id,
            GetMachineName(registry, recipe.MachineId),
            recipe.InputItemId,
            registry.GetItem(recipe.InputItemId).Name,
            recipe.InputQuantity,
            recipe.OutputItemId,
            registry.GetItem(recipe.OutputItemId).Name,
            recipe.OutputQuantity,
            recipe.ProcessingDays,
            economics.InputValue,
            economics.OutputValue,
            economics.ValueAdded,
            opportunityName);
    }

    private static string BuildProductionChainSummary(string rootItemName, IReadOnlyList<ProductionChainStep> steps)
    {
        List<string> names = [rootItemName, .. steps.Select(step => step.OutputItemName)];
        return string.Join(" -> ", names);
    }

    private static string FormatProductionChainStep(ProductionChainStep step, bool indentDetails)
    {
        string valueSegment = string.IsNullOrWhiteSpace(step.OpportunityName)
            ? $"${step.InputValue} -> ${step.OutputValue}"
            : $"${step.InputValue} -> ${step.OutputValue} during {step.OpportunityName}";

        string dayLabel = step.ProcessingDays == 1 ? "day" : "days";
        string prefix = indentDetails ? "  " : string.Empty;
        return $"{prefix}{step.MachineName}: {step.InputQuantity} {step.InputItemName} -> {step.OutputQuantity} {step.OutputItemName} | {step.ProcessingDays} {dayLabel} | {valueSegment} | {FormatSignedCurrency(step.ValueAdded)}";
    }

    private static string FormatDashboardMarketLine(ContentRegistry registry, MarketOpportunity activeMarketOpportunity, int daysUntilNextOpportunity)
    {
        if (activeMarketOpportunity?.IsActive == true)
        {
            string itemName = registry.GetItem(activeMarketOpportunity.ItemId).Name;
            string dayLabel = activeMarketOpportunity.DaysRemaining == 1 ? "DAY" : "DAYS";
            string chainSummary = GetBestChainSummaryForItem(registry, activeMarketOpportunity.ItemId);
            return $"MARKET BONUS: {itemName.ToUpperInvariant()} +{Math.Max(0, activeMarketOpportunity.GetMultiplierPercent() - 100)}% FOR {activeMarketOpportunity.DaysRemaining} {dayLabel} | MAKE {itemName.ToUpperInvariant()}: {chainSummary.ToUpperInvariant()}";
        }

        string nextDayLabel = daysUntilNextOpportunity == 1 ? "DAY" : "DAYS";
        return $"MARKET: NEXT BONUS IN {Math.Max(0, daysUntilNextOpportunity)} {nextDayLabel}";
    }

    private static string FormatMarketOutputValueLine(
        ContentRegistry registry,
        MarketOfferProcessingEconomics economics,
        Func<string, int> getSellPrice)
    {
        ItemDefinition item = registry.GetItem(economics.OutputItemId);
        int baseOutputValue = economics.TotalOutputQuantity * item.SellPrice;
        if (getSellPrice is null)
            return $"${economics.OutputValue}";

        int boostedUnitPrice = getSellPrice(item.Id);
        int boostedOutputValue = economics.TotalOutputQuantity * boostedUnitPrice;
        if (boostedOutputValue == baseOutputValue)
            return $"${economics.OutputValue}";

        return $"${boostedOutputValue} during {item.Name} Boom";
    }

    private static string GetBestChainSummaryForItem(ContentRegistry registry, string outputItemId)
    {
        ProductionChainStep? step = registry.ProcessingRecipes
            .Where(recipe => string.Equals(recipe.OutputItemId, outputItemId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(recipe => recipe.Id, StringComparer.OrdinalIgnoreCase)
            .Select(recipe => new ProductionChainStep(
                recipe.Id,
                GetMachineName(registry, recipe.MachineId),
                recipe.InputItemId,
                registry.GetItem(recipe.InputItemId).Name,
                recipe.InputQuantity,
                recipe.OutputItemId,
                registry.GetItem(recipe.OutputItemId).Name,
                recipe.OutputQuantity,
                recipe.ProcessingDays,
                0,
                0,
                0,
                string.Empty))
            .FirstOrDefault();

        if (step is null || string.IsNullOrWhiteSpace(step.Value.InputItemId))
            return string.Empty;

        return $"{step.Value.InputItemName} -> {step.Value.OutputItemName}";
    }

    public static string GetFarmCycleControlText(ThemeVocabulary themeVocabulary = null)
    {
        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        return $"G: RUN {theme.ToUpperLabel(theme.WorldName)} CYCLE USING UNLOCKED BOTS";
    }

    public static int GetInventoryItemCount(ContentRegistry registry, Func<string, int> getCount)
    {
        return registry.Items.Sum(item => getCount(item.Id));
    }

    private static void AddInventorySection(
        ContentRegistry registry,
        List<string> lines,
        string title,
        IEnumerable<ItemDefinition> items,
        Func<string, int> getCount,
        bool includeDebugDetails,
        HashSet<string> machineRelatedItemIds,
        Func<string, int> getSellPrice = null)
    {
        List<ItemDefinition> visibleItems = items
            .Where(item => getCount(item.Id) > 0)
            .OrderBy(item => machineRelatedItemIds.Contains(item.Id) ? 0 : 1)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (visibleItems.Count == 0)
            return;

        if (lines.Count > 0)
            lines.Add(string.Empty);

        lines.Add(title);
        foreach (ItemDefinition item in visibleItems)
            lines.Add(FormatInventoryLine(item, getCount(item.Id), includeDebugDetails, getSellPrice, isSellable: IsSellableInventoryItem(registry, item)));
    }

    private static string FormatInventoryLine(ItemDefinition item, int count, bool includeDebugDetails, Func<string, int> getSellPrice = null, bool isSellable = true)
    {
        int displaySellPrice = GetDisplaySellPrice(item, getSellPrice);
        string line = isSellable
            ? $"{item.Name} x{count} | ${displaySellPrice} each | ${count * displaySellPrice} total"
            : $"{item.Name} x{count}";

        return includeDebugDetails ? $"{line} | id: {item.Id}" : line;
    }

    private static string GetMachineNeedText(ContentRegistry registry, string machineId)
    {
        List<ProcessingRecipeDefinition> recipes = registry
            .GetProcessingRecipesForMachine(machineId)
            .ToList();

        if (recipes.Count == 0)
            return "No recipes loaded.";

        string neededInputs = string.Join(" or ", recipes.Select(recipe => $"{recipe.InputQuantity} {registry.GetItem(recipe.InputItemId).Name}"));
        return $"No valid input. Need {neededInputs}.";
    }

    private static string GetNeededInputsText(ContentRegistry registry, string machineId)
    {
        return string.Join(" or ", registry
            .GetProcessingRecipesForMachine(machineId)
            .Select(recipe => $"{recipe.InputQuantity} {registry.GetItem(recipe.InputItemId).Name}"));
    }

    private static string GetMachineName(ContentRegistry registry, string machineId)
    {
        return registry.MachinesById.TryGetValue(machineId, out MachineDefinition machineDefinition)
            ? machineDefinition.Name
            : machineId;
    }

    private static string FormatMarketProcessingUse(ContentRegistry registry, MarketOfferProcessingEconomics economics)
    {
        string inputName = registry.GetItem(economics.InputItemId).Name;
        string outputName = registry.GetItem(economics.OutputItemId).Name;
        string dayLabel = economics.ProcessingDays == 1 ? "day" : "days";

        if (economics.BatchCount == 1)
            return $"{economics.InputQuantity} {inputName} -> {economics.OutputQuantity} {outputName}, {economics.ProcessingDays} {dayLabel}";

        return $"{economics.BatchCount} batches: {economics.InputQuantity} {inputName} -> {economics.OutputQuantity} {outputName}, {economics.ProcessingDays} {dayLabel}, total {economics.TotalOutputQuantity} {outputName}";
    }

    private static MarketOfferProcessingSummary? GetBestMarketProcessingOpportunity(ContentRegistry registry, Func<string, int> getSellPrice = null)
    {
        MarketOfferProcessingSummary? bestSummary = null;

        foreach (MarketOfferDefinition marketOffer in registry.MarketOffers)
        {
            MarketOfferProcessingEconomics? economics = FindBestProcessingUseForMarketOffer(registry, marketOffer, getSellPrice);
            if (economics is null)
                continue;

            MarketOfferProcessingSummary candidate = new(
                marketOffer.ItemId,
                economics.Value.OutputItemId,
                economics.Value.ProfitIfBought,
                economics.Value.OutputValue,
                economics.Value);

            if (bestSummary is null
                || candidate.ProfitIfBought > bestSummary.Value.ProfitIfBought
                || (candidate.ProfitIfBought == bestSummary.Value.ProfitIfBought
                    && candidate.OutputValue > bestSummary.Value.OutputValue)
                || (candidate.ProfitIfBought == bestSummary.Value.ProfitIfBought
                    && candidate.OutputValue == bestSummary.Value.OutputValue
                    && string.Compare(candidate.OfferItemId, bestSummary.Value.OfferItemId, StringComparison.OrdinalIgnoreCase) < 0))
            {
                bestSummary = candidate;
            }
        }

        return bestSummary;
    }

    private static int GetDisplaySellPrice(ItemDefinition item, Func<string, int> getSellPrice)
    {
        if (item is null)
            return 0;

        return getSellPrice is null ? item.SellPrice : getSellPrice(item.Id);
    }

    private readonly record struct MarketOfferProcessingSummary(
        string OfferItemId,
        string OutputItemId,
        int ProfitIfBought,
        int OutputValue,
        MarketOfferProcessingEconomics ProcessingEconomics);

    private static string FormatComputerBotLine(string botName, bool isUnlocked, int unlockCost, int money)
    {
        if (isUnlocked)
            return $"{botName}: Unlocked";

        return money >= unlockCost
            ? $"{botName}: Locked, cost ${unlockCost} (affordable)"
            : $"{botName}: Locked, cost ${unlockCost}";
    }

    private static string[] GetScreenFooterLines(bool isPaused, bool isAutoPlayRunning, params string[] contextControlLines)
    {
        List<string> lines = [string.Empty];
        lines.AddRange(contextControlLines.Where(line => !string.IsNullOrWhiteSpace(line)));
        lines.Add(GetSimulationStatusLine(isPaused, isAutoPlayRunning));
        lines.Add("ESC: return to farm");
        return lines.ToArray();
    }

    private static bool IsSellableInventoryItem(ContentRegistry registry, ItemDefinition item)
    {
        return !registry.IsStarterItem(item.Id) && item.SellPrice > 0;
    }

    private static string FormatSignedCurrency(int value)
    {
        if (value > 0)
            return $"+${value}";

        if (value < 0)
            return $"-${Math.Abs(value)}";

        return "$0";
    }

    private static string FormatActivityLogEntry(ActivityLogEntry entry)
    {
        return $"Day {entry.Day} | {entry.Category} | {entry.Message}";
    }

    private static string FormatSignedValue(int value)
    {
        if (value > 0)
            return $"+{value}";

        return value.ToString();
    }

    private static string FormatSavePathForDisplay(string savePath)
    {
        if (string.IsNullOrWhiteSpace(savePath))
            return "none";

        string fileName = Path.GetFileName(savePath);
        return string.IsNullOrWhiteSpace(fileName) ? savePath : fileName;
    }

    private static string FormatStatusLine(bool? succeeded, string message)
    {
        if (succeeded is null || string.IsNullOrWhiteSpace(message))
            return "Never";

        return succeeded.Value
            ? $"Success - {message}"
            : $"Failed - {message}";
    }
}
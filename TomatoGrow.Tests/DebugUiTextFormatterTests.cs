using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class DebugUiTextFormatterTests
{
    [Fact]
    public void BotStatusText_ShowsLockedStateCostAndAffordability()
    {
        string message = DebugUiTextFormatter.GetBotStatusText("Planter Bot", false, 60, 30);

        Assert.Equal("Planter Bot: LOCKED COST 60 NEED 30 MORE", message);
    }

    [Fact]
    public void BotStatusText_ShowsAffordableWhenEnoughMoney()
    {
        string message = DebugUiTextFormatter.GetBotStatusText("Harvester Bot", false, 120, 140);

        Assert.Equal("Harvester Bot: LOCKED COST 120 AFFORDABLE", message);
    }

    [Fact]
    public void ScreenTitle_FarmingDefault_UsesExpectedComputerTitle()
    {
        Assert.Equal("FARM COMPUTER", DebugUiTextFormatter.GetScreenTitle(GameScreen.Computer, ThemeVocabulary.FarmingDefault));
    }

    [Fact]
    public void ScreenTitle_MiningDemo_UsesOperationsComputerTitle()
    {
        Assert.Equal("OPERATIONS COMPUTER", DebugUiTextFormatter.GetScreenTitle(GameScreen.Computer, ThemeVocabulary.MiningDemo));
    }

    [Fact]
    public void ScreenTitle_FarmingDefault_UsesFarmWorldTitle()
    {
        Assert.Equal("FARM GRID", DebugUiTextFormatter.GetScreenTitle(GameScreen.Farm, ThemeVocabulary.FarmingDefault));
    }

    [Fact]
    public void ScreenTitle_MiningDemo_UsesMineWorldTitle()
    {
        Assert.Equal("MINE GRID", DebugUiTextFormatter.GetScreenTitle(GameScreen.Farm, ThemeVocabulary.MiningDemo));
    }

    [Fact]
    public void ScreenTitle_UsesThemeVocabularyForMarketStorageActivityLogAndProductionReport()
    {
        Assert.Equal("SUPPLY MARKET", DebugUiTextFormatter.GetScreenTitle(GameScreen.Market, ThemeVocabulary.FarmingDefault));
        Assert.Equal("SUPPLY MARKET", DebugUiTextFormatter.GetScreenTitle(GameScreen.Market, ThemeVocabulary.MiningDemo));
        Assert.Equal("STORAGE", DebugUiTextFormatter.GetScreenTitle(GameScreen.Storage, ThemeVocabulary.MiningDemo));
        Assert.Equal("ACTIVITY LOG", DebugUiTextFormatter.GetScreenTitle(GameScreen.ActivityLog, ThemeVocabulary.MiningDemo));
        Assert.Equal("PRODUCTION REPORT", DebugUiTextFormatter.GetScreenTitle(GameScreen.ProductionReport, ThemeVocabulary.MiningDemo));
    }

    [Fact]
    public void SelectedPlotSummary_ShowsCropGrowthWaterAndReadyState()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(1, 2);
        plot.TryPlant("turnip");
        plot.Water();
        plot.AdvanceDay();
        plot.Water();

        string[] lines = DebugUiTextFormatter.GetSelectedPlotSummaryLines(plot, registry, false);

        Assert.Equal("SELECTED PLOT 1,2", lines[0]);
        Assert.Equal("TURNIP", lines[1]);
        Assert.Equal("PROGRESS: 1 / 4", lines[2]);
        Assert.Equal("WATER: WET", lines[3]);
        Assert.Equal("OUTPUT: TURNIP", lines[4]);
        Assert.Equal("G: RUN FARM CYCLE", lines[5]);
    }

    [Fact]
    public void SelectedPlotSummary_DebugMode_IncludesRawIds()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(1, 2);
        plot.TryPlant("turnip");

        string[] lines = DebugUiTextFormatter.GetSelectedPlotSummaryLines(plot, registry, false, includeDebugDetails: true);

        Assert.Contains("CROP ID: TURNIP", lines);
        Assert.Contains("HARVEST ID: TURNIP", lines);
    }

    [Fact]
    public void SelectedPlotSummary_UsesThemeVocabularyLabels()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(1, 2);
        plot.TryPlant("turnip");
        plot.Water();

        string[] lines = DebugUiTextFormatter.GetSelectedPlotSummaryLines(plot, registry, false, themeVocabulary: ThemeVocabulary.MiningDemo);

        Assert.Equal("SELECTED CLAIM 1,2", lines[0]);
        Assert.Equal("TURNIP", lines[1]);
        Assert.Equal("POWER: POWERED", lines[3]);
    }

    [Fact]
    public void SelectedPlotSummary_UsesMiningDemoUnpoweredLabelForDryPlot()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(1, 2);
        plot.TryPlant("turnip");

        string[] lines = DebugUiTextFormatter.GetSelectedPlotSummaryLines(plot, registry, false, themeVocabulary: ThemeVocabulary.MiningDemo);

        Assert.Equal("POWER: UNPOWERED", lines[3]);
    }

    [Fact]
    public void CompactTileVisual_EmptyTile_UsesInterfacePackEmptySymbolAndLabel()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);

        string[] lines = DebugUiTextFormatter.GetCompactTileVisualLines(plot, registry, false, InterfacePack.FarmingDefault);

        Assert.Single(lines);
        Assert.Equal(". EMPTY", lines[0]);
    }

    [Fact]
    public void CompactTileVisual_StartedUnmaintained_ShowsStateAndProgress()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);
        plot.TryPlant("turnip");

        string[] lines = DebugUiTextFormatter.GetCompactTileVisualLines(plot, registry, false, InterfacePack.FarmingDefault);

        Assert.Equal("T DRY", lines[0]);
        Assert.Equal("[----] 0/4", lines[1]);
    }

    [Fact]
    public void CompactTileVisual_StartedMaintained_ShowsMaintainedState()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);
        plot.TryPlant("turnip");
        plot.Water();

        string[] lines = DebugUiTextFormatter.GetCompactTileVisualLines(plot, registry, false, InterfacePack.FarmingDefault);

        Assert.Equal("T WET", lines[0]);
    }

    [Fact]
    public void CompactTileVisual_Ready_ShowsReadyTokenAndEmphasis()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);
        plot.TryPlant("turnip");
        plot.Restore("turnip", 4, true);

        string[] lines = DebugUiTextFormatter.GetCompactTileVisualLines(plot, registry, true, InterfacePack.FarmingDefault);

        Assert.Equal("T READY!", lines[0]);
        Assert.Equal("[####] 4/4", lines[1]);
    }

    [Fact]
    public void SelectedTileDetailLines_IncludeCoordinatesProgressAndOutput()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(1, 2);
        plot.TryPlant("turnip");
        plot.Water();
        plot.AdvanceDay();

        string[] lines = DebugUiTextFormatter.GetSelectedTileDetailLines(plot, registry, false);

        Assert.Equal("SELECTED PLOT 1,2", lines[0]);
        Assert.Equal("PROGRESS: 1 / 4", lines[2]);
        Assert.Equal("OUTPUT: TURNIP", lines[4]);
    }

    [Fact]
    public void SelectedTileDetailLines_UseMiningVocabulary_ActionAndState()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(1, 2);
        plot.TryPlant("turnip");

        string[] lines = DebugUiTextFormatter.GetSelectedTileDetailLines(plot, registry, false, ThemeVocabulary.MiningDemo);

        Assert.Equal("SELECTED CLAIM 1,2", lines[0]);
        Assert.Equal("POWER: UNPOWERED", lines[3]);
        Assert.Equal("W: POWER", lines[5]);
    }

    [Fact]
    public void TileDisplayState_UsesEmptyGrowingMaintainedAndReadyStates()
    {
        FarmPlot emptyPlot = new(0, 0);
        FarmPlot dryPlot = new(0, 0);
        dryPlot.TryPlant("turnip");
        FarmPlot wetPlot = new(0, 0);
        wetPlot.TryPlant("turnip");
        wetPlot.Water();
        FarmPlot readyPlot = new(0, 0);
        readyPlot.TryPlant("turnip");
        readyPlot.Restore("turnip", 4, true);

        Assert.Equal(DebugUiTextFormatter.TileDisplayState.Empty, DebugUiTextFormatter.GetTileDisplayState(emptyPlot, false));
        Assert.Equal(DebugUiTextFormatter.TileDisplayState.GrowingDry, DebugUiTextFormatter.GetTileDisplayState(dryPlot, false));
        Assert.Equal(DebugUiTextFormatter.TileDisplayState.GrowingMaintained, DebugUiTextFormatter.GetTileDisplayState(wetPlot, false));
        Assert.Equal(DebugUiTextFormatter.TileDisplayState.Ready, DebugUiTextFormatter.GetTileDisplayState(readyPlot, true));
    }

    [Fact]
    public void TileProgressFraction_HandlesEmptyNewGrowingAndReadyTiles()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot emptyPlot = new(0, 0);
        FarmPlot newPlot = new(0, 0);
        newPlot.TryPlant("turnip");
        FarmPlot growingPlot = new(0, 0);
        growingPlot.TryPlant("turnip");
        growingPlot.Water();
        growingPlot.AdvanceDay();
        FarmPlot readyPlot = new(0, 0);
        readyPlot.TryPlant("turnip");
        readyPlot.Restore("turnip", 4, true);

        Assert.Equal(0f, DebugUiTextFormatter.GetTileProgressFraction(emptyPlot, registry));
        Assert.Equal(0f, DebugUiTextFormatter.GetTileProgressFraction(newPlot, registry));
        Assert.InRange(DebugUiTextFormatter.GetTileProgressFraction(growingPlot, registry), 0f, 1f);
        Assert.Equal(1f, DebugUiTextFormatter.GetTileProgressFraction(readyPlot, registry));
    }

    [Fact]
    public void TilePrimarySymbol_UsesOutputNameFallback()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);
        plot.TryPlant("turnip");

        Assert.Equal("T", DebugUiTextFormatter.GetTilePrimarySymbol(plot, registry));
    }

    [Fact]
    public void TileProgressBarRect_UsesBottomFillGeometry()
    {
        Rectangle tileBounds = new(10, 20, 100, 80);

        Rectangle rect = DebugUiTextFormatter.GetTileProgressBarRect(tileBounds, 0.5f);

        Assert.True(rect.Width > 0);
        Assert.Equal(tileBounds.Bottom - rect.Height - 6, rect.Y);
    }

    [Fact]
    public void TileProgressBar_UsesAsciiGrowthBlocks()
    {
        Assert.Equal("[##---]", DebugUiTextFormatter.GetTileProgressBar(2, 5));
    }

    [Fact]
    public void DashboardSummary_UsesMoneySeedCountSellableCountAndSellValue()
    {
        ContentRegistry registry = BuildRegistry();

        DebugUiTextFormatter.DashboardSummary summary = DebugUiTextFormatter.GetDashboardSummary(
            3,
            125,
            4,
            3,
            registry,
            "turnip_seed",
            itemId => itemId switch
            {
                "turnip_seed" => 4,
                "turnip" => 2,
                _ => 0
            });

        Assert.Equal(3, summary.DayNumber);
        Assert.Equal(125, summary.Money);
        Assert.Equal("4X3", summary.FarmSize);
        Assert.Equal("Turnip Seeds", summary.SelectedSeedName);
        Assert.Equal(4, summary.SelectedSeedCount);
        Assert.Equal(2, summary.SellableCropCount);
        Assert.Equal(60, summary.EstimatedSellValue);
    }

    [Fact]
    public void InventoryLines_IncludeGrapesWhenPresent()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetInventoryLines(registry, itemId => itemId switch
        {
            "grape" => 6,
            _ => 0
        }, includeDebugDetails: false);

        Assert.Contains("Crops / Inputs:", lines);
        Assert.Contains("Grapes x6 | $45 each | $270 total", lines);
    }

    [Fact]
    public void InventoryLines_IncludeHopsWhenPresent()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetInventoryLines(registry, itemId => itemId switch
        {
            "hops" => 3,
            _ => 0
        }, includeDebugDetails: false);

        Assert.Contains("Hops x3 | $40 each | $120 total", lines);
    }

    [Fact]
    public void InventoryLines_IncludeProcessedGoodsWhenPresent()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetInventoryLines(registry, itemId => itemId switch
        {
            "wine" => 1,
            "beer" => 2,
            "aged_wine" => 1,
            _ => 0
        }, includeDebugDetails: false);

        Assert.Contains("Processed:", lines);
        Assert.Contains("Wine x1 | $160 each | $160 total", lines);
        Assert.Contains("Beer x2 | $140 each | $280 total", lines);
        Assert.Contains("Aged Wine x1 | $260 each | $260 total", lines);
    }

    [Fact]
    public void MachineStatus_Idle_ListsAvailableRecipesWhenInputsExist()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");

        string[] lines = DebugUiTextFormatter.GetMachineStatusLines(registry, fermenter, itemId => itemId switch
        {
            "grape" => 3,
            "hops" => 3,
            _ => 0
        });

        Assert.Equal("Status: Idle", lines[0]);
        Assert.Contains("Press M to start the selected recipe.", lines);
        Assert.Contains(lines, line => line.Contains("Ready inputs:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("3 Grapes -> 1 Wine, 2 days, +$25", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("3 Hops -> 1 Beer, 2 days, +$20", StringComparison.Ordinal));
    }

    [Fact]
    public void MachineStatus_Idle_ShowsNeededInputsWhenInventoryIsMissing()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");

        string[] lines = DebugUiTextFormatter.GetMachineStatusLines(registry, fermenter, _ => 0);

        Assert.Contains("No valid input. Need 3 Grapes or 3 Hops.", lines);
    }

    [Fact]
    public void MachineStatus_Working_ShowsInputOutputAndDaysRemaining()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        fermenter.Start("grape_to_wine", "grape", 3, "wine", 1, 2);
        fermenter.AdvanceDay();

        string[] lines = DebugUiTextFormatter.GetMachineStatusLines(registry, fermenter, _ => 0);

        Assert.Contains("Status: Working", lines);
        Assert.Contains("Current: 3 Grapes -> 1 Wine", lines);
        Assert.Contains("Days remaining: 1", lines);
        Assert.Contains("Output: Wine", lines);
    }

    [Fact]
    public void MachineStatus_OutputReady_ShowsOutputAndCollectControl()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        fermenter.Start("grape_to_wine", "grape", 3, "wine", 1, 2);
        fermenter.AdvanceDay();
        fermenter.AdvanceDay();

        string[] lines = DebugUiTextFormatter.GetMachineStatusLines(registry, fermenter, _ => 0);

        Assert.Contains("Status: Output Ready", lines);
        Assert.Contains("Current: 3 Grapes -> 1 Wine", lines);
        Assert.Contains("Output: 1 Wine", lines);
        Assert.Contains("Press O to collect.", lines);
    }

    [Fact]
    public void GrapeToWine_InputValue_UsesThreeTimesGrapeSellPrice()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe("grape_to_wine");

        Assert.Equal(135, DebugUiTextFormatter.GetInputValue(registry, recipe));
    }

    [Fact]
    public void GrapeToWine_OutputValue_UsesWineSellPrice()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe("grape_to_wine");

        Assert.Equal(160, DebugUiTextFormatter.GetOutputValue(registry, recipe));
    }

    [Fact]
    public void GrapeToWine_ValueAdded_IsOutputMinusInput()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe("grape_to_wine");

        Assert.Equal(25, DebugUiTextFormatter.GetValueAdded(registry, recipe));
    }

    [Fact]
    public void HopsToBeer_InputValue_UsesThreeTimesHopsSellPrice()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe("hops_to_beer");

        Assert.Equal(120, DebugUiTextFormatter.GetInputValue(registry, recipe));
    }

    [Fact]
    public void HopsToBeer_OutputValue_UsesBeerSellPrice()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe("hops_to_beer");

        Assert.Equal(140, DebugUiTextFormatter.GetOutputValue(registry, recipe));
    }

    [Fact]
    public void HopsToBeer_ValueAdded_IsOutputMinusInput()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe("hops_to_beer");

        Assert.Equal(20, DebugUiTextFormatter.GetValueAdded(registry, recipe));
    }

    [Fact]
    public void RecipeEconomics_UseRegistrySellPrices_NotHardcodedValues()
    {
        ContentRegistry registry = new(
            [
                new ItemDefinition { Id = "grape", Name = "Grapes", Type = "crop", SellPrice = 10 },
                new ItemDefinition { Id = "wine", Name = "Wine", Type = "processed_good", SellPrice = 99 }
            ],
            [],
            [
                new MachineDefinition { Id = "fermenter", Name = "Fermenter", Type = "processor_machine" }
            ],
            [
                new ProcessingRecipeDefinition
                {
                    Id = "grape_to_wine",
                    MachineId = "fermenter",
                    InputItemId = "grape",
                    InputQuantity = 3,
                    OutputItemId = "wine",
                    OutputQuantity = 1,
                    ProcessingDays = 2
                }
            ]);

        ProcessingRecipeDefinition recipe = registry.GetProcessingRecipe("grape_to_wine");
        DebugUiTextFormatter.ProcessingRecipeEconomics economics = DebugUiTextFormatter.CalculateProcessingRecipeEconomics(registry, recipe);

        Assert.Equal(30, economics.InputValue);
        Assert.Equal(99, economics.OutputValue);
        Assert.Equal(69, economics.ValueAdded);
    }

    [Fact]
    public void ProductionChains_IncludeGrapesToWineToAgedWine()
    {
        ContentRegistry registry = BuildRegistry();

        IReadOnlyList<DebugUiTextFormatter.ProductionChain> chains = DebugUiTextFormatter.BuildProductionChains(registry);

        Assert.Contains(chains, chain => string.Equals(chain.Summary, "Grapes -> Wine -> Aged Wine", StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionChains_IncludeHopsToBeer()
    {
        ContentRegistry registry = BuildRegistry();

        IReadOnlyList<DebugUiTextFormatter.ProductionChain> chains = DebugUiTextFormatter.BuildProductionChains(registry);

        Assert.Contains(chains, chain => string.Equals(chain.Summary, "Hops -> Beer", StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionChainLines_ShowRecipeEconomicsForGrapeToWine()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetProductionChainLines(registry);

        Assert.Contains("Fermenter: 3 Grapes -> 1 Wine | 2 days | $135 -> $160 | +$25", lines);
    }

    [Fact]
    public void ProductionChainLines_ShowRecipeEconomicsForWineToAgedWine()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetProductionChainLines(registry);

        Assert.Contains("Aging Barrel: 1 Wine -> 1 Aged Wine | 5 days | $160 -> $260 | +$100", lines);
    }

    [Fact]
    public void ProductionChainOrdering_IsDeterministic()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetProductionChainLines(registry);

        int grapesIndex = Array.IndexOf(lines, "Grapes -> Wine -> Aged Wine");
        int hopsIndex = Array.IndexOf(lines, "Hops -> Beer");

        Assert.True(grapesIndex >= 0);
        Assert.True(hopsIndex > grapesIndex);
    }

    [Fact]
    public void ProductionChainLines_UseDisplayNames_NotRawIds()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetProductionChainLines(registry);

        Assert.DoesNotContain(lines, line => line.Contains("grape_to_wine", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains("aged_wine", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("Grapes", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("Aged Wine", StringComparison.Ordinal));
    }

    [Fact]
    public void ComputerScreenLines_ShowProductionChainsSection()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.ActivityLog,
            null,
            state.Stats,
            state.DayNumber,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y));

        Assert.Contains("PRODUCTION", lines);
        Assert.Contains("Grapes -> Wine -> Aged Wine", lines);
        Assert.Contains("Hops -> Beer", lines);
        Assert.Contains("Open Production Report for details.", lines);
    }

    [Fact]
    public void ComputerScreenLines_ProductionChains_DoNotDuplicateChainSummaries()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.ActivityLog,
            null,
            state.Stats,
            state.DayNumber,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y));

        Assert.Equal(1, lines.Count(line => string.Equals(line, "Grapes -> Wine -> Aged Wine", StringComparison.Ordinal)));
        Assert.Equal(1, lines.Count(line => string.Equals(line, "Hops -> Beer", StringComparison.Ordinal)));
        Assert.DoesNotContain(lines, line => line.Contains("Aging Barrel: 1 Wine -> 1 Aged Wine", StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionReportScreenLines_ShowDetailedProductionChains()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetProductionReportScreenLines(registry);

        Assert.Contains("PRODUCTION CHAINS", lines);
        Assert.Contains("Grapes -> Wine -> Aged Wine", lines);
        Assert.Contains("  Fermenter: 3 Grapes -> 1 Wine | 2 days | $135 -> $160 | +$25", lines);
        Assert.Contains("  Aging Barrel: 1 Wine -> 1 Aged Wine | 5 days | $160 -> $260 | +$100", lines);
        Assert.Contains("Hops -> Beer", lines);
        Assert.Contains("  Fermenter: 3 Hops -> 1 Beer | 2 days | $120 -> $140 | +$20", lines);
    }

    [Fact]
    public void ProductionReportScreenLines_ShowPausedStatus_WhenPaused()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetProductionReportScreenLines(registry, isPaused: true);

        Assert.Contains("PAUSED - press Space to resume", lines);
    }

    [Fact]
    public void NextHintLines_SayToBuySeeds_WhenNoSeedsAndMoneyAvailable()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        ClearInventory(state);
        SetMoney(state, 25);

        string[] lines = GetNextHintLines(state, registry);

        Assert.Equal("Buy seeds with Y.", lines[0]);
    }

    [Fact]
    public void NextHintLines_SayToPlant_WhenEmptyPlotsAndSeedsExist()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);

        string[] lines = GetNextHintLines(state, registry);

        Assert.Equal("Plant seeds with P.", lines[0]);
    }

    [Fact]
    public void NextHintLines_SayToWater_WhenDryPlantedCropsExist()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        string[] lines = GetNextHintLines(state, registry);

        Assert.Equal("Plant seeds with P.", lines[0]);
        Assert.Equal("Water crops with W.", lines[1]);
    }

    [Fact]
    public void NextHintLines_SayToHarvest_WhenReadyCropsExist()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        for (int day = 0; day < 4; day++)
        {
            state.WaterPlot(0, 0);
            state.AdvanceDay();
        }

        string[] lines = GetNextHintLines(state, registry);

        Assert.Contains("Harvest selected crop with H.", lines);
    }

    [Fact]
    public void NextHintLines_SayToSell_WhenSellableGoodsExist()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "turnip", 1);

        string[] lines = GetNextHintLines(state, registry);

        Assert.Contains("Sell goods with S.", lines);
    }

    [Fact]
    public void NextHintLines_SayToCollectMachineOutput_WhenOutputIsReady()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "grape", 3);
        state.StartFermenterFromInventory();
        state.AdvanceDay();
        state.AdvanceDay();

        string[] lines = GetNextHintLines(state, registry);

        Assert.Contains("Collect machine output with O.", lines);
    }

    [Fact]
    public void NextHintLines_SayToStartProcessor_WhenIdleMachineHasValidInput()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "grape", 3);

        string[] lines = GetNextHintLines(state, registry);

        Assert.Contains("Start processor with M.", lines);
    }

    [Fact]
    public void NextHintLines_SayToUnlockAffordableBot()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, TurnipTestState.PlanterBotUnlockCost);

        string[] lines = GetNextHintLines(state, registry);

        Assert.Contains("Unlock Planter Bot with 1.", lines);
    }

    [Fact]
    public void NextHintLines_SayToExpand_WhenExpansionIsAffordable()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, state.NextFarmExpansionCost);
        PlantAllPlots(state);
        WaterAllPlantedPlots(state);

        string[] lines = GetNextHintLines(state, registry);

        Assert.Contains("Expand farm with E.", lines);
    }

    [Fact]
    public void NextHintLines_SayToClearOldJunkPile_WhenExpansionIsBlocked()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, state.NextFarmExpansionCost);
        state.ExpandFarm();
        PlantAllPlots(state);
        WaterAllPlantedPlots(state);

        string[] lines = GetNextHintLines(state, registry, maxHints: 6);

        Assert.Contains("Clear Old Junk Pile with J to unlock 5x3 expansion.", lines);
    }

    [Fact]
    public void NextHintLines_ReferenceActiveWineBoomOpportunity()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        state.SetActiveMarketOpportunity(new MarketOpportunity
        {
            Id = "wine_boom",
            ItemId = "wine",
            DisplayName = "Wine Boom",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 12
        });
        PlantAllPlots(state);
        WaterAllPlantedPlots(state);

        string[] lines = GetNextHintLines(state, registry, maxHints: 6);

        Assert.Contains("Wine Boom active: produce/sell Wine before it ends.", lines);
    }

    [Fact]
    public void NextHintLines_FallBackToFarmCycle_WhenNothingSpecificIsUrgent()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        PlantAllPlots(state);
        WaterAllPlantedPlots(state);
        AddInventoryItem(state, "turnip_seed", 1);

        string[] lines = GetNextHintLines(state, registry, maxHints: 6);

        Assert.Equal("Run farm cycle with G or continue improving production.", lines[0]);
    }

    [Fact]
    public void NextHintLines_PrioritizeLoadConfirmationAndPauseMessages()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);

        string[] lines = GetNextHintLines(state, registry, isPaused: true, isLoadConfirmationPending: true);

        Assert.Equal("Confirm load with Y or cancel with N/Esc.", lines[0]);
        Assert.Equal("Paused. Press Space to resume.", lines[1]);
    }

    [Fact]
    public void TotalSeedCountCalculation_UsesSeedItemsOnly()
    {
        ContentRegistry registry = BuildRegistry();

        int totalSeeds = DebugUiTextFormatter.GetTotalSeedCount(registry, itemId => itemId switch
        {
            "turnip_seed" => 3,
            "turnip" => 8,
            _ => 0
        });

        Assert.Equal(3, totalSeeds);
    }

    [Fact]
    public void SelectedSeedCountCalculation_UsesSelectedSeedId()
    {
        int selectedSeedCount = DebugUiTextFormatter.GetSelectedSeedCount("turnip_seed", itemId => itemId == "turnip_seed" ? 4 : 0);

        Assert.Equal(4, selectedSeedCount);
    }

    [Fact]
    public void SelectedSeedBuyPrice_UsesJsonBuyPrice()
    {
        ContentRegistry registry = BuildRegistry();

        int buyPrice = DebugUiTextFormatter.GetSelectedSeedBuyPrice(registry, "turnip_seed");

        Assert.Equal(10, buyPrice);
    }

    [Fact]
    public void CanAffordSelectedSeed_UsesJsonBuyPriceThreshold()
    {
        ContentRegistry registry = BuildRegistry();

        bool canAfford = DebugUiTextFormatter.CanAffordSelectedSeed(registry, "turnip_seed", 10);
        bool cannotAfford = DebugUiTextFormatter.CanAffordSelectedSeed(registry, "turnip_seed", 9);

        Assert.True(canAfford);
        Assert.False(cannotAfford);
    }

    [Fact]
    public void TotalSellableCropCount_UsesNonSeedSellableItems()
    {
        ContentRegistry registry = BuildRegistry();

        int sellableCount = DebugUiTextFormatter.GetTotalSellableCropCount(registry, itemId => itemId switch
        {
            "turnip_seed" => 3,
            "turnip" => 2,
            _ => 0
        });

        Assert.Equal(2, sellableCount);
    }

    [Fact]
    public void EstimatedSellValue_UsesJsonSellPrices()
    {
        ContentRegistry registry = BuildRegistry();

        int sellValue = DebugUiTextFormatter.GetEstimatedSellValue(registry, itemId => itemId switch
        {
            "turnip_seed" => 3,
            "turnip" => 2,
            _ => 0
        });

        Assert.Equal(60, sellValue);
    }

    [Fact]
    public void FarmCycleControlText_SaysItUsesUnlockedBots()
    {
        Assert.Equal(
            "G: RUN FARM CYCLE USING UNLOCKED BOTS",
            DebugUiTextFormatter.GetFarmCycleControlText());
    }

    [Fact]
    public void DebugUiState_ToggleDebugDetails_FlipsVisibility()
    {
        DebugUiState state = new();

        Assert.False(state.ShowDebugDetails);

        state.ToggleDebugDetails();
        Assert.True(state.ShowDebugDetails);

        state.ToggleDebugDetails();
        Assert.False(state.ShowDebugDetails);
    }

    [Fact]
    public void DebugUiState_NewGame_StartsOnFarmScreen()
    {
        DebugUiState state = new();

        Assert.False(state.IsPaused);
        Assert.Equal(GameScreen.Farm, state.CurrentScreen);
        Assert.True(state.AllowsFarmActions);
        Assert.True(state.ShowsFarmVisuals);
    }

    [Fact]
    public void DebugUiState_TogglePause_TogglesPausedState()
    {
        DebugUiState state = new();

        state.TogglePause();
        Assert.True(state.IsPaused);

        state.TogglePause();
        Assert.False(state.IsPaused);
    }

    [Fact]
    public void DebugUiState_NavigationStillWorksWhilePaused()
    {
        DebugUiState state = new();
        state.TogglePause();

        state.ShowInventoryScreen();

        Assert.True(state.IsPaused);
        Assert.Equal(GameScreen.Inventory, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_ShowInventoryScreen_SwitchesToInventory()
    {
        DebugUiState state = new();

        state.ShowInventoryScreen();

        Assert.Equal(GameScreen.Inventory, state.CurrentScreen);
        Assert.False(state.ShowsFarmVisuals);
    }

    [Fact]
    public void DebugUiState_ShowStorageScreen_SwitchesToStorage()
    {
        DebugUiState state = new();

        state.ShowStorageScreen();

        Assert.Equal(GameScreen.Storage, state.CurrentScreen);
        Assert.True(state.AllowsStorageActions);
        Assert.False(state.ShowsFarmVisuals);
    }

    [Fact]
    public void DebugUiState_ShowMarketScreen_SwitchesToMarket()
    {
        DebugUiState state = new();

        state.ShowMarketScreen();

        Assert.Equal(GameScreen.Market, state.CurrentScreen);
        Assert.True(state.AllowsMarketActions);
        Assert.False(state.ShowsFarmVisuals);
    }

    [Fact]
    public void DebugUiState_ShowComputerScreen_SwitchesToComputer()
    {
        DebugUiState state = new();

        state.ShowComputerScreen();

        Assert.Equal(GameScreen.Computer, state.CurrentScreen);
        Assert.False(state.ShowsFarmVisuals);
    }

    [Fact]
    public void DebugUiState_ShowProductionReportScreen_SwitchesToProductionReport()
    {
        DebugUiState state = new();

        state.ShowProductionReportScreen();

        Assert.Equal(GameScreen.ProductionReport, state.CurrentScreen);
        Assert.False(state.ShowsFarmVisuals);
    }

    [Fact]
    public void DebugUiState_ShowActivityLogScreen_SwitchesToActivityLog()
    {
        DebugUiState state = new();

        state.ShowActivityLogScreen();

        Assert.Equal(GameScreen.ActivityLog, state.CurrentScreen);
        Assert.False(state.ShowsFarmVisuals);
    }

    [Fact]
    public void DebugUiState_ShowStatsScreen_SwitchesToStats()
    {
        DebugUiState state = new();

        state.ShowStatsScreen();

        Assert.Equal(GameScreen.Stats, state.CurrentScreen);
        Assert.False(state.ShowsFarmVisuals);
    }

    [Fact]
    public void DebugUiState_ShowMachinesScreen_SwitchesToMachines()
    {
        DebugUiState state = new();

        state.ShowMachinesScreen();

        Assert.Equal(GameScreen.Machines, state.CurrentScreen);
        Assert.True(state.AllowsMachineActions);
        Assert.False(state.ShowsFarmVisuals);
    }

    [Fact]
    public void DebugUiState_ShowHelpScreen_SwitchesToHelp()
    {
        DebugUiState state = new();

        state.ShowHelpScreen();

        Assert.Equal(GameScreen.Help, state.CurrentScreen);
        Assert.False(state.ShowsFarmVisuals);
    }

    [Fact]
    public void DebugUiState_StorageActions_DoNotRunOnStatsScreen()
    {
        DebugUiState state = new();
        state.ShowStatsScreen();
        int actionCount = 0;

        bool didRun = state.TryRunStorageAction(() => actionCount++);

        Assert.False(didRun);
        Assert.Equal(0, actionCount);
    }

    [Fact]
    public void DebugUiState_HandleEscape_FromSubScreen_ReturnsToFarmWithoutExit()
    {
        DebugUiState state = new();
        state.ShowInventoryScreen();

        bool shouldExit = state.HandleEscape();

        Assert.False(shouldExit);
        Assert.Equal(GameScreen.Farm, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_HandleEscape_WhilePaused_ReturnsToFarmWithoutClearingPause()
    {
        DebugUiState state = new();
        state.TogglePause();
        state.ShowMachinesScreen();

        bool shouldExit = state.HandleEscape();

        Assert.False(shouldExit);
        Assert.True(state.IsPaused);
        Assert.Equal(GameScreen.Farm, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_HandleEscape_FromStorage_ReturnsToFarmWithoutExit()
    {
        DebugUiState state = new();
        state.ShowStorageScreen();

        bool shouldExit = state.HandleEscape();

        Assert.False(shouldExit);
        Assert.Equal(GameScreen.Farm, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_HandleEscape_FromMarket_ReturnsToFarmWithoutExit()
    {
        DebugUiState state = new();
        state.ShowMarketScreen();

        bool shouldExit = state.HandleEscape();

        Assert.False(shouldExit);
        Assert.Equal(GameScreen.Farm, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_HandleEscape_FromComputer_ReturnsToFarmWithoutExit()
    {
        DebugUiState state = new();
        state.ShowComputerScreen();

        bool shouldExit = state.HandleEscape();

        Assert.False(shouldExit);
        Assert.Equal(GameScreen.Farm, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_HandleEscape_FromProductionReport_ReturnsToFarmWithoutExit()
    {
        DebugUiState state = new();
        state.ShowProductionReportScreen();

        bool shouldExit = state.HandleEscape();

        Assert.False(shouldExit);
        Assert.Equal(GameScreen.Farm, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_HandleEscape_FromActivityLog_ReturnsToFarmWithoutExit()
    {
        DebugUiState state = new();
        state.ShowActivityLogScreen();

        bool shouldExit = state.HandleEscape();

        Assert.False(shouldExit);
        Assert.Equal(GameScreen.Farm, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_HandleEscape_WithPendingLoadConfirmation_CancelsWithoutExitOrNavigation()
    {
        DebugUiState state = new();
        state.ShowComputerScreen();
        state.RequestLoadConfirmation();

        bool shouldExit = state.HandleEscape();

        Assert.False(shouldExit);
        Assert.False(state.IsLoadConfirmationPending);
        Assert.Equal(GameScreen.Computer, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_HandleEscape_FromFarm_RequestsExit()
    {
        DebugUiState state = new();

        bool shouldExit = state.HandleEscape();

        Assert.True(shouldExit);
        Assert.Equal(GameScreen.Farm, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_FarmGameplayActions_RunOnFarmScreen()
    {
        DebugUiState state = new();
        int actionCount = 0;

        bool didRun = state.TryRunFarmAction(() => actionCount++);

        Assert.True(didRun);
        Assert.Equal(1, actionCount);
    }

    [Fact]
    public void DebugUiState_FarmGameplayActions_DoNotRunOnInventoryScreen()
    {
        DebugUiState state = new();
        state.ShowInventoryScreen();
        int actionCount = 0;

        bool didRun = state.TryRunFarmAction(() => actionCount++);

        Assert.False(didRun);
        Assert.Equal(0, actionCount);
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksDayAdvanceWhilePaused()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        uiState.TogglePause();

        bool didRun = uiState.TryRunUnpausedAction(() => state.AdvanceDay());

        Assert.False(didRun);
        Assert.Equal(1, state.DayNumber);
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksPlantingWhilePaused()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        uiState.TogglePause();

        bool didRun = uiState.TryRunUnpausedAction(() => state.PlantSelectedSeed());

        Assert.False(didRun);
        Assert.True(state.GetSelectedPlot().IsEmpty);
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksWateringWhilePaused()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        state.PlantSelectedSeed();
        uiState.TogglePause();

        bool didRun = uiState.TryRunUnpausedAction(() => state.WaterSelectedPlot());

        Assert.False(didRun);
        Assert.False(state.GetSelectedPlot().IsWatered);
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksHarvestingWhilePaused()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        state.PlantSelectedSeed();

        for (int day = 0; day < 4; day++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        uiState.TogglePause();
        bool didRun = uiState.TryRunUnpausedAction(() => state.HarvestCrop());

        Assert.False(didRun);
        Assert.Equal(0, state.GetItemCount("turnip"));
        Assert.False(state.GetSelectedPlot().IsEmpty);
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksSellingWhilePaused()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "turnip", 1);
        uiState.TogglePause();

        bool didRun = uiState.TryRunUnpausedAction(() => state.SellSelectedCrop());

        Assert.False(didRun);
        Assert.Equal(0, state.Money);
        Assert.Equal(1, state.GetItemCount("turnip"));
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksMachineStartWhilePaused()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        uiState.TogglePause();

        bool didRun = uiState.TryRunUnpausedAction(() => state.StartSelectedProcessorMachineFromInventory());

        Assert.False(didRun);
        Assert.False(state.Fermenter.IsWorking);
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksMachineCollectWhilePaused()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        state.StartFermenterFromInventory();
        state.AdvanceDay();
        state.AdvanceDay();
        uiState.TogglePause();

        bool didRun = uiState.TryRunUnpausedAction(() => state.CollectSelectedProcessorMachineOutput());

        Assert.False(didRun);
        Assert.True(state.Fermenter.HasOutputReady);
        Assert.Equal(0, state.GetItemCount("wine"));
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksStorageDepositWhilePaused()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "turnip", 1);
        uiState.TogglePause();

        bool didRun = uiState.TryRunUnpausedAction(() => state.DepositAllSellablesToStorage());

        Assert.False(didRun);
        Assert.Equal(1, state.GetItemCount("turnip"));
        Assert.Equal(0, state.GetStorageItemCount("turnip"));
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksStorageWithdrawWhilePaused()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        AddStorageItem(state, "turnip", 1);
        uiState.TogglePause();

        bool didRun = uiState.TryRunUnpausedAction(() => state.WithdrawAllFromStorage());

        Assert.False(didRun);
        Assert.Equal(0, state.GetItemCount("turnip"));
        Assert.Equal(1, state.GetStorageItemCount("turnip"));
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksMarketPurchaseWhilePaused()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 160);
        uiState.TogglePause();

        bool didRun = uiState.TryRunUnpausedAction(() => state.BuyMarketOffer("buy_grapes"));

        Assert.False(didRun);
        Assert.Equal(0, state.GetItemCount("grape"));
        Assert.Equal(160, state.Money);
        Assert.Empty(state.ActivityLog);
    }

    [Fact]
    public void DebugUiState_ActivityLogScreen_CanBeViewedWhilePaused()
    {
        DebugUiState state = new();
        state.TogglePause();

        state.ShowActivityLogScreen();

        Assert.True(state.IsPaused);
        Assert.Equal(GameScreen.ActivityLog, state.CurrentScreen);
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksAutoplayStepWhilePaused()
    {
        DebugUiState uiState = new();
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        AutoPlayTestController controller = new(state, registry, new AutoPlaySettings { TargetMoney = 50, MaxSteps = 10 });
        uiState.TogglePause();

        bool didRun = uiState.TryRunUnpausedAction(() => controller.RunStep());

        Assert.False(didRun);
        Assert.Equal(0, state.Stats.AutoPlayStepsRun);
    }

    [Fact]
    public void DebugUiState_TryRunUnpausedAction_BlocksGameplayWhileLoadConfirmationIsPending()
    {
        DebugUiState uiState = new();
        TurnipTestState state = new(BuildRegistry());
        uiState.RequestLoadConfirmation();

        bool didRun = uiState.TryRunUnpausedAction(() => state.AdvanceDay());

        Assert.False(didRun);
        Assert.Equal(1, state.DayNumber);
    }

    [Fact]
    public void DebugUiState_Reset_ClearsPauseState()
    {
        DebugUiState state = new();
        state.TogglePause();
        state.RequestLoadConfirmation();

        state.Reset();

        Assert.False(state.IsPaused);
        Assert.False(state.IsLoadConfirmationPending);
        Assert.Equal(GameScreen.Farm, state.CurrentScreen);
    }

    [Fact]
    public void InventoryScreenLines_IncludeGrapeHopsWineAndBeerWhenPresent()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetInventoryScreenLines(
            registry,
            itemId => itemId switch
            {
                "grape" => 4,
                "hops" => 3,
                "wine" => 1,
                "beer" => 2,
                _ => 0
            },
            itemId => itemId switch
            {
                "turnip" => 2,
                _ => 0
            },
            includeDebugDetails: false);

        Assert.Contains("PLAYER INVENTORY", lines);
        Assert.Contains("Grapes x4 | $45 each | $180 total", lines);
        Assert.Contains("Hops x3 | $40 each | $120 total", lines);
        Assert.Contains("Wine x1 | $160 each | $160 total", lines);
        Assert.Contains("Beer x2 | $140 each | $280 total", lines);
        Assert.Contains("SHARED STORAGE SUMMARY", lines);
        Assert.Contains("Items: 2 | Value: $60", lines);
        Assert.Contains("ESC: return to farm", lines);
        Assert.Contains("LIVE - press Space to pause", lines);
        Assert.Contains("F8: storage | F9: computer | F4: help", lines);
    }

    [Fact]
    public void MarketOfferDirectValue_UsesJsonSellPrice()
    {
        ContentRegistry registry = BuildRegistry();

        DebugUiTextFormatter.MarketOfferDirectEconomics economics = DebugUiTextFormatter.CalculateMarketOfferDirectValue(
            registry,
            registry.GetMarketOffer("buy_grapes"));

        Assert.Equal(135, economics.DirectValue);
    }

    [Fact]
    public void MarketOfferDirectResaleProfit_EqualsDirectValueMinusOfferPrice()
    {
        ContentRegistry registry = BuildRegistry();

        DebugUiTextFormatter.MarketOfferDirectEconomics economics = DebugUiTextFormatter.CalculateMarketOfferDirectValue(
            registry,
            registry.GetMarketOffer("buy_grapes"));

        Assert.Equal(-25, economics.DirectResaleProfit);
    }

    [Fact]
    public void MarketOfferBestProcessingUse_FindsGrapeToWineRecipe()
    {
        ContentRegistry registry = BuildRegistry();

        DebugUiTextFormatter.MarketOfferProcessingEconomics? economics = DebugUiTextFormatter.FindBestProcessingUseForMarketOffer(
            registry,
            registry.GetMarketOffer("buy_grapes"));

        Assert.NotNull(economics);
        Assert.Equal("grape_to_wine", economics.Value.RecipeId);
        Assert.Equal("wine", economics.Value.OutputItemId);
    }

    [Fact]
    public void MarketOfferBestProcessingUse_FindsHopsToBeerRecipe()
    {
        ContentRegistry registry = BuildRegistry();

        DebugUiTextFormatter.MarketOfferProcessingEconomics? economics = DebugUiTextFormatter.FindBestProcessingUseForMarketOffer(
            registry,
            registry.GetMarketOffer("buy_hops"));

        Assert.NotNull(economics);
        Assert.Equal("hops_to_beer", economics.Value.RecipeId);
        Assert.Equal("beer", economics.Value.OutputItemId);
    }

    [Fact]
    public void MarketOfferProcessingEconomics_UsesJsonOutputSellPrice()
    {
        ContentRegistry registry = BuildRegistry();

        DebugUiTextFormatter.MarketOfferProcessingEconomics? economics = DebugUiTextFormatter.CalculateMarketOfferProcessingEconomics(
            registry,
            registry.GetMarketOffer("buy_grapes"),
            registry.GetProcessingRecipe("grape_to_wine"));

        Assert.NotNull(economics);
        Assert.Equal(160, economics.Value.OutputValue);
    }

    [Fact]
    public void MarketOfferProcessingEconomics_ProfitIfBought_EqualsOutputValueMinusOfferPrice()
    {
        ContentRegistry registry = BuildRegistry();

        DebugUiTextFormatter.MarketOfferProcessingEconomics? economics = DebugUiTextFormatter.CalculateMarketOfferProcessingEconomics(
            registry,
            registry.GetMarketOffer("buy_hops"),
            registry.GetProcessingRecipe("hops_to_beer"));

        Assert.NotNull(economics);
        Assert.Equal(-5, economics.Value.ProfitIfBought);
    }

    [Fact]
    public void MarketOfferProcessingEconomics_ValueAddedIfGrown_EqualsOutputMinusRawInputValue()
    {
        ContentRegistry registry = BuildRegistry();

        DebugUiTextFormatter.MarketOfferProcessingEconomics? economics = DebugUiTextFormatter.CalculateMarketOfferProcessingEconomics(
            registry,
            registry.GetMarketOffer("buy_grapes"),
            registry.GetProcessingRecipe("grape_to_wine"));

        Assert.NotNull(economics);
        Assert.Equal(25, economics.Value.ValueAddedIfGrown);
    }

    [Fact]
    public void MarketScreenLines_IncludeDirectValueDirectResaleAndBestUse()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetMarketScreenLines(
            registry,
            registry.MarketOffers,
            170,
            includeDebugDetails: false);

        Assert.Contains("SUPPLY MARKET", lines);
        Assert.Contains("Money: $170", lines);
        Assert.Contains("1. 3 Grapes - $160 - Affordable", lines);
        Assert.Contains("   Direct value: $135", lines);
        Assert.Contains("   Direct resale: -$25", lines);
        Assert.Contains("   Best use: 3 Grapes -> 1 Wine, 2 days", lines);
        Assert.Contains("   Profit if bought: $0", lines);
        Assert.Contains("2. 3 Hops - $145 - Affordable", lines);
        Assert.Contains("   Best use: 3 Hops -> 1 Beer, 2 days", lines);
        Assert.Contains("   Profit if bought: -$5", lines);
        Assert.Contains("3. 1 Wine - $190 - Need more money", lines);
        Assert.Contains("1-9: buy offer | I: inventory | F8: storage", lines);
    }

    [Fact]
    public void MarketScreenLines_OfferWithoutRecipe_DisplaysDirectValueCleanly()
    {
        ContentRegistry registry = BuildRegistry();
        MarketOfferDefinition turnipOffer = new()
        {
            Id = "buy_turnip",
            ItemId = "turnip",
            Quantity = 2,
            Price = 70,
            Category = "crop"
        };

        string[] lines = DebugUiTextFormatter.GetMarketOfferDisplayLines(registry, turnipOffer, 4, 100, includeDebugDetails: false);

        Assert.Contains("4. 2 Turnip - $70 - Affordable", lines);
        Assert.Contains("   Direct value: $60", lines);
        Assert.Contains("   Direct resale: -$10", lines);
        Assert.DoesNotContain(lines, line => line.Contains("Best use:", StringComparison.Ordinal));
    }

    [Fact]
    public void ComputerMarketSummaryLines_ShowBestCurrentOpportunity()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetComputerMarketSummaryLines(registry);

        Assert.Contains("MARKET OPPORTUNITY", lines);
        Assert.Contains("Offers available: 3", lines);
        Assert.Contains("Next opportunity in 0 days", lines);
        Assert.Contains("Best market use: 1 Wine -> 1 Aged Wine, 5 days, profit if bought: +$70", lines);
    }

    [Fact]
    public void DashboardSummary_ShowsActiveMarketOpportunityLine()
    {
        ContentRegistry registry = BuildRegistry();
        MarketOpportunity opportunity = new()
        {
            Id = "wine_boom",
            ItemId = "wine",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 14,
            DisplayName = "Wine Boom",
            Message = "Wine sells for 150% for 20 days."
        };

        DebugUiTextFormatter.DashboardSummary summary = DebugUiTextFormatter.GetDashboardSummary(
            30,
            100,
            4,
            3,
            registry,
            "turnip_seed",
            _ => 0,
            itemId => itemId == "wine" ? 240 : registry.GetItem(itemId).SellPrice,
            opportunity,
            0);

        Assert.Equal("MARKET BONUS: WINE +50% FOR 14 DAYS | MAKE WINE: GRAPES -> WINE", summary.MarketStatusLine);
    }

    [Fact]
    public void DashboardSummary_ShowsNextOpportunityCountdown_WhenInactive()
    {
        ContentRegistry registry = BuildRegistry();

        DebugUiTextFormatter.DashboardSummary summary = DebugUiTextFormatter.GetDashboardSummary(
            22,
            100,
            4,
            3,
            registry,
            "turnip_seed",
            _ => 0,
            itemId => registry.GetItem(itemId).SellPrice,
            null,
            8);

        Assert.Equal("MARKET: NEXT BONUS IN 8 DAYS", summary.MarketStatusLine);
    }

    [Fact]
    public void StorageScreenLines_ShowStoredItemsWithCountsAndSellValues()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetStorageScreenLines(registry, itemId => itemId switch
        {
            "grape_seed" => 5,
            "grape" => 4,
            "wine" => 2,
            _ => 0
        }, includeDebugDetails: false);

        Assert.Contains("SHARED STORAGE", lines);
        Assert.Contains("Grape Seeds x5", lines);
        Assert.Contains("Grapes x4 | $45 each | $180 total", lines);
        Assert.Contains("Wine x2 | $160 each | $320 total", lines);
        Assert.Contains("C: deposit | U: withdraw | S: sell", lines);
        Assert.Contains("ESC: return to farm", lines);
    }

    [Fact]
    public void InventoryScreenLines_ShowAutoplayRunningStatus_WhenAutoplayActive()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetInventoryScreenLines(
            registry,
            _ => 0,
            _ => 0,
            includeDebugDetails: false,
            isAutoPlayRunning: true);

        Assert.Contains("AUTOPLAY RUNNING - press Space to pause", lines);
    }

    [Fact]
    public void SimulationStatusLine_ShowsPausedTextForFarmDashboardWhenAutoplayIsOff()
    {
        Assert.Equal("PAUSED - press Space to resume", DebugUiTextFormatter.GetSimulationStatusLine(isPaused: true, isAutoPlayRunning: false));
    }

    [Fact]
    public void SimulationStatusLine_ShowsAutoplayPausedText()
    {
        Assert.Equal("AUTOPLAY PAUSED - press Space to resume", DebugUiTextFormatter.GetSimulationStatusLine(isPaused: true, isAutoPlayRunning: true));
    }

    [Fact]
    public void InventoryScreenLines_ShowPausedStatus_WhenPaused()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetInventoryScreenLines(
            registry,
            _ => 0,
            _ => 0,
            includeDebugDetails: false,
            isAutoPlayRunning: false,
            isPaused: true);

        Assert.Contains("PAUSED - press Space to resume", lines);
    }

    [Fact]
    public void StorageScreenLines_ShowPausedStatus_WhenPaused()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetStorageScreenLines(registry, _ => 0, includeDebugDetails: false, isPaused: true);

        Assert.Contains("PAUSED - press Space to resume", lines);
    }

    [Fact]
    public void GlobalInventoryLines_ShowCombinedCountsAndSeparatePlayerAndStorageCounts()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetGlobalInventoryLines(
            registry,
            itemId => itemId switch
            {
                "grape" => 3,
                "wine" => 1,
                "grape_seed" => 5,
                _ => 0
            },
            itemId => itemId switch
            {
                "grape" => 9,
                "wine" => 1,
                _ => 0
            });

        Assert.Contains("GLOBAL INVENTORY", lines);
        Assert.Contains("Grapes x12 | Player 3 / Storage 9 | $45 each | $540 total", lines);
        Assert.Contains("Wine x2 | Player 1 / Storage 1 | $160 each | $320 total", lines);
        Assert.Contains("Grape Seeds x5 | Player 5 / Storage 0", lines);
    }

    [Fact]
    public void GlobalValueSummaryLines_ShowSellableValueAndPotentialMoney()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetGlobalValueSummaryLines(
            registry,
            420,
            itemId => itemId switch
            {
                "wine" => 1,
                _ => 0
            },
            itemId => itemId switch
            {
                "grape" => 12,
                _ => 0
            });

        Assert.Contains("Money: $420", lines);
        Assert.Contains("Player sellable value: $160", lines);
        Assert.Contains("Storage sellable value: $540", lines);
        Assert.Contains("Total sellable value: $700", lines);
        Assert.Contains("Potential money: $1120", lines);
    }

    [Fact]
    public void ComputerScreenLines_ShowMachineBotsFarmAndHints()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SaveLoadStatus saveLoadStatus = new("savegame.json");
        saveLoadStatus.RecordSave(true, "Game saved.");
        saveLoadStatus.RecordLoad(false, "Load failed: missing item id in player inventory: 'ghost_grape'.");
        AddInventoryItem(state, "grape", 3);
        AddStorageItem(state, "wine", 1);
        SetMoney(state, 400);
        state.UnlockPlanterBot();
        state.UnlockWateringBot();
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.WaterPlot(0, 0);
        state.StartFermenterFromInventory();
        state.AdvanceDay();

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.ActivityLog,
            saveLoadStatus,
            state.Stats,
            state.DayNumber,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y));

        Assert.Contains("MACHINES", lines);
        Assert.Contains("Fermenter: Working", lines);
        Assert.Contains("Aging Barrel: Idle", lines);
        Assert.Contains("Days remaining: 1", lines);
        Assert.Contains("Current value added: +$25", lines);
        Assert.Contains("Lifetime processing profit: $0", lines);
        Assert.Contains("MARKET OPPORTUNITY", lines);
        Assert.Contains("Offers available: 3", lines);
        Assert.Contains("Best market use: 1 Wine -> 1 Aged Wine, 5 days, profit if bought: +$70", lines);
        Assert.Contains("PRODUCTION", lines);
        Assert.Contains("Grapes -> Wine -> Aged Wine", lines);
        Assert.Contains("Hops -> Beer", lines);
        Assert.Contains("Open Production Report for details.", lines);
        Assert.Contains("BOTS", lines);
        Assert.Contains("Planter: Unlocked", lines);
        Assert.Contains("Watering: Unlocked", lines);
        Assert.Contains("Harvester: Locked, cost $120 (affordable)", lines);
        Assert.Contains("FARM", lines);
        Assert.Contains($"Day {state.DayNumber}", lines);
        Assert.Contains("Size: 3x3", lines);
        Assert.Contains("Planted: 1", lines);
        Assert.Contains("Watered: 0", lines);
        Assert.Contains("Empty: 8", lines);
        Assert.Contains("PRODUCTION AREAS", lines);
        Assert.Contains("No production areas available.", lines);
        Assert.Contains("HINTS", lines);
        Assert.Contains("Plant seeds with P or run Planter Bot with V.", lines);
        Assert.Contains("Water crops with W or run Watering Bot with B.", lines);
        Assert.Contains("Start processor with M.", lines);
        Assert.Contains("SAVE STATUS", lines);
        Assert.Contains("Path: savegame.json", lines);
        Assert.Contains("Last save: Success - Game saved.", lines);
        Assert.Contains("Last load: Failed - Load failed: missing item id in player inventory: 'ghost_grape'.", lines);
        Assert.Contains("RECENT ACTIVITY", lines);
        Assert.Contains(lines, line => line.StartsWith("Day 1 | ", StringComparison.Ordinal));
        Assert.Contains("ESC: return to farm", lines);
        Assert.Contains(lines, line => line.Contains("F11: ledger", StringComparison.Ordinal));
        Assert.Contains("LIVE - press Space to pause", lines);
        Assert.DoesNotContain(lines, line => line.Contains("Fermenter: 3 Grapes -> 1 Wine | 2 days | $135 -> $160 | +$25", StringComparison.Ordinal));
    }

    [Fact]
    public void ComputerScreenLines_ShowPausedStatus_WhenPaused()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.ActivityLog,
            new SaveLoadStatus("savegame.json"),
            state.Stats,
            state.DayNumber,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y),
            isPaused: true);

        Assert.Contains("PAUSED - press Space to resume", lines);
    }

    [Fact]
    public void ComputerScreenLines_ShowRestorationSection()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 400);

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.ActivityLog,
            new SaveLoadStatus("savegame.json"),
            state.Stats,
            state.DayNumber,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y),
            cleanupSites: state.RestorationSites);

        Assert.Contains("RESTORATION", lines);
        Assert.Contains("Old Junk Pile: Uncleared", lines);
        Assert.Contains("Cost: $500", lines);
        Assert.Contains("Reward: Unlocks 5x3 farm expansion", lines);
        Assert.Contains("Affordable: no", lines);
    }

    [Fact]
    public void ActivityLogScreenLines_ShowRecentEntriesNewestFirst()
    {
        ActivityLogEntry[] entries =
        [
            new(12, ActivityLogCategory.Market, "Bought 3 Hops for $120."),
            new(12, ActivityLogCategory.Machine, "Started Fermenter: 3 Hops -> 1 Beer, ready in 2 days."),
            new(14, ActivityLogCategory.Sale, "Sold 1 Beer for $140.")
        ];

        string[] lines = DebugUiTextFormatter.GetActivityLogScreenLines(entries);

        Assert.Contains("ACTIVITY LOG", lines);
        Assert.Equal("Day 14 | Sale | Sold 1 Beer for $140.", lines[1]);
        Assert.Equal("Day 12 | Machine | Started Fermenter: 3 Hops -> 1 Beer, ready in 2 days.", lines[2]);
        Assert.Equal("Day 12 | Market | Bought 3 Hops for $120.", lines[3]);
    }

    [Fact]
    public void RecentActivityLines_ReturnConfiguredCount()
    {
        ActivityLogEntry[] entries =
        [
            new(10, ActivityLogCategory.Market, "Bought 3 Grapes for $160."),
            new(11, ActivityLogCategory.Machine, "Started Fermenter: 3 Grapes -> 1 Wine, ready in 2 days."),
            new(13, ActivityLogCategory.Machine, "Collected 1 Wine from Fermenter, value added +$25."),
            new(13, ActivityLogCategory.Sale, "Sold 1 Wine for $160.")
        ];

        string[] lines = DebugUiTextFormatter.GetRecentActivityLines(entries, 3);

        Assert.Equal(3, lines.Length);
        Assert.Equal("Day 13 | Sale | Sold 1 Wine for $160.", lines[0]);
        Assert.Equal("Day 13 | Machine | Collected 1 Wine from Fermenter, value added +$25.", lines[1]);
    }

    [Fact]
    public void FarmControlLines_StayCompactAndOnlyAddComputerHint()
    {
        string[] lines = DebugUiTextFormatter.GetFarmControlLines(BuildRegistry(), ThemeVocabulary.FarmingDefault, null, null, 8);

        Assert.Contains("ACTIONS", lines);
        Assert.Contains("Primary:", lines);
        Assert.Contains("[  P  ] Plant Grape Seeds - READY", lines);
        Assert.Contains("[ SPACE ] Pause", lines);
        Assert.Contains("MARKET: NEXT BONUS IN 8 DAYS", lines);
        Assert.Contains("MORE KEYS", lines);
        Assert.Contains("[ F3 ] Open machines", lines);
        Assert.DoesNotContain("GLOBAL INVENTORY", lines);
        Assert.DoesNotContain(lines, line => line.Contains("I Inventory", StringComparison.Ordinal));
    }

    [Fact]
    public void FarmControlLines_UseThemeVocabularyLabels()
    {
        string[] lines = DebugUiTextFormatter.GetFarmControlLines(BuildRegistry(), ThemeVocabulary.MiningDemo, null, null, 0);

        Assert.Contains("[  P  ] Plant Grape Seeds - READY", lines);
        Assert.Contains("[ TAB ] Change Marker", lines);
        Assert.DoesNotContain(lines, line => line.Contains("[  W  ]", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains("[  H  ]", StringComparison.Ordinal));
    }

    [Fact]
    public void FarmVisualCommands_EmptyTile_ProducesPlantCommand()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetFarmVisualCommands(
            registry,
            plot,
            "grape_seed",
            static (_, _) => false,
            isPaused: false);

        DebugUiTextFormatter.VisualCommand plantCommand = Assert.Single(commands.Where(command => command.KeyLabel == "P"));
        Assert.True(plantCommand.IsEnabled);
        Assert.Equal("Plant Grape Seeds", plantCommand.ActionLabel);
        Assert.True(plantCommand.IsPrimary);
        Assert.Equal("READY", plantCommand.StatusLabel);
        Assert.Equal("P", DebugUiTextFormatter.GetPrimaryVisualCommand(commands)?.KeyLabel);
        Assert.DoesNotContain(commands, command => command.KeyLabel == "W");
        Assert.DoesNotContain(commands, command => command.KeyLabel == "H");
        Assert.True(commands.Length <= 4);
    }

    [Fact]
    public void FarmVisualCommands_UnmaintainedProducer_ProducesWaterCommand()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);
        plot.TryPlant("grape");

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetFarmVisualCommands(
            registry,
            plot,
            "grape_seed",
            static (_, _) => false,
            isPaused: false,
            themeVocabulary: ThemeVocabulary.FarmingDefault);

        DebugUiTextFormatter.VisualCommand waterCommand = Assert.Single(commands.Where(command => command.KeyLabel == "W"));
        Assert.True(waterCommand.IsEnabled);
        Assert.Equal("Water Grapes", waterCommand.ActionLabel);
        Assert.Equal("W", DebugUiTextFormatter.GetPrimaryVisualCommand(commands)?.KeyLabel);
    }

    [Fact]
    public void FarmVisualCommands_ReadyProducer_ProducesHarvestCommand()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);
        plot.TryPlant("grape");

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetFarmVisualCommands(
            registry,
            plot,
            "grape_seed",
            static (_, _) => true,
            isPaused: false,
            themeVocabulary: ThemeVocabulary.FarmingDefault);

        DebugUiTextFormatter.VisualCommand harvestCommand = Assert.Single(commands.Where(command => command.KeyLabel == "H"));
        Assert.True(harvestCommand.IsEnabled);
        Assert.Equal("Harvest Grapes", harvestCommand.ActionLabel);
        Assert.Equal("H", DebugUiTextFormatter.GetPrimaryVisualCommand(commands)?.KeyLabel);
        Assert.DoesNotContain(commands, command => command.KeyLabel == "P");
        Assert.DoesNotContain(commands, command => command.KeyLabel == "W");
    }

    [Fact]
    public void FarmVisualCommands_NotReadyProducer_HidesIrrelevantHarvestCommand()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);
        plot.TryPlant("grape");

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetFarmVisualCommands(
            registry,
            plot,
            "grape_seed",
            static (_, _) => false,
            isPaused: false);

        Assert.DoesNotContain(commands, command => command.KeyLabel == "H");
        Assert.Equal("W", DebugUiTextFormatter.GetPrimaryVisualCommand(commands)?.KeyLabel);
    }

    [Fact]
    public void FarmVisualCommands_WateredProducerWithoutReadyAction_DoesNotInsertDefaultPrimaryCommand()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);
        plot.TryPlant("grape");
        plot.Water();

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetFarmVisualCommands(
            registry,
            plot,
            "grape_seed",
            static (_, _) => false,
            isPaused: false);

        Assert.Equal("G", DebugUiTextFormatter.GetPrimaryVisualCommand(commands)?.KeyLabel);
        Assert.All(commands, command => Assert.False(string.IsNullOrWhiteSpace(command.KeyLabel)));
        Assert.All(commands, command => Assert.False(string.IsNullOrWhiteSpace(command.ActionLabel)));
        Assert.Equal(["G", "TAB", "SPACE"], commands.Select(command => command.KeyLabel).ToArray());
    }

    [Fact]
    public void FarmVisualCommands_Paused_ShowsResumeCommand()
    {
        ContentRegistry registry = BuildRegistry();

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetFarmVisualCommands(
            registry,
            new FarmPlot(0, 0),
            "turnip_seed",
            static (_, _) => false,
            isPaused: true);

        DebugUiTextFormatter.VisualCommand pauseCommand = Assert.Single(commands.Where(command => command.KeyLabel == "SPACE"));
        Assert.Equal("Resume", pauseCommand.ActionLabel);
        Assert.True(pauseCommand.IsEnabled);
    }

    [Fact]
    public void FarmVisualCommands_UseMiningThemeVocabularyLabels()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);
        plot.TryPlant("turnip");

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetFarmVisualCommands(
            registry,
            plot,
            "turnip_seed",
            static (_, _) => true,
            isPaused: false,
            themeVocabulary: ThemeVocabulary.MiningDemo);

        Assert.Equal("Extract Turnip", commands.Single(command => command.KeyLabel == "H").ActionLabel);
        Assert.DoesNotContain(commands, command => command.KeyLabel == "W");
    }

    [Fact]
    public void FarmVisualCommands_ActiveOpportunity_AddsMachineHintCommand()
    {
        ContentRegistry registry = BuildRegistry();
        MarketOpportunity opportunity = new()
        {
            Id = "beer_boom",
            DisplayName = "Beer Boom",
            ItemId = "beer",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 14
        };

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetFarmVisualCommands(
            registry,
            new FarmPlot(0, 0),
            "turnip_seed",
            static (_, _) => false,
            isPaused: false,
            activeMarketOpportunity: opportunity);

        DebugUiTextFormatter.VisualCommand marketCommand = Assert.Single(commands.Where(command => command.KeyLabel == "F3"));
        Assert.Equal("Machines: make Beer", marketCommand.ActionLabel);
        Assert.Equal("MARKET BONUS", marketCommand.StatusLabel);
        Assert.Equal(DebugUiTextFormatter.VisualCommandThemeRole.MarketBonus, marketCommand.ThemeRole);
        Assert.Contains(commands.Take(2), command => command.KeyLabel == "F3");
    }

    [Fact]
    public void FarmVisualCommands_WithMarketBonus_StayCompact()
    {
        ContentRegistry registry = BuildRegistry();
        MarketOpportunity opportunity = new()
        {
            Id = "beer_boom",
            DisplayName = "Beer Boom",
            ItemId = "beer",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 14
        };

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetFarmVisualCommands(
            registry,
            new FarmPlot(0, 0),
            "turnip_seed",
            static (_, _) => false,
            isPaused: false,
            activeMarketOpportunity: opportunity);

        Assert.True(commands.Length <= 4);
        Assert.Equal("P", commands[0].KeyLabel);
    }

    [Fact]
    public void FormatVisualCommand_UsesBracketedKeycapsAndDisabledReason()
    {
        string enabled = DebugUiTextFormatter.FormatVisualCommand(new DebugUiTextFormatter.VisualCommand("P", "Plant Grape Seeds", true));
        string disabled = DebugUiTextFormatter.FormatVisualCommand(new DebugUiTextFormatter.VisualCommand("H", "Harvest", false, ReasonDisabled: "Not ready"));

        Assert.Equal("[  P  ] Plant Grape Seeds", enabled);
        Assert.Equal("[  H  ] Harvest - Not ready", disabled);
    }

    [Fact]
    public void BotStatusText_CanUseMiningDemoBotNames()
    {
        Assert.Equal("Claim Bot: LOCKED COST 60 NEED 60 MORE", DebugUiTextFormatter.GetBotStatusText(ThemeVocabulary.MiningDemo.StarterBotName, false, 60, 0));
        Assert.Equal("Power Bot: LOCKED COST 90 NEED 90 MORE", DebugUiTextFormatter.GetBotStatusText(ThemeVocabulary.MiningDemo.MaintainerBotName, false, 90, 0));
        Assert.Equal("Extractor Bot: LOCKED COST 120 NEED 120 MORE", DebugUiTextFormatter.GetBotStatusText(ThemeVocabulary.MiningDemo.CollectorBotName, false, 120, 0));
    }

    [Fact]
    public void MachineScreenLines_IncludeFermenterEconomicsAndStatus()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        ProcessorMachineInstance agingBarrel = new("aging_barrel");
        GameStats stats = new();
        fermenter.Start("grape_to_wine", "grape", 3, "wine", 1, 2);
        fermenter.AdvanceDay();

        string[] lines = DebugUiTextFormatter.GetMachineScreenLines(registry, [fermenter, agingBarrel], "fermenter", "grape_to_wine", stats, _ => 0);

        Assert.Contains("Selected Machine: Fermenter", lines);
        Assert.Contains("Status: Working", lines);
        Assert.Contains("MACHINE CONTROLS", lines);
        Assert.Contains("[  M  ] Working on Wine - 1 day left", lines);
        Assert.Contains("[ PGUP/PGDN ] Machine: Fermenter", lines);
        Assert.Contains("[ UP/DOWN ] Recipe: Grapes -> Wine", lines);
        Assert.Contains("Recipes:", lines);
        Assert.Contains("Current: 3 Grapes -> 1 Wine", lines);
        Assert.Contains("Output: Wine", lines);
        Assert.Contains("Aging Barrel: Idle", lines);
        Assert.Contains("[  O  ] Collect output - Not ready", lines);
        Assert.Contains("[ ESC ] Return to farm", lines);
        Assert.Contains(lines, line => line.StartsWith("> 1. 3 Grapes -> 1 Wine", StringComparison.Ordinal));
    }

    [Fact]
    public void MachineVisualCommands_SelectedWineRecipe_ShowsStartWineWhenInputsExist()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetMachineVisualCommands(registry, fermenter, "grape_to_wine", itemId => itemId == "grape" ? 3 : 0);

        DebugUiTextFormatter.VisualCommand startCommand = Assert.Single(commands.Where(command => command.KeyLabel == "M"));
        Assert.True(startCommand.IsEnabled);
        Assert.Equal("Make Wine", startCommand.ActionLabel);
    }

    [Fact]
    public void MachineVisualCommands_SelectedBeerRecipe_ShowsStartBeerWhenInputsExist()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetMachineVisualCommands(registry, fermenter, "hops_to_beer", itemId => itemId == "hops" ? 3 : 0);

        DebugUiTextFormatter.VisualCommand startCommand = Assert.Single(commands.Where(command => command.KeyLabel == "M"));
        Assert.True(startCommand.IsEnabled);
        Assert.Equal("Make Beer", startCommand.ActionLabel);
    }

    [Fact]
    public void MachineVisualCommands_SelectedBeerRecipeWithoutHops_ShowsNeedMessage()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetMachineVisualCommands(registry, fermenter, "hops_to_beer", itemId => itemId == "hops" ? 1 : 0);

        DebugUiTextFormatter.VisualCommand startCommand = Assert.Single(commands.Where(command => command.KeyLabel == "M"));
        Assert.False(startCommand.IsEnabled);
        Assert.Equal("Need 3 Hops", startCommand.ActionLabel);
        Assert.Equal("Have 1", startCommand.ReasonDisabled);
    }

    [Fact]
    public void MachineVisualCommands_OutputReady_ShowsCollectOutput()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        fermenter.Restore("hops_to_beer", "hops", 3, "beer", 1, 0, true);

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetMachineVisualCommands(registry, fermenter, "hops_to_beer", _ => 0);

        DebugUiTextFormatter.VisualCommand collectCommand = Assert.Single(commands.Where(command => command.KeyLabel == "O"));
        Assert.True(collectCommand.IsEnabled);
        Assert.Equal("Collect Beer", collectCommand.ActionLabel);
    }

    [Fact]
    public void MachineVisualCommands_BeerBoom_UsesMarketBonusLabel()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        MarketOpportunity opportunity = new()
        {
            Id = "beer_boom",
            DisplayName = "Beer Boom",
            ItemId = "beer",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 14
        };

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetMachineVisualCommands(registry, fermenter, "hops_to_beer", itemId => itemId == "hops" ? 3 : 0, opportunity);

        DebugUiTextFormatter.VisualCommand startCommand = Assert.Single(commands.Where(command => command.KeyLabel == "M"));
        Assert.Equal("Make Beer", startCommand.ActionLabel);
        Assert.Equal("MARKET BONUS", startCommand.StatusLabel);
        Assert.Equal(DebugUiTextFormatter.VisualCommandThemeRole.MarketBonus, startCommand.ThemeRole);
    }

    [Fact]
    public void MachineVisualCommands_IncludeSelectionGuidance()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetMachineVisualCommands(registry, fermenter, "grape_to_wine", itemId => itemId == "grape" ? 3 : 0);

        Assert.Contains(commands, command => command.KeyLabel == "PGUP/PGDN" && command.ActionLabel == "Machine: Fermenter");
        Assert.Contains(commands, command => command.KeyLabel == "UP/DOWN" && command.ActionLabel == "Recipe: Grapes -> Wine");
    }

    [Fact]
    public void MachineVisualCommands_MarketBonus_SortsBeforeNavigationCommands()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        MarketOpportunity opportunity = new()
        {
            Id = "beer_boom",
            DisplayName = "Beer Boom",
            ItemId = "beer",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 14
        };

        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.GetMachineVisualCommands(registry, fermenter, "hops_to_beer", itemId => itemId == "hops" ? 3 : 0, opportunity);

        Assert.Equal("M", commands[0].KeyLabel);
        Assert.Equal(DebugUiTextFormatter.VisualCommandGroup.Market, commands[0].Group);
    }

    [Fact]
    public void VisualCommandFormatting_DoesNotMutateFarmState()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        int originalDay = state.DayNumber;
        int originalMoney = state.Money;
        int originalSeedCount = state.GetItemCount(state.SelectedSeedItemId);

        _ = DebugUiTextFormatter.GetFarmVisualCommands(
            registry,
            state.GetSelectedPlot(),
            state.SelectedSeedItemId,
            state.IsPlotHarvestable,
            isPaused: false,
            themeVocabulary: state.ThemeVocabulary,
            activeMarketOpportunity: state.ActiveMarketOpportunity);

        Assert.Equal(originalDay, state.DayNumber);
        Assert.Equal(originalMoney, state.Money);
        Assert.Equal(originalSeedCount, state.GetItemCount(state.SelectedSeedItemId));
    }

    [Fact]
    public void MachineScreenLines_ShowPausedStatus_WhenPaused()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        ProcessorMachineInstance agingBarrel = new("aging_barrel");

        string[] lines = DebugUiTextFormatter.GetMachineScreenLines(registry, [fermenter, agingBarrel], "fermenter", "grape_to_wine", new GameStats(), _ => 0, isPaused: true);

        Assert.Contains("PAUSED - press Space to resume", lines);
    }

    [Fact]
    public void MachineScreenLines_IncludeProductionTotals()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        ProcessorMachineInstance agingBarrel = new("aging_barrel");
        GameStats stats = new();
        stats.RecordMachineRunStarted(135);
        stats.RecordMachineRunStarted(120);
        stats.RecordMachineOutputCollected("wine", 1, 160, 25);
        stats.RecordMachineOutputCollected("beer", 1, 140, 20);

        string[] lines = DebugUiTextFormatter.GetMachineScreenLines(registry, [fermenter, agingBarrel], "fermenter", "grape_to_wine", stats, _ => 0);

        Assert.Contains("PRODUCTION TOTALS", lines);
        Assert.Contains("Runs: 2", lines);
        Assert.Contains("Outputs: 2", lines);
        Assert.Contains("Goods produced: 2", lines);
        Assert.Contains("Raw value consumed: $255", lines);
        Assert.Contains("Output value created: $300", lines);
        Assert.Contains("Processing profit: +$45", lines);
    }

    [Fact]
    public void MachineScreenLines_IncludePerOutputItemTotals()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        ProcessorMachineInstance agingBarrel = new("aging_barrel");
        GameStats stats = new();
        stats.RecordMachineOutputCollected("wine", 3, 480, 75);
        stats.RecordMachineOutputCollected("beer", 1, 140, 20);

        string[] lines = DebugUiTextFormatter.GetMachineScreenLines(registry, [fermenter, agingBarrel], "fermenter", "grape_to_wine", stats, _ => 0);

        Assert.Contains("PRODUCED", lines);
        Assert.Contains("Wine: 3", lines);
        Assert.Contains("Beer: 1", lines);
    }

    [Fact]
    public void MachineProductionTotalsLines_UseGenericProcessedOutputDictionary()
    {
        ContentRegistry registry = BuildRegistry();
        GameStats stats = new();
        stats.RecordMachineOutputCollected("wine", 1, 160, 25);

        string[] lines = DebugUiTextFormatter.GetProcessedOutputLines(registry, stats);

        Assert.Contains("Wine: 1", lines);
        Assert.Equal(1, stats.GetProcessedOutputCount("wine"));
    }

    [Fact]
    public void CompactMachineStatusLines_StayCompactAndDoNotIncludeProductionTotals()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        fermenter.Start("grape_to_wine", "grape", 3, "wine", 1, 2);
        fermenter.AdvanceDay();

        string[] lines = DebugUiTextFormatter.GetCompactMachineStatusLines(registry, fermenter);

        Assert.Contains("Fermenter: Working on Wine, 1 day left", lines);
        Assert.DoesNotContain("PRODUCTION TOTALS", lines);
        Assert.DoesNotContain("Runs:", lines);
    }

    [Fact]
    public void CompactMachineStatusLines_WithMultipleMachines_ShowSelectionAndBothMachines()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        ProcessorMachineInstance agingBarrel = new("aging_barrel");
        agingBarrel.Start("wine_to_aged_wine", "wine", 1, "aged_wine", 1, 5);
        agingBarrel.AdvanceDay();

        string[] lines = DebugUiTextFormatter.GetCompactMachineStatusLines(registry, [fermenter, agingBarrel], "aging_barrel");

        Assert.Contains("Selected: Aging Barrel", lines);
        Assert.Contains("Fermenter: Idle", lines);
        Assert.Contains("Aging Barrel: Working on Aged Wine, 4 days left", lines);
    }

    [Fact]
    public void FarmMachinePanelLines_ListDefaultMachinesAndDetailHint()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);

        string[] lines = DebugUiTextFormatter.GetFarmMachinePanelLines(
            registry,
            state.ProcessorMachines,
            state.SelectedProcessorMachine.MachineId,
            machineId => registry.GetProcessingRecipesForMachine(machineId).FirstOrDefault()?.Id,
            state.GetAccessibleItemCount,
            state.ActiveMarketOpportunity,
            state.GetEffectiveSellPrice);

        Assert.Contains("[ M ] Fermenter", lines);
        Assert.Contains("[ M ] Aging Barrel", lines);
        Assert.Contains("F3 Machines for details", lines);
    }

    [Fact]
    public void MachineCardLines_IdleMachine_ShowSelectedRecipeHaveCountAndStartHint()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");

        string[] lines = DebugUiTextFormatter.GetMachineCardLines(
            registry,
            fermenter,
            "hops_to_beer",
            itemId => itemId == "hops" ? 3 : 0,
            isSelected: true);

        Assert.Contains("IDLE", lines);
        Assert.Contains("Selected: 3 Hops -> 1 Beer", lines);
        Assert.Contains("Have: 3 Hops", lines);
        Assert.Contains("[  M  ] Start Beer", lines);
    }

    [Fact]
    public void MachineCardLines_WorkingMachine_ShowRecipeOutputAndDaysRemaining()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        fermenter.Start("hops_to_beer", "hops", 3, "beer", 1, 2);
        fermenter.AdvanceDay();

        string[] lines = DebugUiTextFormatter.GetMachineCardLines(registry, fermenter, "hops_to_beer", _ => 0);

        Assert.Contains("WORKING...", lines);
        Assert.Contains("3 Hops -> 1 Beer", lines);
        Assert.Contains("1 day left", lines);
        Assert.DoesNotContain(lines, line => line.Contains("[  M  ]", StringComparison.Ordinal));
    }

    [Fact]
    public void MachineCardLines_ReadyMachine_ShowReadyStateAndCollectHint()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        fermenter.Restore("hops_to_beer", "hops", 3, "beer", 1, 0, true);

        string[] lines = DebugUiTextFormatter.GetMachineCardLines(registry, fermenter, "hops_to_beer", _ => 0);

        Assert.Contains("READY!", lines);
        Assert.Contains("Output: 1 Beer", lines);
        Assert.Contains("[  O  ] Collect Beer - READY", lines);
    }

    [Fact]
    public void MachineCardLines_BeerBoom_ShowMarketBonusAndBoostedValue()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        MarketOpportunity opportunity = new()
        {
            Id = "beer_boom",
            DisplayName = "Beer Boom",
            ItemId = "beer",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 14
        };

        string[] lines = DebugUiTextFormatter.GetMachineCardLines(
            registry,
            fermenter,
            "hops_to_beer",
            itemId => itemId == "hops" ? 3 : 0,
            opportunity,
            itemId => itemId == "beer" ? 210 : registry.GetItem(itemId).SellPrice,
            isSelected: true);

        Assert.Contains("MARKET BONUS: Beer $140 -> $210", lines);
        Assert.Contains("[  M  ] Start Beer - MARKET BONUS", lines);
    }

    [Fact]
    public void FarmMachinePanelFormatting_DoesNotMutateMachineState()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        ProcessorMachineInstance fermenter = state.ProcessorMachines.First(machine => machine.MachineId == "fermenter");
        string originalRecipeId = fermenter.CurrentRecipeId;
        int originalDaysRemaining = fermenter.DaysRemaining;
        bool originalReady = fermenter.HasOutputReady;

        _ = DebugUiTextFormatter.GetFarmMachinePanelLines(
            registry,
            state.ProcessorMachines,
            state.SelectedProcessorMachine.MachineId,
            machineId => registry.GetProcessingRecipesForMachine(machineId).FirstOrDefault()?.Id,
            state.GetAccessibleItemCount,
            state.ActiveMarketOpportunity,
            state.GetEffectiveSellPrice);

        Assert.Equal(originalRecipeId, fermenter.CurrentRecipeId);
        Assert.Equal(originalDaysRemaining, fermenter.DaysRemaining);
        Assert.Equal(originalReady, fermenter.HasOutputReady);
    }

    [Fact]
    public void MachineRecipeLines_ShowSelectedRecipeAndMarketBonus()
    {
        ContentRegistry registry = BuildRegistry();
        MarketOpportunity opportunity = new()
        {
            Id = "beer_boom",
            DisplayName = "Beer Boom",
            ItemId = "beer",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 14,
            Message = "Beer demand spiked."
        };

        string[] lines = DebugUiTextFormatter.GetMachineRecipeLines(
            registry,
            "fermenter",
            "hops_to_beer",
            itemId => itemId == "hops" ? 3 : 0,
            opportunity,
            itemId => itemId == "beer" ? 210 : registry.GetItem(itemId).SellPrice);

        Assert.Contains(lines, line => line.StartsWith("> 2. 3 Hops -> 1 Beer", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("MARKET BONUS", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("Have: 3", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("Output $210 during Beer Boom", StringComparison.Ordinal));
    }

    [Fact]
    public void MachineRecipeLines_ShowHaveNeedAndMissingInputForUnreadyRecipe()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetMachineRecipeLines(
            registry,
            "fermenter",
            "hops_to_beer",
            itemId => itemId == "hops" ? 1 : itemId == "grape" ? 6 : 0);

        Assert.Contains(lines, line => line.StartsWith("> 2. 3 Hops -> 1 Beer", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("Need: 3 Hops", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("Have: 1", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("Missing input", StringComparison.Ordinal));
    }

    [Fact]
    public void MachineStatusLines_ShowOutputReadyStateAndCollectHint()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        fermenter.Restore("hops_to_beer", "hops", 3, "beer", 1, 0, true);

        string[] lines = DebugUiTextFormatter.GetMachineStatusLines(registry, fermenter, _ => 0);

        Assert.Contains("Status: Output Ready", lines);
        Assert.Contains("Output: 1 Beer", lines);
        Assert.Contains("Press O to collect.", lines);
    }

    [Fact]
    public void MachineStatusLines_ShowWorkingRecipeOutputAndMarketBonus()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        fermenter.Start("hops_to_beer", "hops", 3, "beer", 1, 2);
        fermenter.AdvanceDay();
        MarketOpportunity opportunity = new()
        {
            Id = "beer_boom",
            DisplayName = "Beer Boom",
            ItemId = "beer",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 14,
            Message = "Beer demand spiked."
        };

        string[] lines = DebugUiTextFormatter.GetMachineStatusLines(registry, fermenter, _ => 0, activeMarketOpportunity: opportunity, getSellPrice: itemId => itemId == "beer" ? 210 : registry.GetItem(itemId).SellPrice);

        Assert.Contains("Status: Working", lines);
        Assert.Contains("Current: 3 Hops -> 1 Beer", lines);
        Assert.Contains("Days remaining: 1", lines);
        Assert.Contains("Output: Beer", lines);
        Assert.Contains("Market: Beer Boom active, output $210", lines);
    }

    [Fact]
    public void CompactMachineStatusLines_ShowOpportunityHintWhenRelevant()
    {
        ContentRegistry registry = BuildRegistry();
        ProcessorMachineInstance fermenter = new("fermenter");
        ProcessorMachineInstance agingBarrel = new("aging_barrel");
        MarketOpportunity opportunity = new()
        {
            Id = "beer_boom",
            DisplayName = "Beer Boom",
            ItemId = "beer",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 14,
            Message = "Beer demand spiked."
        };

        string[] lines = DebugUiTextFormatter.GetCompactMachineStatusLines(registry, [fermenter, agingBarrel], "fermenter", opportunity);

        Assert.Contains("Beer Boom: use Fermenter recipe Hops -> Beer", lines);
    }

    [Fact]
    public void HelpScreenLines_DocumentMachineRecipeSelectionControls()
    {
        string[] lines = DebugUiTextFormatter.GetHelpScreenLines();

        Assert.Contains("F3 Machines", lines);
        Assert.Contains("PGUP/PGDN select machine | UP/DOWN select recipe", lines);
        Assert.Contains("M start selected recipe | O collect output", lines);
    }

    [Fact]
    public void DebugUiState_RecipeSelectionWrapsPerMachine()
    {
        DebugUiState state = new();

        Assert.Equal(0, state.GetSelectedRecipeIndex("fermenter", 2));
        Assert.Equal(1, state.SelectNextRecipe("fermenter", 2));
        Assert.Equal(0, state.SelectNextRecipe("fermenter", 2));
        Assert.Equal(1, state.SelectPreviousRecipe("fermenter", 2));
    }

    [Fact]
    public void StatsScreenLines_IncludeProcessingStats()
    {
        ContentRegistry registry = BuildRegistry();
        GameStats stats = new();
        stats.RecordMachineRunStarted(135);
        stats.RecordMachineOutputCollected("wine", 1, 160, 25);
        stats.RecordMarketPurchase(160, 3);

        string[] lines = DebugUiTextFormatter.GetStatsScreenLines(registry, stats, 4, 3, includeDebugDetails: false);

        Assert.Contains("MARKET", lines);
        Assert.Contains("Purchases: 1", lines);
        Assert.Contains("Items bought: 3", lines);
        Assert.Contains("Market spend: $160", lines);
        Assert.Contains("PROCESSING", lines);
        Assert.Contains("Runs: 1", lines);
        Assert.Contains("Outputs collected: 1", lines);
        Assert.Contains("Processed goods: 1", lines);
        Assert.Contains("Raw 135 | Out 160 | Add +25", lines);
        Assert.Contains("ESC: return to farm", lines);
    }

    [Fact]
    public void StatsScreenLines_FarmingDefault_PreserveNaturalFarmingLabels()
    {
        ContentRegistry registry = BuildRegistry();
        GameStats stats = new();
        stats.RecordMoneySpentOnSeeds(20, 2);
        stats.RecordManualPlant(3);
        stats.RecordPlanterBotPlant(1);
        stats.RecordManualWater(4);
        stats.RecordWateringBotWater(2);
        stats.RecordManualHarvest(5);
        stats.RecordHarvesterBotHarvest(3);

        string[] lines = DebugUiTextFormatter.GetStatsScreenLines(
            registry,
            stats,
            4,
            3,
            includeDebugDetails: false,
            themeVocabulary: ThemeVocabulary.FarmingDefault);

        Assert.Contains("Seeds bought: 2", lines);
        Assert.Contains("Crops planted: 4", lines);
        Assert.Contains("Crops harvested: 8", lines);
        Assert.Contains("Plots watered manually: 4", lines);
        Assert.Contains("Planter Bot plants: 1", lines);
        Assert.Contains("Watering Bot waters: 2", lines);
        Assert.Contains("Harvester Bot harvests: 3", lines);
        Assert.Contains("Farm size: 4x3", lines);
    }

    [Fact]
    public void StatsScreenLines_MiningDemo_UseThemeAwareLabels()
    {
        ContentRegistry registry = BuildRegistry();
        GameStats stats = new();
        stats.RecordMoneySpentOnSeeds(20, 2);
        stats.RecordManualPlant(3);
        stats.RecordPlanterBotPlant(1);
        stats.RecordManualWater(4);
        stats.RecordWateringBotWater(2);
        stats.RecordManualHarvest(5);
        stats.RecordHarvesterBotHarvest(3);

        string[] lines = DebugUiTextFormatter.GetStatsScreenLines(
            registry,
            stats,
            4,
            3,
            includeDebugDetails: false,
            themeVocabulary: ThemeVocabulary.MiningDemo);

        Assert.Contains("Markers bought: 2", lines);
        Assert.Contains("Veins started: 4", lines);
        Assert.Contains("Veins extracted: 8", lines);
        Assert.Contains("Powered claims manually: 4", lines);
        Assert.Contains("Claim Bot starts: 1", lines);
        Assert.Contains("Power Bot powers: 2", lines);
        Assert.Contains("Extractor Bot extracts: 3", lines);
        Assert.Contains("Mine size: 4x3", lines);
    }

    [Fact]
    public void ComputerBotSummaryLines_UseThemeVocabularyBotNames()
    {
        GameStats stats = new();
        stats.RecordPlanterBotPlant(2);
        stats.RecordWateringBotWater(3);
        stats.RecordHarvesterBotHarvest(4);

        string[] lines = DebugUiTextFormatter.GetComputerBotSummaryLines(
            200,
            isPlanterBotUnlocked: true,
            isWateringBotUnlocked: true,
            isHarvesterBotUnlocked: true,
            stats,
            themeVocabulary: ThemeVocabulary.MiningDemo);

        Assert.Contains("Claim Bot starts: 2", lines);
        Assert.Contains("Power Bot powers: 3", lines);
        Assert.Contains("Extractor Bot extracts: 4", lines);
    }

    [Fact]
    public void StatsScreenFormatting_DoesNotMutateUnderlyingGameStats()
    {
        ContentRegistry registry = BuildRegistry();
        GameStats stats = new();
        stats.RecordMoneySpentOnSeeds(20, 2);
        stats.RecordManualPlant(3);
        stats.RecordPlanterBotPlant(1);
        stats.RecordManualWater(4);
        stats.RecordWateringBotWater(2);
        stats.RecordManualHarvest(5);
        stats.RecordHarvesterBotHarvest(3);

        DebugUiTextFormatter.GetStatsScreenLines(registry, stats, 4, 3, includeDebugDetails: false, themeVocabulary: ThemeVocabulary.FarmingDefault);
        DebugUiTextFormatter.GetStatsScreenLines(registry, stats, 4, 3, includeDebugDetails: false, themeVocabulary: ThemeVocabulary.MiningDemo);

        Assert.Equal(2, stats.SeedsBought);
        Assert.Equal(4, stats.TotalCropsPlanted);
        Assert.Equal(8, stats.TotalCropsHarvested);
        Assert.Equal(4, stats.PlotsWateredManually);
        Assert.Equal(2, stats.PlotsWateredByWateringBot);
        Assert.Equal(20, stats.MoneySpentOnSeeds);
    }

    [Fact]
    public void StatsScreenLines_ShowPausedStatus_WhenPaused()
    {
        ContentRegistry registry = BuildRegistry();

        string[] lines = DebugUiTextFormatter.GetStatsScreenLines(registry, new GameStats(), 4, 3, includeDebugDetails: false, isPaused: true);

        Assert.Contains("PAUSED - press Space to resume", lines);
    }

    [Fact]
    public void HelpScreenLines_IncludeNavigationAndMachineControls()
    {
        string[] lines = DebugUiTextFormatter.GetHelpScreenLines(new SaveLoadStatus("savegame.json"));

        Assert.Contains("NAVIGATION", lines);
        Assert.Contains("I Inventory | F2 Stats | F3 Machines | F8 Storage | F9 Computer | Shift+F9 Production", lines);
        Assert.Contains("F10 Market | Shift+F10 Progression | F11 Ledger | F4 Help", lines);
        Assert.Contains("ESC on sub-screens: return to Farm", lines);
        Assert.Contains("ESC on Farm: quit", lines);
        Assert.Contains("PAUSE", lines);
        Assert.Contains("Space pause or resume", lines);
        Assert.Contains("When paused, navigation works but gameplay actions are frozen.", lines);
        Assert.Contains("N next day | S sell | Y buy seed | Shift+Y buy max | E expand | J cleanup", lines);
        Assert.Contains("MARKET", lines);
        Assert.Contains("F10 open market | 1-9 buy listed offer", lines);
        Assert.Contains("PRODUCTION", lines);
        Assert.Contains("Shift+F9 open production report", lines);
        Assert.Contains("PROGRESSION", lines);
        Assert.Contains("Shift+F10 open progression report | R run report on that screen", lines);
        Assert.Contains("LEDGER", lines);
        Assert.Contains("F11 open activity log", lines);
        Assert.Contains("SAVE / LOAD", lines);
        Assert.Contains("F12 save | Shift+F12 request load", lines);
        Assert.Contains("Y confirm load | N/Esc cancel load", lines);
        Assert.Contains("Path: savegame.json", lines);
        Assert.Contains("Load returns to Farm, unpaused, Auto Play off.", lines);
        Assert.Contains("AUTOPLAY", lines);
        Assert.Contains("DEBUG / RESET / QUIT", lines);
        Assert.Contains("C deposit | U withdraw | S sell from storage screen", lines);
        Assert.Contains("Autoplay remains enabled while paused but does not step.", lines);
        Assert.Contains("PGUP/PGDN select machine | UP/DOWN select recipe", lines);
        Assert.Contains("M start selected recipe | O collect output", lines);
    }

    [Fact]
    public void HelpScreenLines_ShowPausedStatus_WhenPaused()
    {
        string[] lines = DebugUiTextFormatter.GetHelpScreenLines(new SaveLoadStatus("savegame.json"), isPaused: true);

        Assert.Contains("PAUSED - press Space to resume", lines);
    }

    [Fact]
    public void HelpScreenLines_WithRuntimeDiagnostics_IncludeThemePackSectionAndCounts()
    {
        (TurnipTestState state, ContentRegistry registry) = CreateManifestState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        string[] lines = DebugUiTextFormatter.GetHelpScreenLines(registry, state, new SaveLoadStatus("savegame.json"));

        Assert.Contains("THEME PACK", lines);
        Assert.Contains("Theme: Farming (farming)", lines);
        Assert.Contains($"Interface: {InterfacePack.FarmingDefault.Name} ({InterfacePack.FarmingDefault.Id})", lines);
        Assert.Contains(lines, line => line.StartsWith("Modules: 3 | ", StringComparison.Ordinal) && line.Contains("BaseGame", StringComparison.Ordinal) && line.Contains("PotatoPack", StringComparison.Ordinal) && line.Contains("FermentationPack", StringComparison.Ordinal));
        Assert.Contains("Starters: 4 | Tile Producers: 4 | Machines: 2", lines);
        Assert.Contains("Recipes: 3 | Market Offers: 3 | Restoration Sites: 1", lines);
    }

    [Fact]
    public void HelpScreenLines_WithRuntimeDiagnostics_ShowWarningsWhenDiagnosticsWarn()
    {
        ContentRegistry registry = new(
            [
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed", BuyPrice = 10, SellPrice = 1 },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop", BuyPrice = 0, SellPrice = 30 }
            ],
            [
                new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 }
            ],
            Array.Empty<MachineDefinition>(),
            Array.Empty<ProcessingRecipeDefinition>(),
            Array.Empty<MarketOfferDefinition>(),
            TestContentFactory.DefaultRestorationSites());
        TurnipTestState state = new(registry);

        string[] lines = DebugUiTextFormatter.GetHelpScreenLines(registry, state, new SaveLoadStatus("savegame.json"));

        Assert.Contains("Warnings:", lines);
        Assert.Contains("- No machines found.", lines);
        Assert.Contains("- Content validation is assumed OK for referenced ids.", lines);
    }

    [Fact]
    public void HelpScreenLines_WithMiningManifest_ShowMiningThemePackSummary()
    {
        (TurnipTestState state, ContentRegistry registry) = CreateManifestState(ThemePackCatalog.MiningDemoManifestId);

        string[] lines = DebugUiTextFormatter.GetHelpScreenLines(registry, state, new SaveLoadStatus("savegame.json"));

        Assert.Contains("Theme: Mining Demo (mining_demo)", lines);
        Assert.Contains($"Interface: {InterfacePack.MiningDemo.Name} ({InterfacePack.MiningDemo.Id})", lines);
        Assert.Contains(lines, line => line.StartsWith("Modules: 2 | ", StringComparison.Ordinal) && line.Contains("BaseGame", StringComparison.Ordinal) && line.Contains("MiningTestPack", StringComparison.Ordinal));
        Assert.Contains("Starters: 3 | Tile Producers: 3 | Machines: 2", lines);
        Assert.Contains("Recipes: 3 | Market Offers: 2 | Restoration Sites: 1", lines);
    }

    [Fact]
    public void HelpScreenLines_WithRuntimeDiagnostics_StillIncludeExistingControls()
    {
        (TurnipTestState state, ContentRegistry registry) = CreateManifestState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        string[] lines = DebugUiTextFormatter.GetHelpScreenLines(registry, state, new SaveLoadStatus("savegame.json"));

        Assert.Contains("I Inventory | F2 Stats | F3 Machines | F8 Storage | F9 Computer | Shift+F9 Production", lines);
        Assert.Contains("PGUP/PGDN select machine | UP/DOWN select recipe", lines);
        Assert.Contains("M start selected recipe | O collect output", lines);
        Assert.Contains("F12 save | Shift+F12 request load", lines);
        Assert.Contains("DEBUG / RESET / QUIT", lines);
    }

    [Fact]
    public void GameScreen_StillUsesExistingScreenSet()
    {
        Assert.Equal(
            ["Farm", "Inventory", "Storage", "Market", "Computer", "ProgressionReport", "ProductionReport", "ActivityLog", "Machines", "Stats", "Help"],
            Enum.GetNames<GameScreen>());
    }

    [Fact]
    public void HelpScreenLines_UseThemeVocabularyLabels()
    {
        string[] lines = DebugUiTextFormatter.GetHelpScreenLines(new SaveLoadStatus("savegame.json"), themeVocabulary: ThemeVocabulary.MiningDemo);

        Assert.Contains("ESC on sub-screens: return to Mine", lines);
        Assert.Contains("ESC on Mine: quit", lines);
        Assert.Contains("Arrows select | Tab marker | P plant | W power | H extract", lines);
        Assert.Contains("N next day | S sell | Y buy marker | Shift+Y buy max | E expand | J cleanup", lines);
        Assert.Contains("1 2 3 unlock | V claim | B power | X extractor | G cycle", lines);
        Assert.Contains("Load returns to Mine, unpaused, Auto Play off.", lines);
    }

    [Fact]
    public void NextHintLines_UseThemeVocabularyLabels()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);

        state.BuySelectedSeed();

        string[] lines = GetNextHintLines(state, registry, themeVocabulary: state.ThemeVocabulary);

        Assert.Contains("Plant markers with P.", lines);
    }

    [Fact]
    public void NextHintLines_UseMiningDemoPowerHintForDryVeins()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);

        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        string[] lines = GetNextHintLines(state, registry, themeVocabulary: state.ThemeVocabulary);

        Assert.Contains("Power veins with W.", lines);
    }

    [Fact]
    public void NextHintLines_UseMiningDemoExtractHintForReadyVein()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);

        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        for (int day = 0; day < 4; day++)
        {
            state.WaterPlot(0, 0);
            state.AdvanceDay();
        }

        string[] lines = GetNextHintLines(state, registry, themeVocabulary: state.ThemeVocabulary);

        Assert.Contains("Extract selected vein with H.", lines);
    }

    [Fact]
    public void ComputerScreenLines_UseMiningDemoVocabularyLabels()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);
        SetMoney(state, 400);
        state.UnlockPlanterBot();
        state.UnlockWateringBot();
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.AdvanceDay();

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.ActivityLog,
            new SaveLoadStatus("savegame.json"),
            state.Stats,
            state.DayNumber,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y),
            themeVocabulary: state.ThemeVocabulary);

        Assert.Contains("BOTS", lines);
        Assert.Contains("Claim: Unlocked", lines);
        Assert.Contains("Power: Unlocked", lines);
        Assert.Contains("Extractor: Locked, cost $120 (affordable)", lines);
        Assert.Contains("MINE", lines);
        Assert.Contains("Veins: 1", lines);
        Assert.Contains("Powered: 0", lines);
        Assert.Contains("Empty Claims: 8", lines);
    }

    [Fact]
    public void LoadConfirmationPrompt_UsesSaveFileNameOnly()
    {
        string prompt = DebugUiTextFormatter.GetLoadConfirmationPrompt("/tmp/saves/savegame.json");
        string[] lines = DebugUiTextFormatter.GetLoadConfirmationLines("/tmp/saves/savegame.json");

        Assert.Equal("Load savegame.json? Press Y to confirm or N/Esc to cancel.", prompt);
        Assert.Equal("Load savegame.json?", lines[0]);
        Assert.Equal("Y confirm | N cancel | Esc cancel", lines[1]);
    }

    [Fact]
    public void ActivityLogScreenLines_ShowRestorationEntry()
    {
        ActivityLogEntry[] entries =
        [
            new(4, ActivityLogCategory.Restoration, "Cleared Old Junk Pile for $500.")
        ];

        string[] lines = DebugUiTextFormatter.GetActivityLogScreenLines(entries);

        Assert.Contains("Day 4 | Restoration | Cleared Old Junk Pile for $500.", lines);
    }

    private static void AddInventoryItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }

    private static void ClearInventory(TurnipTestState state)
    {
        Dictionary<string, int> inventory = (Dictionary<string, int>)typeof(TurnipTestState)
            .GetField("_inventory", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(state)!;
        inventory.Clear();
    }

    private static void AddStorageItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddStorageItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }

    private static void SetMoney(TurnipTestState state, int amount)
    {
        typeof(TurnipTestState)
            .GetProperty("Money", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(state, amount);
    }

    private static void PlantAllPlots(TurnipTestState state)
    {
        string[] seedIds = ["turnip_seed", "grape_seed", "hops_seed"];
        int seedIndex = 0;

        foreach (FarmPlot plot in state.FarmGrid.GetAllPlots())
        {
            state.PlantSeedAtPlot(plot.X, plot.Y, seedIds[seedIndex % seedIds.Length]);
            seedIndex++;
        }
    }

    private static void WaterAllPlantedPlots(TurnipTestState state)
    {
        foreach (FarmPlot plot in state.FarmGrid.GetAllPlots().Where(plot => !plot.IsEmpty))
            state.WaterPlot(plot.X, plot.Y);
    }

    private static string[] GetNextHintLines(
        TurnipTestState state,
        ContentRegistry registry,
        bool isPaused = false,
        bool isLoadConfirmationPending = false,
        int maxHints = 3,
        ThemeVocabulary themeVocabulary = null)
    {
        return DebugUiTextFormatter.GetNextHintLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.CleanupSite,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y),
            state.ActiveMarketOpportunity,
            state.GetEffectiveSellPrice,
            isPaused,
            isLoadConfirmationPending,
            maxHints,
            themeVocabulary: themeVocabulary);
    }

    private static ContentRegistry BuildRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed", BuyPrice = 10, SellPrice = 1 },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop", BuyPrice = 0, SellPrice = 30 },
                new ItemDefinition { Id = "grape_seed", Name = "Grape Seeds", Type = "seed", BuyPrice = 15, SellPrice = 1 },
                new ItemDefinition { Id = "grape", Name = "Grapes", Type = "crop", BuyPrice = 0, SellPrice = 45 },
                new ItemDefinition { Id = "hops_seed", Name = "Hops Seeds", Type = "seed", BuyPrice = 14, SellPrice = 1 },
                new ItemDefinition { Id = "hops", Name = "Hops", Type = "crop", BuyPrice = 0, SellPrice = 40 },
                new ItemDefinition { Id = "wine", Name = "Wine", Type = "processed_good", BuyPrice = 0, SellPrice = 160 },
                new ItemDefinition { Id = "beer", Name = "Beer", Type = "processed_good", BuyPrice = 0, SellPrice = 140 },
                new ItemDefinition { Id = "aged_wine", Name = "Aged Wine", Type = "processed_good", BuyPrice = 0, SellPrice = 260 }
            ],
            [
                new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 },
                new CropDefinition { Id = "grape", SeedItemId = "grape_seed", HarvestItemId = "grape", GrowthDays = 6 },
                new CropDefinition { Id = "hops", SeedItemId = "hops_seed", HarvestItemId = "hops", GrowthDays = 5 }
            ],
            [
                new MachineDefinition { Id = "fermenter", Name = "Fermenter", Type = "processor_machine" },
                new MachineDefinition { Id = "aging_barrel", Name = "Aging Barrel", Type = "processor_machine" }
            ],
            [
                new ProcessingRecipeDefinition
                {
                    Id = "grape_to_wine",
                    MachineId = "fermenter",
                    InputItemId = "grape",
                    InputQuantity = 3,
                    OutputItemId = "wine",
                    OutputQuantity = 1,
                    ProcessingDays = 2
                },
                new ProcessingRecipeDefinition
                {
                    Id = "hops_to_beer",
                    MachineId = "fermenter",
                    InputItemId = "hops",
                    InputQuantity = 3,
                    OutputItemId = "beer",
                    OutputQuantity = 1,
                    ProcessingDays = 2
                },
                new ProcessingRecipeDefinition
                {
                    Id = "wine_to_aged_wine",
                    MachineId = "aging_barrel",
                    InputItemId = "wine",
                    InputQuantity = 1,
                    OutputItemId = "aged_wine",
                    OutputQuantity = 1,
                    ProcessingDays = 5
                }
            ],
            [
                new MarketOfferDefinition { Id = "buy_grapes", ItemId = "grape", Quantity = 3, Price = 160, Category = "raw_input" },
                new MarketOfferDefinition { Id = "buy_hops", ItemId = "hops", Quantity = 3, Price = 145, Category = "raw_input" },
                new MarketOfferDefinition { Id = "buy_wine", ItemId = "wine", Quantity = 1, Price = 190, Category = "intermediate_input" }
            ],
            TestContentFactory.DefaultRestorationSites());
    }

    private static (TurnipTestState State, ContentRegistry Registry) CreateManifestState(string manifestId)
    {
        ThemePackStateFactory factory = TestContentFactory.CreateThemePackStateFactory();
        GameStateFactoryResult result = factory.CreateState(manifestId);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
        return (result.State, result.ContentRegistry);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class MiningTestPackTests
{
    [Fact]
    public void MiningTestPack_IsNotLoadedByDefault()
    {
        ContentRegistry registry = LoadDefaultRegistry();

        Assert.False(registry.ItemsById.ContainsKey("copper_marker"));
        Assert.False(registry.TileProducersById.ContainsKey("copper_ore"));
        Assert.False(registry.MachinesById.ContainsKey("smelter"));
        Assert.False(registry.ProcessingRecipesById.ContainsKey("copper_ore_to_ingot"));
    }

    [Fact]
    public void MiningTestPack_LoadsAndValidates_WhenIncludedExplicitly()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();
        ContentValidationResult validation = new ContentValidator().Validate(registry);

        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        Assert.True(registry.ItemsById.ContainsKey("copper_marker"));
        Assert.True(registry.ItemsById.ContainsKey("copper_ore"));
        Assert.True(registry.ItemsById.ContainsKey("iron_marker"));
        Assert.True(registry.ItemsById.ContainsKey("iron_ore"));
        Assert.True(registry.TileProducersById.ContainsKey("copper_ore"));
        Assert.True(registry.TileProducersById.ContainsKey("iron_ore"));
        Assert.False(registry.CropsById.ContainsKey("copper_ore"));
        Assert.True(registry.ItemsById.ContainsKey("copper_ingot"));
        Assert.True(registry.ItemsById.ContainsKey("iron_ingot"));
        Assert.True(registry.ItemsById.ContainsKey("copper_parts"));
        Assert.True(registry.MachinesById.ContainsKey("smelter"));
        Assert.True(registry.MachinesById.ContainsKey("forge"));
        Assert.True(registry.ProcessingRecipesById.ContainsKey("copper_ore_to_ingot"));
        Assert.True(registry.ProcessingRecipesById.ContainsKey("copper_ingot_to_parts"));
        Assert.True(registry.MarketOffersById.ContainsKey("buy_copper_ore"));
        Assert.True(registry.MarketOffersById.ContainsKey("buy_iron_ore"));
    }

    [Fact]
    public void MiningTestPack_GenericProducerLookups_ResolveMarkersAndOutputs()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();

        Assert.True(registry.TryGetTileProducerByStarterItemId("copper_marker", out TileProducerDefinition copperProducer));
        Assert.True(registry.TryGetTileProducerByStarterItemId("iron_marker", out TileProducerDefinition ironProducer));
        Assert.True(registry.TryGetTileProducerByOutputItemId("copper_ore", out TileProducerDefinition copperOutputProducer));
        Assert.Equal("copper_ore", copperProducer.Id);
        Assert.Equal("copper_ore", copperProducer.OutputItemId);
        Assert.Equal("iron_ore", ironProducer.Id);
        Assert.Equal("iron_ore", ironProducer.OutputItemId);
        Assert.Equal("copper_marker", copperOutputProducer.StarterItemId);
    }

    [Fact]
    public void MiningTestPack_StarterDiscovery_UsesTileProducerMappingsInsteadOfItemType()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();

        Assert.Equal("marker", registry.GetItem("copper_marker").Type);
        Assert.Equal("marker", registry.GetItem("iron_marker").Type);
        Assert.True(registry.IsStarterItem("copper_marker"));
        Assert.True(registry.IsStarterItem("iron_marker"));
        Assert.Contains("copper_marker", registry.GetStarterItemIds());
        Assert.Contains("iron_marker", registry.GetStarterItemIds());
        Assert.DoesNotContain("copper_ore", registry.GetStarterItemIds());
    }

    [Fact]
    public void MiningOnlyRuntime_CanBuyMarkerStarterItems()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        SetMoney(state, 20);

        string result = state.BuySeedItem("copper_marker");

        Assert.StartsWith("Bought 1 Copper Claim Marker", result, StringComparison.Ordinal);
        Assert.Contains(state.SeedItemIds, itemId => string.Equals(itemId, "copper_marker", StringComparison.OrdinalIgnoreCase));
        Assert.True(state.GetItemCount("copper_marker") > 0);
    }

    [Fact]
    public void MiningTestPack_GenericProcessorInstances_CanRunSmelterAndForgeRecipes()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();
        ProcessingRecipeDefinition smelterRecipe = registry.GetProcessingRecipe("copper_ore_to_ingot");
        ProcessingRecipeDefinition forgeRecipe = registry.GetProcessingRecipe("copper_ingot_to_parts");
        ProcessorMachineInstance smelter = new("smelter");
        ProcessorMachineInstance forge = new("forge");

        smelter.Start(smelterRecipe.Id, smelterRecipe.InputItemId, smelterRecipe.InputQuantity, smelterRecipe.OutputItemId, smelterRecipe.OutputQuantity, smelterRecipe.ProcessingDays);
        smelter.AdvanceDay();
        smelter.AdvanceDay();

        forge.Start(forgeRecipe.Id, forgeRecipe.InputItemId, forgeRecipe.InputQuantity, forgeRecipe.OutputItemId, forgeRecipe.OutputQuantity, forgeRecipe.ProcessingDays);
        for (int day = 0; day < forgeRecipe.ProcessingDays; day++)
            forge.AdvanceDay();

        Assert.True(smelter.HasOutputReady);
        Assert.Equal("copper_ingot", smelter.OutputItemId);
        Assert.True(forge.HasOutputReady);
        Assert.Equal("copper_parts", forge.OutputItemId);
    }

    [Fact]
    public void MiningTestPack_ProcessingEconomics_UseJsonPrices()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();

        DebugUiTextFormatter.ProcessingRecipeEconomics copperIngotEconomics = DebugUiTextFormatter.CalculateProcessingRecipeEconomics(registry, registry.GetProcessingRecipe("copper_ore_to_ingot"));
        DebugUiTextFormatter.ProcessingRecipeEconomics copperPartsEconomics = DebugUiTextFormatter.CalculateProcessingRecipeEconomics(registry, registry.GetProcessingRecipe("copper_ingot_to_parts"));

        Assert.Equal(90, copperIngotEconomics.InputValue);
        Assert.Equal(120, copperIngotEconomics.OutputValue);
        Assert.Equal(30, copperIngotEconomics.ValueAdded);
        Assert.Equal(120, copperPartsEconomics.InputValue);
        Assert.Equal(220, copperPartsEconomics.OutputValue);
        Assert.Equal(100, copperPartsEconomics.ValueAdded);
    }

    [Fact]
    public void MiningTestPack_ProductionReport_IncludesCopperProcessingChain()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();

        string[] lines = DebugUiTextFormatter.GetProductionReportScreenLines(registry);

        Assert.Contains("Copper Ore -> Copper Ingot -> Copper Parts", lines);
        Assert.Contains("  Smelter: 3 Copper Ore -> 1 Copper Ingot | 2 days | $90 -> $120 | +$30", lines);
        Assert.Contains("  Forge: 1 Copper Ingot -> 1 Copper Parts | 4 days | $120 -> $220 | +$100", lines);
    }

    [Fact]
    public void MiningTestPack_MarketPreview_UsesMiningDisplayNamesAndEconomics()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();
        MarketOfferDefinition copperOffer = registry.GetMarketOffer("buy_copper_ore");

        string[] lines = DebugUiTextFormatter.GetMarketOfferDisplayLines(registry, copperOffer, 1, 150, includeDebugDetails: false);

        Assert.Contains("1. 3 Copper Ore - $100 - Affordable", lines);
        Assert.Contains("   Direct value: $90", lines);
        Assert.Contains("   Direct resale: -$10", lines);
        Assert.Contains("   Best use: 3 Copper Ore -> 1 Copper Ingot, 2 days", lines);
        Assert.Contains("   Profit if bought: +$20", lines);
    }

    [Fact]
    public void MiningDemoPresentation_WithMiningContent_UsesClaimPowerAndExtractWording()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        FarmPlot plot = state.FarmGrid.GetPlot(0, 0);

        state.PlantSeedAtPlot(0, 0, "copper_marker");

        string[] selectedPlotLines = DebugUiTextFormatter.GetSelectedPlotSummaryLines(plot, registry, false, themeVocabulary: state.ThemeVocabulary);
        string[] dryHintLines = GetNextHintLines(state, registry);

        for (int day = 0; day < 4; day++)
        {
            state.WaterPlot(0, 0);
            state.AdvanceDay();
        }

        string[] readyHintLines = GetNextHintLines(state, registry);

        Assert.Equal("SELECTED CLAIM 0,0", selectedPlotLines[0]);
        Assert.Equal("COPPER ORE", selectedPlotLines[1]);
        Assert.Equal("POWER: UNPOWERED", selectedPlotLines[3]);
        Assert.Equal("UNPWR", DebugUiTextFormatter.GetTileStatusLine(plot, isReady: false, state.InterfacePack));
        Assert.Contains("Power veins with W.", dryHintLines);
        Assert.Contains("Extract selected vein with H.", readyHintLines);
    }

    [Fact]
    public void MiningOnlyRuntime_CreatesSmelterAndForgeMachineInstancesInContentOrder()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);

        Assert.Equal(["smelter", "forge"], state.ProcessorMachines.Select(machine => machine.MachineId).ToArray());
        Assert.Equal("smelter", state.SelectedProcessorMachine.MachineId);
    }

    [Fact]
    public void MiningOnlyRuntime_SmelterProcessesCopperOre_AndUpdatesStatsAndActivity()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        AddInventoryItem(state, "copper_ore", 3);

        string startMessage = state.StartProcessorMachineFromInventory("smelter");

        Assert.Equal("Started smelter: 3 Copper Ore -> 1 Copper Ingot, ready in 2 days.", startMessage);

        state.AdvanceDay();
        state.AdvanceDay();

        string collectMessage = state.CollectProcessorMachineOutput("smelter");

        Assert.Equal("Collected 1 Copper Ingot from smelter.", collectMessage);
        Assert.Equal(1, state.GetItemCount("copper_ingot"));
        Assert.Equal(1, state.Stats.MachineRunsStarted);
        Assert.Equal(1, state.Stats.MachineOutputsCollected);
        Assert.Equal(90, state.Stats.MachineInputValueConsumed);
        Assert.Equal(120, state.Stats.MachineOutputValueCreated);
        Assert.Equal(30, state.Stats.MachineValueAdded);
        Assert.Equal(1, state.Stats.GetProcessedOutputCount("copper_ingot"));
        Assert.Equal("Started Smelter: 3 Copper Ore -> 1 Copper Ingot, ready in 2 days.", state.ActivityLog[0].Message);
        Assert.Equal("Collected 1 Copper Ingot from Smelter, value added +$30.", state.ActivityLog[^1].Message);
    }

    [Fact]
    public void MiningOnlyRuntime_SmelterProcessesIronOre()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        AddInventoryItem(state, "iron_ore", 3);

        Assert.Equal("Started smelter: 3 Iron Ore -> 1 Iron Ingot, ready in 2 days.", state.StartProcessorMachineFromInventory("smelter"));

        for (int day = 0; day < 2; day++)
            state.AdvanceDay();

        Assert.Equal("Collected 1 Iron Ingot from smelter.", state.CollectProcessorMachineOutput("smelter"));
        Assert.Equal(1, state.GetItemCount("iron_ingot"));
    }

    [Fact]
    public void MiningOnlyRuntime_ForgeProcessesCopperIngot()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        AddInventoryItem(state, "copper_ingot", 1);

        Assert.Equal("Started forge: 1 Copper Ingot -> 1 Copper Parts, ready in 4 days.", state.StartProcessorMachineFromInventory("forge"));

        for (int day = 0; day < 4; day++)
            state.AdvanceDay();

        Assert.Equal("Collected 1 Copper Parts from forge.", state.CollectProcessorMachineOutput("forge"));
        Assert.Equal(1, state.GetItemCount("copper_parts"));
        Assert.Equal(120, state.Stats.MachineInputValueConsumed);
        Assert.Equal(220, state.Stats.MachineOutputValueCreated);
        Assert.Equal(100, state.Stats.MachineValueAdded);
    }

    [Fact]
    public void MiningOnlyRuntime_MachineScreenShowsSmelterAndForge()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);

        string[] lines = DebugUiTextFormatter.GetMachineScreenLines(
            registry,
            state.ProcessorMachines,
            state.SelectedProcessorMachine.MachineId,
            registry.GetProcessingRecipesForMachine(state.SelectedProcessorMachine.MachineId).First().Id,
            state.Stats,
            state.GetAccessibleItemCount);

        Assert.Contains("Selected Machine: Smelter", lines);
        Assert.Contains(lines, line => line.Contains("Forge:", StringComparison.Ordinal));
        Assert.Contains("[ PGUP/PGDN ] Machine: Smelter", lines);
        Assert.Contains(lines, line => line.StartsWith("[ UP/DOWN ] Recipe:", StringComparison.Ordinal));
    }

    [Fact]
    public void MiningDemoFarmMachinePanel_UsesSmelterAndForgeNames()
    {
        TurnipTestState state = TestContentFactory.CreateStateForManifest(ThemePackCatalog.MiningDemoManifestId);
        ContentRegistry registry = TestContentFactory.CreateRegistryForManifest(ThemePackCatalog.MiningDemoManifestId);

        string[] lines = DebugUiTextFormatter.GetFarmMachinePanelLines(
            registry,
            state.ProcessorMachines,
            state.SelectedProcessorMachine.MachineId,
            machineId => registry.GetProcessingRecipesForMachine(machineId).FirstOrDefault()?.Id,
            state.GetAccessibleItemCount,
            state.ActiveMarketOpportunity,
            state.GetEffectiveSellPrice);

        Assert.Contains("[ M ] Smelter", lines);
        Assert.Contains("[ M ] Forge", lines);
        Assert.Contains(lines, line => line.Contains("Copper Ingot", StringComparison.Ordinal) || line.Contains("Copper Parts", StringComparison.Ordinal));
    }

    [Fact]
    public void MiningOnlyRuntime_ValidGenericMachineState_PassesInvariantValidation()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        AddInventoryItem(state, "copper_ore", 3);

        state.StartProcessorMachineFromInventory("smelter");

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void MiningOnlyAutoplay_CanHarvestProcessAndSellMiningGoods()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        SetMoney(state, 200);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings
            {
                TargetMoney = 300,
                MaxDays = 80,
                MaxSteps = 5000,
                MaxNoActionSteps = 12,
                DesiredSeedCount = 10
            });

        controller.Enable();

        while (controller.IsEnabled)
            controller.RunStep();

        Console.WriteLine(
            "Mining autoplay summary: "
            + $"seedOrder={string.Join(",", controller.GetAutoplaySeedPreferenceOrder())}; "
            + $"boughtSeeds={FormatCounts(controller.Stats.BoughtSeedsByItemId)}; "
            + $"planted={FormatCounts(controller.Stats.PlantedSeedsByItemId)}; "
            + $"harvested={FormatCounts(controller.Stats.HarvestedItemsByItemId)}; "
            + $"processedProduced={FormatCounts(state.Stats.ProcessedOutputsByItemId)}; "
            + $"sold={FormatCounts(controller.Stats.SoldItemsByItemId)}; "
            + $"machineRuns={state.Stats.MachineRunsStarted}; "
            + $"machineOutputs={state.Stats.MachineOutputsCollected}; "
            + $"marketPurchases={state.Stats.MarketPurchases}; "
            + $"money={state.Money}");

        Assert.NotEqual(AutoPlayStopReason.InvalidState, controller.Stats.StopReason);
        Assert.Contains("copper_marker", controller.GetSeedItemIdsForProcessorInputs());
        Assert.Contains("iron_marker", controller.GetSeedItemIdsForProcessorInputs());
        Assert.True(controller.Stats.GetHarvestedItemCount("copper_ore") > 0 || controller.Stats.GetHarvestedItemCount("iron_ore") > 0);
        Assert.True(state.Stats.MachineRunsStarted > 0);
        Assert.True(state.Stats.MachineOutputsCollected > 0);
        Assert.True(state.Stats.GetProcessedOutputCount("copper_ingot") > 0 || state.Stats.GetProcessedOutputCount("iron_ingot") > 0);
        Assert.True(state.Stats.GetProcessedOutputCount("copper_parts") > 0);
        Assert.True(controller.Stats.GetSoldItemCount("copper_ingot") > 0 || controller.Stats.GetSoldItemCount("iron_ingot") > 0 || controller.Stats.GetSoldItemCount("copper_parts") > 0);
        Assert.True(state.Stats.MarketPurchases > 0);
    }

    [Fact]
    public void MiningOnlySaveLoad_RoundTripsGenericMachineInstances()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        AddInventoryItem(state, "copper_ore", 3);
        AddInventoryItem(state, "copper_ingot", 1);
        state.StartProcessorMachineFromInventory("smelter");
        state.StartProcessorMachineFromInventory("forge");

        TurnipTestState loadedState = RoundTripState(state, registry);

        Assert.Equal(["smelter", "forge"], loadedState.ProcessorMachines.Select(machine => machine.MachineId).ToArray());
        Assert.Equal(GetProcessorMachineForTest(state, "smelter").CurrentRecipeId, GetProcessorMachineForTest(loadedState, "smelter").CurrentRecipeId);
        Assert.Equal(GetProcessorMachineForTest(state, "forge").CurrentRecipeId, GetProcessorMachineForTest(loadedState, "forge").CurrentRecipeId);
    }

    [Fact]
    public void MiningOnlySaveLoad_LoadedSmelterCanContinueProcessingAfterDayAdvance()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        AddInventoryItem(state, "copper_ore", 3);
        state.StartProcessorMachineFromInventory("smelter");

        TurnipTestState loadedState = RoundTripState(state, registry);
        ProcessorMachineInstance loadedSmelter = GetProcessorMachineForTest(loadedState, "smelter");

        while (!loadedSmelter.HasOutputReady && loadedSmelter.DaysRemaining > 0)
            loadedState.AdvanceDay();

        Assert.True(loadedSmelter.HasOutputReady);
        Assert.Equal(0, loadedSmelter.DaysRemaining);
    }

    [Fact]
    public void MiningOnlySaveLoad_LoadedForgeCanCollectOutput()
    {
        ContentRegistry registry = LoadMiningOnlyRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        AddInventoryItem(state, "copper_ingot", 1);
        state.StartProcessorMachineFromInventory("forge");
        for (int day = 0; day < 4; day++)
            state.AdvanceDay();

        TurnipTestState loadedState = RoundTripState(state, registry);

        Assert.Equal("Collected 1 Copper Parts from forge.", loadedState.CollectProcessorMachineOutput("forge"));
        Assert.Equal(1, loadedState.GetItemCount("copper_parts"));
    }

    private static TurnipTestState RoundTripState(TurnipTestState state, ContentRegistry registry)
    {
        SaveGameFileService service = new();
        string json = service.Serialize(state.CreateSaveGame());
        SaveGameState saveGame = service.Deserialize(json);
        TurnipTestState loadedState = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        loadedState.ApplySaveGame(saveGame);
        return loadedState;
    }

    private static string FormatCounts(IReadOnlyDictionary<string, int> counts)
    {
        return counts.Count == 0
            ? "none"
            : string.Join(",", counts.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).Select(entry => $"{entry.Key}:{entry.Value}"));
    }

    private static void AddInventoryItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }

    private static void SetMoney(TurnipTestState state, int amount)
    {
        typeof(TurnipTestState)
            .GetProperty("Money", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(state, amount);
    }

    private static ProcessorMachineInstance GetProcessorMachineForTest(TurnipTestState state, string machineId)
    {
        return state.ProcessorMachines.Single(machine => string.Equals(machine.MachineId, machineId, StringComparison.OrdinalIgnoreCase));
    }

    private static string[] GetNextHintLines(TurnipTestState state, ContentRegistry registry)
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
            themeVocabulary: state.ThemeVocabulary);
    }

    private static ContentRegistry LoadDefaultRegistry()
    {
        return new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));
    }

    private static ContentRegistry LoadRegistryIncludingTestPacks()
    {
        return new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"), includeTestPacks: true);
    }

    private static ContentRegistry LoadMiningOnlyRegistry()
    {
        string sourcePath = Path.Combine(AppContext.BaseDirectory, "ContentModules", "MiningTestPack");
        string tempRoot = Path.Combine(Path.GetTempPath(), $"TomatoGrowMiningPack_{Guid.NewGuid():N}");
        string targetPath = Path.Combine(tempRoot, "MiningTestPack");
        Directory.CreateDirectory(targetPath);

        foreach (string sourceFile in Directory.GetFiles(sourcePath, "*.json", SearchOption.TopDirectoryOnly))
            File.Copy(sourceFile, Path.Combine(targetPath, Path.GetFileName(sourceFile)), overwrite: true);

        try
        {
            return new ContentModuleLoader().LoadFromModules(tempRoot, includeTestPacks: true);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

}
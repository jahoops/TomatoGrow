using System;
using System.IO;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class GenericTileProducerContentSchemaTests
{
    [Fact]
    public void DefaultContent_CropsRemainAvailableAsTileProducers()
    {
        ContentRegistry registry = LoadDefaultRegistry();

        Assert.True(registry.CropsById.ContainsKey("turnip"));
        Assert.True(registry.CropsById.ContainsKey("potato"));
        Assert.True(registry.CropsById.ContainsKey("grape"));
        Assert.True(registry.CropsById.ContainsKey("hops"));
        Assert.True(registry.TileProducersById.ContainsKey("turnip"));
        Assert.True(registry.TileProducersById.ContainsKey("potato"));
        Assert.True(registry.TileProducersById.ContainsKey("grape"));
        Assert.True(registry.TileProducersById.ContainsKey("hops"));
        Assert.Equal("turnip_seed", registry.GetTileProducer("turnip").StarterItemId);
        Assert.Equal("potato_seed", registry.GetTileProducer("potato").StarterItemId);
        Assert.Equal("grape_seed", registry.GetTileProducer("grape").StarterItemId);
        Assert.Equal("hops_seed", registry.GetTileProducer("hops").StarterItemId);
    }

    [Fact]
    public void MiningTestPack_LoadsTileProducersWithoutCropsJsonCompatibilityRequirement()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();

        Assert.True(registry.TileProducersById.ContainsKey("copper_ore"));
        Assert.True(registry.TileProducersById.ContainsKey("iron_ore"));
        Assert.False(registry.CropsById.ContainsKey("copper_ore"));
        Assert.False(registry.CropsById.ContainsKey("iron_ore"));
    }

    [Fact]
    public void GenericLookups_ResolveMiningAndCropBackedProducers()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();

        Assert.True(registry.TryGetTileProducerByStarterItemId("copper_marker", out TileProducerDefinition copperProducer));
        Assert.True(registry.TryGetTileProducerByOutputItemId("copper_ore", out TileProducerDefinition copperOutputProducer));
        Assert.True(registry.TryGetTileProducerByStarterItemId("grape_seed", out TileProducerDefinition grapeProducer));
        Assert.True(registry.TryGetCropBySeedItemId("grape_seed", out CropDefinition grapeCrop));

        Assert.Equal("copper_ore", copperProducer.Id);
        Assert.Equal("copper_marker", copperOutputProducer.StarterItemId);
        Assert.Equal("grape", grapeProducer.Id);
        Assert.Equal("grape", grapeCrop.Id);
    }

    [Fact]
    public void Validate_DuplicateTileProducerIds_Fails()
    {
        ContentValidationResult result = ValidateTileProducerModule(
            tileProducersJson: """
            [
              { "id": "copper_ore", "starterItemId": "copper_marker", "outputItemId": "copper_ore", "productionDays": 4 },
              { "id": "copper_ore", "starterItemId": "iron_marker", "outputItemId": "iron_ore", "productionDays": 5 }
            ]
            """);

        Assert.Contains(result.Errors, error => error.Contains("Duplicate tile producer id 'copper_ore'.", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MissingStarterItemId_Fails()
    {
        ContentValidationResult result = ValidateTileProducerModule(
            tileProducersJson: """
            [
              { "id": "copper_ore", "starterItemId": "", "outputItemId": "copper_ore", "productionDays": 4 }
            ]
            """);

        Assert.Contains(result.Errors, error => error.Contains("blank StarterItemId", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MissingOutputItemId_Fails()
    {
        ContentValidationResult result = ValidateTileProducerModule(
            tileProducersJson: """
            [
              { "id": "copper_ore", "starterItemId": "copper_marker", "outputItemId": "", "productionDays": 4 }
            ]
            """);

        Assert.Contains(result.Errors, error => error.Contains("blank OutputItemId", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ProductionDaysMustBePositive()
    {
        ContentValidationResult result = ValidateTileProducerModule(
            tileProducersJson: """
            [
              { "id": "copper_ore", "starterItemId": "copper_marker", "outputItemId": "copper_ore", "productionDays": 0 }
            ]
            """);

        Assert.Contains(result.Errors, error => error.Contains("invalid ProductionDays 0", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_DuplicateStarterItemMapping_Fails()
    {
        ContentValidationResult result = ValidateTileProducerModule(
            tileProducersJson: """
            [
              { "id": "copper_ore", "starterItemId": "copper_marker", "outputItemId": "copper_ore", "productionDays": 4 },
              { "id": "copper_parts", "starterItemId": "copper_marker", "outputItemId": "copper_parts", "productionDays": 2 }
            ]
            """);

        Assert.Contains(result.Errors, error => error.Contains("Duplicate tile producer starter item 'copper_marker'.", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_CropAndTileProducerIdConflict_Fails()
    {
        ContentValidationResult result = ValidateTileProducerModule(
            cropsJson: """
            [
              { "id": "copper_ore", "seedItemId": "copper_marker", "harvestItemId": "copper_ore", "growthDays": 4 }
            ]
            """,
            tileProducersJson: """
            [
              { "id": "copper_ore", "starterItemId": "iron_marker", "outputItemId": "iron_ore", "productionDays": 5 }
            ]
            """);

        Assert.Contains(result.Errors, error => error.Contains("conflicts with crop id 'copper_ore'", StringComparison.Ordinal));
    }

    [Fact]
    public void StartMaintainCollect_WorkForMiningTileProducerSchema()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        SelectSeed(state, "copper_marker");

        Assert.Equal("Planted copper_ore at 0,0.", state.StartSelectedTileProducer());
        Assert.Equal("Watered plot 0,0.", state.MaintainSelectedTile());

        for (int day = 0; day < 3; day++)
        {
            state.AdvanceDay();
            state.MaintainSelectedTile();
        }

        state.AdvanceDay();

        Assert.Equal("Harvested Copper Ore from 0,0.", state.CollectSelectedTile());
        Assert.Equal(1, state.GetItemCount("copper_ore"));
    }

    [Fact]
    public void SaveLoad_RoundTrips_PlantedMiningTileProducer()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        SelectSeed(state, "copper_marker");
        state.StartSelectedTileProducer();
        state.MaintainSelectedTile();
        state.AdvanceDay();

        SaveGameState saveGame = state.CreateSaveGame();
        TurnipTestState loadedState = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        loadedState.ApplySaveGame(saveGame);

        Assert.Equal("copper_ore", loadedState.FarmGrid.GetPlot(0, 0).PlantedCrop?.CropDefinitionId);
        Assert.Equal(1, loadedState.FarmGrid.GetPlot(0, 0).PlantedCrop?.DaysGrown);
        Assert.True(loadedState.FarmGrid.GetPlot(0, 0).IsWatered == false);
    }

    [Fact]
    public void ValidateSaveGame_MissingMiningProducerId_Fails()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.Plots.Add(new FarmPlotSaveState { X = 2, Y = 1, CropDefinitionId = "ghost_ore", DaysGrown = 2, IsWatered = true });

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Missing crop id in farm plot (2,1): 'ghost_ore'.", validation.Errors);
    }

    [Fact]
    public void RuntimeValidator_ValidatesMiningProducerIdsAgainstTileProducers()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        state.FarmGrid.GetPlot(0, 0).Restore("ghost_ore", 2, false);

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("references missing crop id 'ghost_ore'", StringComparison.Ordinal));
    }

    [Fact]
    public void Autoplay_DiscoversMiningStarterItemsFromTileProducers()
    {
        ContentRegistry registry = LoadRegistryIncludingTestPacks();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo, interfacePack: InterfacePack.MiningDemo);
        AutoPlayTestController controller = new(state, registry);

        string[] processorStarterItems = controller.GetSeedItemIdsForProcessorInputs().ToArray();
        string[] seedPreferenceOrder = controller.GetAutoplaySeedPreferenceOrder().ToArray();

        Assert.Contains("copper_marker", processorStarterItems);
        Assert.Contains("iron_marker", processorStarterItems);
        Assert.Contains("copper_marker", seedPreferenceOrder);
        Assert.Contains("iron_marker", seedPreferenceOrder);
    }

    private static ContentRegistry LoadDefaultRegistry()
    {
        return new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));
    }

    private static ContentRegistry LoadRegistryIncludingTestPacks()
    {
        return new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"), includeTestPacks: true);
    }

    private static void SelectSeed(TurnipTestState state, string seedItemId)
    {
        for (int index = 0; index < state.SeedItemIds.Count; index++)
        {
            if (string.Equals(state.SelectedSeedItemId, seedItemId, StringComparison.OrdinalIgnoreCase))
                return;

            state.SelectNextSeed();
        }

        throw new InvalidOperationException($"Could not select starter item '{seedItemId}'.");
    }

    private static ContentValidationResult ValidateTileProducerModule(string tileProducersJson, string cropsJson = null)
    {
        string modulesPath = Path.Combine(Path.GetTempPath(), $"tomatogrow-tile-producer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(modulesPath);
        string modulePath = Path.Combine(modulesPath, "BaseGame");
        Directory.CreateDirectory(modulePath);

        try
        {
            File.WriteAllText(Path.Combine(modulePath, "items.json"), """
            [
                            { "id": "copper_marker", "name": "Copper Marker", "type": "marker", "buyPrice": 10, "sellPrice": 1 },
                            { "id": "iron_marker", "name": "Iron Marker", "type": "marker", "buyPrice": 12, "sellPrice": 1 },
              { "id": "copper_ore", "name": "Copper Ore", "type": "crop", "buyPrice": 0, "sellPrice": 30 },
              { "id": "iron_ore", "name": "Iron Ore", "type": "crop", "buyPrice": 0, "sellPrice": 40 },
              { "id": "copper_parts", "name": "Copper Parts", "type": "processed_good", "buyPrice": 0, "sellPrice": 220 }
            ]
            """);
            File.WriteAllText(Path.Combine(modulePath, "restoration_sites.json"), """
            [
              {
                "id": "old_junk_pile",
                "name": "Old Junk Pile",
                "description": "A pile of rusted scrap blocking usable farm space.",
                "cost": 500,
                "rewardType": "unlock_expansion_level",
                "rewardValue": 2,
                "rewardDescription": "Unlocks 5x3 farm expansion"
              }
            ]
            """);

            if (cropsJson is not null)
                File.WriteAllText(Path.Combine(modulePath, "crops.json"), cropsJson);

            File.WriteAllText(Path.Combine(modulePath, "tile_producers.json"), tileProducersJson);

            ContentRegistry registry = new ContentModuleLoader().LoadFromModules(modulesPath);
            return new ContentValidator().Validate(registry);
        }
        finally
        {
            Directory.Delete(modulesPath, recursive: true);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class ThemePackStateFactoryTests
{
    [Fact]
    public void Factory_CreatesTomatoGrowDefaultState()
    {
        ThemePackStateFactory factory = TestContentFactory.CreateThemePackStateFactory();

        GameStateFactoryResult result = factory.CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        Assert.True(result.Success);
        Assert.Equal(ThemeVocabulary.FarmingDefault, result.State.ThemeVocabulary);
        Assert.Equal(InterfacePack.FarmingDefault, result.State.InterfacePack);
        Assert.Contains("turnip_seed", result.State.SeedItemIds);
        Assert.Contains("grape_seed", result.State.SeedItemIds);
        Assert.Contains("hops_seed", result.State.SeedItemIds);
        Assert.Contains(result.State.ProcessorMachines, machine => string.Equals(machine.MachineId, "fermenter", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.State.ProcessorMachines, machine => string.Equals(machine.MachineId, "aging_barrel", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Factory_CreatesMiningDemoState()
    {
        ThemePackStateFactory factory = TestContentFactory.CreateThemePackStateFactory();

        GameStateFactoryResult result = factory.CreateState(ThemePackCatalog.MiningDemoManifestId);

        Assert.True(result.Success);
        Assert.Equal(ThemeVocabulary.MiningDemo, result.State.ThemeVocabulary);
        Assert.Equal(InterfacePack.MiningDemo, result.State.InterfacePack);
        Assert.Contains("copper_marker", result.State.SeedItemIds);
        Assert.Contains("iron_marker", result.State.SeedItemIds);
        Assert.Contains(result.State.ProcessorMachines, machine => string.Equals(machine.MachineId, "smelter", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.State.ProcessorMachines, machine => string.Equals(machine.MachineId, "forge", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FactoryCreatedMiningDemoState_CanStartMaintainCollectAndSmelt()
    {
        ThemePackStateFactory factory = TestContentFactory.CreateThemePackStateFactory();
        GameStateFactoryResult result = factory.CreateState(ThemePackCatalog.MiningDemoManifestId);

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));

        TurnipTestState state = result.State;
        SelectSeed(state, "copper_marker");
        state.BuySeedItem("copper_marker", buyAsManyAsPossible: true);

        string startMessage = state.StartSelectedTileProducer();
        string maintainMessage = state.MaintainSelectedTile();

        for (int day = 0; day < result.ContentRegistry.GetTileProducer("copper_ore").ProductionDays; day++)
        {
            state.AdvanceDay();
            if (day < result.ContentRegistry.GetTileProducer("copper_ore").ProductionDays - 1)
                state.MaintainSelectedTile();
        }

        string collectMessage = state.CollectSelectedTile();
        AddInventoryItem(state, "copper_ore", 2);
        string smeltMessage = state.StartProcessorMachineFromInventory("smelter");
        state.AdvanceDay();
        state.AdvanceDay();
        string collectMachineMessage = state.CollectProcessorMachineOutput("smelter");

        Assert.Equal("Planted copper_ore at 0,0.", startMessage);
        Assert.False(string.IsNullOrWhiteSpace(maintainMessage));
        Assert.False(string.IsNullOrWhiteSpace(collectMessage));
        Assert.StartsWith("Started", smeltMessage, StringComparison.Ordinal);
        Assert.StartsWith("Collected", collectMachineMessage, StringComparison.Ordinal);
        Assert.True(state.GetItemCount("copper_ingot") > 0);
    }

    [Fact]
    public void Factory_UnknownManifestId_ReturnsClearFailure()
    {
        ThemePackStateFactory factory = TestContentFactory.CreateThemePackStateFactory();

        GameStateFactoryResult result = factory.CreateState("ghost_manifest");

        Assert.False(result.Success);
        Assert.Equal(["Unknown theme pack manifest 'ghost_manifest'."], result.Errors);
    }

    [Fact]
    public void Factory_MissingContentModule_ReturnsClearFailure()
    {
        ThemePackCatalog catalog = new(
            manifests:
            [
                new ThemePackManifest
                {
                    Id = "missing_module_manifest",
                    Name = "Missing Module Manifest",
                    Description = "Broken manifest for testing.",
                    ContentModuleIds = ["GhostModule"],
                    ThemeVocabularyId = ThemePackCatalog.FarmingDefaultVocabularyId,
                    InterfacePackId = ThemePackCatalog.FarmingDefaultInterfacePackId
                }
            ]);
        ThemePackStateFactory factory = TestContentFactory.CreateThemePackStateFactory(catalog);

        GameStateFactoryResult result = factory.CreateState("missing_module_manifest");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("references missing content module 'GhostModule'", StringComparison.Ordinal));
    }

    [Fact]
    public void Factory_UnknownVocabularyId_ReturnsClearFailure()
    {
        ThemePackCatalog catalog = new(
            manifests:
            [
                new ThemePackManifest
                {
                    Id = "broken_manifest",
                    Name = "Broken Manifest",
                    Description = "Broken manifest for testing.",
                    ContentModuleIds = ["BaseGame"],
                    ThemeVocabularyId = "ghost_vocabulary",
                    InterfacePackId = ThemePackCatalog.FarmingDefaultInterfacePackId
                }
            ]);
        ThemePackStateFactory factory = TestContentFactory.CreateThemePackStateFactory(catalog);

        GameStateFactoryResult result = factory.CreateState("broken_manifest");

        Assert.False(result.Success);
        Assert.Contains("Manifest 'broken_manifest' references unknown theme vocabulary 'ghost_vocabulary'.", result.Errors);
    }

    [Fact]
    public void Factory_UnknownInterfacePackId_ReturnsClearFailure()
    {
        ThemePackCatalog catalog = new(
            manifests:
            [
                new ThemePackManifest
                {
                    Id = "broken_manifest",
                    Name = "Broken Manifest",
                    Description = "Broken manifest for testing.",
                    ContentModuleIds = ["BaseGame"],
                    ThemeVocabularyId = ThemePackCatalog.FarmingDefaultVocabularyId,
                    InterfacePackId = "ghost_interface"
                }
            ]);
        ThemePackStateFactory factory = TestContentFactory.CreateThemePackStateFactory(catalog);

        GameStateFactoryResult result = factory.CreateState("broken_manifest");

        Assert.False(result.Success);
        Assert.Contains("Manifest 'broken_manifest' references unknown interface pack 'ghost_interface'.", result.Errors);
    }

    [Fact]
    public void Factory_ContentValidationFailure_ReturnsClearFailure()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"TomatoGrowFactoryValidation_{Guid.NewGuid():N}");
        string brokenModulePath = Path.Combine(tempRoot, "BrokenGame");
        Directory.CreateDirectory(brokenModulePath);

        try
        {
            File.WriteAllText(Path.Combine(brokenModulePath, "items.json"), """
            [
              { "id": "broken_seed", "name": "Broken Seed", "type": "seed", "buyPrice": 10, "sellPrice": 1 }
            ]
            """);

            ThemePackCatalog catalog = new(
                manifests:
                [
                    new ThemePackManifest
                    {
                        Id = "broken_content_manifest",
                        Name = "Broken Content Manifest",
                        Description = "Broken manifest for validation testing.",
                        ContentModuleIds = ["BrokenGame"],
                        ThemeVocabularyId = ThemePackCatalog.FarmingDefaultVocabularyId,
                        InterfacePackId = ThemePackCatalog.FarmingDefaultInterfacePackId
                    }
                ]);
            ThemePackStateFactory factory = new(tempRoot, catalog);

            GameStateFactoryResult result = factory.CreateState("broken_content_manifest");

            Assert.False(result.Success);
            Assert.Contains(result.Errors, error => error.Contains("Manifest 'broken_content_manifest' content validation failed:", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
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

    private static void AddInventoryItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }
}
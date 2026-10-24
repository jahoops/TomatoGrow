using System;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class ThemePackRuntimeDiagnosticsTests
{
    [Fact]
    public void TomatoGrowDefault_Diagnostics_IncludeFarmingVocabularyAndInterface()
    {
        (TurnipTestState state, ContentRegistry registry) = CreateManifestState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        string[] lines = ThemePackRuntimeDiagnostics.Format(state, registry);

        Assert.Contains("Theme vocabulary: farming (Farming)", lines);
        Assert.Contains($"Interface pack: {InterfacePack.FarmingDefault.Id} ({InterfacePack.FarmingDefault.Name})", lines);
    }

    [Fact]
    public void TomatoGrowDefault_Diagnostics_ListExpectedStartersMachinesAndRecipes()
    {
        (TurnipTestState state, ContentRegistry registry) = CreateManifestState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        string[] lines = ThemePackRuntimeDiagnostics.Format(state, registry);

        Assert.Contains("turnip_seed: Turnip Seeds", lines);
        Assert.Contains("grape_seed: Grape Seeds", lines);
        Assert.Contains("hops_seed: Hops Seeds", lines);
        Assert.Contains("fermenter: Fermenter", lines);
        Assert.Contains("aging_barrel: Aging Barrel", lines);
        Assert.Contains(lines, line => line.StartsWith("grape_to_wine: Fermenter, 3 Grapes -> 1 Wine, 2 days", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("wine_to_aged_wine: Aging Barrel, 1 Wine -> 1 Aged Wine, 5 days", StringComparison.Ordinal));
    }

    [Fact]
    public void MiningDemo_Diagnostics_IncludeMiningVocabularyAndInterface()
    {
        (TurnipTestState state, ContentRegistry registry) = CreateManifestState(ThemePackCatalog.MiningDemoManifestId);

        string[] lines = ThemePackRuntimeDiagnostics.Format(state, registry);

        Assert.Contains("Theme vocabulary: mining_demo (Mining Demo)", lines);
        Assert.Contains($"Interface pack: {InterfacePack.MiningDemo.Id} ({InterfacePack.MiningDemo.Name})", lines);
    }

    [Fact]
    public void MiningDemo_Diagnostics_ListExpectedStartersProducersMachinesAndRecipes()
    {
        (TurnipTestState state, ContentRegistry registry) = CreateManifestState(ThemePackCatalog.MiningDemoManifestId);

        string[] lines = ThemePackRuntimeDiagnostics.Format(state, registry);

        Assert.Contains("copper_marker: Copper Claim Marker", lines);
        Assert.Contains("iron_marker: Iron Claim Marker", lines);
        Assert.Contains(lines, line => line.StartsWith("copper_ore: Copper Claim Marker -> Copper Ore, 4 days", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("iron_ore: Iron Claim Marker -> Iron Ore, 5 days", StringComparison.Ordinal));
        Assert.Contains("smelter: Smelter", lines);
        Assert.Contains("forge: Forge", lines);
        Assert.Contains(lines, line => line.StartsWith("copper_ore_to_ingot: Smelter, 3 Copper Ore -> 1 Copper Ingot, 2 days", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("copper_ingot_to_parts: Forge, 1 Copper Ingot -> 1 Copper Parts, 4 days", StringComparison.Ordinal));
    }

    [Fact]
    public void Diagnostics_ReportOrdering_IsDeterministic()
    {
        (TurnipTestState state, ContentRegistry registry) = CreateManifestState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        string[] first = ThemePackRuntimeDiagnostics.Format(state, registry);
        string[] second = ThemePackRuntimeDiagnostics.Format(state, registry);

        Assert.Equal(first, second);
        Assert.True(Array.IndexOf(first, "grape_seed: Grape Seeds") < Array.IndexOf(first, "hops_seed: Hops Seeds"));
        Assert.True(Array.IndexOf(first, "hops_seed: Hops Seeds") < Array.IndexOf(first, "turnip_seed: Turnip Seeds"));
    }

    [Fact]
    public void Diagnostics_Warn_WhenNoMachinesExist()
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

        string[] lines = ThemePackRuntimeDiagnostics.Format(state, registry);

        Assert.Contains("WARNINGS", lines);
        Assert.Contains("No machines found.", lines);
        Assert.Contains("Content validation is assumed OK for referenced ids.", lines);
    }

    private static (TurnipTestState State, ContentRegistry Registry) CreateManifestState(string manifestId)
    {
        ThemePackStateFactory factory = TestContentFactory.CreateThemePackStateFactory();
        GameStateFactoryResult result = factory.CreateState(manifestId);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
        return (result.State, result.ContentRegistry);
    }
}
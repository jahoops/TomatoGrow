using System.Reflection;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class BotRoleFormatterTests
{
    [Fact]
    public void FarmingDefault_BotRoleLabels_UseExistingBotNames()
    {
        Assert.Equal("Planter Bot", BotRoleFormatter.GetBotDisplayName(BotRole.Starter, ThemeVocabulary.FarmingDefault));
        Assert.Equal("Watering Bot", BotRoleFormatter.GetBotDisplayName(BotRole.Maintainer, ThemeVocabulary.FarmingDefault));
        Assert.Equal("Harvester Bot", BotRoleFormatter.GetBotDisplayName(BotRole.Collector, ThemeVocabulary.FarmingDefault));
    }

    [Fact]
    public void MiningDemo_BotRoleLabels_UseThemeAwareBotNames()
    {
        Assert.Equal("Claim Bot", BotRoleFormatter.GetBotDisplayName(BotRole.Starter, ThemeVocabulary.MiningDemo));
        Assert.Equal("Power Bot", BotRoleFormatter.GetBotDisplayName(BotRole.Maintainer, ThemeVocabulary.MiningDemo));
        Assert.Equal("Extractor Bot", BotRoleFormatter.GetBotDisplayName(BotRole.Collector, ThemeVocabulary.MiningDemo));
    }

    [Fact]
    public void BotRoleFormatter_UsesExpectedActionVerbs()
    {
        Assert.Equal("plants", BotRoleFormatter.GetBotActionVerb(BotRole.Starter, ThemeVocabulary.FarmingDefault));
        Assert.Equal("waters", BotRoleFormatter.GetBotActionVerb(BotRole.Maintainer, ThemeVocabulary.FarmingDefault));
        Assert.Equal("harvests", BotRoleFormatter.GetBotActionVerb(BotRole.Collector, ThemeVocabulary.FarmingDefault));
        Assert.Equal("starts", BotRoleFormatter.GetBotActionVerb(BotRole.Starter, ThemeVocabulary.MiningDemo));
        Assert.Equal("powers", BotRoleFormatter.GetBotActionVerb(BotRole.Maintainer, ThemeVocabulary.MiningDemo));
        Assert.Equal("extracts", BotRoleFormatter.GetBotActionVerb(BotRole.Collector, ThemeVocabulary.MiningDemo));
    }

    [Fact]
    public void BotRoleFormatter_ReportsExistingUnlockKeysAndCosts()
    {
        Assert.Equal("1", BotRoleFormatter.GetBotUnlockKey(BotRole.Starter));
        Assert.Equal("2", BotRoleFormatter.GetBotUnlockKey(BotRole.Maintainer));
        Assert.Equal("3", BotRoleFormatter.GetBotUnlockKey(BotRole.Collector));
        Assert.Equal(60, BotRoleFormatter.GetBotCost(BotRole.Starter, GameBalanceConfig.Default));
        Assert.Equal(90, BotRoleFormatter.GetBotCost(BotRole.Maintainer, GameBalanceConfig.Default));
        Assert.Equal(120, BotRoleFormatter.GetBotCost(BotRole.Collector, GameBalanceConfig.Default));
    }

    [Fact]
    public void BotRoleAliases_MapToExistingUnlockStateAndBehavior()
    {
        TurnipTestState state = new(TestContentFactory.CreateRegistryForManifest(ThemePackCatalog.TomatoGrowDefaultManifestId));
        SetMoney(state, 500);

        Assert.Equal("Unlocked Planter Bot.", state.UnlockBot(BotRole.Starter));
        Assert.Equal("Unlocked Watering Bot.", state.UnlockBot(BotRole.Maintainer));
        Assert.Equal("Unlocked Harvester Bot.", state.UnlockBot(BotRole.Collector));
        Assert.True(state.IsBotUnlocked(BotRole.Starter));
        Assert.True(state.IsBotUnlocked(BotRole.Maintainer));
        Assert.True(state.IsBotUnlocked(BotRole.Collector));

        string starterResult = state.RunBot(BotRole.Starter);
        state.PlantSelectedSeed();
        state.AdvanceDay();
        string maintainerResult = state.RunBot(BotRole.Maintainer);
        for (int day = 0; day < 4; day++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        string collectorResult = state.RunBot(BotRole.Collector);

        Assert.StartsWith("Planter Bot", starterResult, System.StringComparison.Ordinal);
        Assert.StartsWith("Watering Bot", maintainerResult, System.StringComparison.Ordinal);
        Assert.StartsWith("Harvester Bot", collectorResult, System.StringComparison.Ordinal);
    }

    [Fact]
    public void BotRoleFormatter_HelpScreenAndStatusPanelUseRoleLabels()
    {
        string[] helpLines = DebugUiTextFormatter.GetHelpScreenLines(themeVocabulary: ThemeVocabulary.MiningDemo);

        Assert.Contains("1 2 3 unlock | V claim | B power | X extractor | G cycle", helpLines);
        Assert.Equal("Claim Bot: LOCKED COST 60 NEED 60 MORE", DebugUiTextFormatter.GetBotStatusText(BotRoleFormatter.GetBotDisplayName(BotRole.Starter, ThemeVocabulary.MiningDemo), false, BotRoleFormatter.GetBotCost(BotRole.Starter), 0));
        Assert.Equal("Power Bot: LOCKED COST 90 NEED 90 MORE", DebugUiTextFormatter.GetBotStatusText(BotRoleFormatter.GetBotDisplayName(BotRole.Maintainer, ThemeVocabulary.MiningDemo), false, BotRoleFormatter.GetBotCost(BotRole.Maintainer), 0));
        Assert.Equal("Extractor Bot: LOCKED COST 120 NEED 120 MORE", DebugUiTextFormatter.GetBotStatusText(BotRoleFormatter.GetBotDisplayName(BotRole.Collector, ThemeVocabulary.MiningDemo), false, BotRoleFormatter.GetBotCost(BotRole.Collector), 0));
    }

    private static void SetMoney(TurnipTestState state, int amount)
    {
        typeof(TurnipTestState)
            .GetProperty(nameof(TurnipTestState.Money), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(state, amount);
    }
}
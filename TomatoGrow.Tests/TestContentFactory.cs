using System;
using System.Collections.Generic;
using System.IO;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow.Tests;

internal static class TestContentFactory
{
    public static ThemePackCatalog CreateThemePackCatalog()
    {
        return new ThemePackCatalog();
    }

    public static ThemePackStateFactory CreateThemePackStateFactory(ThemePackCatalog catalog = null)
    {
        return new ThemePackStateFactory(
            Path.Combine(AppContext.BaseDirectory, "ContentModules"),
            catalog ?? CreateThemePackCatalog());
    }

    public static ContentRegistry CreateRegistryForManifest(string manifestId, ThemePackCatalog catalog = null)
    {
        GameStateFactoryResult result = CreateThemePackStateFactory(catalog).CreateState(manifestId);
        if (!result.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));

        return result.ContentRegistry;
    }

    public static TurnipTestState CreateStateForManifest(string manifestId, ThemePackCatalog catalog = null, GameBalanceConfig balanceConfig = null)
    {
        GameStateFactoryResult result = CreateThemePackStateFactory(catalog).CreateState(manifestId, balanceConfig);
        if (!result.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));

        return result.State;
    }

    public static RestorationSiteDefinition[] DefaultRestorationSites()
    {
        return
        [
            new RestorationSiteDefinition
            {
                Id = TurnipTestState.OldJunkPileId,
                Name = TurnipTestState.OldJunkPileName,
                Description = TurnipTestState.OldJunkPileDescription,
                Cost = TurnipTestState.OldJunkPileCleanupCost,
                RewardType = "unlock_expansion_level",
                RewardValue = TurnipTestState.MaxFarmExpansionLevel,
                RewardDescription = TurnipTestState.OldJunkPileReward
            }
        ];
    }
}
using System;
using System.Reflection;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class MarketOpportunityGeneratorTests
{
    [Fact]
    public void NewGame_StartsWithoutGeneratedOpportunityActive()
    {
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        Assert.Null(state.ActiveMarketOpportunity);
        Assert.Equal(0, state.LastGeneratedMarketOpportunityDay);
        Assert.Equal(0, state.GeneratedMarketOpportunityCount);
    }

    [Fact]
    public void AdvancingDaysBeforeInterval_DoesNotGenerateOpportunity()
    {
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        AdvanceToDay(state, 29);

        Assert.Null(state.ActiveMarketOpportunity);
        Assert.Equal(0, state.GeneratedMarketOpportunityCount);
    }

    [Fact]
    public void AdvancingToInterval_GeneratesPositiveOpportunity()
    {
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        AdvanceToDay(state, 30);

        Assert.NotNull(state.ActiveMarketOpportunity);
        Assert.Equal("wine", state.ActiveMarketOpportunity!.ItemId);
        Assert.True(state.ActiveMarketOpportunity.SellPriceMultiplier > 0);
        Assert.True(state.ActiveMarketOpportunity.DaysRemaining > 0);
        Assert.Contains(state.ActiveMarketOpportunity.ItemId, state.GetMarketOpportunityCandidateItems());
        Assert.Equal(30, state.LastGeneratedMarketOpportunityDay);
        Assert.Equal(1, state.GeneratedMarketOpportunityCount);
    }

    [Fact]
    public void GeneratedOpportunity_RecordsMarketStartEntry()
    {
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        AdvanceToDay(state, 30);

        ActivityLogEntry entry = state.ActivityLog[^1];
        Assert.Equal(ActivityLogCategory.Market, entry.Category);
        Assert.Equal(30, entry.Day);
        Assert.Equal("Wine Boom started: Wine sells for 150% for 20 days.", entry.Message);
    }

    [Fact]
    public void GeneratedOpportunity_ExpiresAfterDuration_AndRecordsEndEntry()
    {
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        AdvanceToDay(state, 50);

        Assert.Null(state.ActiveMarketOpportunity);
        ActivityLogEntry entry = state.ActivityLog[^1];
        Assert.Equal(ActivityLogCategory.Market, entry.Category);
        Assert.Equal(50, entry.Day);
        Assert.Equal("Wine Boom ended.", entry.Message);
    }

    [Fact]
    public void NoSecondOpportunityStartsWhileOneIsActive()
    {
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        AdvanceToDay(state, 49);

        Assert.NotNull(state.ActiveMarketOpportunity);
        Assert.Equal("wine", state.ActiveMarketOpportunity!.ItemId);
        Assert.Equal(1, state.GeneratedMarketOpportunityCount);
    }

    [Fact]
    public void AfterFirstOpportunityExpires_AndIntervalPasses_AnotherOpportunityGenerates()
    {
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);

        AdvanceToDay(state, 60);

        Assert.NotNull(state.ActiveMarketOpportunity);
        Assert.Equal("beer", state.ActiveMarketOpportunity!.ItemId);
        Assert.Equal(60, state.LastGeneratedMarketOpportunityDay);
        Assert.Equal(2, state.GeneratedMarketOpportunityCount);
    }

    [Fact]
    public void GeneratorOrder_IsDeterministic_ForDefaultAndMiningManifests()
    {
        TurnipTestState defaultState = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);
        TurnipTestState miningState = CreateState(ThemePackCatalog.MiningDemoManifestId);

        Assert.Equal(["wine", "beer", "aged_wine"], defaultState.GetMarketOpportunityCandidateItems());
        Assert.Equal(["copper_ingot", "iron_ingot", "copper_parts"], miningState.GetMarketOpportunityCandidateItems());
    }

    [Fact]
    public void GeneratedOpportunity_AffectsSellPriceThroughExistingPath()
    {
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);
        AdvanceToDay(state, 30);
        AddInventoryItem(state, "wine", 1);

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 240.", message);
        Assert.Equal(240, state.Money);
    }

    [Fact]
    public void Autoplay_RespondsNaturallyToGeneratedOpportunity()
    {
        ContentRegistry registry = CreateRegistry(ThemePackCatalog.TomatoGrowDefaultManifestId);
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);
        AdvanceToDay(state, 30);
        AddInventoryItem(state, "wine", 1);
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.StartsWith("Auto: sold processed goods.", message, StringComparison.Ordinal);
        Assert.Equal("wine", controller.GetActiveOpportunityItemId());
        Assert.Equal(240, state.Money);
    }

    [Fact]
    public void SaveLoad_RoundTripsGeneratorState_AndDoesNotDuplicateImmediately()
    {
        ContentRegistry registry = CreateRegistry(ThemePackCatalog.TomatoGrowDefaultManifestId);
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);
        AdvanceToDay(state, 50);

        TurnipTestState loadedState = RoundTripState(state, registry);

        Assert.Null(loadedState.ActiveMarketOpportunity);
        Assert.Equal(30, loadedState.LastGeneratedMarketOpportunityDay);
        Assert.Equal(1, loadedState.GeneratedMarketOpportunityCount);

        AdvanceToDay(loadedState, 59);
        Assert.Null(loadedState.ActiveMarketOpportunity);
        Assert.Equal(1, loadedState.GeneratedMarketOpportunityCount);

        loadedState.AdvanceDay();
        Assert.NotNull(loadedState.ActiveMarketOpportunity);
        Assert.Equal("beer", loadedState.ActiveMarketOpportunity!.ItemId);
        Assert.Equal(2, loadedState.GeneratedMarketOpportunityCount);
    }

    [Fact]
    public void SaveValidation_FailsWhenGeneratorStateIsNegative()
    {
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.LastGeneratedMarketOpportunityDay = -1;
        saveGame.GeneratedMarketOpportunityCount = -2;

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Last generated market opportunity day cannot be negative: -1.", validation.Errors);
        Assert.Contains("Generated market opportunity count cannot be negative: -2.", validation.Errors);
    }

    [Fact]
    public void RuntimeInvariantChecker_ValidatesGeneratorState()
    {
        TurnipTestState state = CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);
        SetPrivateAutoProperty(state, nameof(TurnipTestState.LastGeneratedMarketOpportunityDay), 99);
        SetPrivateAutoProperty(state, nameof(TurnipTestState.GeneratedMarketOpportunityCount), -1);

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Last generated market opportunity day 99 cannot be after current day", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.Contains("Generated market opportunity count cannot be negative", StringComparison.Ordinal));
    }

    private static TurnipTestState CreateState(string manifestId)
    {
        GameStateFactoryResult result = TestContentFactory.CreateThemePackStateFactory().CreateState(manifestId);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
        return result.State;
    }

    private static ContentRegistry CreateRegistry(string manifestId)
    {
        GameStateFactoryResult result = TestContentFactory.CreateThemePackStateFactory().CreateState(manifestId);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
        return result.ContentRegistry;
    }

    private static TurnipTestState RoundTripState(TurnipTestState state, ContentRegistry registry)
    {
        SaveGameFileService service = new();
        SaveGameState saveGame = service.Deserialize(service.Serialize(state.CreateSaveGame()));
        TurnipTestState loadedState = new(registry);
        loadedState.ApplySaveGame(saveGame);
        return loadedState;
    }

    private static void AdvanceToDay(TurnipTestState state, int targetDay)
    {
        while (state.DayNumber < targetDay)
            state.AdvanceDay();
    }

    private static void AddInventoryItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }

    private static void SetPrivateAutoProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        typeof(TTarget)
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }
}
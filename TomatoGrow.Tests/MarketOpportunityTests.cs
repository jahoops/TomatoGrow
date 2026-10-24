using System;
using System.IO;
using System.Reflection;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class MarketOpportunityTests
{
    [Fact]
    public void ActiveWineOpportunity_BoostsWineEffectiveSellPrice()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(30));

        Assert.Equal(240, state.GetEffectiveSellPrice("wine"));
    }

    [Fact]
    public void SellingWineDuringOpportunity_IncreasesMoneyByBoostedPrice()
    {
        TurnipTestState state = CreateState();
        AddInventoryItem(state, "wine", 1);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 240.", message);
        Assert.Equal(240, state.Money);
        Assert.Equal("Sold 1 Wine for $240.", state.ActivityLog[^1].Message);
    }

    [Fact]
    public void SellingWineFromStorageDuringOpportunity_UsesBoostedPrice()
    {
        TurnipTestState state = CreateState();
        AddStorageItem(state, "wine", 1);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 240.", message);
        Assert.Equal(240, state.Money);
        Assert.Equal(0, state.GetStorageItemCount("wine"));
    }

    [Fact]
    public void SellingUnrelatedItem_UsesNormalPrice()
    {
        TurnipTestState state = CreateState();
        AddInventoryItem(state, "beer", 1);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 140.", message);
        Assert.Equal(140, state.Money);
    }

    [Fact]
    public void OpportunityDaysDecrease_WhenDayAdvances()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(30));

        state.AdvanceDay();

        Assert.NotNull(state.ActiveMarketOpportunity);
        Assert.Equal(29, state.ActiveMarketOpportunity!.DaysRemaining);
    }

    [Fact]
    public void OpportunityExpires_WhenDaysReachZero()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(1));

        state.AdvanceDay();

        Assert.Null(state.ActiveMarketOpportunity);
    }

    [Fact]
    public void ExpiredOpportunity_NoLongerBoostsSellPrice()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(1));
        state.AdvanceDay();
        AddInventoryItem(state, "wine", 1);

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 160.", message);
        Assert.Equal(160, state.Money);
    }

    [Fact]
    public void ActivityLog_RecordsOpportunityStart()
    {
        TurnipTestState state = CreateState();

        state.SetActiveMarketOpportunity(CreateWineBoom(30));

        ActivityLogEntry entry = state.ActivityLog[^1];
        Assert.Equal(ActivityLogCategory.Market, entry.Category);
        Assert.Equal(1, entry.Day);
        Assert.Equal("Wine Boom started: Wine sells for 150% for 30 days.", entry.Message);
    }

    [Fact]
    public void ActivityLog_RecordsOpportunityEnd()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(1));

        state.AdvanceDay();

        ActivityLogEntry entry = state.ActivityLog[^1];
        Assert.Equal(ActivityLogCategory.Market, entry.Category);
        Assert.Equal(2, entry.Day);
        Assert.Equal("Wine Boom ended.", entry.Message);
    }

    [Fact]
    public void MarketScreen_ShowsActiveOpportunity()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(23));

        string[] lines = DebugUiTextFormatter.GetMarketScreenLines(
            Registry,
            state.MarketOffers,
            state.Money,
            includeDebugDetails: false,
            activeMarketOpportunity: state.ActiveMarketOpportunity,
            getSellPrice: state.GetEffectiveSellPrice);

        Assert.Contains("Market Opportunity:", lines);
        Assert.Contains("Wine Boom - Wine sells for 150% - 23 days left", lines);
    }

    [Fact]
    public void FarmComputer_ShowsActiveOpportunity()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(23));

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            Registry,
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
            plot => state.IsPlotHarvestable(plot.X, plot.Y),
            activeMarketOpportunity: state.ActiveMarketOpportunity,
            getSellPrice: state.GetEffectiveSellPrice,
            daysUntilNextOpportunity: state.GetDaysUntilNextMarketOpportunity(),
            nextCandidateItemId: state.GetNextMarketOpportunityCandidateItemId());

        Assert.Contains("MARKET OPPORTUNITY", lines);
        Assert.Contains("Wine Boom", lines);
        Assert.Contains("Wine: $160 -> $240", lines);
        Assert.Contains("Bonus: +50%", lines);
        Assert.Contains("Days left: 23", lines);
        Assert.Contains("Next candidate chain: Grapes -> Wine", lines);
    }

    [Fact]
    public void ProductionChainLines_ShowOpportunityAdjustedOutputValue()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(30));

        string[] lines = DebugUiTextFormatter.GetProductionChainLines(
            Registry,
            state.ActiveMarketOpportunity,
            state.GetEffectiveSellPrice);

        Assert.Contains("Fermenter: 3 Grapes -> 1 Wine | 2 days | $135 -> $240 during Wine Boom | +$105", lines);
    }

    [Fact]
    public void ProductionReportScreenLines_ShowOpportunityAdjustedOutputValue()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(30));

        string[] lines = DebugUiTextFormatter.GetProductionReportScreenLines(
            Registry,
            state.ActiveMarketOpportunity,
            state.GetEffectiveSellPrice);

        Assert.Contains("  Fermenter: 3 Grapes -> 1 Wine | 2 days | $135 -> $240 during Wine Boom | +$105", lines);
    }

    [Fact]
    public void MarketPreview_UsesBoostedOutputValue_WhenOpportunityAffectsOutputItem()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(30));

        DebugUiTextFormatter.MarketOfferProcessingEconomics? economics = DebugUiTextFormatter.CalculateMarketOfferProcessingEconomics(
            Registry,
            Registry.GetMarketOffer("buy_grapes"),
            Registry.GetProcessingRecipe("grape_to_wine"),
            state.GetEffectiveSellPrice);

        Assert.NotNull(economics);
        Assert.Equal(240, economics.Value.OutputValue);
        Assert.Equal(80, economics.Value.ProfitIfBought);

        string[] lines = DebugUiTextFormatter.GetMarketOfferDisplayLines(
            Registry,
            Registry.GetMarketOffer("buy_grapes"),
            1,
            500,
            includeDebugDetails: false,
            getSellPrice: state.GetEffectiveSellPrice);

        Assert.Contains("   Output value: $240 during Wine Boom", lines);
    }

    [Fact]
    public void SaveLoad_RoundTripsActiveOpportunity()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(18));
        SaveGameFileService service = new();
        string json = service.Serialize(state.CreateSaveGame());
        SaveGameState saveGame = service.Deserialize(json);
        TurnipTestState loadedState = CreateState();

        loadedState.ApplySaveGame(saveGame);

        Assert.NotNull(loadedState.ActiveMarketOpportunity);
        Assert.Equal("wine_boom", loadedState.ActiveMarketOpportunity!.Id);
        Assert.Equal("wine", loadedState.ActiveMarketOpportunity.ItemId);
        Assert.Equal(1.5, loadedState.ActiveMarketOpportunity.SellPriceMultiplier);
        Assert.Equal(18, loadedState.ActiveMarketOpportunity.DaysRemaining);
        Assert.Equal("Wine Boom", loadedState.ActiveMarketOpportunity.DisplayName);
    }

    [Fact]
    public void SaveValidation_FailsWhenOpportunityItemIdIsMissing()
    {
        TurnipTestState state = CreateState();
        state.SetActiveMarketOpportunity(CreateWineBoom(12));
        SaveGameState saveGame = state.CreateSaveGame();
        saveGame.MarketOpportunity!.ItemId = "ghost_wine";

        SaveLoadValidationResult validation = state.ValidateSaveGame(saveGame);

        Assert.False(validation.IsValid);
        Assert.Contains("Missing item id in market opportunity: 'ghost_wine'.", validation.Errors);
    }

    [Fact]
    public void RuntimeInvariantChecker_CatchesInvalidOpportunityItemId()
    {
        TurnipTestState state = CreateState();
        SetPrivateAutoProperty(state, nameof(TurnipTestState.ActiveMarketOpportunity), new MarketOpportunity
        {
            Id = "wine_boom",
            ItemId = "ghost_wine",
            SellPriceMultiplier = 1.5,
            DaysRemaining = 10,
            DisplayName = "Wine Boom",
            Message = "Wine sells for 150% for 30 days."
        });

        GameStateValidationResult result = state.ValidateCurrentState();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Market opportunity references missing item id 'ghost_wine'.", StringComparison.Ordinal));
    }

    [Fact]
    public void AutoPlay_SellingGetsBoostedValueNaturally()
    {
        ContentRegistry registry = BuildAutoPlaySellRegistry();
        TurnipTestState state = new(registry);
        AddInventoryItem(state, "wine", 1);
        state.SetActiveMarketOpportunity(CreateWineBoom(30));
        AutoPlayTestController controller = new(state, registry);

        string message = controller.RunStep();

        Assert.StartsWith("Auto: sold processed goods.", message, StringComparison.Ordinal);
        Assert.Equal(240, state.Money);
        Assert.Equal(1, controller.Stats.GetSoldItemCount("wine"));
    }

    private static ContentRegistry Registry => new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));

    private static TurnipTestState CreateState()
    {
        return new(Registry);
    }

    private static MarketOpportunity CreateWineBoom(int daysRemaining)
    {
        return new MarketOpportunity
        {
            Id = "wine_boom",
            ItemId = "wine",
            SellPriceMultiplier = 1.5,
            DaysRemaining = daysRemaining,
            DisplayName = "Wine Boom",
            Message = "Wine sells for 150% for 30 days."
        };
    }

    private static ContentRegistry BuildAutoPlaySellRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed", BuyPrice = 10, SellPrice = 1 },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop", BuyPrice = 0, SellPrice = 30 },
                new ItemDefinition { Id = "wine", Name = "Wine", Type = "processed_good", BuyPrice = 0, SellPrice = 160 }
            ],
            [
                new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 }
            ]);
    }

    private static void AddInventoryItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }

    private static void AddStorageItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddStorageItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }

    private static void SetPrivateAutoProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
    {
        typeof(TTarget)
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }
}

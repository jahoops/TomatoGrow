using System.Reflection;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class ProcessorMachineTests
{
    [Fact]
    public void Fermenter_StartsIdle()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.False(state.Fermenter.IsWorking);
        Assert.False(state.Fermenter.HasOutputReady);
        Assert.Equal(0, state.Fermenter.DaysRemaining);
    }

    [Fact]
    public void AgingBarrel_StartsIdle()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.False(state.AgingBarrel.IsWorking);
        Assert.False(state.AgingBarrel.HasOutputReady);
        Assert.Equal(0, state.AgingBarrel.DaysRemaining);
    }

    [Fact]
    public void RuntimeMachineInstances_AreCreatedFromContentDefinitionsInOrder()
    {
        TurnipTestState state = new(BuildRegistry());

        Assert.Equal(["fermenter", "aging_barrel"], state.ProcessorMachines.Select(machine => machine.MachineId).ToArray());
        Assert.Equal("fermenter", state.SelectedProcessorMachine.MachineId);
    }

    [Fact]
    public void StartingFermenterWithEnoughGrapes_ConsumesThreeGrapes()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);

        string message = state.StartFermenterFromInventory();

        Assert.Equal("Started fermenter: 3 Grapes -> 1 Wine, ready in 2 days.", message);
        Assert.Equal(0, state.GetItemCount("grape"));
        Assert.Equal(1, state.Stats.MachineRunsStarted);
        Assert.Equal(135, state.Stats.MachineInputValueConsumed);
        Assert.Equal(0, state.Stats.MachineOutputValueCreated);
        Assert.Equal(0, state.Stats.MachineValueAdded);
    }

    [Fact]
    public void StartingFermenter_AddsMachineStartActivityEntry()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);

        state.StartFermenterFromInventory();

        ActivityLogEntry entry = Assert.Single(state.ActivityLog);
        Assert.Equal(ActivityLogCategory.Machine, entry.Category);
        Assert.Equal("Started Fermenter: 3 Grapes -> 1 Wine, ready in 2 days.", entry.Message);
    }

    [Fact]
    public void StartingFermenterWithEnoughGrapes_SetsDaysRemainingFromRecipe()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);

        state.StartFermenterFromInventory();

        Assert.Equal(2, state.Fermenter.DaysRemaining);
        Assert.True(state.Fermenter.IsWorking);
    }

    [Fact]
    public void StartingFermenter_CanUseSharedStorageInputWhenPlayerInventoryIsEmpty()
    {
        TurnipTestState state = new(BuildRegistry());
        AddStorageItem(state, "grape", 3);

        string message = state.StartFermenterFromInventory();

        Assert.Equal("Started fermenter: 3 Grapes -> 1 Wine, ready in 2 days.", message);
        Assert.Equal(0, state.GetStorageItemCount("grape"));
        Assert.Equal(1, state.Stats.MachineRunsStarted);
    }

    [Fact]
    public void StartingFermenter_PrefersPlayerInventoryBeforeSharedStorage()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        AddStorageItem(state, "grape", 3);

        state.StartFermenterFromInventory();

        Assert.Equal(0, state.GetItemCount("grape"));
        Assert.Equal(3, state.GetStorageItemCount("grape"));
    }

    [Fact]
    public void AdvancingDays_CompletesProcessing()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        state.StartFermenterFromInventory();

        state.AdvanceDay();
        state.AdvanceDay();

        Assert.False(state.Fermenter.IsWorking);
        Assert.True(state.Fermenter.HasOutputReady);
        Assert.Equal(0, state.Fermenter.DaysRemaining);
    }

    [Fact]
    public void CollectingOutput_AddsWineToInventory()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        state.StartFermenterFromInventory();
        state.AdvanceDay();
        state.AdvanceDay();

        string message = state.CollectFermenterOutput();

        Assert.Equal("Collected 1 Wine from fermenter.", message);
        Assert.Equal(1, state.GetItemCount("wine"));
        Assert.Equal(1, state.Stats.MachineOutputsCollected);
        Assert.Equal(1, state.Stats.ProcessedGoodsProduced);
        Assert.Equal(160, state.Stats.MachineOutputValueCreated);
        Assert.Equal(25, state.Stats.MachineValueAdded);
        Assert.Equal(1, state.Stats.GetProcessedOutputCount("wine"));
    }

    [Fact]
    public void CollectingOutput_AddsMachineCollectActivityEntryWithValueAdded()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        state.StartFermenterFromInventory();
        state.AdvanceDay();
        state.AdvanceDay();

        state.CollectFermenterOutput();

        ActivityLogEntry entry = state.ActivityLog[^1];
        Assert.Equal(ActivityLogCategory.Machine, entry.Category);
        Assert.Equal("Collected 1 Wine from Fermenter, value added +$25.", entry.Message);
        Assert.Equal(25, entry.ValueAmount);
    }

    [Fact]
    public void Fermenter_ClearsAfterCollectingOutput()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        state.StartFermenterFromInventory();
        state.AdvanceDay();
        state.AdvanceDay();

        state.CollectFermenterOutput();

        Assert.False(state.Fermenter.IsWorking);
        Assert.False(state.Fermenter.HasOutputReady);
        Assert.Equal(0, state.Fermenter.DaysRemaining);
        Assert.Null(state.Fermenter.CurrentRecipeId);
    }

    [Fact]
    public void CannotStartFermenterWhileAlreadyWorking()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 6);
        state.StartFermenterFromInventory();
        state.AdvanceDay();

        string message = state.StartFermenterFromInventory();

        Assert.Equal("Fermenter is already working: 1 Wine ready in 1 day.", message);
        Assert.Equal(3, state.GetItemCount("grape"));
    }

    [Fact]
    public void CannotStartFermenterWhenOutputIsWaiting()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 6);
        state.StartFermenterFromInventory();
        state.AdvanceDay();
        state.AdvanceDay();

        string message = state.StartFermenterFromInventory();

        Assert.Equal("Collect fermenter output first with O.", message);
        Assert.True(state.Fermenter.HasOutputReady);
        Assert.Equal(1, state.Stats.MachineRunsStarted);
    }

    [Fact]
    public void StartingFermenterWithHops_ProducesBeerUsingJsonRecipe()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "hops", 3);

        string message = state.StartFermenterFromInventory();

        Assert.Equal("Started fermenter: 3 Hops -> 1 Beer, ready in 2 days.", message);
        state.AdvanceDay();
        state.AdvanceDay();
        state.CollectFermenterOutput();
        Assert.Equal(1, state.GetItemCount("beer"));
        Assert.Equal(120, state.Stats.MachineInputValueConsumed);
        Assert.Equal(140, state.Stats.MachineOutputValueCreated);
        Assert.Equal(20, state.Stats.MachineValueAdded);
    }

    [Fact]
    public void StartingFermenterWithPreferredRecipe_UsesSelectedRecipeWhenInputsExist()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        AddInventoryItem(state, "hops", 3);

        string message = state.StartProcessorMachineFromInventory("fermenter", "hops_to_beer", requireSpecificRecipe: true);

        Assert.Equal("Started fermenter: 3 Hops -> 1 Beer, ready in 2 days.", message);
        Assert.Equal("hops_to_beer", state.Fermenter.CurrentRecipeId);
    }

    [Fact]
    public void StartingFermenterWithSelectedWineRecipe_UsesGrapesWhenAvailable()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        AddInventoryItem(state, "hops", 3);

        string message = state.StartProcessorMachineFromInventory("fermenter", "grape_to_wine", requireSpecificRecipe: true);

        Assert.Equal("Started fermenter: 3 Grapes -> 1 Wine, ready in 2 days.", message);
        Assert.Equal("grape_to_wine", state.Fermenter.CurrentRecipeId);
    }

    [Fact]
    public void StartingFermenterWithPreferredRecipe_ReturnsMissingInputMessageWhenSelectedRecipeCannotStart()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);

        string message = state.StartProcessorMachineFromInventory("fermenter", "hops_to_beer", requireSpecificRecipe: true);

        Assert.Equal("Need 3 Hops for Beer.", message);
        Assert.Null(state.Fermenter.CurrentRecipeId);
    }

    [Fact]
    public void StartingAgingBarrelWithWine_ProducesAgedWineUsingJsonRecipe()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "wine", 1);

        string message = state.StartProcessorMachineFromInventory("aging_barrel");

        Assert.Equal("Started aging barrel: 1 Wine -> 1 Aged Wine, ready in 5 days.", message);

        for (int day = 0; day < 5; day++)
            state.AdvanceDay();

        Assert.Equal("Collected 1 Aged Wine from aging barrel.", state.CollectProcessorMachineOutput("aging_barrel"));
        Assert.Equal(1, state.GetItemCount("aged_wine"));
        Assert.Equal(160, state.Stats.MachineInputValueConsumed);
        Assert.Equal(260, state.Stats.MachineOutputValueCreated);
        Assert.Equal(100, state.Stats.MachineValueAdded);
    }

    [Fact]
    public void StartingAgingBarrel_AddsMachineStartActivityEntry()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "wine", 1);

        state.StartProcessorMachineFromInventory("aging_barrel");

        ActivityLogEntry entry = Assert.Single(state.ActivityLog);
        Assert.Equal(ActivityLogCategory.Machine, entry.Category);
        Assert.Equal("Started Aging Barrel: 1 Wine -> 1 Aged Wine, ready in 5 days.", entry.Message);
    }

    [Fact]
    public void CollectingAgedWine_AddsMachineCollectActivityEntry()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "wine", 1);
        state.StartProcessorMachineFromInventory("aging_barrel");

        for (int day = 0; day < 5; day++)
            state.AdvanceDay();

        state.CollectProcessorMachineOutput("aging_barrel");

        ActivityLogEntry entry = state.ActivityLog[^1];
        Assert.Equal(ActivityLogCategory.Machine, entry.Category);
        Assert.Equal("Collected 1 Aged Wine from Aging Barrel, value added +$100.", entry.Message);
    }

    [Fact]
    public void MarketBoughtGrapes_CanFeedFermenter()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 160);

        Assert.Equal("Bought 3 Grapes for 160.", state.BuyMarketOffer("buy_grapes"));
        Assert.Equal("Started fermenter: 3 Grapes -> 1 Wine, ready in 2 days.", state.StartFermenterFromInventory());
        state.AdvanceDay();
        state.AdvanceDay();

        Assert.Equal("Collected 1 Wine from fermenter.", state.CollectFermenterOutput());
        Assert.Equal(1, state.GetItemCount("wine"));
    }

    [Fact]
    public void MarketBoughtWine_CanFeedAgingBarrel()
    {
        TurnipTestState state = new(BuildRegistry());
        SetMoney(state, 190);

        Assert.Equal("Bought 1 Wine for 190.", state.BuyMarketOffer("buy_wine"));
        Assert.Equal("Started aging barrel: 1 Wine -> 1 Aged Wine, ready in 5 days.", state.StartProcessorMachineFromInventory("aging_barrel"));

        for (int day = 0; day < 5; day++)
            state.AdvanceDay();

        Assert.Equal("Collected 1 Aged Wine from aging barrel.", state.CollectProcessorMachineOutput("aging_barrel"));
        Assert.Equal(1, state.GetItemCount("aged_wine"));
    }

    [Fact]
    public void StartingFermenterWithNoValidInput_ReportsNoValidInput()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.StartFermenterFromInventory();

        Assert.Equal("No valid fermenter input. Need 3 Grapes or 3 Hops.", message);
        Assert.Equal(0, state.Stats.MachineRunsStarted);
    }

    [Fact]
    public void PressingMWhileWorking_DoesNotIncrementMachineRunsStartedAgain()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 6);
        state.StartFermenterFromInventory();
        state.AdvanceDay();

        state.StartFermenterFromInventory();

        Assert.Equal(1, state.Stats.MachineRunsStarted);
        Assert.Equal(135, state.Stats.MachineInputValueConsumed);
    }

    [Fact]
    public void PressingOWithNoOutputReady_DoesNotChangeMachineStats()
    {
        TurnipTestState state = new(BuildRegistry());

        string message = state.CollectFermenterOutput();

        Assert.Equal("No fermenter output ready.", message);
        Assert.Equal(0, state.Stats.MachineOutputsCollected);
        Assert.Equal(0, state.Stats.ProcessedGoodsProduced);
        Assert.Equal(0, state.Stats.MachineOutputValueCreated);
        Assert.Equal(0, state.Stats.MachineValueAdded);
    }

    [Fact]
    public void Reset_ClearsMachineStats()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        state.StartFermenterFromInventory();
        state.AdvanceDay();
        state.AdvanceDay();
        state.CollectFermenterOutput();

        state.Reset();

        Assert.Equal(0, state.Stats.MachineRunsStarted);
        Assert.Equal(0, state.Stats.MachineOutputsCollected);
        Assert.Equal(0, state.Stats.MachineInputValueConsumed);
        Assert.Equal(0, state.Stats.MachineOutputValueCreated);
        Assert.Equal(0, state.Stats.MachineValueAdded);
        Assert.Equal(0, state.Stats.ProcessedGoodsProduced);
        Assert.Equal(0, state.Stats.GetProcessedOutputCount("wine"));
    }

    [Fact]
    public void CollectingOutput_ClearsMachineStateAndAllowsRestartWhenInputRemains()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 6);
        state.StartFermenterFromInventory();
        state.AdvanceDay();
        state.AdvanceDay();

        string collectMessage = state.CollectFermenterOutput();
        string restartMessage = state.StartFermenterFromInventory();

        Assert.Equal("Collected 1 Wine from fermenter.", collectMessage);
        Assert.Equal("Started fermenter: 3 Grapes -> 1 Wine, ready in 2 days.", restartMessage);
        Assert.True(state.Fermenter.IsWorking);
        Assert.False(state.Fermenter.HasOutputReady);
    }

    [Fact]
    public void Fermenter_CanProcessGrapesThenHopsInSequence()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "grape", 3);
        AddInventoryItem(state, "hops", 3);

        Assert.Equal("Started fermenter: 3 Grapes -> 1 Wine, ready in 2 days.", state.StartFermenterFromInventory());
        state.AdvanceDay();
        state.AdvanceDay();
        Assert.Equal("Collected 1 Wine from fermenter.", state.CollectFermenterOutput());

        Assert.Equal("Started fermenter: 3 Hops -> 1 Beer, ready in 2 days.", state.StartFermenterFromInventory());
        state.AdvanceDay();
        state.AdvanceDay();
        Assert.Equal("Collected 1 Beer from fermenter.", state.CollectFermenterOutput());

        Assert.Equal(1, state.GetItemCount("wine"));
        Assert.Equal(1, state.GetItemCount("beer"));
    }

    [Fact]
    public void SellingWine_UsesJsonSellPrice()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "wine", 1);

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 160.", message);
        Assert.Equal(160, state.Money);
    }

    [Fact]
    public void SellingBeer_UsesJsonSellPrice()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "beer", 1);

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 140.", message);
        Assert.Equal(140, state.Money);
    }

    [Fact]
    public void SellingAgedWine_UsesJsonSellPrice()
    {
        TurnipTestState state = new(BuildRegistry());
        AddInventoryItem(state, "aged_wine", 1);

        string message = state.SellSelectedCrop();

        Assert.Equal("Sold 1 items for 260.", message);
        Assert.Equal(260, state.Money);
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

    private static void SetMoney(TurnipTestState state, int amount)
    {
        typeof(TurnipTestState)
            .GetProperty("Money", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(state, amount);
    }
}
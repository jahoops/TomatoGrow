using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class ThemeVocabularyTests
{
    [Fact]
    public void MiningDemo_HasExpectedBuiltInLabels()
    {
        ThemeVocabulary theme = ThemeVocabulary.MiningDemo;

        Assert.Equal("mining_demo", theme.ThemeId);
        Assert.Equal("Mining Demo", theme.ThemeName);
        Assert.Equal("Mine", theme.WorldName);
        Assert.Equal("Mines", theme.WorldPluralName);
        Assert.Equal("Claim", theme.TileName);
        Assert.Equal("Claims", theme.TilePluralName);
        Assert.Equal("Marker", theme.StarterItemName);
        Assert.Equal("Markers", theme.StarterItemPluralName);
        Assert.Equal("Vein", theme.ProducerName);
        Assert.Equal("Veins", theme.ProducerPluralName);
        Assert.Equal("Power", theme.MaintainActionName);
        Assert.Equal("Power Bot", theme.MaintainerBotName);
        Assert.Equal("Powered", theme.MaintainedStateName);
        Assert.Equal("Unpowered", theme.UnmaintainedStateName);
        Assert.Equal("Extract", theme.CollectActionName);
        Assert.Equal("Extractor Bot", theme.CollectorBotName);
        Assert.Equal("Claim Bot", theme.StarterBotName);
        Assert.Equal("Processing", theme.ProcessingName);
        Assert.Equal("Storage", theme.StorageName);
        Assert.Equal("Supply Market", theme.MarketName);
        Assert.Equal("Restoration", theme.RestorationName);
        Assert.Equal("Operations Computer", theme.ComputerName);
        Assert.Equal("Production Report", theme.ProductionReportName);
        Assert.Equal("Activity Log", theme.ActivityLogName);
        Assert.Equal("Selected Claim", theme.GetTileSelectionLabel());
        Assert.Equal("Mine Controls", theme.GetWorldControlsLabel());
    }

    [Fact]
    public void MiningDemo_DoesNotChangeMoneyForEquivalentActions()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);

        SetMoney(farmingState, 50);
        SetMoney(miningState, 50);

        string farmingMessage = farmingState.BuySelectedSeed();
        string miningMessage = miningState.BuySelectedSeed();

        Assert.Equal(farmingMessage, miningMessage);
        Assert.Equal(farmingState.Money, miningState.Money);
    }

    [Fact]
    public void MiningDemo_DoesNotChangeInventoryForEquivalentActions()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);

        SetMoney(farmingState, 50);
        SetMoney(miningState, 50);
        farmingState.BuySelectedSeed();
        miningState.BuySelectedSeed();
        farmingState.PlantSeedAtPlot(0, 0, "turnip_seed");
        miningState.PlantSeedAtPlot(0, 0, "turnip_seed");

        foreach (string itemId in registry.Items.Select(item => item.Id))
            Assert.Equal(farmingState.GetItemCount(itemId), miningState.GetItemCount(itemId));
    }

    [Fact]
    public void MiningDemo_DoesNotChangeCropGrowthBehavior()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);

        farmingState.PlantSeedAtPlot(0, 0, "turnip_seed");
        miningState.PlantSeedAtPlot(0, 0, "turnip_seed");
        farmingState.WaterPlot(0, 0);
        miningState.WaterPlot(0, 0);
        farmingState.AdvanceDay();
        miningState.AdvanceDay();

        FarmPlot farmingPlot = farmingState.FarmGrid.GetPlot(0, 0);
        FarmPlot miningPlot = miningState.FarmGrid.GetPlot(0, 0);

        Assert.Equal(farmingPlot.PlantedCrop?.CropDefinitionId, miningPlot.PlantedCrop?.CropDefinitionId);
        Assert.Equal(farmingPlot.PlantedCrop?.DaysGrown, miningPlot.PlantedCrop?.DaysGrown);
        Assert.Equal(farmingPlot.IsWatered, miningPlot.IsWatered);
        Assert.Equal(farmingState.IsPlotHarvestable(0, 0), miningState.IsPlotHarvestable(0, 0));
    }

    [Fact]
    public void MiningDemo_DoesNotChangeMachineBehavior()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);

        AddInventoryItem(farmingState, "grape", 3);
        AddInventoryItem(miningState, "grape", 3);

        Assert.Equal(farmingState.StartFermenterFromInventory(), miningState.StartFermenterFromInventory());

        farmingState.AdvanceDay();
        miningState.AdvanceDay();

        Assert.Equal(farmingState.Fermenter.CurrentRecipeId, miningState.Fermenter.CurrentRecipeId);
        Assert.Equal(farmingState.Fermenter.IsWorking, miningState.Fermenter.IsWorking);
        Assert.Equal(farmingState.Fermenter.DaysRemaining, miningState.Fermenter.DaysRemaining);
        Assert.Equal(farmingState.Fermenter.HasOutputReady, miningState.Fermenter.HasOutputReady);
        Assert.Equal(farmingState.Stats.MachineRunsStarted, miningState.Stats.MachineRunsStarted);
    }

    [Fact]
    public void MiningDemo_DoesNotChangeAutoplayResultForEquivalentStartingState()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);
        AutoPlaySettings settings = new()
        {
            TargetMoney = 500,
            MaxDays = 365,
            MaxSteps = 5000,
            MaxNoActionSteps = 8,
            DesiredSeedCount = 8
        };

        AutoPlayTestController farmingController = new(farmingState, registry, settings);
        AutoPlayTestController miningController = new(miningState, registry, settings);

        farmingController.Enable();
        miningController.Enable();

        while (farmingController.IsEnabled)
            farmingController.RunStep();

        while (miningController.IsEnabled)
            miningController.RunStep();

        Assert.Equal(farmingController.Stats.StopReason, miningController.Stats.StopReason);
        Assert.Equal(farmingState.Money, miningState.Money);
        Assert.Equal(farmingState.DayNumber, miningState.DayNumber);
        Assert.Equal(farmingController.Stats.DaysSimulated, miningController.Stats.DaysSimulated);
        Assert.Equal(farmingController.Stats.StepsRun, miningController.Stats.StepsRun);
        Assert.Equal(farmingController.Stats.CropsHarvested, miningController.Stats.CropsHarvested);
        Assert.Equal(farmingController.Stats.CropsSold, miningController.Stats.CropsSold);
    }

    [Fact]
    public void MiningDemo_SaveLoadDoesNotPersistThemeVocabulary()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);
        SaveGameFileService service = new();

        SetMoney(state, 200);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        state.WaterPlot(0, 0);
        state.AdvanceDay();

        string json = service.Serialize(state.CreateSaveGame());
        SaveGameState saveGame = service.Deserialize(json);
        TurnipTestState loadedState = new(registry);
        loadedState.ApplySaveGame(saveGame);

        Assert.DoesNotContain("ThemeVocabulary", json, StringComparison.Ordinal);
        Assert.DoesNotContain("mining_demo", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ThemeVocabulary.FarmingDefault, loadedState.ThemeVocabulary);
        Assert.Equal(state.DayNumber, loadedState.DayNumber);
        Assert.Equal(state.Money, loadedState.Money);
        Assert.Equal(state.GetItemCount("turnip_seed"), loadedState.GetItemCount("turnip_seed"));
        Assert.Equal(state.FarmGrid.GetPlot(0, 0).PlantedCrop?.CropDefinitionId, loadedState.FarmGrid.GetPlot(0, 0).PlantedCrop?.CropDefinitionId);
        Assert.Equal(state.FarmGrid.GetPlot(0, 0).PlantedCrop?.DaysGrown, loadedState.FarmGrid.GetPlot(0, 0).PlantedCrop?.DaysGrown);
    }

    private static ContentRegistry BuildRegistry()
    {
        return new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));
    }

    private static void SetMoney(TurnipTestState state, int amount)
    {
        typeof(TurnipTestState)
            .GetProperty("Money", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(state, amount);
    }

    private static void AddInventoryItem(TurnipTestState state, string itemId, int amount)
    {
        typeof(TurnipTestState)
            .GetMethod("AddItem", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [itemId, amount]);
    }
}
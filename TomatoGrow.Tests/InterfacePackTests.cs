using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class InterfacePackTests
{
    [Fact]
    public void FarmingDefaultInterface_HasIdAndName()
    {
        Assert.Equal("farming_default_interface", InterfacePack.FarmingDefault.Id);
        Assert.Equal("Farming Default Interface", InterfacePack.FarmingDefault.Name);
    }

    [Fact]
    public void MiningDemoInterface_HasIdAndName()
    {
        Assert.Equal("mining_demo_interface", InterfacePack.MiningDemo.Id);
        Assert.Equal("Mining Demo Interface", InterfacePack.MiningDemo.Name);
    }

    [Fact]
    public void FarmingAndMiningInterfaces_UseDifferentStateAndSelectionTokens()
    {
        Assert.NotEqual(InterfacePack.FarmingDefault.UnmaintainedShortLabel, InterfacePack.MiningDemo.UnmaintainedShortLabel);
        Assert.NotEqual(InterfacePack.FarmingDefault.MaintainedShortLabel, InterfacePack.MiningDemo.MaintainedShortLabel);
        Assert.NotEqual(InterfacePack.FarmingDefault.ReadyTileSymbol, InterfacePack.MiningDemo.ReadyTileSymbol);
        Assert.NotEqual(InterfacePack.FarmingDefault.SelectedTileMarker, InterfacePack.MiningDemo.SelectedTileMarker);
    }

    [Fact]
    public void InterfacePack_CanBeAssignedWithoutChangingInitialGameplayState()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, interfacePack: InterfacePack.MiningDemo);

        Assert.Equal(InterfacePack.FarmingDefault, farmingState.InterfacePack);
        Assert.Equal(InterfacePack.MiningDemo, miningState.InterfacePack);
        Assert.Equal(farmingState.DayNumber, miningState.DayNumber);
        Assert.Equal(farmingState.Money, miningState.Money);
        Assert.Equal(farmingState.SelectedSeedItemId, miningState.SelectedSeedItemId);
        Assert.Equal(farmingState.FarmGrid.Width, miningState.FarmGrid.Width);
        Assert.Equal(farmingState.FarmGrid.Height, miningState.FarmGrid.Height);
    }

    [Fact]
    public void TileHelpers_UseFarmingDefaultVisualTokens()
    {
        FarmPlot plot = new(0, 0);

        Assert.Equal(InterfacePack.FarmingDefault.EmptyShortLabel, DebugUiTextFormatter.GetTileStateLabel(plot, InterfacePack.FarmingDefault));
        Assert.Equal(InterfacePack.FarmingDefault.EmptyTileSymbol, DebugUiTextFormatter.GetTileSymbol(plot, isReady: false, InterfacePack.FarmingDefault));
        Assert.Equal(InterfacePack.FarmingDefault.EmptyTileFillColor, DebugUiTextFormatter.GetTileFillColor(plot, InterfacePack.FarmingDefault));
    }

    [Fact]
    public void TileHelpers_UseMaintainedAndUnmaintainedLabelsAndColors()
    {
        FarmPlot dryPlot = new(0, 0);
        dryPlot.TryPlant("turnip");
        FarmPlot wetPlot = new(1, 0);
        wetPlot.TryPlant("turnip");
        wetPlot.Water();

        Assert.Equal(InterfacePack.FarmingDefault.UnmaintainedShortLabel, DebugUiTextFormatter.GetTileStatusLine(dryPlot, isReady: false, InterfacePack.FarmingDefault));
        Assert.Equal(InterfacePack.FarmingDefault.UnmaintainedTileFillColor, DebugUiTextFormatter.GetTileFillColor(dryPlot, InterfacePack.FarmingDefault));
        Assert.Equal(InterfacePack.FarmingDefault.MaintainedShortLabel, DebugUiTextFormatter.GetTileStatusLine(wetPlot, isReady: false, InterfacePack.FarmingDefault));
        Assert.Equal(InterfacePack.FarmingDefault.MaintainedTileFillColor, DebugUiTextFormatter.GetTileFillColor(wetPlot, InterfacePack.FarmingDefault));
    }

    [Fact]
    public void TileHelpers_UseReadyTokenAndReadyColor()
    {
        FarmPlot readyPlot = new(0, 0);
        readyPlot.TryPlant("turnip");

        Assert.Equal(InterfacePack.MiningDemo.ReadyTileSymbol, DebugUiTextFormatter.GetTileSymbol(readyPlot, isReady: true, InterfacePack.MiningDemo));
        Assert.Equal(InterfacePack.MiningDemo.ReadyShortLabel, DebugUiTextFormatter.GetTileStatusLine(readyPlot, isReady: true, InterfacePack.MiningDemo));
        Assert.Equal(InterfacePack.MiningDemo.ReadyTextColor, DebugUiTextFormatter.GetTileReadyTextColor(InterfacePack.MiningDemo));
    }

    [Fact]
    public void TileHelpers_UseSelectedMarkerAndBorderColor()
    {
        Assert.Equal(InterfacePack.MiningDemo.SelectedTileMarker, DebugUiTextFormatter.GetTileSelectionMarker(InterfacePack.MiningDemo));
        Assert.Equal(InterfacePack.MiningDemo.SelectedBorderColor, DebugUiTextFormatter.GetTileSelectedBorderColor(InterfacePack.MiningDemo));
        Assert.Equal(InterfacePack.FarmingDefault.NormalBorderColor, DebugUiTextFormatter.GetTileNormalBorderColor(InterfacePack.FarmingDefault));
    }

    [Fact]
    public void MiningDemoInterface_UsesMiningStyleGridTokens()
    {
        FarmPlot emptyPlot = new(0, 0);
        FarmPlot plantedPlot = new(1, 0);
        plantedPlot.TryPlant("turnip");
        FarmPlot poweredPlot = new(2, 0);
        poweredPlot.TryPlant("turnip");
        poweredPlot.Water();

        Assert.Equal(".", DebugUiTextFormatter.GetTileSymbol(emptyPlot, isReady: false, InterfacePack.MiningDemo));
        Assert.Equal("V", DebugUiTextFormatter.GetTileSymbol(plantedPlot, isReady: false, InterfacePack.MiningDemo));
        Assert.Equal("UNPWR", DebugUiTextFormatter.GetTileStatusLine(plantedPlot, isReady: false, InterfacePack.MiningDemo));
        Assert.Equal("PWRD", DebugUiTextFormatter.GetTileStatusLine(poweredPlot, isReady: false, InterfacePack.MiningDemo));
        Assert.Equal(ColorEquality(InterfacePack.MiningDemo.UnmaintainedTileFillColor), ColorEquality(DebugUiTextFormatter.GetTileFillColor(plantedPlot, InterfacePack.MiningDemo)));
    }

    [Fact]
    public void CompactTileVisual_ChangesWithInterfacePack_WithoutChangingState()
    {
        ContentRegistry registry = BuildRegistry();
        FarmPlot plot = new(0, 0);
        plot.TryPlant("turnip");

        string[] farmingLines = DebugUiTextFormatter.GetCompactTileVisualLines(plot, registry, false, InterfacePack.FarmingDefault);
        string[] miningLines = DebugUiTextFormatter.GetCompactTileVisualLines(plot, registry, false, InterfacePack.MiningDemo);

        Assert.NotEqual(farmingLines[0], miningLines[0]);
        Assert.Equal(0, plot.PlantedCrop?.DaysGrown ?? 0);
    }

    [Fact]
    public void SwitchingInterfacePack_DoesNotChangeMoney()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, interfacePack: InterfacePack.MiningDemo);

        SetMoney(farmingState, 50);
        SetMoney(miningState, 50);

        Assert.Equal(farmingState.BuySelectedSeed(), miningState.BuySelectedSeed());
        Assert.Equal(farmingState.Money, miningState.Money);
    }

    [Fact]
    public void SwitchingInterfacePack_DoesNotChangeInventory()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, interfacePack: InterfacePack.MiningDemo);

        farmingState.PlantSeedAtPlot(0, 0, "turnip_seed");
        miningState.PlantSeedAtPlot(0, 0, "turnip_seed");

        foreach (string itemId in registry.Items.Select(item => item.Id))
            Assert.Equal(farmingState.GetItemCount(itemId), miningState.GetItemCount(itemId));
    }

    [Fact]
    public void SwitchingInterfacePack_DoesNotChangeCropGrowth()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, interfacePack: InterfacePack.MiningDemo);

        farmingState.PlantSeedAtPlot(0, 0, "turnip_seed");
        miningState.PlantSeedAtPlot(0, 0, "turnip_seed");
        farmingState.WaterPlot(0, 0);
        miningState.WaterPlot(0, 0);
        farmingState.AdvanceDay();
        miningState.AdvanceDay();

        Assert.Equal(farmingState.FarmGrid.GetPlot(0, 0).PlantedCrop?.DaysGrown, miningState.FarmGrid.GetPlot(0, 0).PlantedCrop?.DaysGrown);
        Assert.Equal(farmingState.IsPlotHarvestable(0, 0), miningState.IsPlotHarvestable(0, 0));
    }

    [Fact]
    public void SwitchingInterfacePack_DoesNotChangeMachineBehavior()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, interfacePack: InterfacePack.MiningDemo);

        AddInventoryItem(farmingState, "grape", 3);
        AddInventoryItem(miningState, "grape", 3);
        farmingState.StartFermenterFromInventory();
        miningState.StartFermenterFromInventory();
        farmingState.AdvanceDay();
        miningState.AdvanceDay();

        Assert.Equal(farmingState.Fermenter.CurrentRecipeId, miningState.Fermenter.CurrentRecipeId);
        Assert.Equal(farmingState.Fermenter.DaysRemaining, miningState.Fermenter.DaysRemaining);
    }

    [Fact]
    public void SwitchingInterfacePack_DoesNotChangeAutoplayResult()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingState = new(registry);
        TurnipTestState miningState = new(registry, interfacePack: InterfacePack.MiningDemo);
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

        Assert.Equal(farmingState.Money, miningState.Money);
        Assert.Equal(farmingController.Stats.StopReason, miningController.Stats.StopReason);
        Assert.Equal(farmingController.Stats.StepsRun, miningController.Stats.StepsRun);
    }

    [Fact]
    public void SaveLoad_DoesNotPersistInterfacePack()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry, interfacePack: InterfacePack.MiningDemo);
        SaveGameFileService service = new();

        SetMoney(state, 200);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");
        string json = service.Serialize(state.CreateSaveGame());
        SaveGameState saveGame = service.Deserialize(json);
        TurnipTestState loadedState = new(registry);
        loadedState.ApplySaveGame(saveGame);

        Assert.DoesNotContain("InterfacePack", json, StringComparison.Ordinal);
        Assert.DoesNotContain("mining_demo_interface", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(InterfacePack.FarmingDefault, loadedState.InterfacePack);
        Assert.Equal(state.Money, loadedState.Money);
    }

    private static string ColorEquality(Color color)
    {
        return $"{color.R},{color.G},{color.B},{color.A}";
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
using System;
using System.IO;
using System.Reflection;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class GenericActionAliasIntegrationTests
{
    [Fact]
    public void PlantSelectedSeed_And_StartSelectedTileProducer_RemainEquivalent()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingNamedState = new(registry);
        TurnipTestState genericNamedState = new(registry);

        string farmingMessage = farmingNamedState.PlantSelectedSeed();
        string genericMessage = genericNamedState.StartSelectedTileProducer();

        Assert.Equal(farmingMessage, genericMessage);
        Assert.Equal(farmingNamedState.GetSelectedPlot().PlantedCrop?.CropDefinitionId, genericNamedState.GetSelectedPlot().PlantedCrop?.CropDefinitionId);
        Assert.Equal(farmingNamedState.GetItemCount("turnip_seed"), genericNamedState.GetItemCount("turnip_seed"));
    }

    [Fact]
    public void WaterSelectedPlot_And_MaintainSelectedTile_RemainEquivalent()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingNamedState = new(registry);
        TurnipTestState genericNamedState = new(registry);
        farmingNamedState.PlantSelectedSeed();
        genericNamedState.StartSelectedTileProducer();

        string farmingMessage = farmingNamedState.WaterSelectedPlot();
        string genericMessage = genericNamedState.MaintainSelectedTile();

        Assert.Equal(farmingMessage, genericMessage);
        Assert.Equal(farmingNamedState.GetSelectedPlot().IsWatered, genericNamedState.GetSelectedPlot().IsWatered);
    }

    [Fact]
    public void HarvestCrop_And_CollectSelectedTile_RemainEquivalent()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState farmingNamedState = CreateReadyTurnipState(registry);
        TurnipTestState genericNamedState = CreateReadyTurnipState(registry);

        string farmingMessage = farmingNamedState.HarvestCrop();
        string genericMessage = genericNamedState.CollectSelectedTile();

        Assert.Equal(farmingMessage, genericMessage);
        Assert.Equal(farmingNamedState.GetItemCount("turnip"), genericNamedState.GetItemCount("turnip"));
        Assert.Equal(farmingNamedState.ActivityLog[^1].Message, genericNamedState.ActivityLog[^1].Message);
    }

    [Fact]
    public void Game1_PlantInputWrapper_UsesGenericStartAliasBehavior()
    {
        using Game1 game = new();
        TurnipTestState state = GetGameState(game);

        InvokePrivate(game, "PlantSelectedSeed");

        Assert.False(state.GetSelectedPlot().IsEmpty);
        Assert.Equal("turnip", state.GetSelectedPlot().PlantedCrop?.CropDefinitionId);
        Assert.Equal("Planted turnip at 0,0.", GetLastActionMessage(game));
    }

    [Fact]
    public void Game1_WaterInputWrapper_UsesGenericMaintainAliasBehavior()
    {
        using Game1 game = new();
        TurnipTestState state = GetGameState(game);
        state.PlantSelectedSeed();

        InvokePrivate(game, "WaterSelectedPlot");

        Assert.True(state.GetSelectedPlot().IsWatered);
        Assert.Equal("Watered plot 0,0.", GetLastActionMessage(game));
    }

    [Fact]
    public void Game1_HarvestInputWrapper_UsesGenericCollectAliasBehavior()
    {
        using Game1 game = new();
        TurnipTestState state = GetGameState(game);
        PrepareReadySelectedTurnip(state);

        InvokePrivate(game, "HarvestSelectedPlot");

        Assert.Equal(1, state.GetItemCount("turnip"));
        Assert.True(state.GetSelectedPlot().IsEmpty);
        Assert.Equal("Harvested Turnip from 0,0.", GetLastActionMessage(game));
    }

    [Fact]
    public void AutoPlay_ManualFallback_RemainsCompatibleWithGenericAliases()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState state = new(registry);
        SetMoney(state, 50);
        AutoPlayTestController controller = new(
            state,
            registry,
            new AutoPlaySettings { TargetMoney = 120, MaxDays = 20, MaxSteps = 400, MaxNoActionSteps = 8, DesiredSeedCount = 3 });

        controller.Enable();

        for (int step = 0; step < 80 && controller.Stats.GetHarvestedItemCount("turnip") == 0; step++)
            controller.RunStep();

        Assert.NotEqual(AutoPlayStopReason.InvalidState, controller.Stats.StopReason);
        Assert.True(controller.Stats.GetPlantedSeedCount("turnip_seed") > 0);
        Assert.True(controller.Stats.GetHarvestedItemCount("turnip") > 0);
    }

    private static ContentRegistry BuildRegistry()
    {
        return new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));
    }

    private static TurnipTestState CreateReadyTurnipState(ContentRegistry registry)
    {
        TurnipTestState state = new(registry);
        PrepareReadySelectedTurnip(state);
        return state;
    }

    private static void PrepareReadySelectedTurnip(TurnipTestState state)
    {
        state.PlantSelectedSeed();
        for (int day = 0; day < 4; day++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }
    }

    private static TurnipTestState GetGameState(Game1 game)
    {
        return (TurnipTestState)typeof(Game1)
            .GetField("_turnipTestState", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(game)!;
    }

    private static string GetLastActionMessage(Game1 game)
    {
        return (string)typeof(Game1)
            .GetField("_lastActionMessage", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(game)!;
    }

    private static void InvokePrivate(Game1 game, string methodName)
    {
        typeof(Game1)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(game, null);
    }

    private static void SetMoney(TurnipTestState state, int amount)
    {
        typeof(TurnipTestState)
            .GetProperty("Money", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(state, amount);
    }
}
using System;
using System.Linq;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class ProductionAreaTests
{
    [Fact]
    public void NewGame_HasOneProductionArea()
    {
        TurnipTestState state = CreateState();

        Assert.Single(state.GetProductionAreas());
    }

    [Fact]
    public void DefaultProductionArea_HasExpectedIdentity_AndIsUnlocked()
    {
        TurnipTestState state = CreateState();

        ProductionArea area = state.GetDefaultProductionArea();

        Assert.Equal("outdoor_garden", area.Id);
        Assert.Equal("Outdoor Garden", area.Name);
        Assert.Equal("production_tiles", area.AreaType);
        Assert.True(area.IsUnlocked);
    }

    [Fact]
    public void DefaultProductionArea_WrapsExistingFarmGrid()
    {
        TurnipTestState state = CreateState();

        ProductionArea area = state.GetDefaultProductionArea();

        Assert.Same(state.FarmGrid, area.ProductionGrid.Grid);
    }

    [Fact]
    public void DefaultProductionArea_Dimensions_AndTileCount_MatchFarmGrid()
    {
        TurnipTestState state = CreateState();

        ProductionArea area = state.GetDefaultProductionArea();

        Assert.Equal(state.FarmGrid.Width, area.Width);
        Assert.Equal(state.FarmGrid.Height, area.Height);
        Assert.Equal(state.FarmGrid.Width * state.FarmGrid.Height, area.TileCount);
    }

    [Fact]
    public void DefaultProductionArea_EmptyTileCount_MatchesProductionGrid()
    {
        TurnipTestState state = CreateState();

        Assert.Equal(state.GetProductionGrid().EmptyTileCount, state.GetDefaultProductionArea().EmptyTileCount);
    }

    [Fact]
    public void DefaultProductionArea_MaintainedTileCount_MatchesProductionGrid_AfterMaintainingTile()
    {
        TurnipTestState state = CreateState();
        state.StartSelectedTileProducer();
        state.MaintainSelectedTile();

        Assert.Equal(state.GetProductionGrid().MaintainedTileCount, state.GetDefaultProductionArea().MaintainedTileCount);
    }

    [Fact]
    public void DefaultProductionArea_ReadyTileCount_MatchesProductionGrid_AfterProductionCompletes()
    {
        TurnipTestState state = CreateState();
        state.StartSelectedTileProducer();

        for (int day = 0; day < 4; day++)
        {
            state.MaintainSelectedTile();
            state.AdvanceDay();
        }

        Assert.Equal(state.GetProductionGrid().ReadyToCollectTileCount, state.GetDefaultProductionArea().ReadyToCollectTileCount);
    }

    [Fact]
    public void GetUnlockedProductionAreas_ReturnsOutdoorGarden()
    {
        TurnipTestState state = CreateState();

        ProductionArea area = Assert.Single(state.GetUnlockedProductionAreas());

        Assert.Equal("outdoor_garden", area.Id);
    }

    [Fact]
    public void SaveLoad_RoundTrip_StillExposesOutdoorGardenProductionArea()
    {
        ContentRegistry registry = CreateRegistry();
        TurnipTestState originalState = CreateState();
        originalState.StartSelectedTileProducer();
        originalState.MaintainSelectedTile();
        originalState.AdvanceDay();

        SaveGameFileService service = new();
        SaveGameState saveGame = service.Deserialize(service.Serialize(originalState.CreateSaveGame()));
        TurnipTestState loadedState = new(registry);
        loadedState.ApplySaveGame(saveGame);

        ProductionArea area = loadedState.GetDefaultProductionArea();
        Assert.Equal("outdoor_garden", area.Id);
        Assert.Same(loadedState.FarmGrid, area.ProductionGrid.Grid);
        Assert.Equal(loadedState.GetProductionGrid().MaintainedTileCount, area.MaintainedTileCount);
    }

    [Fact]
    public void ComputerScreenLines_ShowOutdoorGardenProductionArea()
    {
        TurnipTestState state = CreateState();
        ContentRegistry registry = CreateRegistry();

        string[] lines = DebugUiTextFormatter.GetComputerScreenLines(
            registry,
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
            productionAreas: state.GetUnlockedProductionAreas());

        Assert.Contains("PRODUCTION AREAS", lines);
        Assert.Contains("Outdoor Garden: unlocked, 9 tiles", lines);
    }

    private static TurnipTestState CreateState()
    {
        GameStateFactoryResult result = TestContentFactory.CreateThemePackStateFactory().CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
        return result.State;
    }

    private static ContentRegistry CreateRegistry()
    {
        GameStateFactoryResult result = TestContentFactory.CreateThemePackStateFactory().CreateState(ThemePackCatalog.TomatoGrowDefaultManifestId);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors));
        return result.ContentRegistry;
    }
}
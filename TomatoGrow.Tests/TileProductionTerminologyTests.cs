using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class TileProductionTerminologyTests
{
    [Fact]
    public void TileProducerDefinitionView_StarterItemId_MapsToSeedItemId()
    {
        TileProducerDefinitionView producer = new(new CropDefinition { Id = "grape", SeedItemId = "grape_seed", HarvestItemId = "grape", GrowthDays = 6 });

        Assert.Equal("grape_seed", producer.StarterItemId);
    }

    [Fact]
    public void TileProducerDefinitionView_OutputItemId_MapsToHarvestItemId()
    {
        TileProducerDefinitionView producer = new(new CropDefinition { Id = "grape", SeedItemId = "grape_seed", HarvestItemId = "grape", GrowthDays = 6 });

        Assert.Equal("grape", producer.OutputItemId);
    }

    [Fact]
    public void TileProducerDefinitionView_ProductionDays_MapsToGrowthDays()
    {
        TileProducerDefinitionView producer = new(new CropDefinition { Id = "grape", SeedItemId = "grape_seed", HarvestItemId = "grape", GrowthDays = 6 });

        Assert.Equal(6, producer.ProductionDays);
    }

    [Fact]
    public void TryGetProducerByStarterItemId_ResolvesGrapeSeedToGrapeProducer()
    {
        ContentRegistry registry = LoadDefaultRegistry();

        bool found = registry.TryGetProducerByStarterItemId("grape_seed", out TileProducerDefinitionView producer);

        Assert.True(found);
        Assert.Equal("grape", producer.Id);
    }

    [Fact]
    public void TryGetProducerByOutputItemId_ResolvesGrapeToGrapeProducer()
    {
        ContentRegistry registry = LoadDefaultRegistry();

        bool found = registry.TryGetProducerByOutputItemId("grape", out TileProducerDefinitionView producer);

        Assert.True(found);
        Assert.Equal("grape", producer.Id);
        Assert.Equal("grape_seed", producer.StarterItemId);
    }

    [Fact]
    public void GetAllTileProducers_IncludesTurnipPotatoGrapeAndHops()
    {
        ContentRegistry registry = LoadDefaultRegistry();

        string[] producerIds = registry.GetAllTileProducers().Select(producer => producer.Id).ToArray();

        Assert.Contains("turnip", producerIds);
        Assert.Contains("potato", producerIds);
        Assert.Contains("grape", producerIds);
        Assert.Contains("hops", producerIds);
    }

    [Fact]
    public void ProductionTileView_IsMaintained_ReflectsIsWatered()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        state.WaterPlot(0, 0);

        ProductionTileView tile = state.GetProductionTile(0, 0);

        Assert.True(tile.IsMaintained);
    }

    [Fact]
    public void ProductionGridView_WidthAndHeight_MatchFarmGrid()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry);

        ProductionGridView grid = state.GetProductionGrid();

        Assert.Equal(state.FarmGrid.Width, grid.Width);
        Assert.Equal(state.FarmGrid.Height, grid.Height);
    }

    [Fact]
    public void ProductionGridView_TileCount_MatchesWidthTimesHeight()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry);

        ProductionGridView grid = state.GetProductionGrid();

        Assert.Equal(state.FarmGrid.Width * state.FarmGrid.Height, grid.TileCount);
    }

    [Fact]
    public void GetSelectedTile_ReturnsSamePlotAsSelectedCoordinates()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry);
        state.MoveSelectedPlot(1, 2);

        FarmPlot selectedTile = state.GetSelectedTile();

        Assert.Same(state.FarmGrid.GetPlot(state.SelectedPlotX, state.SelectedPlotY), selectedTile);
    }

    [Fact]
    public void ProductionTileView_IsEmpty_IsTrueForEmptyPlot()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry);

        ProductionTileView tile = state.GetProductionGrid().GetTile(0, 0);

        Assert.True(tile.IsEmpty);
        Assert.False(tile.HasProducer);
    }

    [Fact]
    public void ProductionTileView_HasProducer_IsTrueAfterStartingProducer()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry);
        state.StartSelectedTileProducer();

        ProductionTileView tile = state.GetSelectedProductionTile();

        Assert.True(tile.HasProducer);
        Assert.Equal("turnip", tile.Plot.PlantedCrop?.CropDefinitionId);
    }

    [Fact]
    public void ProductionTileView_IsReadyToCollect_ReflectsReadyToHarvest()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        for (int day = 0; day < 4; day++)
        {
            state.WaterPlot(0, 0);
            state.AdvanceDay();
        }

        ProductionTileView tile = state.GetProductionTile(0, 0);

        Assert.True(tile.HasProducer);
        Assert.Equal(4, tile.DaysProduced);
        Assert.Equal(4, tile.RequiredProductionDays);
        Assert.True(tile.IsReadyToCollect);
    }

    [Fact]
    public void ProductionGridView_EmptyTileCount_UpdatesAfterStartingProducer()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry);
        ProductionGridView initialGrid = state.GetProductionGrid();

        state.StartSelectedTileProducer();

        ProductionGridView updatedGrid = state.GetProductionGrid();
        Assert.Equal(initialGrid.TileCount - 1, updatedGrid.EmptyTileCount);
    }

    [Fact]
    public void ProductionGridView_MaintainedTileCount_UpdatesAfterMaintainingTile()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry);
        state.StartSelectedTileProducer();

        state.MaintainSelectedTile();

        Assert.Equal(1, state.GetProductionGrid().MaintainedTileCount);
    }

    [Fact]
    public void ProductionGridView_ReadyToCollectTileCount_UpdatesAfterEnoughMaintainedDays()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry);
        state.StartSelectedTileProducer();

        for (int day = 0; day < 4; day++)
        {
            state.MaintainSelectedTile();
            state.AdvanceDay();
        }

        Assert.Equal(1, state.GetProductionGrid().ReadyToCollectTileCount);
    }

    [Fact]
    public void CollectSelectedTile_UsesSameUnderlyingBehaviorAsHarvestCrop()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState farmingNamedState = CreateReadyTurnipState(registry);
        TurnipTestState genericNamedState = CreateReadyTurnipState(registry);

        string harvestMessage = farmingNamedState.HarvestCrop();
        string collectMessage = genericNamedState.CollectSelectedTile();

        Assert.Equal(harvestMessage, collectMessage);
        Assert.Equal(farmingNamedState.GetItemCount("turnip"), genericNamedState.GetItemCount("turnip"));
        Assert.Equal(farmingNamedState.ActivityLog[^1].Message, genericNamedState.ActivityLog[^1].Message);
    }

    [Fact]
    public void MaintainSelectedTile_UsesSameUnderlyingBehaviorAsWaterSelectedPlot()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState farmingNamedState = new(registry);
        TurnipTestState genericNamedState = new(registry);

        string waterMessage = farmingNamedState.WaterSelectedPlot();
        string maintainMessage = genericNamedState.MaintainSelectedTile();

        Assert.Equal(waterMessage, maintainMessage);
        Assert.Equal(farmingNamedState.GetSelectedPlot().IsWatered, genericNamedState.GetSelectedPlot().IsWatered);
    }

    [Fact]
    public void StartSelectedTileProducer_UsesSameUnderlyingBehaviorAsPlantSelectedSeed()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState farmingNamedState = new(registry);
        TurnipTestState genericNamedState = new(registry);

        string plantMessage = farmingNamedState.PlantSelectedSeed();
        string startMessage = genericNamedState.StartSelectedTileProducer();

        Assert.Equal(plantMessage, startMessage);
        Assert.Equal(farmingNamedState.GetSelectedPlot().PlantedCrop?.CropDefinitionId, genericNamedState.GetSelectedPlot().PlantedCrop?.CropDefinitionId);
        Assert.Equal(farmingNamedState.GetItemCount("turnip_seed"), genericNamedState.GetItemCount("turnip_seed"));
    }

    [Fact]
    public void OldFarmingNamedMethods_StillWorkAlongsideGenericWrappers()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = CreateReadyTurnipState(registry);

        Assert.Equal("Harvested Turnip from 0,0.", state.HarvestCrop());

        state.Reset();

        Assert.Equal("Planted turnip at 0,0.", state.PlantSelectedSeed());
        Assert.Equal("Watered plot 0,0.", state.WaterSelectedPlot());
    }

    [Fact]
    public void MiningDemoUi_StillUsesExtractAndPowerWording()
    {
        ContentRegistry registry = LoadDefaultRegistry();
        TurnipTestState state = new(registry, themeVocabulary: ThemeVocabulary.MiningDemo);
        state.PlantSeedAtPlot(0, 0, "turnip_seed");

        string[] dryHintLines = GetNextHintLines(state, registry);

        for (int day = 0; day < 4; day++)
        {
            state.WaterPlot(0, 0);
            state.AdvanceDay();
        }

        string[] readyHintLines = GetNextHintLines(state, registry);

        Assert.Contains("Power veins with W.", dryHintLines);
        Assert.Contains("Extract selected vein with H.", readyHintLines);
    }

    private static TurnipTestState CreateReadyTurnipState(ContentRegistry registry)
    {
        TurnipTestState state = new(registry);
        state.PlantSelectedSeed();
        for (int day = 0; day < 4; day++)
        {
            state.WaterSelectedPlot();
            state.AdvanceDay();
        }

        return state;
    }

    private static string[] GetNextHintLines(TurnipTestState state, ContentRegistry registry)
    {
        return DebugUiTextFormatter.GetNextHintLines(
            registry,
            state.FarmGrid,
            state.ProcessorMachines,
            state.Money,
            state.SelectedSeedItemId,
            state.IsPlanterBotUnlocked,
            state.IsWateringBotUnlocked,
            state.IsHarvesterBotUnlocked,
            state.IsFarmFullyExpanded,
            state.NextFarmExpansionCost,
            state.IsFinalFarmExpansionBlockedByCleanup,
            state.CleanupSite,
            state.GetItemCount,
            state.GetStorageItemCount,
            state.GetAccessibleItemCount,
            plot => state.IsPlotHarvestable(plot.X, plot.Y),
            state.ActiveMarketOpportunity,
            state.GetEffectiveSellPrice,
            themeVocabulary: state.ThemeVocabulary);
    }

    private static ContentRegistry LoadDefaultRegistry()
    {
        return new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));
    }
}
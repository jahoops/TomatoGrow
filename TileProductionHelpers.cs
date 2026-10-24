using System;
using System.Collections.Generic;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public readonly struct TileProducerDefinitionView
{
    private readonly TileProducerDefinition _tileProducerDefinition;
    private readonly CropDefinition _cropDefinition;

    public TileProducerDefinitionView(CropDefinition cropDefinition)
        : this(
            new TileProducerDefinition
            {
                Id = cropDefinition?.Id ?? string.Empty,
                StarterItemId = cropDefinition?.SeedItemId ?? string.Empty,
                OutputItemId = cropDefinition?.HarvestItemId ?? string.Empty,
                ProductionDays = cropDefinition?.GrowthDays ?? 0
            },
            cropDefinition)
    {
    }

    public TileProducerDefinitionView(TileProducerDefinition tileProducerDefinition, CropDefinition cropDefinition = null)
    {
        _tileProducerDefinition = tileProducerDefinition ?? throw new ArgumentNullException(nameof(tileProducerDefinition));
        _cropDefinition = cropDefinition;
    }

    public CropDefinition CropDefinition => _cropDefinition;
    public TileProducerDefinition TileProducerDefinition => _tileProducerDefinition;
    public string Id => _tileProducerDefinition.Id;
    public string StarterItemId => _tileProducerDefinition.StarterItemId;
    public string OutputItemId => _tileProducerDefinition.OutputItemId;
    public int ProductionDays => _tileProducerDefinition.ProductionDays;
}

public readonly struct ProductionTileView
{
    private readonly FarmPlot _plot;
    private readonly ContentRegistry _contentRegistry;

    public ProductionTileView(FarmPlot plot, ContentRegistry contentRegistry)
    {
        _plot = plot ?? throw new ArgumentNullException(nameof(plot));
        _contentRegistry = contentRegistry ?? throw new ArgumentNullException(nameof(contentRegistry));
    }

    public FarmPlot Plot => _plot;
    public bool IsEmpty => _plot.IsEmpty;
    public bool HasProducer => _plot.PlantedCrop is not null;
    public bool IsMaintained => _plot.IsWatered;
    public int DaysProduced => _plot.PlantedCrop?.DaysGrown ?? 0;
    public int RequiredProductionDays => TryGetProducer(out TileProducerDefinitionView producer)
        ? producer.ProductionDays
        : 0;
    public bool IsReadyToCollect => HasProducer && DaysProduced >= RequiredProductionDays;

    public bool TryGetProducer(out TileProducerDefinitionView producer)
    {
        if (_plot.PlantedCrop is null)
        {
            producer = default;
            return false;
        }

        if (_contentRegistry.TryGetTileProducer(_plot.PlantedCrop.CropDefinitionId, out TileProducerDefinition tileProducer))
        {
            producer = new TileProducerDefinitionView(tileProducer, _contentRegistry.CropsById.TryGetValue(tileProducer.Id, out CropDefinition crop) ? crop : null);
            return true;
        }

        producer = default;
        return false;
    }
}

public readonly struct ProductionGridView
{
    private readonly FarmGrid _grid;
    private readonly ContentRegistry _contentRegistry;

    public ProductionGridView(FarmGrid grid, ContentRegistry contentRegistry)
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _contentRegistry = contentRegistry ?? throw new ArgumentNullException(nameof(contentRegistry));
    }

    public FarmGrid Grid => _grid;
    public int Width => _grid.Width;
    public int Height => _grid.Height;
    public int TileCount => Width * Height;
    public int EmptyTileCount => _grid.GetAllPlots().Count(plot => plot.IsEmpty);
    public int MaintainedTileCount => _grid.GetAllPlots().Count(plot => plot.IsWatered);
    public int ReadyToCollectTileCount
    {
        get
        {
            FarmGrid grid = _grid;
            ContentRegistry contentRegistry = _contentRegistry;
            return grid.GetAllPlots().Count(plot => new ProductionTileView(grid.GetPlot(plot.X, plot.Y), contentRegistry).IsReadyToCollect);
        }
    }

    public ProductionTileView GetTile(int x, int y)
    {
        return new ProductionTileView(_grid.GetPlot(x, y), _contentRegistry);
    }

    public IEnumerable<ProductionTileView> GetAllTiles()
    {
        FarmGrid grid = _grid;
        ContentRegistry contentRegistry = _contentRegistry;
        return grid.GetAllPlots().Select(plot => new ProductionTileView(plot, contentRegistry));
    }
}
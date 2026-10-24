namespace TomatoGrow;

public sealed class FarmPlot
{
    public FarmPlot(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }
    public PlantedCrop PlantedCrop { get; private set; }
    public bool IsWatered { get; private set; }

    public bool IsEmpty => PlantedCrop is null;

    public bool TryPlant(string cropDefinitionId)
    {
        if (!IsEmpty)
            return false;

        PlantedCrop = new PlantedCrop(cropDefinitionId);
        return true;
    }

    public void Water()
    {
        IsWatered = true;
    }

    public void AdvanceDay()
    {
        if (IsWatered)
            PlantedCrop?.AdvanceDay();

        IsWatered = false;
    }

    public PlantedCrop Harvest()
    {
        PlantedCrop harvested = PlantedCrop;
        PlantedCrop = null;
        return harvested;
    }

    public void Clear()
    {
        PlantedCrop = null;
        IsWatered = false;
    }

    public void Restore(string cropDefinitionId, int daysGrown, bool isWatered)
    {
        PlantedCrop = string.IsNullOrWhiteSpace(cropDefinitionId)
            ? null
            : new PlantedCrop(cropDefinitionId, daysGrown);
        IsWatered = isWatered;
    }
}
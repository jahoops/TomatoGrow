using System;

namespace TomatoGrow;

public sealed class PlanterBot
{
    public PlanterBot(FarmRegion region, string assignedSeedItemId)
    {
        Region = region;
        AssignedSeedItemId = assignedSeedItemId;
    }

    public FarmRegion Region { get; private set; }
    public string AssignedSeedItemId { get; set; }

    public void SetRegion(FarmRegion region)
    {
        Region = region;
    }

    public int Run(FarmGrid farmGrid, Func<int, int, bool> tryPlantPlot, Func<bool> canKeepPlanting)
    {
        int plantedPlots = 0;

        int startX = Math.Clamp(Region.X, 0, farmGrid.Width - 1);
        int startY = Math.Clamp(Region.Y, 0, farmGrid.Height - 1);
        int endX = Math.Clamp(Region.X + Region.Width - 1, 0, farmGrid.Width - 1);
        int endY = Math.Clamp(Region.Y + Region.Height - 1, 0, farmGrid.Height - 1);

        if (endX < startX || endY < startY)
            return plantedPlots;

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                if (!canKeepPlanting())
                    return plantedPlots;

                FarmPlot plot = farmGrid.GetPlot(x, y);
                if (!plot.IsEmpty)
                    continue;

                if (tryPlantPlot(x, y))
                    plantedPlots++;
            }
        }

        return plantedPlots;
    }
}
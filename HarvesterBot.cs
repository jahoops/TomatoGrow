using System;

namespace TomatoGrow;

public sealed class HarvesterBot
{
    public HarvesterBot(FarmRegion region)
    {
        Region = region;
    }

    public FarmRegion Region { get; private set; }

    public void SetRegion(FarmRegion region)
    {
        Region = region;
    }

    public int Run(FarmGrid farmGrid, Func<int, int, bool> tryHarvestPlot, Func<int, int, bool> isHarvestable)
    {
        int harvestedPlots = 0;

        int startX = Math.Clamp(Region.X, 0, farmGrid.Width - 1);
        int startY = Math.Clamp(Region.Y, 0, farmGrid.Height - 1);
        int endX = Math.Clamp(Region.X + Region.Width - 1, 0, farmGrid.Width - 1);
        int endY = Math.Clamp(Region.Y + Region.Height - 1, 0, farmGrid.Height - 1);

        if (endX < startX || endY < startY)
            return harvestedPlots;

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                if (!isHarvestable(x, y))
                    continue;

                if (tryHarvestPlot(x, y))
                    harvestedPlots++;
            }
        }

        return harvestedPlots;
    }
}
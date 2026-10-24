using System;

namespace TomatoGrow;

public sealed class WateringBot
{
    public WateringBot(FarmRegion region)
    {
        Region = region;
        Enabled = false;
    }

    public FarmRegion Region { get; private set; }
    public bool Enabled { get; set; }

    public void SetRegion(FarmRegion region)
    {
        Region = region;
    }

    public int Run(FarmGrid farmGrid, Action<int, int> waterPlot)
    {
        int wateredPlots = 0;

        int startX = Math.Clamp(Region.X, 0, farmGrid.Width - 1);
        int startY = Math.Clamp(Region.Y, 0, farmGrid.Height - 1);
        int endX = Math.Clamp(Region.X + Region.Width - 1, 0, farmGrid.Width - 1);
        int endY = Math.Clamp(Region.Y + Region.Height - 1, 0, farmGrid.Height - 1);

        if (endX < startX || endY < startY)
            return wateredPlots;

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                FarmPlot plot = farmGrid.GetPlot(x, y);
                if (plot.IsEmpty || plot.IsWatered)
                    continue;

                waterPlot(x, y);
                wateredPlots++;
            }
        }

        return wateredPlots;
    }
}
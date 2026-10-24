using System;
using System.Collections.Generic;

namespace TomatoGrow;

public sealed class FarmGrid
{
    private FarmPlot[,] _plots;

    public FarmGrid(int width, int height)
    {
        _plots = new FarmPlot[1, 1];
        Resize(width, height);
    }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public void Resize(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        FarmPlot[,] resizedPlots = new FarmPlot[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x < Width && y < Height)
                    resizedPlots[x, y] = _plots[x, y];
                else
                    resizedPlots[x, y] = new FarmPlot(x, y);
            }
        }

        Width = width;
        Height = height;
        _plots = resizedPlots;
    }

    public FarmPlot GetPlot(int x, int y)
    {
        if (!IsInBounds(x, y))
            throw new ArgumentOutOfRangeException($"Plot coordinates out of bounds: ({x}, {y}).");

        return _plots[x, y];
    }

    public IEnumerable<FarmPlot> GetAllPlots()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
                yield return _plots[x, y];
        }
    }

    public void Clear()
    {
        foreach (FarmPlot plot in GetAllPlots())
            plot.Clear();
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
}
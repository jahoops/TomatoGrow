namespace TomatoGrow;

public sealed class FarmRegion
{
    public FarmRegion(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public bool Contains(int x, int y)
    {
        return x >= X && x < X + Width && y >= Y && y < Y + Height;
    }
}
namespace TomatoGrow;

public sealed class ProductionArea
{
    public ProductionArea(
        string id,
        string name,
        string areaType,
        bool isUnlocked,
        ProductionGridView productionGrid,
        int displayOrder = 0,
        string interfaceTokenId = "")
    {
        Id = id ?? string.Empty;
        Name = name ?? string.Empty;
        AreaType = areaType ?? string.Empty;
        IsUnlocked = isUnlocked;
        ProductionGrid = productionGrid;
        DisplayOrder = displayOrder;
        InterfaceTokenId = interfaceTokenId ?? string.Empty;
    }

    public string Id { get; }
    public string Name { get; }
    public string AreaType { get; }
    public bool IsUnlocked { get; }
    public ProductionGridView ProductionGrid { get; }
    public int DisplayOrder { get; }
    public string InterfaceTokenId { get; }

    public int Width => ProductionGrid.Width;
    public int Height => ProductionGrid.Height;
    public int TileCount => ProductionGrid.TileCount;
    public int EmptyTileCount => ProductionGrid.EmptyTileCount;
    public int MaintainedTileCount => ProductionGrid.MaintainedTileCount;
    public int ReadyToCollectTileCount => ProductionGrid.ReadyToCollectTileCount;
}
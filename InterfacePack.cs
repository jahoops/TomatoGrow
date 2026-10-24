using Microsoft.Xna.Framework;

namespace TomatoGrow;

public sealed record InterfacePack
{
    public static InterfacePack FarmingDefault { get; } = new()
    {
        Id = "farming_default_interface",
        Name = "Farming Default Interface",
        EmptyTileSymbol = ".",
        OccupiedTileSymbol = "C",
        ReadyTileSymbol = "R",
        SelectedTileMarker = "S",
        EmptyShortLabel = "EMPTY",
        MaintainedShortLabel = "WET",
        UnmaintainedShortLabel = "DRY",
        ReadyShortLabel = "READY",
        EmptyTileFillColor = new Color(144, 103, 69),
        MaintainedEmptyTileFillColor = new Color(96, 125, 158),
        UnmaintainedTileFillColor = new Color(116, 145, 82),
        MaintainedTileFillColor = new Color(82, 132, 174),
        ReadyTextColor = new Color(184, 36, 36),
        SelectedBorderColor = new Color(255, 224, 128),
        NormalBorderColor = new Color(68, 58, 48)
    };

    public static InterfacePack MiningDemo { get; } = new()
    {
        Id = "mining_demo_interface",
        Name = "Mining Demo Interface",
        EmptyTileSymbol = ".",
        OccupiedTileSymbol = "V",
        ReadyTileSymbol = "X",
        SelectedTileMarker = "M",
        EmptyShortLabel = "OPEN",
        MaintainedShortLabel = "PWRD",
        UnmaintainedShortLabel = "UNPWR",
        ReadyShortLabel = "READY",
        EmptyTileFillColor = new Color(74, 74, 84),
        MaintainedEmptyTileFillColor = new Color(78, 118, 144),
        UnmaintainedTileFillColor = new Color(88, 88, 102),
        MaintainedTileFillColor = new Color(70, 144, 148),
        ReadyTextColor = new Color(212, 110, 38),
        SelectedBorderColor = new Color(255, 245, 164),
        NormalBorderColor = new Color(54, 54, 62)
    };

    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string EmptyTileSymbol { get; init; }
    public required string OccupiedTileSymbol { get; init; }
    public required string ReadyTileSymbol { get; init; }
    public required string SelectedTileMarker { get; init; }
    public required string EmptyShortLabel { get; init; }
    public required string MaintainedShortLabel { get; init; }
    public required string UnmaintainedShortLabel { get; init; }
    public required string ReadyShortLabel { get; init; }
    public required Color EmptyTileFillColor { get; init; }
    public required Color MaintainedEmptyTileFillColor { get; init; }
    public required Color UnmaintainedTileFillColor { get; init; }
    public required Color MaintainedTileFillColor { get; init; }
    public required Color ReadyTextColor { get; init; }
    public required Color SelectedBorderColor { get; init; }
    public required Color NormalBorderColor { get; init; }
}
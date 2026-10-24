using System;

namespace TomatoGrow;

public sealed record ThemeVocabulary
{
    public static ThemeVocabulary FarmingDefault { get; } = new()
    {
        ThemeId = "farming",
        ThemeName = "Farming",
        WorldName = "Farm",
        WorldPluralName = "Farms",
        TileName = "Plot",
        TilePluralName = "Plots",
        StarterItemName = "Seed",
        StarterItemPluralName = "Seeds",
        ProducerName = "Crop",
        ProducerPluralName = "Crops",
        MaintainActionName = "Water",
        MaintainerBotName = "Watering Bot",
        MaintainedStateName = "Wet",
        UnmaintainedStateName = "Dry",
        CollectActionName = "Harvest",
        CollectorBotName = "Harvester Bot",
        StarterBotName = "Planter Bot",
        ProcessingName = "Production",
        StorageName = "Storage",
        MarketName = "Supply Market",
        RestorationName = "Restoration",
        ComputerName = "Farm Computer",
        ProductionReportName = "Production Report",
        ActivityLogName = "Activity Log"
    };

    public static ThemeVocabulary MiningDemo { get; } = new()
    {
        ThemeId = "mining_demo",
        ThemeName = "Mining Demo",
        WorldName = "Mine",
        WorldPluralName = "Mines",
        TileName = "Claim",
        TilePluralName = "Claims",
        StarterItemName = "Marker",
        StarterItemPluralName = "Markers",
        ProducerName = "Vein",
        ProducerPluralName = "Veins",
        MaintainActionName = "Power",
        MaintainerBotName = "Power Bot",
        MaintainedStateName = "Powered",
        UnmaintainedStateName = "Unpowered",
        CollectActionName = "Extract",
        CollectorBotName = "Extractor Bot",
        StarterBotName = "Claim Bot",
        ProcessingName = "Processing",
        StorageName = "Storage",
        MarketName = "Supply Market",
        RestorationName = "Restoration",
        ComputerName = "Operations Computer",
        ProductionReportName = "Production Report",
        ActivityLogName = "Activity Log"
    };

    public required string ThemeId { get; init; }
    public required string ThemeName { get; init; }
    public required string WorldName { get; init; }
    public required string WorldPluralName { get; init; }
    public required string TileName { get; init; }
    public required string TilePluralName { get; init; }
    public required string StarterItemName { get; init; }
    public required string StarterItemPluralName { get; init; }
    public required string ProducerName { get; init; }
    public required string ProducerPluralName { get; init; }
    public required string MaintainActionName { get; init; }
    public required string MaintainerBotName { get; init; }
    public required string MaintainedStateName { get; init; }
    public required string UnmaintainedStateName { get; init; }
    public required string CollectActionName { get; init; }
    public required string CollectorBotName { get; init; }
    public required string StarterBotName { get; init; }
    public required string ProcessingName { get; init; }
    public required string StorageName { get; init; }
    public required string MarketName { get; init; }
    public required string RestorationName { get; init; }
    public required string ComputerName { get; init; }
    public required string ProductionReportName { get; init; }
    public required string ActivityLogName { get; init; }

    public string ToUpperLabel(string label)
    {
        return string.IsNullOrWhiteSpace(label) ? string.Empty : label.ToUpperInvariant();
    }

    public string GetTileSelectionLabel()
    {
        return $"Selected {TileName}";
    }

    public string GetWorldControlsLabel()
    {
        return $"{WorldName} Controls";
    }
}
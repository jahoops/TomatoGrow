using System;

namespace TomatoGrow;

public static class ThemeStatLabelFormatter
{
    public static string GetStarterItemsBoughtLabel(ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);
        return UsesLegacyFarmingLabels(theme) ? "Seeds bought" : $"{theme.StarterItemPluralName} bought";
    }

    public static string GetMoneySpentOnStarterItemsLabel(ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);
        return UsesLegacyFarmingLabels(theme) ? "Spent on seeds" : $"Spent on {theme.StarterItemPluralName.ToLowerInvariant()}";
    }

    public static string GetProducersStartedLabel(ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);
        return UsesLegacyFarmingLabels(theme) ? "Crops planted" : $"{theme.ProducerPluralName} started";
    }

    public static string GetProducersCollectedLabel(ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);
        return UsesLegacyFarmingLabels(theme)
            ? "Crops harvested"
            : $"{theme.ProducerPluralName} {ToPastTense(theme.CollectActionName).ToLowerInvariant()}";
    }

    public static string GetTilesMaintainedLabel(ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);
        return UsesLegacyFarmingLabels(theme)
            ? "Watered plots"
            : $"{ToPastTense(theme.MaintainActionName)} {theme.TilePluralName.ToLowerInvariant()}";
    }

    public static string GetManualStartedLabel(ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);
        return UsesLegacyFarmingLabels(theme)
            ? "Crops planted manually"
            : $"{theme.ProducerPluralName} started manually";
    }

    public static string GetManualMaintainedLabel(ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);
        return UsesLegacyFarmingLabels(theme)
            ? "Plots watered manually"
            : $"{ToPastTense(theme.MaintainActionName)} {theme.TilePluralName.ToLowerInvariant()} manually";
    }

    public static string GetManualCollectedLabel(ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);
        return UsesLegacyFarmingLabels(theme)
            ? "Crops harvested manually"
            : $"{theme.ProducerPluralName} {ToPastTense(theme.CollectActionName).ToLowerInvariant()} manually";
    }

    public static string GetStarterBotActionLabel(ThemeVocabulary themeVocabulary)
    {
        return BotRoleFormatter.GetBotStatLabel(BotRole.Starter, themeVocabulary);
    }

    public static string GetMaintainerBotActionLabel(ThemeVocabulary themeVocabulary)
    {
        return BotRoleFormatter.GetBotStatLabel(BotRole.Maintainer, themeVocabulary);
    }

    public static string GetCollectorBotActionLabel(ThemeVocabulary themeVocabulary)
    {
        return BotRoleFormatter.GetBotStatLabel(BotRole.Collector, themeVocabulary);
    }

    public static string GetStarterSafetyLabel(ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);
        return UsesLegacyFarmingLabels(theme) ? "Seed safety" : $"{theme.StarterItemName} safety";
    }

    public static string GetStarterItemsAtEndLabel(ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);
        return UsesLegacyFarmingLabels(theme) ? "Seeds at end" : $"{theme.StarterItemPluralName} at end";
    }

    public static string GetMilestoneDescription(ProgressionMilestone milestone, ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);

        return milestone.Type switch
        {
            ProgressionMilestoneType.FirstSeedBought => UsesLegacyFarmingLabels(theme)
                ? "First seed bought"
                : $"First {theme.StarterItemName.ToLowerInvariant()} bought",
            ProgressionMilestoneType.FirstCropPlanted => UsesLegacyFarmingLabels(theme)
                ? "First crop planted"
                : $"First {theme.ProducerName.ToLowerInvariant()} started",
            ProgressionMilestoneType.FirstCropHarvested => UsesLegacyFarmingLabels(theme)
                ? "First crop harvested"
                : $"First {theme.ProducerName.ToLowerInvariant()} {ToPastTense(theme.CollectActionName).ToLowerInvariant()}",
            ProgressionMilestoneType.FirstCropSold => UsesLegacyFarmingLabels(theme)
                ? "First crop sold"
                : $"First {theme.ProducerName.ToLowerInvariant()} sold",
            ProgressionMilestoneType.PlanterBotUnlocked => $"{BotRoleFormatter.GetBotDisplayName(BotRole.Starter, theme)} unlocked",
            ProgressionMilestoneType.WateringBotUnlocked => $"{BotRoleFormatter.GetBotDisplayName(BotRole.Maintainer, theme)} unlocked",
            ProgressionMilestoneType.HarvesterBotUnlocked => $"{BotRoleFormatter.GetBotDisplayName(BotRole.Collector, theme)} unlocked",
            _ => milestone.Description
        };
    }

    public static string GetMilestoneSummaryLabel(ProgressionMilestoneType milestoneType, ThemeVocabulary themeVocabulary)
    {
        ThemeVocabulary theme = Normalize(themeVocabulary);

        return milestoneType switch
        {
            ProgressionMilestoneType.FirstSeedBought => UsesLegacyFarmingLabels(theme)
                ? "First seed bought"
                : $"First {theme.StarterItemName.ToLowerInvariant()} bought",
            ProgressionMilestoneType.FirstCropPlanted => UsesLegacyFarmingLabels(theme)
                ? "First crop planted"
                : $"First {theme.ProducerName.ToLowerInvariant()} started",
            ProgressionMilestoneType.FirstCropSold => UsesLegacyFarmingLabels(theme)
                ? "First crop sold"
                : $"First {theme.ProducerName.ToLowerInvariant()} sold",
            ProgressionMilestoneType.PlanterBotUnlocked => $"{BotRoleFormatter.GetBotDisplayName(BotRole.Starter, theme)} unlocked",
            ProgressionMilestoneType.WateringBotUnlocked => $"{BotRoleFormatter.GetBotDisplayName(BotRole.Maintainer, theme)} unlocked",
            ProgressionMilestoneType.HarvesterBotUnlocked => $"{BotRoleFormatter.GetBotDisplayName(BotRole.Collector, theme)} unlocked",
            _ => milestoneType.ToString()
        };
    }

    private static ThemeVocabulary Normalize(ThemeVocabulary themeVocabulary)
    {
        return themeVocabulary ?? ThemeVocabulary.FarmingDefault;
    }

    private static bool UsesLegacyFarmingLabels(ThemeVocabulary themeVocabulary)
    {
        return string.Equals(
            Normalize(themeVocabulary).ThemeId,
            ThemeVocabulary.FarmingDefault.ThemeId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ToPastTense(string verb)
    {
        if (string.IsNullOrWhiteSpace(verb))
            return "Done";

        return verb.EndsWith("e", StringComparison.OrdinalIgnoreCase)
            ? verb + "d"
            : verb + "ed";
    }

}
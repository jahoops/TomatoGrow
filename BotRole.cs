using System;

namespace TomatoGrow;

public enum BotRole
{
    Starter,
    Maintainer,
    Collector
}

public static class BotRoleFormatter
{
    public static string GetBotDisplayName(BotRole role, ThemeVocabulary vocabulary = null)
    {
        ThemeVocabulary theme = Normalize(vocabulary);
        return role switch
        {
            BotRole.Starter => theme.StarterBotName,
            BotRole.Maintainer => theme.MaintainerBotName,
            BotRole.Collector => theme.CollectorBotName,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported bot role.")
        };
    }

    public static string GetBotActionVerb(BotRole role, ThemeVocabulary vocabulary = null)
    {
        ThemeVocabulary theme = Normalize(vocabulary);

        return role switch
        {
            BotRole.Starter => UsesLegacyFarmingLabels(theme) ? "plants" : "starts",
            BotRole.Maintainer => ToThirdPerson(theme.MaintainActionName),
            BotRole.Collector => ToThirdPerson(theme.CollectActionName),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported bot role.")
        };
    }

    public static string GetBotStatLabel(BotRole role, ThemeVocabulary vocabulary = null)
    {
        return $"{GetBotDisplayName(role, vocabulary)} {GetBotActionVerb(role, vocabulary)}";
    }

    public static string GetBotUnlockKey(BotRole role)
    {
        return role switch
        {
            BotRole.Starter => "1",
            BotRole.Maintainer => "2",
            BotRole.Collector => "3",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported bot role.")
        };
    }

    public static string GetBotRunKey(BotRole role)
    {
        return role switch
        {
            BotRole.Starter => "V",
            BotRole.Maintainer => "B",
            BotRole.Collector => "X",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported bot role.")
        };
    }

    public static int GetBotCost(BotRole role, GameBalanceConfig balanceConfig = null)
    {
        GameBalanceConfig config = balanceConfig ?? GameBalanceConfig.Default;
        return role switch
        {
            BotRole.Starter => config.PlanterBotCost,
            BotRole.Maintainer => config.WateringBotCost,
            BotRole.Collector => config.HarvesterBotCost,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported bot role.")
        };
    }

    public static string GetBotShortDisplayName(BotRole role, ThemeVocabulary vocabulary = null)
    {
        return GetBotDisplayName(role, vocabulary).Replace(" Bot", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static ThemeVocabulary Normalize(ThemeVocabulary vocabulary)
    {
        return vocabulary ?? ThemeVocabulary.FarmingDefault;
    }

    private static bool UsesLegacyFarmingLabels(ThemeVocabulary vocabulary)
    {
        return string.Equals(
            Normalize(vocabulary).ThemeId,
            ThemeVocabulary.FarmingDefault.ThemeId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ToThirdPerson(string verb)
    {
        if (string.IsNullOrWhiteSpace(verb))
            return "acts";

        string normalizedVerb = verb.ToLowerInvariant();
        return normalizedVerb.EndsWith("s", StringComparison.OrdinalIgnoreCase)
            ? normalizedVerb
            : normalizedVerb + "s";
    }
}
using System;
using System.Collections.Generic;

namespace TomatoGrow;

public static class ProgressionReportFormatter
{
    public static string[] Format(ProgressionSimulationResult result, ThemeVocabulary themeVocabulary = null)
    {
        if (result is null)
            return ["No progression report yet. Press R to run simulation."];

        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;

        List<string> lines =
        [
            "PROGRESSION REPORT",
            $"Stop reason: {FormatStopReason(result)}",
            $"Days simulated: {result.DaysSimulated}",
            $"Steps run: {result.StepsRun}",
            $"Final money: ${result.FinalMoney}",
            $"Current sellable value: ${result.CurrentSellableValue}",
            $"Potential money: ${result.PotentialMoney}",
            string.Empty,
            "MILESTONES"
        ];

        if (result.Milestones.Count == 0)
        {
            lines.Add("Not reached yet.");
        }
        else
        {
            foreach (ProgressionMilestone milestone in result.Milestones)
                lines.Add($"Day {milestone.Day}: {ThemeStatLabelFormatter.GetMilestoneDescription(milestone, theme)}");
        }

        lines.Add(string.Empty);
        lines.Add("ECONOMY");
        lines.Add($"Earned from sales: ${result.MoneyEarnedFromSales}");
        lines.Add($"{ThemeStatLabelFormatter.GetMoneySpentOnStarterItemsLabel(theme)}: ${result.MoneySpentOnSeeds}");
        lines.Add($"Spent on bots: ${result.MoneySpentOnBots}");
        lines.Add($"Spent on expansion: ${result.MoneySpentOnExpansion}");
        lines.Add($"Spent on cleanup: ${result.MoneySpentOnCleanup}");
        lines.Add($"Spent on market: ${result.MoneySpentOnMarket}");
        lines.Add($"Net retained money: ${result.NetMoneyChange}");
        lines.Add($"Average net money per day: {result.AverageNetMoneyPerDay:0.0}");

        lines.Add(string.Empty);
        lines.Add("AUTOMATION");
        lines.Add($"Bots unlocked: {result.BotsUnlocked}");
        lines.Add($"Bot actions saved: {result.BotActionsSaved}");
        lines.Add($"{ThemeStatLabelFormatter.GetManualStartedLabel(theme)}: {result.ManualPlantActions}");
        lines.Add($"{ThemeStatLabelFormatter.GetManualMaintainedLabel(theme)}: {result.ManualWaterActions}");
        lines.Add($"{ThemeStatLabelFormatter.GetManualCollectedLabel(theme)}: {result.ManualHarvestActions}");
        lines.Add($"{ThemeStatLabelFormatter.GetStarterBotActionLabel(theme)}: {result.PlanterBotActions}");
        lines.Add($"{ThemeStatLabelFormatter.GetMaintainerBotActionLabel(theme)}: {result.WateringBotActions}");
        lines.Add($"{ThemeStatLabelFormatter.GetCollectorBotActionLabel(theme)}: {result.HarvesterBotActions}");

        lines.Add(string.Empty);
        lines.Add("PROCESSING");
        lines.Add($"Machine runs: {result.MachineRunsStarted}");
        lines.Add($"Outputs collected: {result.MachineOutputsCollected}");
        lines.Add($"Processed goods: {result.ProcessedGoodsProduced}");
        lines.Add($"Machine value added: {FormatSignedCurrency(result.MachineValueAdded)}");
        lines.Add($"Average value added per run: {FormatSignedCurrency(result.AverageMachineValueAddedPerRun)}");
        lines.Add($"Market purchases: {result.MarketPurchases} items {result.MarketItemsBought} spent ${result.MarketMoneySpent}");
        lines.Add($"Activity log entries: {result.ActivityLogEntriesCount}");

        lines.Add(string.Empty);
        lines.Add("MARKET OPPORTUNITIES");
        lines.Add($"Generated: {result.MarketOpportunitiesGenerated}");
        lines.Add($"First opportunity day: {(result.FirstGeneratedMarketOpportunityDay.HasValue ? result.FirstGeneratedMarketOpportunityDay.Value : 0)}");
        lines.Add($"First boosted sale day: {(result.FirstBoostedSaleDay.HasValue ? result.FirstBoostedSaleDay.Value : 0)}");
        lines.Add($"Active at end: {(string.IsNullOrWhiteSpace(result.ActiveMarketOpportunityAtEnd) ? "none" : result.ActiveMarketOpportunityAtEnd)}");

        lines.Add(string.Empty);
        lines.Add("HEALTH");
        lines.Add($"Invariant status: {(result.InvariantStatusOk ? "OK" : "Issue detected")}");
        if (!result.InvariantStatusOk)
            lines.AddRange(result.InvariantErrors);
        lines.Add($"{ThemeStatLabelFormatter.GetStarterSafetyLabel(theme)}: {result.GetSeedSafetyStatusText()}");
        lines.Add($"{ThemeStatLabelFormatter.GetStarterItemsAtEndLabel(theme)}: {result.FinalSeedCount}/{result.RequiredSeedSafetyCount}");
        lines.Add($"Stalled: {(result.Stalled ? "yes" : "no")}");
        lines.Add($"Loop health: {result.GetLoopHealthStatusText()}");

        return lines.ToArray();
    }

    private static string FormatStopReason(ProgressionSimulationResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.StopReasonText))
            return result.StopReasonText;

        return result.StopReason switch
        {
            AutoPlayStopReason.TargetMoneyReached => "Target money reached",
            AutoPlayStopReason.MaxDaysReached => "Stopped at max days",
            AutoPlayStopReason.MaxStepsReached => "Stopped at max steps",
            AutoPlayStopReason.InvalidState => "Invariant failure detected",
            AutoPlayStopReason.NoUsefulActionPossible => "No useful action possible",
            AutoPlayStopReason.StoppedByUser => "Stopped manually",
            _ => "Not reached"
        };
    }

    private static string FormatSignedCurrency(int amount)
    {
        return amount >= 0 ? $"+${amount}" : $"-${Math.Abs(amount)}";
    }

    private static string FormatSignedCurrency(double amount)
    {
        double rounded = Math.Round(amount, 1, MidpointRounding.AwayFromZero);
        return rounded >= 0 ? $"+${rounded:0.0}" : $"-${Math.Abs(rounded):0.0}";
    }
}
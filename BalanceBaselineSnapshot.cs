using System;
using System.Collections.Generic;
using TomatoGrow.GameContent;

namespace TomatoGrow;

public sealed record BalanceBaselineSnapshot
{
    public const string MilestoneName = "Balance Baseline Snapshot v1";

    public required GameBalanceConfig BalanceConfig { get; init; }
    public required ProgressionSimulationResult SimulationResult { get; init; }
    public required string StopReasonText { get; init; }
    public required AutoPlayStopReason StopReason { get; init; }
    public required int DaysSimulated { get; init; }
    public required int StepsRun { get; init; }
    public required int FinalMoney { get; init; }
    public required int PotentialMoney { get; init; }
    public required int? FirstCropPlantedDay { get; init; }
    public required int? FirstCropSoldDay { get; init; }
    public required int? FirstSeedBoughtDay { get; init; }
    public required int? PlanterBotUnlockDay { get; init; }
    public required int? WateringBotUnlockDay { get; init; }
    public required int? HarvesterBotUnlockDay { get; init; }
    public required int? FirstExpansionDay { get; init; }
    public required int? OldJunkPileClearedDay { get; init; }
    public required int? FinalExpansionDay { get; init; }
    public required int? FirstMachineRunDay { get; init; }
    public required int? FirstMachineOutputCollectedDay { get; init; }
    public required int? FirstProcessedGoodSoldDay { get; init; }
    public required int? FirstAgedGoodSoldDay { get; init; }
    public required int? FirstMarketPurchaseDay { get; init; }
    public required int MachineValueAdded { get; init; }
    public required int MarketMoneySpent { get; init; }
    public required int MarketPurchases { get; init; }
    public required int BotsUnlocked { get; init; }
    public required int ExpansionsCompleted { get; init; }
    public required int CleanupSitesCleared { get; init; }
    public required bool InvariantStatusOk { get; init; }
    public required bool Stalled { get; init; }

    public string HealthStatus => InvariantStatusOk && !Stalled ? "OK" : "Review";

    public static BalanceBaselineSnapshot Capture(ContentRegistry registry, GameBalanceConfig balanceConfig = null)
    {
        GameBalanceConfig effectiveConfig = balanceConfig ?? GameBalanceConfig.Default;
        ProgressionSimulationRunner runner = new(registry, effectiveConfig);
        ProgressionSimulationResult result = runner.Run();
        return FromSimulationResult(result, effectiveConfig);
    }

    public static BalanceBaselineSnapshot FromSimulationResult(ProgressionSimulationResult result, GameBalanceConfig balanceConfig = null)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        GameBalanceConfig effectiveConfig = balanceConfig ?? GameBalanceConfig.Default;
        string stopReasonText = !string.IsNullOrWhiteSpace(result.StopReasonText)
            ? result.StopReasonText
            : result.StopReason.ToString();

        return new BalanceBaselineSnapshot
        {
            BalanceConfig = effectiveConfig,
            SimulationResult = result,
            StopReasonText = stopReasonText,
            StopReason = result.StopReason,
            DaysSimulated = result.DaysSimulated,
            StepsRun = result.StepsRun,
            FinalMoney = result.FinalMoney,
            PotentialMoney = result.PotentialMoney,
            FirstCropPlantedDay = result.GetMilestoneDay(ProgressionMilestoneType.FirstCropPlanted),
            FirstCropSoldDay = result.GetMilestoneDay(ProgressionMilestoneType.FirstCropSold),
            FirstSeedBoughtDay = result.GetMilestoneDay(ProgressionMilestoneType.FirstSeedBought),
            PlanterBotUnlockDay = result.GetMilestoneDay(ProgressionMilestoneType.PlanterBotUnlocked),
            WateringBotUnlockDay = result.GetMilestoneDay(ProgressionMilestoneType.WateringBotUnlocked),
            HarvesterBotUnlockDay = result.GetMilestoneDay(ProgressionMilestoneType.HarvesterBotUnlocked),
            FirstExpansionDay = result.GetMilestoneDay(ProgressionMilestoneType.FirstFarmExpansion),
            OldJunkPileClearedDay = result.GetMilestoneDay(ProgressionMilestoneType.OldJunkPileCleared),
            FinalExpansionDay = result.GetMilestoneDay(ProgressionMilestoneType.FinalFarmExpansion),
            FirstMachineRunDay = result.GetMilestoneDay(ProgressionMilestoneType.FirstMachineRunStarted),
            FirstMachineOutputCollectedDay = result.GetMilestoneDay(ProgressionMilestoneType.FirstMachineOutputCollected),
            FirstProcessedGoodSoldDay = result.GetMilestoneDay(ProgressionMilestoneType.FirstProcessedGoodSold),
            FirstAgedGoodSoldDay = result.GetMilestoneDay(ProgressionMilestoneType.FirstAgedGoodSold),
            FirstMarketPurchaseDay = result.GetMilestoneDay(ProgressionMilestoneType.FirstMarketPurchase),
            MachineValueAdded = result.MachineValueAdded,
            MarketMoneySpent = result.MarketMoneySpent,
            MarketPurchases = result.MarketPurchases,
            BotsUnlocked = result.BotsUnlocked,
            ExpansionsCompleted = result.FarmExpansionsPurchased,
            CleanupSitesCleared = result.CleanupSitesCleared,
            InvariantStatusOk = result.InvariantStatusOk,
            Stalled = result.Stalled
        };
    }
}

public static class BalanceBaselineReportFormatter
{
    public static string[] Format(BalanceBaselineSnapshot snapshot, ThemeVocabulary themeVocabulary = null)
    {
        if (snapshot is null)
            return ["No balance baseline snapshot available."];

        ThemeVocabulary theme = themeVocabulary ?? ThemeVocabulary.FarmingDefault;

        List<string> lines =
        [
            "BALANCE BASELINE",
            $"Milestone: {BalanceBaselineSnapshot.MilestoneName}",
            $"Stop reason: {snapshot.StopReasonText}",
            $"Days simulated: {snapshot.DaysSimulated}",
            $"Steps run: {snapshot.StepsRun}",
            $"Final money: ${snapshot.FinalMoney}",
            $"Potential money: ${snapshot.PotentialMoney}",
            $"Bots unlocked: {snapshot.BotsUnlocked}",
            $"Expansions: {snapshot.ExpansionsCompleted}",
            $"Cleanup cleared: {snapshot.CleanupSitesCleared}",
            $"Machine value added: {FormatSignedCurrency(snapshot.MachineValueAdded)}",
            $"Health: {snapshot.HealthStatus}",
            string.Empty,
            "MILESTONES",
            $"{ThemeStatLabelFormatter.GetMilestoneSummaryLabel(ProgressionMilestoneType.FirstCropPlanted, theme)}: {FormatDay(snapshot.FirstCropPlantedDay)}",
            $"{ThemeStatLabelFormatter.GetMilestoneSummaryLabel(ProgressionMilestoneType.FirstCropSold, theme)}: {FormatDay(snapshot.FirstCropSoldDay)}",
            $"{ThemeStatLabelFormatter.GetMilestoneSummaryLabel(ProgressionMilestoneType.FirstSeedBought, theme)}: {FormatDay(snapshot.FirstSeedBoughtDay)}",
            $"{ThemeStatLabelFormatter.GetMilestoneSummaryLabel(ProgressionMilestoneType.PlanterBotUnlocked, theme)}: {FormatDay(snapshot.PlanterBotUnlockDay)}",
            $"{ThemeStatLabelFormatter.GetMilestoneSummaryLabel(ProgressionMilestoneType.WateringBotUnlocked, theme)}: {FormatDay(snapshot.WateringBotUnlockDay)}",
            $"{ThemeStatLabelFormatter.GetMilestoneSummaryLabel(ProgressionMilestoneType.HarvesterBotUnlocked, theme)}: {FormatDay(snapshot.HarvesterBotUnlockDay)}",
            $"First expansion: {FormatDay(snapshot.FirstExpansionDay)}",
            $"Old Junk Pile cleared: {FormatDay(snapshot.OldJunkPileClearedDay)}",
            $"Final 5x3 expansion: {FormatDay(snapshot.FinalExpansionDay)}",
            $"First machine run: {FormatDay(snapshot.FirstMachineRunDay)}",
            $"First machine output collected: {FormatDay(snapshot.FirstMachineOutputCollectedDay)}",
            $"First processed good sold: {FormatDay(snapshot.FirstProcessedGoodSoldDay)}",
            $"First aged good sold: {FormatDay(snapshot.FirstAgedGoodSoldDay)}",
            $"First market purchase: {FormatDay(snapshot.FirstMarketPurchaseDay)}",
            string.Empty,
            "ECONOMY",
            $"Final money: ${snapshot.FinalMoney}",
            $"Potential money: ${snapshot.PotentialMoney}",
            $"Market money spent: ${snapshot.MarketMoneySpent}",
            string.Empty,
            "AUTOMATION",
            $"Bots unlocked: {snapshot.BotsUnlocked}",
            $"Expansions completed: {snapshot.ExpansionsCompleted}",
            $"Cleanup sites cleared: {snapshot.CleanupSitesCleared}",
            string.Empty,
            "PROCESSING",
            $"Machine value added: {FormatSignedCurrency(snapshot.MachineValueAdded)}",
            $"Market purchases: {snapshot.MarketPurchases}",
            string.Empty,
            "HEALTH",
            $"Invariant status: {(snapshot.InvariantStatusOk ? "OK" : "Issue detected")}",
            $"Stalled: {(snapshot.Stalled ? "yes" : "no")}",
            $"Stop reason: {snapshot.StopReasonText}"
        ];

        return lines.ToArray();
    }

    private static string FormatDay(int? day)
    {
        return day.HasValue ? $"Day {day.Value}" : "Not reached";
    }

    private static string FormatSignedCurrency(int amount)
    {
        return amount >= 0 ? $"+${amount}" : $"-${Math.Abs(amount)}";
    }
}
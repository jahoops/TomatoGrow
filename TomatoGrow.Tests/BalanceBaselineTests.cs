using System;
using System.IO;
using System.Linq;
using TomatoGrow.GameContent;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class BalanceBaselineTests
{
    private static readonly Lazy<BalanceBaselineSnapshot> BaselineSnapshot = new(CreateBaselineSnapshot);

    [Fact]
    public void BalanceBaseline_RunsWithDefaultGameBalanceConfig()
    {
        BalanceBaselineSnapshot snapshot = BaselineSnapshot.Value;

        Assert.Equal(GameBalanceConfig.Default, snapshot.BalanceConfig);
        Assert.NotNull(snapshot.SimulationResult);
        Assert.True(snapshot.DaysSimulated > 0);
        Assert.True(snapshot.StepsRun > 0);
    }

    [Fact]
    public void BalanceBaseline_StopsCleanly()
    {
        BalanceBaselineSnapshot snapshot = BaselineSnapshot.Value;

        Assert.Equal(AutoPlayStopReason.TargetMoneyReached, snapshot.StopReason);
        Assert.NotEqual(AutoPlayStopReason.NoUsefulActionPossible, snapshot.StopReason);
    }

    [Fact]
    public void BalanceBaseline_InvariantStatus_IsOk()
    {
        Assert.True(BaselineSnapshot.Value.InvariantStatusOk);
    }

    [Fact]
    public void BalanceBaseline_UnlocksAllThreeBots()
    {
        BalanceBaselineSnapshot snapshot = BaselineSnapshot.Value;

        Assert.Equal(3, snapshot.BotsUnlocked);
        Assert.NotNull(snapshot.PlanterBotUnlockDay);
        Assert.NotNull(snapshot.WateringBotUnlockDay);
        Assert.NotNull(snapshot.HarvesterBotUnlockDay);
        Assert.True(snapshot.PlanterBotUnlockDay <= snapshot.WateringBotUnlockDay);
        Assert.True(snapshot.WateringBotUnlockDay <= snapshot.HarvesterBotUnlockDay);
    }

    [Fact]
    public void BalanceBaseline_ClearsOldJunkPile_AndExpandsAfterCleanup()
    {
        BalanceBaselineSnapshot snapshot = BaselineSnapshot.Value;

        Assert.True(snapshot.CleanupSitesCleared >= 1);
        Assert.NotNull(snapshot.OldJunkPileClearedDay);
        Assert.NotNull(snapshot.FinalExpansionDay);
        Assert.True(snapshot.ExpansionsCompleted >= 2);
        Assert.True(snapshot.OldJunkPileClearedDay <= snapshot.FinalExpansionDay);
    }

    [Fact]
    public void BalanceBaseline_StartsAndCollectsMachineWork()
    {
        BalanceBaselineSnapshot snapshot = BaselineSnapshot.Value;

        Assert.True(snapshot.SimulationResult.MachineRunsStarted > 0);
        Assert.True(snapshot.SimulationResult.MachineOutputsCollected > 0);
        Assert.NotNull(snapshot.FirstMachineRunDay);
        Assert.NotNull(snapshot.FirstMachineOutputCollectedDay);
        Assert.True(snapshot.FirstMachineRunDay <= snapshot.FirstMachineOutputCollectedDay);
    }

    [Fact]
    public void BalanceBaseline_SellsProcessedGoods()
    {
        BalanceBaselineSnapshot snapshot = BaselineSnapshot.Value;

        Assert.NotNull(snapshot.FirstProcessedGoodSoldDay);
        Assert.True(snapshot.SimulationResult.ProcessedGoodsProduced > 0);
        Assert.True(snapshot.MachineValueAdded > 0);
    }

    [Fact]
    public void BalanceBaseline_UsesTheMarket()
    {
        BalanceBaselineSnapshot snapshot = BaselineSnapshot.Value;

        Assert.True(snapshot.MarketPurchases >= 1);
        Assert.NotNull(snapshot.FirstMarketPurchaseDay);
        Assert.True(snapshot.MarketMoneySpent > 0);
    }

    [Fact]
    public void BalanceBaseline_FinishesWithNonNegativeMoney_AndNoStall()
    {
        BalanceBaselineSnapshot snapshot = BaselineSnapshot.Value;

        Assert.True(snapshot.FinalMoney >= 0);
        Assert.True(snapshot.PotentialMoney >= snapshot.FinalMoney);
        Assert.False(snapshot.Stalled);
    }

    [Fact]
    public void BalanceBaseline_ReportFormatter_IncludesCoreSections()
    {
        string[] lines = BalanceBaselineReportFormatter.Format(BaselineSnapshot.Value);
        Console.WriteLine(string.Join(Environment.NewLine, lines));

        Assert.Contains("BALANCE BASELINE", lines);
        Assert.Contains("MILESTONES", lines);
        Assert.Contains("ECONOMY", lines);
        Assert.Contains("AUTOMATION", lines);
        Assert.Contains("PROCESSING", lines);
        Assert.Contains("HEALTH", lines);
        Assert.Contains(lines, line => line.StartsWith("Stop reason:", StringComparison.Ordinal));
    }

    [Fact]
    public void BalanceBaseline_ReportFormatter_MiningDemo_UsesThemeAwareMilestoneLabels()
    {
        string[] lines = BalanceBaselineReportFormatter.Format(BaselineSnapshot.Value, ThemeVocabulary.MiningDemo);

        Assert.Contains(lines, line => line.StartsWith("First vein started:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("First marker bought:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("Claim Bot unlocked:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("Power Bot unlocked:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("Extractor Bot unlocked:", StringComparison.Ordinal));
    }

    [Fact]
    public void BalanceBaseline_DocumentationFile_Exists()
    {
        string docPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "TomatoGrow_Balance_Baseline.md"));

        Assert.True(File.Exists(docPath));
        string docText = File.ReadAllText(docPath);
        Assert.Contains("Balance Baseline Snapshot v1", docText, StringComparison.Ordinal);
        Assert.Contains("baseline progression summary", docText, StringComparison.OrdinalIgnoreCase);
    }

    private static BalanceBaselineSnapshot CreateBaselineSnapshot()
    {
        ContentRegistry registry = new ContentModuleLoader().LoadFromModules(Path.Combine(AppContext.BaseDirectory, "ContentModules"));
        return BalanceBaselineSnapshot.Capture(registry, GameBalanceConfig.Default);
    }
}
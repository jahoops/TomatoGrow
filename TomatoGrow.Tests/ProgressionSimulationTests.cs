using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class ProgressionSimulationTests
{
    [Fact]
    public void ProgressionSimulation_Run_DoesNotMutateLiveStateOrAutoplay()
    {
        ContentRegistry registry = BuildRegistry();
        TurnipTestState liveState = new(registry);
        liveState.PlantSeedAtPlot(0, 0, liveState.SelectedSeedItemId);
        AutoPlayTestController liveController = new(liveState, registry);
        liveController.Enable();

        Dictionary<string, int> inventorySnapshot = registry.Items.ToDictionary(item => item.Id, item => liveState.GetItemCount(item.Id), StringComparer.OrdinalIgnoreCase);
        int dayBefore = liveState.DayNumber;
        int moneyBefore = liveState.Money;
        int activityCountBefore = liveState.ActivityLog.Count;
        int liveStepsBefore = liveController.Stats.StepsRun;

        ProgressionSimulationRunner runner = new(registry);
        ProgressionSimulationResult result = runner.Run(new ProgressionSimulationSettings
        {
            TargetMoney = 800,
            MaxDays = 120,
            MaxSteps = 3000
        });

        Assert.NotNull(result);
        Assert.Equal(dayBefore, liveState.DayNumber);
        Assert.Equal(moneyBefore, liveState.Money);
        Assert.Equal(activityCountBefore, liveState.ActivityLog.Count);
        Assert.True(liveController.IsEnabled);
        Assert.Equal(liveStepsBefore, liveController.Stats.StepsRun);

        foreach ((string itemId, int expectedCount) in inventorySnapshot)
            Assert.Equal(expectedCount, liveState.GetItemCount(itemId));
    }

    [Fact]
    public void ProgressionSimulation_Run_ProducesCoreResultAndEarlyMilestones()
    {
        ProgressionSimulationResult result = RunSimulation(new ProgressionSimulationSettings
        {
            TargetMoney = 800,
            MaxDays = 120,
            MaxSteps = 3000
        });

        Assert.NotEqual(AutoPlayStopReason.None, result.StopReason);
        Assert.True(result.DaysSimulated > 0);
        Assert.True(result.StepsRun > 0);
        Assert.True(result.FinalMoney >= 0);
        Assert.True(result.InvariantStatusOk);
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstCropPlanted));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstCropHarvested));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstCropSold));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstSeedBought));
    }

    [Fact]
    public void ProgressionSimulation_Run_RecordsLongLoopMilestonesAndTotals()
    {
        ProgressionSimulationResult result = RunSimulation(new ProgressionSimulationSettings
        {
            TargetMoney = 3000,
            MaxDays = 365,
            MaxSteps = 10000
        });

        Assert.Equal(AutoPlayStopReason.TargetMoneyReached, result.StopReason);
        Assert.True(result.HasMilestone(ProgressionMilestoneType.PlanterBotUnlocked));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.WateringBotUnlocked));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.HarvesterBotUnlocked));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstFarmExpansion));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.OldJunkPileCleared));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FinalFarmExpansion));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstMachineRunStarted));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstMachineOutputCollected));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstProcessedGoodSold));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstAgedGoodProduced));
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstAgedGoodSold));
        Assert.True(result.MoneyEarnedFromSales > 0);
        Assert.True(result.MoneySpentOnSeeds > 0);
        Assert.True(result.MoneySpentOnBots > 0);
        Assert.True(result.MoneySpentOnExpansion > 0);
        Assert.True(result.MoneySpentOnCleanup > 0);
        Assert.True(result.MoneySpentOnMarket > 0);
        Assert.True(result.PotentialMoney >= result.FinalMoney);
        Assert.True(result.BotActionsSaved > 0);
        Assert.True(result.PlanterBotActions > 0);
        Assert.True(result.WateringBotActions > 0);
        Assert.True(result.HarvesterBotActions > 0);
        Assert.True(result.MachineRunsStarted > 0);
        Assert.True(result.MachineOutputsCollected > 0);
        Assert.True(result.MachineValueAdded > 0);
    }

    [Fact]
    public void ProgressionSimulation_Run_WithWineOpportunity_RecordsOpportunitySale()
    {
        ProgressionSimulationResult result = RunSimulation(new ProgressionSimulationSettings
        {
            TargetMoney = 3000,
            MaxDays = 365,
            MaxSteps = 10000,
            EnableMarketOpportunity = true,
            MarketOpportunityId = "wine_boom",
            MarketOpportunityItemId = "wine",
            MarketOpportunityDisplayName = "Wine Boom",
            MarketOpportunityMessage = "Wine sells for 150% for 90 days.",
            MarketOpportunityDurationDays = 90,
            MarketOpportunitySellPriceMultiplier = 1.5
        });

        Assert.True(result.InvariantStatusOk);
        Assert.True(result.HasMilestone(ProgressionMilestoneType.FirstMarketOpportunitySale));
    }

    [Fact]
    public void ProgressionReportFormatter_AndScreenLines_IncludeRequiredSections()
    {
        ProgressionSimulationResult result = RunSimulation(new ProgressionSimulationSettings
        {
            TargetMoney = 800,
            MaxDays = 120,
            MaxSteps = 3000
        });

        string[] formattedLines = ProgressionReportFormatter.Format(result);
        string[] emptyScreenLines = DebugUiTextFormatter.GetProgressionReportScreenLines(null);
        string[] reportScreenLines = DebugUiTextFormatter.GetProgressionReportScreenLines(result);

        Assert.Contains("PROGRESSION REPORT", formattedLines);
        Assert.Contains("MILESTONES", formattedLines);
        Assert.Contains("ECONOMY", formattedLines);
        Assert.Contains("AUTOMATION", formattedLines);
        Assert.Contains("PROCESSING", formattedLines);
        Assert.Contains("MARKET OPPORTUNITIES", formattedLines);
        Assert.Contains("HEALTH", formattedLines);
        Assert.Contains("No progression report yet. Press R to run simulation.", emptyScreenLines);
        Assert.Contains("PROGRESSION REPORT", reportScreenLines);
        Assert.Contains(reportScreenLines, line => line.StartsWith("Stop reason:", StringComparison.Ordinal));
    }

    [Fact]
    public void ProgressionReportFormatter_MiningDemo_UsesThemeAwareLabels()
    {
        ProgressionSimulationResult result = RunSimulation(new ProgressionSimulationSettings
        {
            TargetMoney = 800,
            MaxDays = 120,
            MaxSteps = 3000
        });

        string[] lines = ProgressionReportFormatter.Format(result, ThemeVocabulary.MiningDemo);

        Assert.Contains(lines, line => line.StartsWith("Spent on markers:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("Claim Bot starts:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("Power Bot powers:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("Extractor Bot extracts:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("Marker safety:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("First marker bought", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("First vein started", StringComparison.Ordinal));
    }

    [Fact]
    public void HelpScreenLines_IncludeProgressionReportControls()
    {
        string[] lines = DebugUiTextFormatter.GetHelpScreenLines(new SaveLoadStatus("savegame.json"));

        Assert.Contains("F10 Market | Shift+F10 Progression | F11 Ledger | F4 Help", lines);
        Assert.Contains("PROGRESSION", lines);
        Assert.Contains("Shift+F10 open progression report | R run report on that screen", lines);
        Assert.Contains("Market opportunities are positive bonuses; ignoring them has no penalty.", lines);
    }

    [Fact]
    public void ProgressionReportFormatter_IncludesMarketOpportunitySummaryValues()
    {
        ProgressionSimulationResult result = RunSimulation(new ProgressionSimulationSettings
        {
            TargetMoney = 3000,
            MaxDays = 365,
            MaxSteps = 10000
        });

        string[] lines = ProgressionReportFormatter.Format(result);

        Assert.Contains(lines, line => line.StartsWith("Generated:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("First opportunity day:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("First boosted sale day:", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("Active at end:", StringComparison.Ordinal));
    }

    private static ProgressionSimulationResult RunSimulation(ProgressionSimulationSettings settings)
    {
        ProgressionSimulationRunner runner = new(BuildRegistry());
        return runner.Run(settings);
    }

    private static ContentRegistry BuildRegistry()
    {
        return new ContentRegistry(
            [
                new ItemDefinition { Id = "grape_seed", Name = "Grape Seeds", Type = "seed", BuyPrice = 15, SellPrice = 1 },
                new ItemDefinition { Id = "grape", Name = "Grapes", Type = "crop", BuyPrice = 0, SellPrice = 45 },
                new ItemDefinition { Id = "hops_seed", Name = "Hops Seeds", Type = "seed", BuyPrice = 14, SellPrice = 1 },
                new ItemDefinition { Id = "hops", Name = "Hops", Type = "crop", BuyPrice = 0, SellPrice = 40 },
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed", BuyPrice = 10, SellPrice = 1 },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop", BuyPrice = 0, SellPrice = 30 },
                new ItemDefinition { Id = "wine", Name = "Wine", Type = "processed_good", BuyPrice = 0, SellPrice = 160 },
                new ItemDefinition { Id = "beer", Name = "Beer", Type = "processed_good", BuyPrice = 0, SellPrice = 140 },
                new ItemDefinition { Id = "aged_wine", Name = "Aged Wine", Type = "processed_good", BuyPrice = 0, SellPrice = 260 }
            ],
            [
                new CropDefinition { Id = "grape", SeedItemId = "grape_seed", HarvestItemId = "grape", GrowthDays = 4 },
                new CropDefinition { Id = "hops", SeedItemId = "hops_seed", HarvestItemId = "hops", GrowthDays = 4 },
                new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 }
            ],
            [
                new MachineDefinition { Id = "fermenter", Name = "Fermenter", Type = "processor_machine" },
                new MachineDefinition { Id = "aging_barrel", Name = "Aging Barrel", Type = "processor_machine" }
            ],
            [
                new ProcessingRecipeDefinition
                {
                    Id = "grape_to_wine",
                    MachineId = "fermenter",
                    InputItemId = "grape",
                    InputQuantity = 3,
                    OutputItemId = "wine",
                    OutputQuantity = 1,
                    ProcessingDays = 2
                },
                new ProcessingRecipeDefinition
                {
                    Id = "hops_to_beer",
                    MachineId = "fermenter",
                    InputItemId = "hops",
                    InputQuantity = 3,
                    OutputItemId = "beer",
                    OutputQuantity = 1,
                    ProcessingDays = 2
                },
                new ProcessingRecipeDefinition
                {
                    Id = "wine_to_aged_wine",
                    MachineId = "aging_barrel",
                    InputItemId = "wine",
                    InputQuantity = 1,
                    OutputItemId = "aged_wine",
                    OutputQuantity = 1,
                    ProcessingDays = 5
                }
            ],
            [
                new MarketOfferDefinition { Id = "buy_grapes", ItemId = "grape", Quantity = 3, Price = 160, Category = "raw_input" },
                new MarketOfferDefinition { Id = "buy_hops", ItemId = "hops", Quantity = 3, Price = 120, Category = "raw_input" },
                new MarketOfferDefinition { Id = "buy_wine", ItemId = "wine", Quantity = 1, Price = 190, Category = "intermediate_input" }
            ],
            TestContentFactory.DefaultRestorationSites());
    }
}
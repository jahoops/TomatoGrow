using System;
using System.Collections.Generic;
using System.Linq;

namespace TomatoGrow;

public sealed class GameStats
{
    private readonly Dictionary<string, int> _processedOutputsByItemId = new(StringComparer.OrdinalIgnoreCase);

    public int DaysAdvanced { get; private set; }
    public int MoneyEarnedFromSelling { get; private set; }
    public int MoneySpentOnSeeds { get; private set; }
    public int MoneySpentOnBotUnlocks { get; private set; }
    public int MoneySpentOnExpansion { get; private set; }
    public int SeedsBought { get; private set; }
    public int CropsPlantedManually { get; private set; }
    public int CropsPlantedByPlanterBot { get; private set; }
    public int PlotsWateredManually { get; private set; }
    public int PlotsWateredByWateringBot { get; private set; }
    public int CropsHarvestedManually { get; private set; }
    public int CropsHarvestedByHarvesterBot { get; private set; }
    public int CropsSold { get; private set; }
    public int FarmExpansionsPurchased { get; private set; }
    public int BotsUnlocked { get; private set; }
    public int FarmCyclesRun { get; private set; }
    public int AutoPlayStepsRun { get; private set; }
    public int MachineRunsStarted { get; private set; }
    public int MachineOutputsCollected { get; private set; }
    public int MachineInputValueConsumed { get; private set; }
    public int MachineOutputValueCreated { get; private set; }
    public int MachineValueAdded { get; private set; }
    public int ProcessedGoodsProduced { get; private set; }
    public int MarketPurchases { get; private set; }
    public int MarketMoneySpent { get; private set; }
    public int MarketItemsBought { get; private set; }
    public int CleanupSitesCleared { get; private set; }
    public int MoneySpentOnCleanup { get; private set; }
    public IReadOnlyDictionary<string, int> ProcessedOutputsByItemId => _processedOutputsByItemId;

    public int TotalMoneySpent => MoneySpentOnSeeds + MoneySpentOnBotUnlocks + MoneySpentOnExpansion + MarketMoneySpent + MoneySpentOnCleanup;
    public int TotalCropsPlanted => CropsPlantedManually + CropsPlantedByPlanterBot;
    public int TotalCropsHarvested => CropsHarvestedManually + CropsHarvestedByHarvesterBot;
    public int BotActionsSaved => CropsPlantedByPlanterBot + PlotsWateredByWateringBot + CropsHarvestedByHarvesterBot;

    public void Reset()
    {
        DaysAdvanced = 0;
        MoneyEarnedFromSelling = 0;
        MoneySpentOnSeeds = 0;
        MoneySpentOnBotUnlocks = 0;
        MoneySpentOnExpansion = 0;
        SeedsBought = 0;
        CropsPlantedManually = 0;
        CropsPlantedByPlanterBot = 0;
        PlotsWateredManually = 0;
        PlotsWateredByWateringBot = 0;
        CropsHarvestedManually = 0;
        CropsHarvestedByHarvesterBot = 0;
        CropsSold = 0;
        FarmExpansionsPurchased = 0;
        BotsUnlocked = 0;
        FarmCyclesRun = 0;
        AutoPlayStepsRun = 0;
        MachineRunsStarted = 0;
        MachineOutputsCollected = 0;
        MachineInputValueConsumed = 0;
        MachineOutputValueCreated = 0;
        MachineValueAdded = 0;
        ProcessedGoodsProduced = 0;
        MarketPurchases = 0;
        MarketMoneySpent = 0;
        MarketItemsBought = 0;
        CleanupSitesCleared = 0;
        MoneySpentOnCleanup = 0;
        _processedOutputsByItemId.Clear();
    }

    public void RecordDayAdvanced() => DaysAdvanced++;
    public void RecordMoneyEarnedFromSelling(int money, int cropsSold)
    {
        MoneyEarnedFromSelling += money;
        CropsSold += cropsSold;
    }

    public void RecordMoneySpentOnSeeds(int money, int seedsBought)
    {
        MoneySpentOnSeeds += money;
        SeedsBought += seedsBought;
    }

    public void RecordMoneySpentOnBotUnlock(int money)
    {
        MoneySpentOnBotUnlocks += money;
        BotsUnlocked++;
    }

    public void RecordMoneySpentOnExpansion(int money)
    {
        MoneySpentOnExpansion += money;
        FarmExpansionsPurchased++;
    }

    public void RecordManualPlant(int count) => CropsPlantedManually += count;
    public void RecordPlanterBotPlant(int count) => CropsPlantedByPlanterBot += count;
    public void RecordManualWater(int count) => PlotsWateredManually += count;
    public void RecordWateringBotWater(int count) => PlotsWateredByWateringBot += count;
    public void RecordManualHarvest(int count) => CropsHarvestedManually += count;
    public void RecordHarvesterBotHarvest(int count) => CropsHarvestedByHarvesterBot += count;
    public void RecordFarmCycle() => FarmCyclesRun++;
    public void RecordAutoPlayStep() => AutoPlayStepsRun++;
    public void RecordMachineRunStarted(int inputValue)
    {
        MachineRunsStarted++;
        MachineInputValueConsumed += inputValue;
    }

    public void RecordMachineOutputCollected(string outputItemId, int outputQuantity, int outputValue, int valueAdded)
    {
        MachineOutputsCollected++;
        ProcessedGoodsProduced += outputQuantity;
        MachineOutputValueCreated += outputValue;
        MachineValueAdded += valueAdded;

        if (string.IsNullOrWhiteSpace(outputItemId))
            return;

        _processedOutputsByItemId[outputItemId] = GetProcessedOutputCount(outputItemId) + outputQuantity;
    }

    public int GetProcessedOutputCount(string outputItemId)
    {
        return _processedOutputsByItemId.TryGetValue(outputItemId, out int count) ? count : 0;
    }

    public void RecordMarketPurchase(int moneySpent, int itemsBought)
    {
        MarketPurchases++;
        MarketMoneySpent += moneySpent;
        MarketItemsBought += itemsBought;
    }

    public void RecordCleanupCleared(int moneySpent)
    {
        CleanupSitesCleared++;
        MoneySpentOnCleanup += moneySpent;
    }

    public GameStatsSaveState CreateSaveState()
    {
        return new GameStatsSaveState
        {
            DaysAdvanced = DaysAdvanced,
            MoneyEarnedFromSelling = MoneyEarnedFromSelling,
            MoneySpentOnSeeds = MoneySpentOnSeeds,
            MoneySpentOnBotUnlocks = MoneySpentOnBotUnlocks,
            MoneySpentOnExpansion = MoneySpentOnExpansion,
            SeedsBought = SeedsBought,
            CropsPlantedManually = CropsPlantedManually,
            CropsPlantedByPlanterBot = CropsPlantedByPlanterBot,
            PlotsWateredManually = PlotsWateredManually,
            PlotsWateredByWateringBot = PlotsWateredByWateringBot,
            CropsHarvestedManually = CropsHarvestedManually,
            CropsHarvestedByHarvesterBot = CropsHarvestedByHarvesterBot,
            CropsSold = CropsSold,
            FarmExpansionsPurchased = FarmExpansionsPurchased,
            BotsUnlocked = BotsUnlocked,
            FarmCyclesRun = FarmCyclesRun,
            AutoPlayStepsRun = AutoPlayStepsRun,
            MachineRunsStarted = MachineRunsStarted,
            MachineOutputsCollected = MachineOutputsCollected,
            MachineInputValueConsumed = MachineInputValueConsumed,
            MachineOutputValueCreated = MachineOutputValueCreated,
            MachineValueAdded = MachineValueAdded,
            ProcessedGoodsProduced = ProcessedGoodsProduced,
            MarketPurchases = MarketPurchases,
            MarketMoneySpent = MarketMoneySpent,
            MarketItemsBought = MarketItemsBought,
            CleanupSitesCleared = CleanupSitesCleared,
            MoneySpentOnCleanup = MoneySpentOnCleanup,
            ProcessedOutputs = _processedOutputsByItemId
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Select(entry => new ItemStackSaveState { ItemId = entry.Key, Quantity = entry.Value })
                .ToList()
        };
    }

    public void Load(GameStatsSaveState saveState)
    {
        Reset();

        if (saveState is null)
            return;

        DaysAdvanced = Math.Max(0, saveState.DaysAdvanced);
        MoneyEarnedFromSelling = Math.Max(0, saveState.MoneyEarnedFromSelling);
        MoneySpentOnSeeds = Math.Max(0, saveState.MoneySpentOnSeeds);
        MoneySpentOnBotUnlocks = Math.Max(0, saveState.MoneySpentOnBotUnlocks);
        MoneySpentOnExpansion = Math.Max(0, saveState.MoneySpentOnExpansion);
        SeedsBought = Math.Max(0, saveState.SeedsBought);
        CropsPlantedManually = Math.Max(0, saveState.CropsPlantedManually);
        CropsPlantedByPlanterBot = Math.Max(0, saveState.CropsPlantedByPlanterBot);
        PlotsWateredManually = Math.Max(0, saveState.PlotsWateredManually);
        PlotsWateredByWateringBot = Math.Max(0, saveState.PlotsWateredByWateringBot);
        CropsHarvestedManually = Math.Max(0, saveState.CropsHarvestedManually);
        CropsHarvestedByHarvesterBot = Math.Max(0, saveState.CropsHarvestedByHarvesterBot);
        CropsSold = Math.Max(0, saveState.CropsSold);
        FarmExpansionsPurchased = Math.Max(0, saveState.FarmExpansionsPurchased);
        BotsUnlocked = Math.Max(0, saveState.BotsUnlocked);
        FarmCyclesRun = Math.Max(0, saveState.FarmCyclesRun);
        AutoPlayStepsRun = Math.Max(0, saveState.AutoPlayStepsRun);
        MachineRunsStarted = Math.Max(0, saveState.MachineRunsStarted);
        MachineOutputsCollected = Math.Max(0, saveState.MachineOutputsCollected);
        MachineInputValueConsumed = Math.Max(0, saveState.MachineInputValueConsumed);
        MachineOutputValueCreated = Math.Max(0, saveState.MachineOutputValueCreated);
        MachineValueAdded = Math.Max(0, saveState.MachineValueAdded);
        ProcessedGoodsProduced = Math.Max(0, saveState.ProcessedGoodsProduced);
        MarketPurchases = Math.Max(0, saveState.MarketPurchases);
        MarketMoneySpent = Math.Max(0, saveState.MarketMoneySpent);
        MarketItemsBought = Math.Max(0, saveState.MarketItemsBought);
        CleanupSitesCleared = Math.Max(0, saveState.CleanupSitesCleared);
        MoneySpentOnCleanup = Math.Max(0, saveState.MoneySpentOnCleanup);

        foreach (ItemStackSaveState processedOutput in saveState.ProcessedOutputs ?? [])
        {
            if (string.IsNullOrWhiteSpace(processedOutput.ItemId) || processedOutput.Quantity <= 0)
                continue;

            _processedOutputsByItemId[processedOutput.ItemId] = processedOutput.Quantity;
        }
    }
}
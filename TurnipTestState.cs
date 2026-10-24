using System;
using System.Collections.Generic;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public sealed class TurnipTestState
{
    public static int ActivityLogMaxEntries => GameBalanceConfig.Default.ActivityLogMaxEntries;
    public const string OldJunkPileId = "old_junk_pile";
    public const string OldJunkPileName = "Old Junk Pile";
    public const string OldJunkPileDescription = "A pile of rusted scrap blocking usable farm space.";
    public const int OldJunkPileCleanupCost = 500;
    public const string OldJunkPileReward = "Unlocks 5x3 farm expansion";
    public const int InitialFarmWidth = 3;
    public const int InitialFarmHeight = 3;
    public static int FirstFarmExpansionCost => GameBalanceConfig.Default.FirstExpansionCost;
    public static int SecondFarmExpansionCost => GameBalanceConfig.Default.SecondExpansionCost;
    public const int MaxFarmExpansionLevel = 2;
    public static int PlanterBotUnlockCost => GameBalanceConfig.Default.PlanterBotCost;
    public static int WateringBotUnlockCost => GameBalanceConfig.Default.WateringBotCost;
    public static int HarvesterBotUnlockCost => GameBalanceConfig.Default.HarvesterBotCost;

    private readonly ContentRegistry _contentRegistry;
    private readonly List<string> _seedItemIds;
    private readonly List<ProcessorMachineInstance> _processorMachines;
    private readonly List<MarketOfferDefinition> _marketOffers;
    private readonly List<ActivityLogEntry> _activityLog = [];
    private readonly Dictionary<string, int> _inventory = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _sharedStorage = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _clearedRestorationSiteIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _usesDefaultWateringBotRegion;
    private readonly bool _usesDefaultPlanterBotRegion;
    private readonly bool _usesDefaultHarvesterBotRegion;

    public TurnipTestState(
        ContentRegistry contentRegistry,
        FarmRegion wateringBotRegion = null,
        FarmRegion planterBotRegion = null,
        FarmRegion harvesterBotRegion = null,
        GameBalanceConfig balanceConfig = null,
        ThemeVocabulary themeVocabulary = null,
        InterfacePack interfacePack = null)
    {
        _contentRegistry = contentRegistry;
        BalanceConfig = balanceConfig ?? GameBalanceConfig.Default;
        ThemeVocabulary = themeVocabulary ?? ThemeVocabulary.FarmingDefault;
        InterfacePack = interfacePack ?? InterfacePack.FarmingDefault;
        FarmGrid = new FarmGrid(InitialFarmWidth, InitialFarmHeight);
        _usesDefaultWateringBotRegion = wateringBotRegion is null;
        _usesDefaultPlanterBotRegion = planterBotRegion is null;
        _usesDefaultHarvesterBotRegion = harvesterBotRegion is null;
        _seedItemIds = _contentRegistry.GetStarterItemIds().ToList();

        if (_seedItemIds.Count == 0)
            throw new InvalidOperationException("No starter items were loaded. Turnip Test cannot start.");

        WateringBot = new WateringBot(wateringBotRegion ?? new FarmRegion(0, 0, FarmGrid.Width, FarmGrid.Height));
        PlanterBot = new PlanterBot(
            planterBotRegion ?? new FarmRegion(0, 0, FarmGrid.Width, FarmGrid.Height),
            GetDefaultSeedItemId(_seedItemIds));
        HarvesterBot = new HarvesterBot(harvesterBotRegion ?? new FarmRegion(0, 0, FarmGrid.Width, FarmGrid.Height));

        SelectedSeedItemId = GetDefaultSeedItemId(_seedItemIds);
        _processorMachines = CreateProcessorMachines(_contentRegistry);
        _marketOffers = _contentRegistry.MarketOffers
            .OrderBy(offer => offer.Price)
            .ThenBy(offer => _contentRegistry.GetItem(offer.ItemId).Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Reset();
    }

    public FarmGrid FarmGrid { get; }
    public GameBalanceConfig BalanceConfig { get; }
    public ThemeVocabulary ThemeVocabulary { get; }
    public InterfacePack InterfacePack { get; }
    public WateringBot WateringBot { get; }
    public PlanterBot PlanterBot { get; }
    public HarvesterBot HarvesterBot { get; }
    public ProcessorMachineInstance Fermenter => GetProcessorMachine("fermenter");
    public ProcessorMachineInstance AgingBarrel => GetProcessorMachine("aging_barrel");
    public IReadOnlyList<ProcessorMachineInstance> ProcessorMachines => _processorMachines;
    public IReadOnlyList<string> SeedItemIds => _seedItemIds;
    public IReadOnlyList<MarketOfferDefinition> MarketOffers => _marketOffers;
    public IReadOnlyList<ActivityLogEntry> ActivityLog => _activityLog;
    public IReadOnlyDictionary<string, int> Inventory => _inventory;
    public IReadOnlyDictionary<string, int> SharedStorage => _sharedStorage;
    public IReadOnlyCollection<string> ClearedRestorationSiteIds => _clearedRestorationSiteIds.ToArray();
    public GameStats Stats { get; } = new();
    public MarketOpportunity ActiveMarketOpportunity { get; private set; }
    public int LastGeneratedMarketOpportunityDay { get; private set; }
    public int GeneratedMarketOpportunityCount { get; private set; }
    public IReadOnlyList<CleanupSiteState> RestorationSites => _contentRegistry.RestorationSites
        .Select(CreateCleanupSiteState)
        .ToList();
    public CleanupSiteState CleanupSite => GetCleanupSiteById(OldJunkPileId) ?? GetNextRelevantCleanupSite();
    public string SelectedSeedItemId { get; private set; }
    public int DayNumber { get; private set; }
    public int Money { get; private set; }
    public int SelectedPlotX { get; private set; }
    public int SelectedPlotY { get; private set; }
    public int SelectedProcessorMachineIndex { get; private set; }
    public int FarmExpansionLevel => FarmGrid.Width - InitialFarmWidth;
    public int NextFarmExpansionCost => GetNextFarmExpansionCost();
    public bool IsFarmFullyExpanded => FarmExpansionLevel >= MaxFarmExpansionLevel;
    public bool IsFinalFarmExpansionBlockedByCleanup => FarmExpansionLevel == 1 && GetBlockingCleanupSiteForNextExpansion() is { IsCleared: false };
    public bool IsPlanterBotUnlocked { get; private set; }
    public bool IsWateringBotUnlocked { get; private set; }
    public bool IsHarvesterBotUnlocked { get; private set; }
    public string LastBotActionMessage { get; private set; } = "Watering Bot has not run yet.";
    public BotActivityReplay PendingBotActivityReplay { get; private set; }
    public ProcessorMachineInstance SelectedProcessorMachine => _processorMachines.Count > 0
        ? _processorMachines[SelectedProcessorMachineIndex]
        : throw new InvalidOperationException("No processor machines were loaded.");

    public void Reset()
    {
        Stats.Reset();
        FarmGrid.Resize(InitialFarmWidth, InitialFarmHeight);
        DayNumber = 1;
        Money = 0;
        SelectedPlotX = 0;
        SelectedPlotY = 0;
        SelectedProcessorMachineIndex = 0;
        PendingBotActivityReplay = null;
        _inventory.Clear();
        _sharedStorage.Clear();
        _activityLog.Clear();
        _clearedRestorationSiteIds.Clear();
        ActiveMarketOpportunity = null;
        LastGeneratedMarketOpportunityDay = 0;
        GeneratedMarketOpportunityCount = 0;
        FarmGrid.Clear();
        UpdateDefaultBotRegions();

        foreach (string seedItemId in _seedItemIds)
            AddItem(seedItemId, 3);

        SelectedSeedItemId = GetDefaultSeedItemId(_seedItemIds);
        IsPlanterBotUnlocked = false;
        IsWateringBotUnlocked = false;
        IsHarvesterBotUnlocked = false;
        PlanterBot.AssignedSeedItemId = SelectedSeedItemId;
        WateringBot.Enabled = false;
        foreach (ProcessorMachineInstance machine in _processorMachines)
            machine.Reset();

        LastBotActionMessage = "No bot action yet.";
    }

    public string ExpandFarm()
    {
        if (IsFarmFullyExpanded)
            return "Farm is already fully expanded.";

        if (IsFinalFarmExpansionBlockedByCleanup)
        {
            CleanupSiteState blockingSite = GetBlockingCleanupSiteForNextExpansion();
            return $"Clear {blockingSite?.Name ?? OldJunkPileName} before expanding further.";
        }

        int expansionCost = GetNextFarmExpansionCost();
        if (Money < expansionCost)
            return $"Need {expansionCost} to expand farm.";

        Money -= expansionCost;
        FarmGrid.Resize(FarmGrid.Width + 1, InitialFarmHeight);
        UpdateDefaultBotRegions();
        ClampSelectedPlotToBounds();
        Stats.RecordMoneySpentOnExpansion(expansionCost);
        AddActivity(ActivityLogCategory.Expansion, $"Expanded farm to {FarmGrid.Width}x{FarmGrid.Height} for ${expansionCost}.", expansionCost);
        return $"Expanded farm to {FarmGrid.Width}x{FarmGrid.Height} for {expansionCost}.";
    }

    public string ClearCleanupSite()
    {
        CleanupSiteState cleanupSite = GetNextRelevantCleanupSite();
        if (cleanupSite is null)
        {
            CleanupSiteState primaryCleanupSite = CleanupSite;
            return primaryCleanupSite is not null && primaryCleanupSite.IsCleared
                ? $"{primaryCleanupSite.Name} already cleared."
                : "No cleanup sites available.";
        }

        if (cleanupSite.IsCleared)
            return $"{cleanupSite.Name} already cleared.";

        if (Money < cleanupSite.Cost)
            return $"Need ${cleanupSite.Cost} to clear {cleanupSite.Name}.";

        Money -= cleanupSite.Cost;
        _clearedRestorationSiteIds.Add(cleanupSite.Id);
        Stats.RecordCleanupCleared(cleanupSite.Cost);
        AddActivity(ActivityLogCategory.Restoration, $"Cleared {cleanupSite.Name} for ${cleanupSite.Cost}.", cleanupSite.Cost);
        return $"Cleared {cleanupSite.Name} for ${cleanupSite.Cost}.";
    }

    public string UnlockPlanterBot()
    {
        return UnlockBot(
            "Planter Bot",
            BalanceConfig.PlanterBotCost,
            IsPlanterBotUnlocked,
            () => IsPlanterBotUnlocked = true);
    }

    public string UnlockWateringBot()
    {
        return UnlockBot(
            "Watering Bot",
            BalanceConfig.WateringBotCost,
            IsWateringBotUnlocked,
            () => IsWateringBotUnlocked = true);
    }

    public string UnlockHarvesterBot()
    {
        return UnlockBot(
            "Harvester Bot",
            BalanceConfig.HarvesterBotCost,
            IsHarvesterBotUnlocked,
            () => IsHarvesterBotUnlocked = true);
    }

    public string UnlockBot(BotRole role)
    {
        return role switch
        {
            BotRole.Starter => UnlockPlanterBot(),
            BotRole.Maintainer => UnlockWateringBot(),
            BotRole.Collector => UnlockHarvesterBot(),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported bot role.")
        };
    }

    public bool IsBotUnlocked(BotRole role)
    {
        return role switch
        {
            BotRole.Starter => IsPlanterBotUnlocked,
            BotRole.Maintainer => IsWateringBotUnlocked,
            BotRole.Collector => IsHarvesterBotUnlocked,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported bot role.")
        };
    }

    public int GetBotCost(BotRole role)
    {
        return BotRoleFormatter.GetBotCost(role, BalanceConfig);
    }

    public string MoveSelectedPlot(int xOffset, int yOffset)
    {
        int newX = Math.Clamp(SelectedPlotX + xOffset, 0, FarmGrid.Width - 1);
        int newY = Math.Clamp(SelectedPlotY + yOffset, 0, FarmGrid.Height - 1);

        SelectedPlotX = newX;
        SelectedPlotY = newY;
        return $"Selected plot {SelectedPlotX},{SelectedPlotY}.";
    }

    public string SelectNextSeed()
    {
        int currentIndex = _seedItemIds.FindIndex(seedItemId =>
            string.Equals(seedItemId, SelectedSeedItemId, StringComparison.OrdinalIgnoreCase));
        int nextIndex = (currentIndex + 1) % _seedItemIds.Count;
        SelectedSeedItemId = _seedItemIds[nextIndex];
        PlanterBot.AssignedSeedItemId = SelectedSeedItemId;
        return $"Selected {ThemeVocabulary.StarterItemName.ToLowerInvariant()}: {_contentRegistry.GetItem(SelectedSeedItemId).Name}";
    }

    public string AdvanceDay()
    {
        string botMessage;
        List<BotActivityStep> replaySteps = [];

        if (WateringBot.Enabled && IsWateringBotUnlocked)
            botMessage = RunWateringBot(replaySteps);
        else
            botMessage = LastBotActionMessage = "Watering Bot skipped (OFF).";

        foreach (FarmPlot plot in FarmGrid.GetAllPlots())
            plot.AdvanceDay();

        foreach (ProcessorMachineInstance machine in _processorMachines)
            machine.AdvanceDay();

        DayNumber++;
        Stats.RecordDayAdvanced();
        AdvanceMarketOpportunity();
        TryGenerateMarketOpportunity();
        SetPendingBotActivityReplay(replaySteps);
        return $"{botMessage} Advanced to day {DayNumber}.";
    }

    public bool TryGenerateMarketOpportunity()
    {
        if (ActiveMarketOpportunity?.IsActive == true)
            return false;

        if (BalanceConfig.MarketOpportunityGenerationIntervalDays <= 0
            || BalanceConfig.MarketOpportunityGeneratedDurationDays <= 0
            || BalanceConfig.MarketOpportunityGeneratedMultiplier <= 0)
            return false;

        if (DayNumber - LastGeneratedMarketOpportunityDay < BalanceConfig.MarketOpportunityGenerationIntervalDays)
            return false;

        IReadOnlyList<string> candidateItemIds = GetMarketOpportunityCandidateItems();
        if (candidateItemIds.Count == 0)
            return false;

        int candidateIndex = GeneratedMarketOpportunityCount % candidateItemIds.Count;
        string itemId = candidateItemIds[candidateIndex];
        MarketOpportunity opportunity = CreateGeneratedMarketOpportunity(itemId);
        if (opportunity is null)
            return false;

        LastGeneratedMarketOpportunityDay = DayNumber;
        GeneratedMarketOpportunityCount++;
        ReplaceActiveMarketOpportunity(opportunity, addActivity: true);
        return true;
    }

    public IReadOnlyList<string> GetMarketOpportunityCandidateItems()
    {
        List<string> processedOutputs = _contentRegistry.ProcessingRecipes
            .Select(recipe => recipe.OutputItemId)
            .Where(IsMeaningfulOpportunityItem)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (processedOutputs.Count > 0)
            return processedOutputs;

        return _contentRegistry.TileProducers
            .Select(tileProducer => tileProducer.OutputItemId)
            .Where(IsMeaningfulOpportunityItem)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(itemId => _contentRegistry.GetItem(itemId).Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void SetActiveMarketOpportunity(MarketOpportunity opportunity)
    {
        ReplaceActiveMarketOpportunity(opportunity, addActivity: true);
    }

    public MarketOpportunity GetActiveMarketOpportunityForItem(string itemId)
    {
        if (ActiveMarketOpportunity is null || !ActiveMarketOpportunity.IsActive)
            return null;

        return string.Equals(ActiveMarketOpportunity.ItemId, itemId, StringComparison.OrdinalIgnoreCase)
            ? ActiveMarketOpportunity
            : null;
    }

    public int GetEffectiveSellPrice(string itemId)
    {
        ItemDefinition item = _contentRegistry.GetItem(itemId);
        MarketOpportunity opportunity = GetActiveMarketOpportunityForItem(itemId);
        if (opportunity is null)
            return item.SellPrice;

        double boostedPrice = item.SellPrice * opportunity.SellPriceMultiplier;
        return (int)Math.Round(boostedPrice, MidpointRounding.AwayFromZero);
    }

    public int GetDaysUntilNextMarketOpportunity()
    {
        if (ActiveMarketOpportunity?.IsActive == true)
            return 0;

        int intervalDays = BalanceConfig.MarketOpportunityGenerationIntervalDays;
        if (intervalDays <= 0)
            return 0;

        int elapsedDays = DayNumber - LastGeneratedMarketOpportunityDay;
        return Math.Max(0, intervalDays - elapsedDays);
    }

    public string GetNextMarketOpportunityCandidateItemId()
    {
        IReadOnlyList<string> candidateItemIds = GetMarketOpportunityCandidateItems();
        if (candidateItemIds.Count == 0)
            return string.Empty;

        int candidateIndex = GeneratedMarketOpportunityCount % candidateItemIds.Count;
        return candidateItemIds[candidateIndex];
    }

    public string StartFermenterFromInventory()
    {
        return StartProcessorMachineFromInventory(Fermenter.MachineId);
    }

    public string CollectFermenterOutput()
    {
        return CollectProcessorMachineOutput(Fermenter.MachineId);
    }

    public string StartSelectedProcessorMachineFromInventory(string preferredRecipeId = null, bool requireSpecificRecipe = false)
    {
        if (_processorMachines.Count == 0)
            return "No processor machines loaded.";

        return StartProcessorMachineFromInventory(SelectedProcessorMachine.MachineId, preferredRecipeId, requireSpecificRecipe);
    }

    public string CollectSelectedProcessorMachineOutput()
    {
        if (_processorMachines.Count == 0)
            return "No processor machines loaded.";

        return CollectProcessorMachineOutput(SelectedProcessorMachine.MachineId);
    }

    public string StartProcessorMachineFromInventory(string machineId, string preferredRecipeId = null, bool requireSpecificRecipe = false)
    {
        ProcessorMachineInstance machine = GetProcessorMachine(machineId);
        string machineActionName = GetMachineActionName(machineId);

        if (machine.HasOutputReady)
            return $"Collect {machineActionName} output first with O.";

        if (machine.IsWorking)
            return $"{GetMachineDisplayName(machineId)} is already working: {machine.OutputQuantity} {GetItemDisplayName(machine.OutputItemId)} ready in {FormatDayCount(machine.DaysRemaining)}.";

        if (!_contentRegistry.MachinesById.ContainsKey(machine.MachineId))
            return $"{machineActionName} machine content is missing.";

        ProcessingRecipeDefinition recipe = requireSpecificRecipe && !string.IsNullOrWhiteSpace(preferredRecipeId)
            ? GetMachineRecipes(machineId).FirstOrDefault(candidate =>
                string.Equals(candidate.Id, preferredRecipeId, StringComparison.OrdinalIgnoreCase)
                && CanConsumeMachineInputFromInventoryOrStorage(candidate.InputItemId, candidate.InputQuantity))
            : GetStartableMachineRecipe(machineId, preferredRecipeId);
        if (recipe is null)
            return requireSpecificRecipe && !string.IsNullOrWhiteSpace(preferredRecipeId)
                ? GetPreferredRecipeMissingInputMessage(machineId, preferredRecipeId)
                : GetNoValidMachineInputMessage(machineId);

        DebugUiTextFormatter.ProcessingRecipeEconomics economics = DebugUiTextFormatter.CalculateProcessingRecipeEconomics(_contentRegistry, recipe);
        if (!TryConsumeMachineInputFromInventoryOrStorage(recipe.InputItemId, recipe.InputQuantity))
            return requireSpecificRecipe && !string.IsNullOrWhiteSpace(preferredRecipeId)
                ? GetPreferredRecipeMissingInputMessage(machineId, preferredRecipeId)
                : GetNoValidMachineInputMessage(machineId);

        machine.Start(recipe.Id, recipe.InputItemId, recipe.InputQuantity, recipe.OutputItemId, recipe.OutputQuantity, recipe.ProcessingDays);
        Stats.RecordMachineRunStarted(economics.InputValue);
        AddActivity(
            ActivityLogCategory.Machine,
            $"Started {GetMachineDisplayName(machineId)}: {recipe.InputQuantity} {GetItemDisplayName(recipe.InputItemId)} -> {recipe.OutputQuantity} {GetItemDisplayName(recipe.OutputItemId)}, ready in {recipe.ProcessingDays} days.",
            economics.ValueAdded,
            recipe.OutputItemId,
            recipe.OutputQuantity);
        return $"Started {machineActionName}: {recipe.InputQuantity} {GetItemDisplayName(recipe.InputItemId)} -> {recipe.OutputQuantity} {GetItemDisplayName(recipe.OutputItemId)}, ready in {recipe.ProcessingDays} days.";
    }

    public string CollectProcessorMachineOutput(string machineId)
    {
        ProcessorMachineInstance machine = GetProcessorMachine(machineId);
        string machineActionName = GetMachineActionName(machineId);
        if (!machine.HasOutputReady || string.IsNullOrWhiteSpace(machine.OutputItemId))
            return $"No {machineActionName} output ready.";

        ProcessingRecipeDefinition recipe = _contentRegistry.GetProcessingRecipe(machine.CurrentRecipeId);
        DebugUiTextFormatter.ProcessingRecipeEconomics economics = DebugUiTextFormatter.CalculateProcessingRecipeEconomics(_contentRegistry, recipe);
        AddItem(machine.OutputItemId, machine.OutputQuantity);
        int outputQuantity = machine.OutputQuantity;
        string outputItemId = machine.OutputItemId;
        string outputName = GetItemDisplayName(machine.OutputItemId);
        machine.Reset();
        Stats.RecordMachineOutputCollected(outputItemId, outputQuantity, economics.OutputValue, economics.ValueAdded);
        string collectMessage = economics.ValueAdded == 0
            ? $"Collected {outputQuantity} {outputName} from {GetMachineDisplayName(machineId)}."
            : $"Collected {outputQuantity} {outputName} from {GetMachineDisplayName(machineId)}, value added {FormatSignedCurrency(economics.ValueAdded)}.";
        AddActivity(ActivityLogCategory.Machine, collectMessage, economics.ValueAdded, outputItemId, outputQuantity);
        return $"Collected {outputQuantity} {outputName} from {machineActionName}.";
    }

    public string SelectNextProcessorMachine()
    {
        if (_processorMachines.Count == 0)
            return "No processor machines loaded.";

        SelectedProcessorMachineIndex = (SelectedProcessorMachineIndex + 1) % _processorMachines.Count;
        return $"Selected machine: {GetMachineDisplayName(SelectedProcessorMachine.MachineId)}";
    }

    public string SelectPreviousProcessorMachine()
    {
        if (_processorMachines.Count == 0)
            return "No processor machines loaded.";

        SelectedProcessorMachineIndex = (SelectedProcessorMachineIndex - 1 + _processorMachines.Count) % _processorMachines.Count;
        return $"Selected machine: {GetMachineDisplayName(SelectedProcessorMachine.MachineId)}";
    }

    public string ToggleWateringBotEnabled()
    {
        if (!IsWateringBotUnlocked)
            return "Watering Bot is locked.";

        WateringBot.Enabled = !WateringBot.Enabled;
        return $"Watering Bot {(WateringBot.Enabled ? "ON" : "OFF")}.";
    }

    public string WaterSelectedPlot()
    {
        return MaintainSelectedTile();
    }

    public string MaintainSelectedTile()
    {
        string message = WaterPlot(SelectedPlotX, SelectedPlotY);
        Stats.RecordManualWater(1);
        return message;
    }

    public string WaterPlot(int plotX, int plotY)
    {
        FarmPlot plot = FarmGrid.GetPlot(plotX, plotY);
        plot.Water();
        return $"Watered plot {plotX},{plotY}.";
    }

    public string RunWateringBot()
    {
        return RunWateringBot(null);
    }

    private string RunWateringBot(List<BotActivityStep> replaySteps)
    {
        if (!IsWateringBotUnlocked)
            return LastBotActionMessage = "Watering Bot is locked.";

        List<BotActivityStep> localReplaySteps = replaySteps ?? [];
        int wateredCount = WateringBot.Run(FarmGrid, (x, y) =>
        {
            FarmPlotVisualSnapshot before = FarmPlotVisualSnapshot.FromPlot(FarmGrid.GetPlot(x, y));
            WaterPlot(x, y);
            FarmPlotVisualSnapshot after = FarmPlotVisualSnapshot.FromPlot(FarmGrid.GetPlot(x, y));
            localReplaySteps.Add(new BotActivityStep(BotRole.Maintainer, x, y, before, after, BuildBotActivityStatusLine(BotRole.Maintainer, x, y)));
        });

        LastBotActionMessage = wateredCount == 0
            ? "Watering Bot found no dry planted plots."
            : $"Watering Bot watered {wateredCount} plots.";

        if (wateredCount > 0)
            Stats.RecordWateringBotWater(wateredCount);

        if (replaySteps is null)
            SetPendingBotActivityReplay(localReplaySteps);

        return LastBotActionMessage;
    }

    public string RunBot(BotRole role)
    {
        return role switch
        {
            BotRole.Starter => RunPlanterBot(),
            BotRole.Maintainer => RunWateringBot(),
            BotRole.Collector => RunHarvesterBot(),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported bot role.")
        };
    }

    public string PlantSelectedSeed()
    {
        return StartSelectedTileProducer();
    }

    public string StartSelectedTileProducer()
    {
        return PlantSeedAtPlot(SelectedPlotX, SelectedPlotY, SelectedSeedItemId);
    }

    public string PlantSeedItem(string seedItemId)
    {
        return PlantSeedAtPlot(SelectedPlotX, SelectedPlotY, seedItemId);
    }

    public string PlantSeedAtPlot(int plotX, int plotY, string seedItemId)
    {
        bool didPlant = TryPlantSeedAtPlot(plotX, plotY, seedItemId, out string message);
        if (didPlant)
        {
            Stats.RecordManualPlant(1);
            TileProducerDefinition tileProducer = _contentRegistry.GetTileProducer(FarmGrid.GetPlot(plotX, plotY).PlantedCrop!.CropDefinitionId);
            AddActivity(ActivityLogCategory.Farm, $"Planted {GetItemDisplayName(tileProducer.OutputItemId)} at {plotX},{plotY}.", null, seedItemId, 1);
        }

        return message;
    }

    public string RunPlanterBot()
    {
        return RunPlanterBot(null);
    }

    private string RunPlanterBot(List<BotActivityStep> replaySteps)
    {
        if (!IsPlanterBotUnlocked)
            return LastBotActionMessage = "Planter Bot is locked.";

        string seedItemId = PlanterBot.AssignedSeedItemId;

        if (!_contentRegistry.TryGetTileProducerByStarterItemId(seedItemId, out _))
            return LastBotActionMessage = "Planter Bot seed is not plantable.";

        if (CountEmptyPlotsInRegion(PlanterBot.Region) == 0)
            return LastBotActionMessage = "Planter Bot found no empty plots.";

        if (GetItemCount(seedItemId) <= 0)
            return LastBotActionMessage = "Planter Bot has no seeds.";

        List<BotActivityStep> localReplaySteps = replaySteps ?? [];
        int plantedCount = PlanterBot.Run(
            FarmGrid,
            (x, y) =>
            {
                FarmPlotVisualSnapshot before = FarmPlotVisualSnapshot.FromPlot(FarmGrid.GetPlot(x, y));
                bool didPlant = TryPlantSeedAtPlot(x, y, seedItemId, out _);
                if (!didPlant)
                    return false;

                FarmPlotVisualSnapshot after = FarmPlotVisualSnapshot.FromPlot(FarmGrid.GetPlot(x, y));
                localReplaySteps.Add(new BotActivityStep(BotRole.Starter, x, y, before, after, BuildBotActivityStatusLine(BotRole.Starter, x, y)));
                return true;
            },
            () => GetItemCount(seedItemId) > 0);

        LastBotActionMessage = plantedCount == 0
            ? "Planter Bot found no empty plots."
            : $"Planter Bot planted {plantedCount} {seedItemId}.";

        if (plantedCount > 0)
        {
            Stats.RecordPlanterBotPlant(plantedCount);
            AddActivity(ActivityLogCategory.Farm, $"Planter Bot planted {plantedCount} {GetItemDisplayName(seedItemId)}.", null, seedItemId, plantedCount);
        }

        if (replaySteps is null)
            SetPendingBotActivityReplay(localReplaySteps);

        return LastBotActionMessage;
    }

    public string RunFarmCycle()
    {
        Stats.RecordFarmCycle();
        List<BotActivityStep> replaySteps = [];
        string plantSummary;
        if (!IsPlanterBotUnlocked)
        {
            plantSummary = "planter locked";
        }
        else
        {
            int plantedPlotsBefore = CountPlantedPlots();
            string planterMessage = RunPlanterBot(replaySteps);
            int plantedCount = CountPlantedPlots() - plantedPlotsBefore;
            plantSummary = BuildPlantCycleSummary(plantedCount, planterMessage);
        }

        string waterSummary;
        if (!IsWateringBotUnlocked)
        {
            waterSummary = "watering locked";
        }
        else
        {
            int wateredPlotsBefore = CountWateredPlots();
            string wateringMessage = RunWateringBot(replaySteps);
            int wateredCount = CountWateredPlots() - wateredPlotsBefore;
            waterSummary = BuildWaterCycleSummary(wateredCount, wateringMessage);
        }

        AdvanceDay();

        string harvestSummary;
        if (!IsHarvesterBotUnlocked)
        {
            harvestSummary = "harvester locked";
        }
        else
        {
            int harvestedItemsBefore = GetSellableInventoryItemCount();
            string harvesterMessage = RunHarvesterBot(replaySteps);
            int harvestedCount = GetSellableInventoryItemCount() - harvestedItemsBefore;
            harvestSummary = BuildHarvestCycleSummary(harvestedCount, harvesterMessage);
        }

        int sellableItemsBeforeSell = GetSellableInventoryItemCount();
        int moneyBeforeSell = Money;
        string sellMessage = SellSelectedCrop();
        int soldCount = sellableItemsBeforeSell - GetSellableInventoryItemCount();
        int moneyGained = Money - moneyBeforeSell;

        string summary = "Cycle: " + string.Join(", ",
            new[]
            {
                plantSummary,
                waterSummary,
                $"advanced to day {DayNumber}",
                harvestSummary,
                BuildSellCycleSummary(soldCount, moneyGained, sellMessage)
            });

        LastBotActionMessage = summary;
        SetPendingBotActivityReplay(replaySteps);
        return summary;
    }

    public string HarvestCrop()
    {
        return CollectSelectedTile();
    }

    public string CollectSelectedTile()
    {
        FarmPlot selectedPlot = FarmGrid.GetPlot(SelectedPlotX, SelectedPlotY);
        string harvestedItemId = selectedPlot.PlantedCrop is null
            ? string.Empty
            : _contentRegistry.GetTileProducer(selectedPlot.PlantedCrop.CropDefinitionId).OutputItemId;
        string message = HarvestPlot(SelectedPlotX, SelectedPlotY);
        if (message.StartsWith("Harvested", StringComparison.Ordinal))
        {
            Stats.RecordManualHarvest(1);
            if (!string.IsNullOrWhiteSpace(harvestedItemId))
                AddActivity(ActivityLogCategory.Farm, message, null, harvestedItemId, 1);
        }

        return message;
    }

    public string HarvestAllReady()
    {
        int harvestedCount = 0;

        foreach (FarmPlot plot in FarmGrid.GetAllPlots())
        {
            if (!IsPlotHarvestable(plot.X, plot.Y))
                continue;

            HarvestPlot(plot.X, plot.Y);
            harvestedCount++;
        }

        return harvestedCount == 0
            ? "No plots were ready to harvest."
            : $"Harvested {harvestedCount} ready plots.";
    }

    public string RunHarvesterBot()
    {
        return RunHarvesterBot(null);
    }

    private string RunHarvesterBot(List<BotActivityStep> replaySteps)
    {
        if (!IsHarvesterBotUnlocked)
            return LastBotActionMessage = "Harvester Bot is locked.";

        int immaturePlots = CountImmaturePlotsInRegion(HarvesterBot.Region);
        List<BotActivityStep> localReplaySteps = replaySteps ?? [];
        int harvestedCount = HarvesterBot.Run(
            FarmGrid,
            (x, y) =>
            {
                FarmPlotVisualSnapshot before = FarmPlotVisualSnapshot.FromPlot(FarmGrid.GetPlot(x, y));
                bool didHarvest = TryHarvestPlot(x, y);
                if (!didHarvest)
                    return false;

                FarmPlotVisualSnapshot after = FarmPlotVisualSnapshot.FromPlot(FarmGrid.GetPlot(x, y));
                localReplaySteps.Add(new BotActivityStep(BotRole.Collector, x, y, before, after, BuildBotActivityStatusLine(BotRole.Collector, x, y)));
                return true;
            },
            IsPlotHarvestable);

        LastBotActionMessage = harvestedCount == 0
            ? (immaturePlots > 0
                ? $"Harvester Bot found no ready crops. Skipped {immaturePlots} immature crops."
                : "Harvester Bot found no ready crops.")
            : (immaturePlots > 0
                ? $"Harvester Bot harvested {harvestedCount} crops. Skipped {immaturePlots} immature crops."
                : $"Harvester Bot harvested {harvestedCount} crops.");

        if (harvestedCount > 0)
        {
            Stats.RecordHarvesterBotHarvest(harvestedCount);
            AddActivity(ActivityLogCategory.Farm, $"Harvester Bot harvested {harvestedCount} crops.", null, null, harvestedCount);
        }

        if (replaySteps is null)
            SetPendingBotActivityReplay(localReplaySteps);

        return LastBotActionMessage;
    }

    public BotActivityReplay ConsumePendingBotActivityReplay()
    {
        BotActivityReplay replay = PendingBotActivityReplay;
        PendingBotActivityReplay = null;
        return replay;
    }

    public string HarvestPlot(int plotX, int plotY)
    {
        FarmPlot plot = FarmGrid.GetPlot(plotX, plotY);
        if (plot.PlantedCrop is null)
            return $"Plot {plotX},{plotY} is empty.";

        TileProducerDefinition tileProducer = _contentRegistry.GetTileProducer(plot.PlantedCrop.CropDefinitionId);
        if (plot.PlantedCrop.DaysGrown < tileProducer.ProductionDays)
            return $"{tileProducer.Id} at {plotX},{plotY} is not ready yet.";

        AddItem(tileProducer.OutputItemId, 1);
        plot.Harvest();
        return $"Harvested {_contentRegistry.GetItem(tileProducer.OutputItemId).Name} from {plotX},{plotY}.";
    }

    public string SellSelectedCrop()
    {
        return SellInventoryExceptReserved();
    }

    public string SellInventoryExceptReserved(IReadOnlyDictionary<string, int> reservedItemCounts = null)
    {
        reservedItemCounts ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int totalItemsSold = 0;
        int totalValue = 0;
        Dictionary<string, int> soldItemCounts = new(StringComparer.OrdinalIgnoreCase);

        foreach (ItemDefinition itemDefinition in _contentRegistry.Items)
        {
            if (_contentRegistry.IsStarterItem(itemDefinition.Id))
                continue;

            if (itemDefinition.SellPrice <= 0)
                continue;

            string itemId = itemDefinition.Id;
            int itemCount = GetAccessibleItemCount(itemId);
            if (itemCount <= 0)
                continue;

            int reservedCount = reservedItemCounts.TryGetValue(itemId, out int reserved) ? reserved : 0;
            int quantityToSell = Math.Max(0, itemCount - reservedCount);
            if (quantityToSell <= 0)
                continue;

            int inventoryCount = GetItemCount(itemId);
            int quantityFromInventory = Math.Min(inventoryCount, quantityToSell);
            int quantityFromStorage = Math.Min(GetStorageItemCount(itemId), quantityToSell - quantityFromInventory);
            int effectiveSellPrice = GetEffectiveSellPrice(itemId);

            totalItemsSold += quantityToSell;
            totalValue += quantityToSell * effectiveSellPrice;
            soldItemCounts[itemId] = quantityToSell;
            if (quantityFromInventory > 0)
                RemoveItem(itemId, quantityFromInventory);

            if (quantityFromStorage > 0)
                RemoveStorageItem(itemId, quantityFromStorage);
        }

        if (totalItemsSold == 0)
            return "No harvested crops to sell.";

        Money += totalValue;
        Stats.RecordMoneyEarnedFromSelling(totalValue, totalItemsSold);
        string soldItemId = soldItemCounts.Count == 1 ? soldItemCounts.Keys.First() : null;
        AddActivity(ActivityLogCategory.Sale, BuildSaleActivityMessage(soldItemCounts, totalItemsSold, totalValue), totalValue, soldItemId, totalItemsSold);
        return $"Sold {totalItemsSold} items for {totalValue}.";
    }

    public string BuySelectedSeed(bool buyAsManyAsPossible = false)
    {
        return BuySeedItem(SelectedSeedItemId, buyAsManyAsPossible);
    }

    public string BuySeedItem(string seedItemId, bool buyAsManyAsPossible = false)
    {
        ItemDefinition selectedSeed = _contentRegistry.GetItem(seedItemId);

        if (!_contentRegistry.IsStarterItem(selectedSeed.Id))
            return $"{selectedSeed.Name} is not a starter item.";

        if (selectedSeed.BuyPrice <= 0)
            return $"{selectedSeed.Name} cannot be bought.";

        if (Money < selectedSeed.BuyPrice)
            return $"Need {selectedSeed.BuyPrice} to buy {selectedSeed.Name}.";

        int quantityToBuy = buyAsManyAsPossible
            ? Money / selectedSeed.BuyPrice
            : 1;

        if (quantityToBuy <= 0)
            return $"Need {selectedSeed.BuyPrice} to buy {selectedSeed.Name}.";

        int totalCost = quantityToBuy * selectedSeed.BuyPrice;
        Money -= totalCost;
        AddItem(selectedSeed.Id, quantityToBuy);
        Stats.RecordMoneySpentOnSeeds(totalCost, quantityToBuy);
        AddActivity(ActivityLogCategory.Farm, $"Bought {quantityToBuy} {selectedSeed.Name} for ${totalCost}.", totalCost, selectedSeed.Id, quantityToBuy);

        return $"Bought {quantityToBuy} {selectedSeed.Name} for {totalCost}.";
    }

    public string BuyMarketOffer(string marketOfferId)
    {
        MarketOfferDefinition marketOffer = _contentRegistry.GetMarketOffer(marketOfferId);
        ItemDefinition item = _contentRegistry.GetItem(marketOffer.ItemId);

        if (Money < marketOffer.Price)
            return $"Not enough money for {marketOffer.Quantity} {item.Name}.";

        Money -= marketOffer.Price;
        AddItem(marketOffer.ItemId, marketOffer.Quantity);
        Stats.RecordMarketPurchase(marketOffer.Price, marketOffer.Quantity);
        AddActivity(ActivityLogCategory.Market, $"Bought {marketOffer.Quantity} {item.Name} for ${marketOffer.Price}.", marketOffer.Price, marketOffer.ItemId, marketOffer.Quantity);
        return $"Bought {marketOffer.Quantity} {item.Name} for {marketOffer.Price}.";
    }

    public bool IsPlantedCropHarvestable()
    {
        return IsPlotHarvestable(SelectedPlotX, SelectedPlotY);
    }

    public string DepositAllSellablesToStorage()
    {
        int itemsMoved = 0;
        foreach (string itemId in _inventory.Keys.ToList())
        {
            if (!_contentRegistry.ItemsById.TryGetValue(itemId, out ItemDefinition itemDefinition))
                continue;

            if (_contentRegistry.IsStarterItem(itemDefinition.Id) || itemDefinition.SellPrice <= 0)
                continue;

            int itemCount = GetItemCount(itemId);
            if (itemCount <= 0)
                continue;

            RemoveItem(itemId, itemCount);
            AddStorageItem(itemId, itemCount);
            itemsMoved += itemCount;
        }

        return itemsMoved == 0
            ? "No sellable items to store."
            : RecordStorageActivity($"Stored {itemsMoved} items in shared storage.", itemsMoved);
    }

    public string WithdrawAllFromStorage()
    {
        if (_sharedStorage.Count == 0)
            return "Shared storage is empty.";

        int itemsMoved = 0;
        foreach (string itemId in _sharedStorage.Keys.ToList())
        {
            int itemCount = GetStorageItemCount(itemId);
            if (itemCount <= 0)
                continue;

            RemoveStorageItem(itemId, itemCount);
            AddItem(itemId, itemCount);
            itemsMoved += itemCount;
        }

        return RecordStorageActivity($"Withdrew {itemsMoved} items from shared storage.", itemsMoved);
    }

    public bool IsPlotHarvestable(int plotX, int plotY)
    {
        FarmPlot plot = FarmGrid.GetPlot(plotX, plotY);
        if (plot.PlantedCrop is null)
            return false;

        TileProducerDefinition tileProducer = _contentRegistry.GetTileProducer(plot.PlantedCrop.CropDefinitionId);
        return plot.PlantedCrop.DaysGrown >= tileProducer.ProductionDays;
    }

    public FarmPlot GetSelectedPlot()
    {
        return FarmGrid.GetPlot(SelectedPlotX, SelectedPlotY);
    }

    public FarmPlot GetSelectedTile()
    {
        return GetSelectedPlot();
    }

    public ProductionGridView GetProductionGrid()
    {
        return new ProductionGridView(FarmGrid, _contentRegistry);
    }

    public IReadOnlyList<ProductionArea> GetProductionAreas()
    {
        return [GetDefaultProductionArea()];
    }

    public ProductionArea GetDefaultProductionArea()
    {
        return new ProductionArea(
            id: "outdoor_garden",
            name: "Outdoor Garden",
            areaType: "production_tiles",
            isUnlocked: true,
            productionGrid: GetProductionGrid(),
            displayOrder: 0,
            interfaceTokenId: ThemeVocabulary.ThemeId);
    }

    public IReadOnlyList<ProductionArea> GetUnlockedProductionAreas()
    {
        return GetProductionAreas()
            .Where(area => area.IsUnlocked)
            .OrderBy(area => area.DisplayOrder)
            .ThenBy(area => area.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ProductionTileView GetSelectedProductionTile()
    {
        return new ProductionTileView(GetSelectedTile(), _contentRegistry);
    }

    public ProductionTileView GetProductionTile(int plotX, int plotY)
    {
        return new ProductionTileView(FarmGrid.GetPlot(plotX, plotY), _contentRegistry);
    }

    public int GetItemCount(string itemId)
    {
        return _inventory.TryGetValue(itemId, out int count) ? count : 0;
    }

    public int GetStorageItemCount(string itemId)
    {
        return _sharedStorage.TryGetValue(itemId, out int count) ? count : 0;
    }

    public int GetAccessibleItemCount(string itemId)
    {
        return GetItemCount(itemId) + GetStorageItemCount(itemId);
    }

    public int GetSharedStorageItemCount()
    {
        return _sharedStorage.Values.Sum();
    }

    public int GetSharedStorageSellValue()
    {
        return _sharedStorage.Sum(entry =>
            _contentRegistry.ItemsById.TryGetValue(entry.Key, out ItemDefinition itemDefinition) && itemDefinition.SellPrice > 0
                ? entry.Value * GetEffectiveSellPrice(entry.Key)
                : 0);
    }

    public SaveGameState CreateSaveGame()
    {
        return new SaveGameState
        {
            Version = SaveGameState.CurrentVersion,
            DayNumber = DayNumber,
            Money = Money,
            FarmWidth = FarmGrid.Width,
            FarmHeight = FarmGrid.Height,
            SelectedPlotX = SelectedPlotX,
            SelectedPlotY = SelectedPlotY,
            SelectedSeedItemId = SelectedSeedItemId,
            SelectedProcessorMachineIndex = SelectedProcessorMachineIndex,
            Plots = FarmGrid.GetAllPlots()
                .Select(plot => new FarmPlotSaveState
                {
                    X = plot.X,
                    Y = plot.Y,
                    CropDefinitionId = plot.PlantedCrop?.CropDefinitionId,
                    DaysGrown = plot.PlantedCrop?.DaysGrown ?? 0,
                    IsWatered = plot.IsWatered
                })
                .ToList(),
            Inventory = _inventory
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Select(entry => new ItemStackSaveState { ItemId = entry.Key, Quantity = entry.Value })
                .ToList(),
            SharedStorage = _sharedStorage
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Select(entry => new ItemStackSaveState { ItemId = entry.Key, Quantity = entry.Value })
                .ToList(),
            Bots = new BotSaveState
            {
                IsPlanterUnlocked = IsPlanterBotUnlocked,
                IsWateringUnlocked = IsWateringBotUnlocked,
                IsHarvesterUnlocked = IsHarvesterBotUnlocked,
                IsWateringEnabled = WateringBot.Enabled,
                PlanterAssignedSeedItemId = PlanterBot.AssignedSeedItemId,
                PlanterRegion = ToSaveState(PlanterBot.Region),
                WateringRegion = ToSaveState(WateringBot.Region),
                HarvesterRegion = ToSaveState(HarvesterBot.Region)
            },
            Machines = _processorMachines
                .Select(machine => new ProcessorMachineSaveState
                {
                    MachineId = machine.MachineId,
                    CurrentRecipeId = machine.CurrentRecipeId,
                    InputItemId = machine.InputItemId,
                    InputQuantity = machine.InputQuantity,
                    OutputItemId = machine.OutputItemId,
                    OutputQuantity = machine.OutputQuantity,
                    DaysRemaining = machine.DaysRemaining,
                    HasOutputReady = machine.HasOutputReady
                })
                .ToList(),
            Stats = Stats.CreateSaveState(),
            CleanupSite = CleanupSite is null
                ? null
                : new CleanupSiteSaveState
                {
                    Id = CleanupSite.Id,
                    IsCleared = CleanupSite.IsCleared
                },
            ClearedRestorationSiteIds = _clearedRestorationSiteIds
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            MarketOpportunity = ActiveMarketOpportunity is null
                ? null
                : new MarketOpportunitySaveState
                {
                    Id = ActiveMarketOpportunity.Id,
                    ItemId = ActiveMarketOpportunity.ItemId,
                    SellPriceMultiplier = ActiveMarketOpportunity.SellPriceMultiplier,
                    DaysRemaining = ActiveMarketOpportunity.DaysRemaining,
                    DisplayName = ActiveMarketOpportunity.DisplayName,
                    Message = ActiveMarketOpportunity.Message
                },
            LastGeneratedMarketOpportunityDay = LastGeneratedMarketOpportunityDay,
            GeneratedMarketOpportunityCount = GeneratedMarketOpportunityCount,
            ActivityLog = _activityLog
                .Select(entry => new ActivityLogEntrySaveState
                {
                    Day = entry.Day,
                    Category = entry.Category.ToString(),
                    Message = entry.Message,
                    ValueAmount = entry.ValueAmount,
                    ItemId = entry.ItemId,
                    Quantity = entry.Quantity
                })
                .ToList()
        };
    }

    public GameStateValidationResult ValidateCurrentState()
    {
        return new GameStateValidator().Validate(this, _contentRegistry);
    }

    public SaveLoadValidationResult ValidateSaveGame(SaveGameState saveGame)
    {
        if (saveGame is null)
            return new SaveLoadValidationResult(["Save data was missing."]);

        List<string> errors = [];

        if (saveGame.Version != SaveGameState.CurrentVersion)
            errors.Add($"Unsupported save version {saveGame.Version}.");

        if (saveGame.FarmHeight != InitialFarmHeight)
            errors.Add($"Unsupported farm height {saveGame.FarmHeight}.");

        if (saveGame.FarmWidth < InitialFarmWidth || saveGame.FarmWidth > InitialFarmWidth + MaxFarmExpansionLevel)
            errors.Add($"Unsupported farm width {saveGame.FarmWidth}.");

        ValidateSavedItems(errors, saveGame.Inventory, "player inventory");
        ValidateSavedItems(errors, saveGame.SharedStorage, "storage");
        ValidateSavedPlots(errors, saveGame.Plots, saveGame.FarmWidth, saveGame.FarmHeight);
        ValidateSavedSeed(errors, saveGame.SelectedSeedItemId, "selected seed");
        ValidateSavedSeed(errors, saveGame.Bots?.PlanterAssignedSeedItemId, "planter assigned seed");
        ValidateSavedMachines(errors, saveGame.Machines);
        ValidateProcessedOutputs(errors, saveGame.Stats?.ProcessedOutputs);
        ValidateSavedCleanupState(errors, saveGame.CleanupSite, saveGame.ClearedRestorationSiteIds, saveGame.FarmWidth, saveGame.Stats);
        ValidateSavedMarketOpportunity(errors, saveGame.MarketOpportunity);
        ValidateSavedMarketOpportunityGeneratorState(errors, saveGame);
        ValidateActivityLog(errors, saveGame.ActivityLog);

        return new SaveLoadValidationResult(errors);
    }

    public void ApplySaveGame(SaveGameState saveGame)
    {
        SaveLoadValidationResult validation = ValidateSaveGame(saveGame);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Errors));

        Reset();
        FarmGrid.Resize(saveGame.FarmWidth, saveGame.FarmHeight);
        FarmGrid.Clear();
        _inventory.Clear();
        _sharedStorage.Clear();
        _activityLog.Clear();
        Stats.Load(saveGame.Stats);

        DayNumber = Math.Max(1, saveGame.DayNumber);
        Money = Math.Max(0, saveGame.Money);
        SelectedPlotX = Math.Clamp(saveGame.SelectedPlotX, 0, FarmGrid.Width - 1);
        SelectedPlotY = Math.Clamp(saveGame.SelectedPlotY, 0, FarmGrid.Height - 1);
        SelectedProcessorMachineIndex = saveGame.SelectedProcessorMachineIndex >= 0 && saveGame.SelectedProcessorMachineIndex < _processorMachines.Count
            ? saveGame.SelectedProcessorMachineIndex
            : 0;

        foreach (ItemStackSaveState item in saveGame.Inventory ?? [])
            AddSavedItem(_inventory, item, isStorage: false);

        foreach (ItemStackSaveState item in saveGame.SharedStorage ?? [])
            AddSavedItem(_sharedStorage, item, isStorage: true);

        foreach (FarmPlotSaveState plotState in saveGame.Plots ?? [])
            RestorePlot(plotState);

        BotSaveState botState = saveGame.Bots ?? new BotSaveState();
        IsPlanterBotUnlocked = botState.IsPlanterUnlocked;
        IsWateringBotUnlocked = botState.IsWateringUnlocked;
        IsHarvesterBotUnlocked = botState.IsHarvesterUnlocked;
        SelectedSeedItemId = NormalizeSeedItemId(saveGame.SelectedSeedItemId);
        PlanterBot.AssignedSeedItemId = NormalizeSeedItemId(botState.PlanterAssignedSeedItemId, SelectedSeedItemId);
        PlanterBot.SetRegion(ToRegion(botState.PlanterRegion, new FarmRegion(0, 0, FarmGrid.Width, FarmGrid.Height)));
        WateringBot.SetRegion(ToRegion(botState.WateringRegion, new FarmRegion(0, 0, FarmGrid.Width, FarmGrid.Height)));
        HarvesterBot.SetRegion(ToRegion(botState.HarvesterRegion, new FarmRegion(0, 0, FarmGrid.Width, FarmGrid.Height)));
        WateringBot.Enabled = botState.IsWateringEnabled && IsWateringBotUnlocked;

        foreach (ProcessorMachineInstance machine in _processorMachines)
            machine.Reset();

        foreach (ProcessorMachineSaveState machineState in saveGame.Machines ?? [])
            RestoreMachine(machineState);

        RestoreClearedRestorationSiteIds(saveGame.ClearedRestorationSiteIds, saveGame.CleanupSite);
        ActiveMarketOpportunity = CreateMarketOpportunity(saveGame.MarketOpportunity);
        LastGeneratedMarketOpportunityDay = Math.Max(0, saveGame.LastGeneratedMarketOpportunityDay);
        GeneratedMarketOpportunityCount = Math.Max(0, saveGame.GeneratedMarketOpportunityCount);

        foreach (ActivityLogEntrySaveState activity in saveGame.ActivityLog ?? [])
        {
            if (string.IsNullOrWhiteSpace(activity?.Message) || string.IsNullOrWhiteSpace(activity.Category))
                continue;

            if (!Enum.TryParse(activity.Category, ignoreCase: true, out ActivityLogCategory category))
                throw new InvalidOperationException($"Unknown activity log category '{activity.Category}'.");

            _activityLog.Add(new ActivityLogEntry(
                Math.Max(1, activity.Day),
                category,
                activity.Message,
                activity.ValueAmount,
                activity.ItemId,
                activity.Quantity));
        }

        if (_activityLog.Count > BalanceConfig.ActivityLogMaxEntries)
            _activityLog.RemoveRange(0, _activityLog.Count - BalanceConfig.ActivityLogMaxEntries);

        PendingBotActivityReplay = null;
        LastBotActionMessage = "No bot action yet.";
    }

    private static string GetDefaultSeedItemId(IReadOnlyList<string> seedItemIds)
    {
        string turnipSeed = seedItemIds.FirstOrDefault(seedItemId =>
            string.Equals(seedItemId, "turnip_seed", StringComparison.OrdinalIgnoreCase));

        return turnipSeed ?? seedItemIds[0];
    }

    private static List<ProcessorMachineInstance> CreateProcessorMachines(ContentRegistry contentRegistry)
    {
        return contentRegistry.LoadedMachines
            .Select(loadedMachine => loadedMachine.Definition.Id)
            .Where(machineId => !string.IsNullOrWhiteSpace(machineId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(machineId => new ProcessorMachineInstance(machineId))
            .ToList();
    }

    private static FarmRegionSaveState ToSaveState(FarmRegion region)
    {
        return new FarmRegionSaveState
        {
            X = region.X,
            Y = region.Y,
            Width = region.Width,
            Height = region.Height
        };
    }

    private int CountEmptyPlotsInRegion(FarmRegion region)
    {
        return FarmGrid.GetAllPlots()
            .Count(plot => region.Contains(plot.X, plot.Y) && plot.IsEmpty);
    }

    private int CountPlantedPlots()
    {
        return FarmGrid.GetAllPlots().Count(plot => !plot.IsEmpty);
    }

    private int CountWateredPlots()
    {
        return FarmGrid.GetAllPlots().Count(plot => plot.IsWatered);
    }

    private int CountImmaturePlotsInRegion(FarmRegion region)
    {
        return FarmGrid.GetAllPlots()
            .Count(plot =>
                region.Contains(plot.X, plot.Y)
                && plot.PlantedCrop is not null
                && !IsPlotHarvestable(plot.X, plot.Y));
    }

    private int GetSellableInventoryItemCount()
    {
        return _contentRegistry.Items
            .Where(item => !_contentRegistry.IsStarterItem(item.Id) && item.SellPrice > 0)
            .Sum(item => GetAccessibleItemCount(item.Id));
    }

    private int GetNextFarmExpansionCost()
    {
        return FarmExpansionLevel switch
        {
            0 => BalanceConfig.FirstExpansionCost,
            1 => BalanceConfig.SecondExpansionCost,
            _ => 0
        };
    }

    private CleanupSiteState CreateCleanupSiteState(RestorationSiteDefinition restorationSite)
    {
        return new CleanupSiteState
        {
            Id = restorationSite.Id,
            Name = restorationSite.Name,
            Description = restorationSite.Description,
            Cost = restorationSite.Cost,
            RewardDescription = restorationSite.RewardDescription,
            IsCleared = _clearedRestorationSiteIds.Contains(restorationSite.Id)
        };
    }

    public CleanupSiteState GetNextRelevantCleanupSite()
    {
        CleanupSiteState blockingSite = GetBlockingCleanupSiteForNextExpansion();
        if (blockingSite is not null && !blockingSite.IsCleared)
            return blockingSite;

        return RestorationSites
            .Where(site => !site.IsCleared)
            .OrderBy(site => site.Cost)
            .ThenBy(site => site.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private void UpdateDefaultBotRegions()
    {
        FarmRegion fullFarmRegion = new(0, 0, FarmGrid.Width, FarmGrid.Height);

        if (_usesDefaultPlanterBotRegion)
            PlanterBot.SetRegion(fullFarmRegion);

        if (_usesDefaultWateringBotRegion)
            WateringBot.SetRegion(fullFarmRegion);

        if (_usesDefaultHarvesterBotRegion)
            HarvesterBot.SetRegion(fullFarmRegion);
    }

    private void ClampSelectedPlotToBounds()
    {
        SelectedPlotX = Math.Clamp(SelectedPlotX, 0, FarmGrid.Width - 1);
        SelectedPlotY = Math.Clamp(SelectedPlotY, 0, FarmGrid.Height - 1);
    }

    private string UnlockBot(string botName, int unlockCost, bool isUnlocked, Action unlock)
    {
        if (isUnlocked)
            return $"{botName} already unlocked.";

        if (Money < unlockCost)
            return $"Need {unlockCost} to unlock {botName}.";

        Money -= unlockCost;
        unlock();
        Stats.RecordMoneySpentOnBotUnlock(unlockCost);
        AddActivity(ActivityLogCategory.Bot, $"Unlocked {botName} for ${unlockCost}.", unlockCost);
        return $"Unlocked {botName}.";
    }

    private string RecordStorageActivity(string message, int quantity)
    {
        AddActivity(ActivityLogCategory.Storage, message, null, null, quantity);
        return message;
    }

    private void ReplaceActiveMarketOpportunity(MarketOpportunity opportunity, bool addActivity)
    {
        if (addActivity && ActiveMarketOpportunity is not null)
            AddActivity(ActivityLogCategory.Market, $"{ActiveMarketOpportunity.DisplayName} ended.");

        ActiveMarketOpportunity = opportunity?.Clone();
        if (ActiveMarketOpportunity is null || !ActiveMarketOpportunity.IsActive)
        {
            ActiveMarketOpportunity = null;
            return;
        }

        if (addActivity)
            AddActivity(ActivityLogCategory.Market, $"{ActiveMarketOpportunity.DisplayName} started: {ActiveMarketOpportunity.Message}");
    }

    private void AdvanceMarketOpportunity()
    {
        if (ActiveMarketOpportunity is null)
            return;

        ActiveMarketOpportunity.DaysRemaining = Math.Max(0, ActiveMarketOpportunity.DaysRemaining - 1);
        if (ActiveMarketOpportunity.DaysRemaining > 0)
            return;

        AddActivity(ActivityLogCategory.Market, $"{ActiveMarketOpportunity.DisplayName} ended.");
        ActiveMarketOpportunity = null;
    }

    private MarketOpportunity CreateGeneratedMarketOpportunity(string itemId)
    {
        if (!IsMeaningfulOpportunityItem(itemId))
            return null;

        ItemDefinition item = _contentRegistry.GetItem(itemId);
        int durationDays = BalanceConfig.MarketOpportunityGeneratedDurationDays;
        double multiplier = BalanceConfig.MarketOpportunityGeneratedMultiplier;
        int multiplierPercent = (int)Math.Round(multiplier * 100, MidpointRounding.AwayFromZero);

        return new MarketOpportunity
        {
            Id = $"generated_{item.Id}_{GeneratedMarketOpportunityCount + 1}",
            ItemId = item.Id,
            SellPriceMultiplier = multiplier,
            DaysRemaining = durationDays,
            DisplayName = $"{item.Name} Boom",
            Message = $"{item.Name} sells for {multiplierPercent}% for {durationDays} days."
        };
    }

    private string BuildSaleActivityMessage(IReadOnlyDictionary<string, int> soldItemCounts, int totalItemsSold, int totalValue)
    {
        if (soldItemCounts.Count == 1)
        {
            KeyValuePair<string, int> soldItem = soldItemCounts.Single();
            return $"Sold {soldItem.Value} {GetItemDisplayName(soldItem.Key)} for ${totalValue}.";
        }

        return $"Sold {totalItemsSold} items for ${totalValue}.";
    }

    private void AddActivity(ActivityLogCategory category, string message, int? valueAmount = null, string itemId = null, int? quantity = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _activityLog.Add(new ActivityLogEntry(DayNumber, category, message, valueAmount, itemId, quantity));
        if (_activityLog.Count <= BalanceConfig.ActivityLogMaxEntries)
            return;

        _activityLog.RemoveRange(0, _activityLog.Count - BalanceConfig.ActivityLogMaxEntries);
    }

    private static string FormatSignedCurrency(int value)
    {
        if (value > 0)
            return $"+${value}";

        if (value < 0)
            return $"-${Math.Abs(value)}";

        return "$0";
    }

    private static string BuildPlantCycleSummary(int plantedCount, string planterMessage)
    {
        if (plantedCount > 0)
            return $"planted {plantedCount}";

        if (planterMessage.Contains("no seeds", StringComparison.OrdinalIgnoreCase))
            return "no seeds";

        if (planterMessage.Contains("not plantable", StringComparison.OrdinalIgnoreCase))
            return "seed not plantable";

        return "no empty plots";
    }

    private static string BuildWaterCycleSummary(int wateredCount, string wateringMessage)
    {
        return wateredCount > 0
            ? $"watered {wateredCount}"
            : "no dry planted plots";
    }

    private static string BuildHarvestCycleSummary(int harvestedCount, string harvesterMessage)
    {
        if (harvestedCount > 0)
            return $"harvested {harvestedCount}";

        return harvesterMessage.Contains("no ready crops", StringComparison.OrdinalIgnoreCase)
            ? "no ready crops"
            : "harvested 0";
    }

    private static string BuildSellCycleSummary(int soldCount, int moneyGained, string sellMessage)
    {
        return soldCount > 0
            ? $"sold {soldCount} for {moneyGained}"
            : "nothing to sell";
    }

    private ProcessorMachineInstance GetProcessorMachine(string machineId)
    {
        ProcessorMachineInstance machine = _processorMachines.FirstOrDefault(candidate =>
            string.Equals(candidate.MachineId, machineId, StringComparison.OrdinalIgnoreCase));

        if (machine is null)
            throw new KeyNotFoundException($"Missing processor machine '{machineId}'.");

        return machine;
    }

    private ProcessingRecipeDefinition GetStartableMachineRecipe(string machineId, string preferredRecipeId = null)
    {
        List<ProcessingRecipeDefinition> recipes = GetMachineRecipes(machineId);

        if (!string.IsNullOrWhiteSpace(preferredRecipeId))
        {
            ProcessingRecipeDefinition preferredRecipe = recipes.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, preferredRecipeId, StringComparison.OrdinalIgnoreCase)
                && CanConsumeMachineInputFromInventoryOrStorage(candidate.InputItemId, candidate.InputQuantity));

            if (preferredRecipe is not null)
                return preferredRecipe;
        }

        return recipes.FirstOrDefault(candidate => CanConsumeMachineInputFromInventoryOrStorage(candidate.InputItemId, candidate.InputQuantity));
    }

    private List<ProcessingRecipeDefinition> GetMachineRecipes(string machineId)
    {
        return _contentRegistry
            .GetProcessingRecipesForMachine(machineId)
            .ToList();
    }

    private string GetNoValidMachineInputMessage(string machineId)
    {
        List<ProcessingRecipeDefinition> recipes = GetMachineRecipes(machineId);
        string machineActionName = GetMachineActionName(machineId);
        if (recipes.Count == 0)
            return $"No valid {machineActionName} input.";

        string neededInputs = string.Join(" or ", recipes.Select(recipe => $"{recipe.InputQuantity} {GetItemDisplayName(recipe.InputItemId)}"));
        return $"No valid {machineActionName} input. Need {neededInputs}.";
    }

    private string GetPreferredRecipeMissingInputMessage(string machineId, string preferredRecipeId)
    {
        ProcessingRecipeDefinition recipe = GetMachineRecipes(machineId)
            .FirstOrDefault(candidate => string.Equals(candidate.Id, preferredRecipeId, StringComparison.OrdinalIgnoreCase));

        if (recipe is null)
            return GetNoValidMachineInputMessage(machineId);

        return $"Need {recipe.InputQuantity} {GetItemDisplayName(recipe.InputItemId)} for {GetItemDisplayName(recipe.OutputItemId)}.";
    }

    private string GetMachineDisplayName(string machineId)
    {
        return _contentRegistry.GetMachine(machineId).Name;
    }

    private string GetMachineActionName(string machineId)
    {
        return GetMachineDisplayName(machineId).ToLowerInvariant();
    }

    private string GetItemDisplayName(string itemId)
    {
        return _contentRegistry.GetItem(itemId).Name;
    }

    private static string FormatDayCount(int daysRemaining)
    {
        return daysRemaining == 1 ? "1 day" : $"{daysRemaining} days";
    }

    private string BuildBotActivityStatusLine(BotRole role, int tileX, int tileY)
    {
        string botName = BotRoleFormatter.GetBotDisplayName(role, ThemeVocabulary);
        string verb = role switch
        {
            BotRole.Starter => UsesDefaultFarmingTheme() ? "planting" : "starting",
            BotRole.Maintainer => ToPresentParticiple(ThemeVocabulary.MaintainActionName),
            BotRole.Collector => ToPresentParticiple(ThemeVocabulary.CollectActionName),
            _ => "working"
        };

        return $"{botName} {verb} tile {tileX},{tileY}";
    }

    private void SetPendingBotActivityReplay(List<BotActivityStep> replaySteps)
    {
        PendingBotActivityReplay = replaySteps is { Count: > 0 }
            ? new BotActivityReplay(replaySteps)
            : null;
    }

    private bool UsesDefaultFarmingTheme()
    {
        return string.Equals(ThemeVocabulary.ThemeId, ThemeVocabulary.FarmingDefault.ThemeId, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToPresentParticiple(string verb)
    {
        if (string.IsNullOrWhiteSpace(verb))
            return "working";

        string normalized = verb.ToLowerInvariant();
        if (normalized.EndsWith("e", StringComparison.Ordinal) && !normalized.EndsWith("ee", StringComparison.Ordinal))
            return normalized[..^1] + "ing";

        return normalized + "ing";
    }

    private bool TryHarvestPlot(int plotX, int plotY)
    {
        if (!IsPlotHarvestable(plotX, plotY))
            return false;

        HarvestPlot(plotX, plotY);
        return true;
    }

    private bool TryPlantSeedAtPlot(int plotX, int plotY, string seedItemId, out string message)
    {
        FarmPlot plot = FarmGrid.GetPlot(plotX, plotY);

        if (!plot.IsEmpty)
        {
            message = $"Plot {plotX},{plotY} is already planted.";
            return false;
        }

        if (!_contentRegistry.TryGetTileProducerByStarterItemId(seedItemId, out TileProducerDefinition tileProducer))
        {
            message = $"No producer uses starter item '{seedItemId}'.";
            return false;
        }

        ItemDefinition seedItem = _contentRegistry.GetItem(seedItemId);
        if (GetItemCount(seedItemId) <= 0)
        {
            message = $"No {seedItem.Name} left to plant.";
            return false;
        }

        RemoveItem(seedItemId, 1);
        plot.TryPlant(tileProducer.Id);
        message = $"Planted {tileProducer.Id} at {plotX},{plotY}.";
        return true;
    }

    private bool CanConsumeMachineInputFromInventoryOrStorage(string itemId, int quantity)
    {
        return GetItemCount(itemId) >= quantity || GetStorageItemCount(itemId) >= quantity;
    }

    private bool TryConsumeMachineInputFromInventoryOrStorage(string itemId, int quantity)
    {
        if (GetItemCount(itemId) >= quantity)
        {
            RemoveItem(itemId, quantity);
            return true;
        }

        if (GetStorageItemCount(itemId) >= quantity)
        {
            RemoveStorageItem(itemId, quantity);
            return true;
        }

        return false;
    }

    private void AddSavedItem(IDictionary<string, int> target, ItemStackSaveState item, bool isStorage)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.ItemId) || item.Quantity <= 0)
            return;

        _contentRegistry.GetItem(item.ItemId);
        target[item.ItemId] = item.Quantity;
    }

    private void RestorePlot(FarmPlotSaveState plotState)
    {
        if (plotState is null)
            return;

        if (plotState.X < 0 || plotState.X >= FarmGrid.Width || plotState.Y < 0 || plotState.Y >= FarmGrid.Height)
            throw new InvalidOperationException($"Plot {plotState.X},{plotState.Y} is outside the saved farm bounds.");

        if (!string.IsNullOrWhiteSpace(plotState.CropDefinitionId))
            _contentRegistry.GetTileProducer(plotState.CropDefinitionId);

        FarmGrid.GetPlot(plotState.X, plotState.Y).Restore(plotState.CropDefinitionId, plotState.DaysGrown, plotState.IsWatered);
    }

    private void RestoreMachine(ProcessorMachineSaveState machineState)
    {
        if (machineState is null || string.IsNullOrWhiteSpace(machineState.MachineId))
            return;

        ProcessorMachineInstance machine = GetProcessorMachine(machineState.MachineId);

        if (!string.IsNullOrWhiteSpace(machineState.CurrentRecipeId))
        {
            _contentRegistry.GetProcessingRecipe(machineState.CurrentRecipeId);

            if (!string.IsNullOrWhiteSpace(machineState.InputItemId))
                _contentRegistry.GetItem(machineState.InputItemId);

            if (!string.IsNullOrWhiteSpace(machineState.OutputItemId))
                _contentRegistry.GetItem(machineState.OutputItemId);
        }

        machine.Restore(
            machineState.CurrentRecipeId,
            machineState.InputItemId,
            machineState.InputQuantity,
            machineState.OutputItemId,
            machineState.OutputQuantity,
            machineState.DaysRemaining,
            machineState.HasOutputReady);
    }

    private MarketOpportunity CreateMarketOpportunity(MarketOpportunitySaveState marketOpportunity)
    {
        if (marketOpportunity is null)
            return null;

        MarketOpportunity restoredOpportunity = new()
        {
            Id = marketOpportunity.Id,
            ItemId = marketOpportunity.ItemId,
            SellPriceMultiplier = marketOpportunity.SellPriceMultiplier,
            DaysRemaining = marketOpportunity.DaysRemaining,
            DisplayName = marketOpportunity.DisplayName,
            Message = marketOpportunity.Message
        };

        return restoredOpportunity.IsActive ? restoredOpportunity : null;
    }

    private void ValidateSavedMarketOpportunity(List<string> errors, MarketOpportunitySaveState marketOpportunity)
    {
        if (marketOpportunity is null)
            return;

        if (string.IsNullOrWhiteSpace(marketOpportunity.ItemId) || !_contentRegistry.ItemsById.ContainsKey(marketOpportunity.ItemId))
            errors.Add($"Missing item id in market opportunity: '{marketOpportunity.ItemId}'.");

        if (marketOpportunity.SellPriceMultiplier <= 0)
            errors.Add($"Market opportunity multiplier must be greater than 0: {marketOpportunity.SellPriceMultiplier}.");

        if (marketOpportunity.DaysRemaining < 0)
            errors.Add($"Market opportunity days remaining cannot be negative: {marketOpportunity.DaysRemaining}.");
    }

    private void ValidateSavedMarketOpportunityGeneratorState(List<string> errors, SaveGameState saveGame)
    {
        if (saveGame.LastGeneratedMarketOpportunityDay < 0)
            errors.Add($"Last generated market opportunity day cannot be negative: {saveGame.LastGeneratedMarketOpportunityDay}.");

        if (saveGame.GeneratedMarketOpportunityCount < 0)
            errors.Add($"Generated market opportunity count cannot be negative: {saveGame.GeneratedMarketOpportunityCount}.");
    }

    private bool IsMeaningfulOpportunityItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || !_contentRegistry.ItemsById.TryGetValue(itemId, out ItemDefinition item))
            return false;

        return item.SellPrice > 0 && !_contentRegistry.IsStarterItem(itemId);
    }

    private void ValidateSavedCleanupState(
        List<string> errors,
        CleanupSiteSaveState cleanupSite,
        IReadOnlyCollection<string> clearedRestorationSiteIds,
        int farmWidth,
        GameStatsSaveState stats)
    {
        HashSet<string> normalizedClearedSiteIds = new(StringComparer.OrdinalIgnoreCase);

        foreach (string siteId in clearedRestorationSiteIds ?? [])
        {
            if (string.IsNullOrWhiteSpace(siteId))
                continue;

            if (!_contentRegistry.RestorationSitesById.ContainsKey(siteId))
                errors.Add($"Unknown restoration site id '{siteId}'.");
            else
                normalizedClearedSiteIds.Add(siteId);
        }

        if (cleanupSite is not null)
        {
            if (!_contentRegistry.RestorationSitesById.ContainsKey(cleanupSite.Id))
                errors.Add($"Unknown cleanup site id '{cleanupSite.Id}'.");
            else if (cleanupSite.IsCleared)
                normalizedClearedSiteIds.Add(cleanupSite.Id);
        }

        if (stats is not null)
        {
            if (stats.CleanupSitesCleared < 0)
                errors.Add($"CleanupSitesCleared cannot be negative: {stats.CleanupSitesCleared}.");

            if (stats.MoneySpentOnCleanup < 0)
                errors.Add($"MoneySpentOnCleanup cannot be negative: {stats.MoneySpentOnCleanup}.");
        }

        RestorationSiteDefinition requiredSite = GetRestorationSiteUnlockingExpansionLevel(MaxFarmExpansionLevel);
        if (farmWidth > InitialFarmWidth + 1 && requiredSite is null)
            errors.Add($"Farm width {farmWidth} requires a restoration site that unlocks expansion level {MaxFarmExpansionLevel}.");

        if (farmWidth > InitialFarmWidth + 1 && requiredSite is not null && !normalizedClearedSiteIds.Contains(requiredSite.Id))
            errors.Add($"Farm width {farmWidth} requires {requiredSite.Name} to be cleared.");
    }

    private void RestoreClearedRestorationSiteIds(IReadOnlyCollection<string> clearedRestorationSiteIds, CleanupSiteSaveState cleanupSite)
    {
        _clearedRestorationSiteIds.Clear();

        foreach (string siteId in clearedRestorationSiteIds ?? [])
        {
            if (!string.IsNullOrWhiteSpace(siteId))
                _clearedRestorationSiteIds.Add(siteId);
        }

        if (cleanupSite?.IsCleared == true && !string.IsNullOrWhiteSpace(cleanupSite.Id))
            _clearedRestorationSiteIds.Add(cleanupSite.Id);
    }

    private CleanupSiteState GetCleanupSiteById(string siteId)
    {
        if (string.IsNullOrWhiteSpace(siteId) || !_contentRegistry.RestorationSitesById.TryGetValue(siteId, out RestorationSiteDefinition restorationSite))
            return null;

        return CreateCleanupSiteState(restorationSite);
    }

    private CleanupSiteState GetBlockingCleanupSiteForNextExpansion()
    {
        if (FarmExpansionLevel != MaxFarmExpansionLevel - 1)
            return null;

        RestorationSiteDefinition requiredSite = GetRestorationSiteUnlockingExpansionLevel(FarmExpansionLevel + 1);
        return requiredSite is null ? null : CreateCleanupSiteState(requiredSite);
    }

    private RestorationSiteDefinition GetRestorationSiteUnlockingExpansionLevel(int expansionLevel)
    {
        return _contentRegistry.RestorationSites
            .FirstOrDefault(site =>
                string.Equals(site.RewardType, "unlock_expansion_level", StringComparison.OrdinalIgnoreCase)
                && site.RewardValue == expansionLevel);
    }

    private string NormalizeSeedItemId(string seedItemId, string fallbackSeedItemId = null)
    {
        string normalizedFallback = string.IsNullOrWhiteSpace(fallbackSeedItemId)
            ? GetDefaultSeedItemId(_seedItemIds)
            : fallbackSeedItemId;

        if (string.IsNullOrWhiteSpace(seedItemId))
            return normalizedFallback;

        string matchingSeed = _seedItemIds.FirstOrDefault(candidate =>
            string.Equals(candidate, seedItemId, StringComparison.OrdinalIgnoreCase));

        return matchingSeed ?? normalizedFallback;
    }

    private static FarmRegion ToRegion(FarmRegionSaveState regionState, FarmRegion fallbackRegion)
    {
        if (regionState is null || regionState.Width <= 0 || regionState.Height <= 0)
            return fallbackRegion;

        return new FarmRegion(regionState.X, regionState.Y, regionState.Width, regionState.Height);
    }

    private void ValidateSavedItems(List<string> errors, IEnumerable<ItemStackSaveState> items, string location)
    {
        foreach (ItemStackSaveState item in items ?? [])
        {
            if (string.IsNullOrWhiteSpace(item?.ItemId) || item.Quantity <= 0)
                continue;

            if (!_contentRegistry.ItemsById.ContainsKey(item.ItemId))
                errors.Add($"Missing item id in {location}: '{item.ItemId}'.");
        }
    }

    private void ValidateSavedPlots(List<string> errors, IEnumerable<FarmPlotSaveState> plots, int farmWidth, int farmHeight)
    {
        foreach (FarmPlotSaveState plot in plots ?? [])
        {
            if (plot is null)
                continue;

            if (plot.X < 0 || plot.X >= farmWidth || plot.Y < 0 || plot.Y >= farmHeight)
            {
                errors.Add($"Saved plot ({plot.X},{plot.Y}) is outside the farm bounds.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(plot.CropDefinitionId) && !_contentRegistry.TileProducersById.ContainsKey(plot.CropDefinitionId))
                errors.Add($"Missing crop id in farm plot ({plot.X},{plot.Y}): '{plot.CropDefinitionId}'.");
        }
    }

    private void ValidateSavedSeed(List<string> errors, string itemId, string location)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        if (!_contentRegistry.ItemsById.TryGetValue(itemId, out ItemDefinition itemDefinition))
        {
            errors.Add($"Missing item id in {location}: '{itemId}'.");
            return;
        }

        if (!_contentRegistry.IsStarterItem(itemId))
            errors.Add($"Invalid seed id in {location}: '{itemId}' is not a starter item.");
    }

    private void ValidateSavedMachines(List<string> errors, IEnumerable<ProcessorMachineSaveState> machines)
    {
        foreach (ProcessorMachineSaveState machine in machines ?? [])
        {
            if (machine is null || string.IsNullOrWhiteSpace(machine.MachineId))
                continue;

            if (!_contentRegistry.MachinesById.ContainsKey(machine.MachineId))
            {
                errors.Add($"Missing machine id: '{machine.MachineId}'.");
                continue;
            }

            ProcessingRecipeDefinition recipe = null;
            if (!string.IsNullOrWhiteSpace(machine.CurrentRecipeId))
            {
                if (!_contentRegistry.ProcessingRecipesById.TryGetValue(machine.CurrentRecipeId, out recipe))
                {
                    errors.Add($"Missing processing recipe id: '{machine.CurrentRecipeId}'.");
                }
                else if (!string.Equals(recipe.MachineId, machine.MachineId, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Machine '{machine.MachineId}' references recipe '{machine.CurrentRecipeId}' for machine '{recipe.MachineId}'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(machine.InputItemId) && !_contentRegistry.ItemsById.ContainsKey(machine.InputItemId))
                errors.Add($"Missing machine input item id for '{machine.MachineId}': '{machine.InputItemId}'.");

            if (!string.IsNullOrWhiteSpace(machine.OutputItemId) && !_contentRegistry.ItemsById.ContainsKey(machine.OutputItemId))
                errors.Add($"Missing machine output item id for '{machine.MachineId}': '{machine.OutputItemId}'.");
        }
    }

    private void ValidateProcessedOutputs(List<string> errors, IEnumerable<ItemStackSaveState> processedOutputs)
    {
        foreach (ItemStackSaveState processedOutput in processedOutputs ?? [])
        {
            if (string.IsNullOrWhiteSpace(processedOutput?.ItemId) || processedOutput.Quantity <= 0)
                continue;

            if (!_contentRegistry.ItemsById.ContainsKey(processedOutput.ItemId))
                errors.Add($"Missing processed output item id: '{processedOutput.ItemId}'.");
        }
    }

    private void ValidateActivityLog(List<string> errors, IEnumerable<ActivityLogEntrySaveState> activityLog)
    {
        foreach (ActivityLogEntrySaveState entry in activityLog ?? [])
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.ItemId))
                continue;

            if (!_contentRegistry.ItemsById.ContainsKey(entry.ItemId))
                errors.Add($"Missing activity log item id: '{entry.ItemId}'.");
        }
    }

    private void AddItem(string itemId, int amount)
    {
        _inventory[itemId] = GetItemCount(itemId) + amount;
    }

    private void RemoveItem(string itemId, int amount)
    {
        int remaining = Math.Max(0, GetItemCount(itemId) - amount);
        if (remaining == 0)
        {
            _inventory.Remove(itemId);
            return;
        }

        _inventory[itemId] = remaining;
    }

    private void AddStorageItem(string itemId, int amount)
    {
        _sharedStorage[itemId] = GetStorageItemCount(itemId) + amount;
    }

    private void RemoveStorageItem(string itemId, int amount)
    {
        int remaining = Math.Max(0, GetStorageItemCount(itemId) - amount);
        if (remaining == 0)
        {
            _sharedStorage.Remove(itemId);
            return;
        }

        _sharedStorage[itemId] = remaining;
    }
}
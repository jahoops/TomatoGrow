using System;
using System.Collections.Generic;
using System.Linq;
using TomatoGrow;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow.GameContent;

public sealed class ContentRegistry
{
    public ContentRegistry(IEnumerable<ItemDefinition> items, IEnumerable<CropDefinition> crops)
        : this(items, crops, [], [], [], [], null)
    {
    }

    public ContentRegistry(IEnumerable<ItemDefinition> items, IEnumerable<CropDefinition> crops, IEnumerable<TileProducerDefinition> tileProducers)
        : this(items, crops, tileProducers, [], [], [], null)
    {
    }

    public ContentRegistry(
        IEnumerable<ItemDefinition> items,
        IEnumerable<CropDefinition> crops,
        IEnumerable<MachineDefinition> machines,
        IEnumerable<ProcessingRecipeDefinition> processingRecipes,
        IEnumerable<MarketOfferDefinition> marketOffers,
        IEnumerable<RestorationSiteDefinition> restorationSites = null)
        : this(items, crops, [], machines, processingRecipes, marketOffers, restorationSites)
    {
    }

    public ContentRegistry(
        IEnumerable<ItemDefinition> items,
        IEnumerable<CropDefinition> crops,
        IEnumerable<TileProducerDefinition> tileProducers,
        IEnumerable<MachineDefinition> machines,
        IEnumerable<ProcessingRecipeDefinition> processingRecipes,
        IEnumerable<MarketOfferDefinition> marketOffers,
        IEnumerable<RestorationSiteDefinition> restorationSites = null)
        : this(
            items.Select(item => new LoadedDefinition<ItemDefinition>(
                item,
                new DefinitionSource("in-memory", "<in-memory>", "item", item.Id ?? string.Empty))),
            crops.Select(crop => new LoadedDefinition<CropDefinition>(
                crop,
                new DefinitionSource("in-memory", "<in-memory>", "crop", crop.Id ?? string.Empty))),
            tileProducers.Select(tileProducer => new LoadedDefinition<TileProducerDefinition>(
                tileProducer,
                new DefinitionSource("in-memory", "<in-memory>", "tile_producer", tileProducer.Id ?? string.Empty))),
            machines.Select(machine => new LoadedDefinition<MachineDefinition>(
                machine,
                new DefinitionSource("in-memory", "<in-memory>", "machine", machine.Id ?? string.Empty))),
            processingRecipes.Select(processingRecipe => new LoadedDefinition<ProcessingRecipeDefinition>(
                processingRecipe,
                new DefinitionSource("in-memory", "<in-memory>", "processing_recipe", processingRecipe.Id ?? string.Empty))),
            marketOffers.Select(marketOffer => new LoadedDefinition<MarketOfferDefinition>(
                marketOffer,
                new DefinitionSource("in-memory", "<in-memory>", "market_offer", marketOffer.Id ?? string.Empty))),
            (restorationSites ?? []).Select(restorationSite => new LoadedDefinition<RestorationSiteDefinition>(
                restorationSite,
                new DefinitionSource("in-memory", "<in-memory>", "restoration_site", restorationSite.Id ?? string.Empty)))
        )
    {
    }

    public ContentRegistry(
        IEnumerable<ItemDefinition> items,
        IEnumerable<CropDefinition> crops,
        IEnumerable<MachineDefinition> machines,
        IEnumerable<ProcessingRecipeDefinition> processingRecipes)
        : this(items, crops, [], machines, processingRecipes, [], null)
    {
    }

    public ContentRegistry(
        IEnumerable<LoadedDefinition<ItemDefinition>> loadedItems,
        IEnumerable<LoadedDefinition<CropDefinition>> loadedCrops,
        IEnumerable<LoadedDefinition<MachineDefinition>> loadedMachines,
        IEnumerable<LoadedDefinition<ProcessingRecipeDefinition>> loadedProcessingRecipes,
        IEnumerable<LoadedDefinition<MarketOfferDefinition>> loadedMarketOffers,
        IEnumerable<LoadedDefinition<RestorationSiteDefinition>> loadedRestorationSites = null)
        : this(loadedItems, loadedCrops, [], loadedMachines, loadedProcessingRecipes, loadedMarketOffers, loadedRestorationSites)
    {
    }

    public ContentRegistry(
        IEnumerable<LoadedDefinition<ItemDefinition>> loadedItems,
        IEnumerable<LoadedDefinition<CropDefinition>> loadedCrops,
        IEnumerable<LoadedDefinition<TileProducerDefinition>> loadedTileProducerDefinitions,
        IEnumerable<LoadedDefinition<MachineDefinition>> loadedMachines,
        IEnumerable<LoadedDefinition<ProcessingRecipeDefinition>> loadedProcessingRecipes,
        IEnumerable<LoadedDefinition<MarketOfferDefinition>> loadedMarketOffers,
        IEnumerable<LoadedDefinition<RestorationSiteDefinition>> loadedRestorationSites = null)
    {
        LoadedItems = loadedItems.ToList();
        LoadedCrops = loadedCrops.ToList();
        LoadedTileProducerDefinitions = (loadedTileProducerDefinitions ?? []).ToList();
        LoadedMachines = loadedMachines.ToList();
        LoadedProcessingRecipes = loadedProcessingRecipes.ToList();
        LoadedMarketOffers = loadedMarketOffers.ToList();
        LoadedRestorationSites = (loadedRestorationSites ?? []).ToList();
        LoadedTileProducers = BuildLoadedTileProducers(LoadedCrops, LoadedTileProducerDefinitions);
        Items = LoadedItems.Select(loaded => loaded.Definition).ToList();
        Crops = LoadedCrops.Select(loaded => loaded.Definition).ToList();
        TileProducerDefinitions = LoadedTileProducerDefinitions.Select(loaded => loaded.Definition).ToList();
        TileProducers = LoadedTileProducers.Select(loaded => loaded.Definition).ToList();
        Machines = LoadedMachines.Select(loaded => loaded.Definition).ToList();
        ProcessingRecipes = LoadedProcessingRecipes.Select(loaded => loaded.Definition).ToList();
        MarketOffers = LoadedMarketOffers.Select(loaded => loaded.Definition).ToList();
        RestorationSites = LoadedRestorationSites.Select(loaded => loaded.Definition).ToList();
        ItemsById = BuildLookup(Items, item => item.Id);
        CropsById = BuildLookup(Crops, crop => crop.Id);
        TileProducerDefinitionsById = BuildLookup(TileProducerDefinitions, tileProducer => tileProducer.Id);
        TileProducersById = BuildLookup(TileProducers, tileProducer => tileProducer.Id);
        MachinesById = BuildLookup(Machines, machine => machine.Id);
        ProcessingRecipesById = BuildLookup(ProcessingRecipes, processingRecipe => processingRecipe.Id);
        MarketOffersById = BuildLookup(MarketOffers, marketOffer => marketOffer.Id);
        RestorationSitesById = BuildLookup(RestorationSites, restorationSite => restorationSite.Id);
    }

    public IReadOnlyList<LoadedDefinition<ItemDefinition>> LoadedItems { get; }
    public IReadOnlyList<LoadedDefinition<CropDefinition>> LoadedCrops { get; }
    public IReadOnlyList<LoadedDefinition<TileProducerDefinition>> LoadedTileProducerDefinitions { get; }
    public IReadOnlyList<LoadedDefinition<TileProducerDefinition>> LoadedTileProducers { get; }
    public IReadOnlyList<LoadedDefinition<MachineDefinition>> LoadedMachines { get; }
    public IReadOnlyList<LoadedDefinition<ProcessingRecipeDefinition>> LoadedProcessingRecipes { get; }
    public IReadOnlyList<LoadedDefinition<MarketOfferDefinition>> LoadedMarketOffers { get; }
    public IReadOnlyList<LoadedDefinition<RestorationSiteDefinition>> LoadedRestorationSites { get; }
    public IReadOnlyList<ItemDefinition> Items { get; }
    public IReadOnlyList<CropDefinition> Crops { get; }
    public IReadOnlyList<TileProducerDefinition> TileProducerDefinitions { get; }
    public IReadOnlyList<TileProducerDefinition> TileProducers { get; }
    public IReadOnlyList<MachineDefinition> Machines { get; }
    public IReadOnlyList<ProcessingRecipeDefinition> ProcessingRecipes { get; }
    public IReadOnlyList<MarketOfferDefinition> MarketOffers { get; }
    public IReadOnlyList<RestorationSiteDefinition> RestorationSites { get; }
    public IReadOnlyDictionary<string, ItemDefinition> ItemsById { get; }
    public IReadOnlyDictionary<string, CropDefinition> CropsById { get; }
    public IReadOnlyDictionary<string, TileProducerDefinition> TileProducerDefinitionsById { get; }
    public IReadOnlyDictionary<string, TileProducerDefinition> TileProducersById { get; }
    public IReadOnlyDictionary<string, MachineDefinition> MachinesById { get; }
    public IReadOnlyDictionary<string, ProcessingRecipeDefinition> ProcessingRecipesById { get; }
    public IReadOnlyDictionary<string, MarketOfferDefinition> MarketOffersById { get; }
    public IReadOnlyDictionary<string, RestorationSiteDefinition> RestorationSitesById { get; }

    public ItemDefinition GetItem(string id)
    {
        if (!ItemsById.TryGetValue(id, out ItemDefinition item))
            throw new KeyNotFoundException($"Missing item definition '{id}'.");

        return item;
    }

    public CropDefinition GetCrop(string id)
    {
        if (!CropsById.TryGetValue(id, out CropDefinition crop))
            throw new KeyNotFoundException($"Missing crop definition '{id}'.");

        return crop;
    }

    public TileProducerDefinition GetTileProducer(string id)
    {
        if (!TileProducersById.TryGetValue(id, out TileProducerDefinition tileProducer))
            throw new KeyNotFoundException($"Missing tile producer definition '{id}'.");

        return tileProducer;
    }

    public bool TryGetTileProducer(string id, out TileProducerDefinition tileProducer)
    {
        return TileProducersById.TryGetValue(id, out tileProducer);
    }

    public MachineDefinition GetMachine(string id)
    {
        if (!MachinesById.TryGetValue(id, out MachineDefinition machine))
            throw new KeyNotFoundException($"Missing machine definition '{id}'.");

        return machine;
    }

    public ProcessingRecipeDefinition GetProcessingRecipe(string id)
    {
        if (!ProcessingRecipesById.TryGetValue(id, out ProcessingRecipeDefinition processingRecipe))
            throw new KeyNotFoundException($"Missing processing recipe definition '{id}'.");

        return processingRecipe;
    }

    public MarketOfferDefinition GetMarketOffer(string id)
    {
        if (!MarketOffersById.TryGetValue(id, out MarketOfferDefinition marketOffer))
            throw new KeyNotFoundException($"Missing market offer definition '{id}'.");

        return marketOffer;
    }

    public RestorationSiteDefinition GetRestorationSite(string id)
    {
        if (!RestorationSitesById.TryGetValue(id, out RestorationSiteDefinition restorationSite))
            throw new KeyNotFoundException($"Missing restoration site definition '{id}'.");

        return restorationSite;
    }

    public IReadOnlyList<ProcessingRecipeDefinition> GetProcessingRecipesForMachine(string machineId)
    {
        return ProcessingRecipes
            .Where(processingRecipe => string.Equals(processingRecipe.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public bool TryGetCropBySeedItemId(string seedItemId, out CropDefinition crop)
    {
        crop = Crops.FirstOrDefault(candidate =>
            string.Equals(candidate.SeedItemId, seedItemId, StringComparison.OrdinalIgnoreCase));

        return crop is not null;
    }

    public bool TryGetTileProducerByStarterItemId(string starterItemId, out TileProducerDefinition tileProducer)
    {
        tileProducer = TileProducers.FirstOrDefault(candidate =>
            string.Equals(candidate.StarterItemId, starterItemId, StringComparison.OrdinalIgnoreCase));

        return tileProducer is not null;
    }

    public bool TryGetTileProducerByOutputItemId(string outputItemId, out TileProducerDefinition tileProducer)
    {
        tileProducer = TileProducers.FirstOrDefault(candidate =>
            string.Equals(candidate.OutputItemId, outputItemId, StringComparison.OrdinalIgnoreCase));

        return tileProducer is not null;
    }

    public IReadOnlyList<string> GetStarterItemIds()
    {
        return LoadedTileProducers
            .Select(loaded => loaded.Definition.StarterItemId)
            .Where(starterItemId => !string.IsNullOrWhiteSpace(starterItemId) && ItemsById.ContainsKey(starterItemId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(starterItemId => starterItemId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<ItemDefinition> GetStarterItems()
    {
        return GetStarterItemIds()
            .Select(GetItem)
            .ToList();
    }

    public bool IsStarterItem(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId)
            && TileProducers.Any(tileProducer => string.Equals(tileProducer.StarterItemId, itemId, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryGetProducerByStarterItemId(string starterItemId, out TileProducerDefinitionView producer)
    {
        if (TryGetTileProducerByStarterItemId(starterItemId, out TileProducerDefinition tileProducer))
        {
            producer = new TileProducerDefinitionView(tileProducer, TryGetCropCompatibility(tileProducer.Id));
            return true;
        }

        producer = default;
        return false;
    }

    public bool TryGetProducerByOutputItemId(string outputItemId, out TileProducerDefinitionView producer)
    {
        if (TryGetTileProducerByOutputItemId(outputItemId, out TileProducerDefinition tileProducer))
        {
            producer = new TileProducerDefinitionView(tileProducer, TryGetCropCompatibility(tileProducer.Id));
            return true;
        }

        producer = default;
        return false;
    }

    public IReadOnlyList<TileProducerDefinitionView> GetAllTileProducers()
    {
        return TileProducers
            .Select(tileProducer => new TileProducerDefinitionView(tileProducer, TryGetCropCompatibility(tileProducer.Id)))
            .ToList();
    }

    private CropDefinition TryGetCropCompatibility(string id)
    {
        CropsById.TryGetValue(id, out CropDefinition crop);
        return crop;
    }

    private static IReadOnlyList<LoadedDefinition<TileProducerDefinition>> BuildLoadedTileProducers(
        IEnumerable<LoadedDefinition<CropDefinition>> loadedCrops,
        IEnumerable<LoadedDefinition<TileProducerDefinition>> loadedTileProducerDefinitions)
    {
        List<LoadedDefinition<TileProducerDefinition>> loadedTileProducers = loadedCrops
            .Select(loadedCrop => new LoadedDefinition<TileProducerDefinition>(
                new TileProducerDefinition
                {
                    Id = loadedCrop.Definition.Id,
                    StarterItemId = loadedCrop.Definition.SeedItemId,
                    OutputItemId = loadedCrop.Definition.HarvestItemId,
                    ProductionDays = loadedCrop.Definition.GrowthDays
                },
                new DefinitionSource(
                    loadedCrop.Source.ModuleName,
                    loadedCrop.Source.FilePath,
                    "tile_producer",
                    loadedCrop.Definition.Id ?? string.Empty)))
            .ToList();

        loadedTileProducers.AddRange(loadedTileProducerDefinitions);
        return loadedTileProducers;
    }

    private static IReadOnlyDictionary<string, TDefinition> BuildLookup<TDefinition>(
        IEnumerable<TDefinition> definitions,
        Func<TDefinition, string> getId)
    {
        Dictionary<string, TDefinition> lookup = new(StringComparer.OrdinalIgnoreCase);

        foreach (TDefinition definition in definitions)
        {
            string id = getId(definition) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id) || lookup.ContainsKey(id))
                continue;

            lookup[id] = definition;
        }

        return lookup;
    }
}
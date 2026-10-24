using System;
using System.Linq;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow.GameContent;

public sealed class ContentValidator
{
    public ContentValidationResult Validate(ContentRegistry registry)
    {
        ContentValidationResult result = new();

        ValidateDuplicateItems(registry, result);
        ValidateDuplicateCrops(registry, result);
        ValidateDuplicateTileProducerDefinitions(registry, result);
        ValidateCropAndTileProducerIdConflicts(registry, result);
        ValidateDuplicateMachines(registry, result);
        ValidateDuplicateProcessingRecipes(registry, result);
        ValidateDuplicateMarketOffers(registry, result);
        ValidateDuplicateRestorationSites(registry, result);
        ValidateRequiredRestorationSites(registry, result);

        foreach (LoadedDefinition<ItemDefinition> loadedItem in registry.LoadedItems)
            ValidateItem(loadedItem, result);

        foreach (LoadedDefinition<CropDefinition> loadedCrop in registry.LoadedCrops)
            ValidateCrop(loadedCrop, registry, result);

        foreach (LoadedDefinition<TileProducerDefinition> loadedTileProducer in registry.LoadedTileProducerDefinitions)
            ValidateTileProducerDefinition(loadedTileProducer, registry, result);

        ValidateDuplicateStarterItemMappings(registry, result);
        ValidateDuplicateOutputItemMappings(registry, result);

        foreach (LoadedDefinition<MachineDefinition> loadedMachine in registry.LoadedMachines)
            ValidateMachine(loadedMachine, result);

        foreach (LoadedDefinition<ProcessingRecipeDefinition> loadedProcessingRecipe in registry.LoadedProcessingRecipes)
            ValidateProcessingRecipe(loadedProcessingRecipe, registry, result);

        foreach (LoadedDefinition<MarketOfferDefinition> loadedMarketOffer in registry.LoadedMarketOffers)
            ValidateMarketOffer(loadedMarketOffer, registry, result);

        foreach (LoadedDefinition<RestorationSiteDefinition> loadedRestorationSite in registry.LoadedRestorationSites)
            ValidateRestorationSite(loadedRestorationSite, result);

        if (registry.Items.Count == 0)
            result.Warnings.Add("No item definitions were loaded.");

        if (registry.Crops.Count == 0)
            result.Warnings.Add("No crop definitions were loaded.");

        if (registry.TileProducers.Count == 0)
            result.Warnings.Add("No tile producer definitions were loaded.");

        return result;
    }

    private static void ValidateDuplicateTileProducerDefinitions(ContentRegistry registry, ContentValidationResult result)
    {
        var duplicateGroups = registry.LoadedTileProducerDefinitions
            .Where(loaded => !string.IsNullOrWhiteSpace(loaded.Definition.Id))
            .GroupBy(loaded => loaded.Definition.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateGroup in duplicateGroups)
            AddDuplicateError("tile producer", duplicateGroup.Key, duplicateGroup.ToArray(), result);
    }

    private static void ValidateCropAndTileProducerIdConflicts(ContentRegistry registry, ContentValidationResult result)
    {
        foreach (LoadedDefinition<TileProducerDefinition> loadedTileProducer in registry.LoadedTileProducerDefinitions)
        {
            LoadedDefinition<CropDefinition> loadedCrop = registry.LoadedCrops.FirstOrDefault(candidate =>
                string.Equals(candidate.Definition.Id, loadedTileProducer.Definition.Id, StringComparison.OrdinalIgnoreCase));

            if (loadedCrop is null)
                continue;

            result.Errors.Add(
                $"Tile producer id '{loadedTileProducer.Definition.Id}' conflicts with crop id '{loadedCrop.Definition.Id}'. Crop source: {DescribeSource(loadedCrop.Source)}. Tile producer source: {DescribeSource(loadedTileProducer.Source)}.");
        }
    }

    private static void ValidateDuplicateMachines(ContentRegistry registry, ContentValidationResult result)
    {
        var duplicateGroups = registry.LoadedMachines
            .Where(loaded => !string.IsNullOrWhiteSpace(loaded.Definition.Id))
            .GroupBy(loaded => loaded.Definition.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateGroup in duplicateGroups)
            AddDuplicateError("machine", duplicateGroup.Key, duplicateGroup.ToArray(), result);
    }

    private static void ValidateDuplicateProcessingRecipes(ContentRegistry registry, ContentValidationResult result)
    {
        var duplicateGroups = registry.LoadedProcessingRecipes
            .Where(loaded => !string.IsNullOrWhiteSpace(loaded.Definition.Id))
            .GroupBy(loaded => loaded.Definition.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateGroup in duplicateGroups)
            AddDuplicateError("processing recipe", duplicateGroup.Key, duplicateGroup.ToArray(), result);
    }

    private static void ValidateDuplicateMarketOffers(ContentRegistry registry, ContentValidationResult result)
    {
        var duplicateGroups = registry.LoadedMarketOffers
            .Where(loaded => !string.IsNullOrWhiteSpace(loaded.Definition.Id))
            .GroupBy(loaded => loaded.Definition.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateGroup in duplicateGroups)
            AddDuplicateError("market offer", duplicateGroup.Key, duplicateGroup.ToArray(), result);
    }

    private static void ValidateDuplicateRestorationSites(ContentRegistry registry, ContentValidationResult result)
    {
        var duplicateGroups = registry.LoadedRestorationSites
            .Where(loaded => !string.IsNullOrWhiteSpace(loaded.Definition.Id))
            .GroupBy(loaded => loaded.Definition.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateGroup in duplicateGroups)
            AddDuplicateError("restoration site", duplicateGroup.Key, duplicateGroup.ToArray(), result);
    }

    private static void ValidateRequiredRestorationSites(ContentRegistry registry, ContentValidationResult result)
    {
        if (!registry.RestorationSitesById.ContainsKey(TurnipTestState.OldJunkPileId))
            result.Errors.Add($"Missing required restoration site '{TurnipTestState.OldJunkPileId}' for expansion gate.");
    }

    private static void ValidateDuplicateItems(ContentRegistry registry, ContentValidationResult result)
    {
        var duplicateGroups = registry.LoadedItems
            .Where(loaded => !string.IsNullOrWhiteSpace(loaded.Definition.Id))
            .GroupBy(loaded => loaded.Definition.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateGroup in duplicateGroups)
            AddDuplicateError("item", duplicateGroup.Key, duplicateGroup.ToArray(), result);
    }

    private static void ValidateDuplicateCrops(ContentRegistry registry, ContentValidationResult result)
    {
        var duplicateGroups = registry.LoadedCrops
            .Where(loaded => !string.IsNullOrWhiteSpace(loaded.Definition.Id))
            .GroupBy(loaded => loaded.Definition.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateGroup in duplicateGroups)
            AddDuplicateError("crop", duplicateGroup.Key, duplicateGroup.ToArray(), result);
    }

    private static void AddDuplicateError<TDefinition>(
        string definitionType,
        string duplicateId,
        LoadedDefinition<TDefinition>[] duplicates,
        ContentValidationResult result)
    {
        LoadedDefinition<TDefinition> first = duplicates[0];
        LoadedDefinition<TDefinition> duplicate = duplicates[1];

        result.Errors.Add(
            $"Duplicate {definitionType} id '{duplicateId}'. First seen in {DescribeSource(first.Source)}. Duplicate found in {DescribeSource(duplicate.Source)}.");
    }

    private static void ValidateItem(LoadedDefinition<ItemDefinition> loadedItem, ContentValidationResult result)
    {
        ItemDefinition item = loadedItem.Definition;
        string sourceContext = DescribeSource(loadedItem.Source);

        if (string.IsNullOrWhiteSpace(item.Id))
            result.Errors.Add($"Item definition in {sourceContext} has a blank Id.");

        if (string.IsNullOrWhiteSpace(item.Name))
            result.Errors.Add($"Item '{item.Id}' in {sourceContext} has a blank Name.");

        if (string.IsNullOrWhiteSpace(item.Type))
            result.Errors.Add($"Item '{item.Id}' in {sourceContext} has a blank Type.");
    }

    private static void ValidateCrop(
        LoadedDefinition<CropDefinition> loadedCrop,
        ContentRegistry registry,
        ContentValidationResult result)
    {
        CropDefinition crop = loadedCrop.Definition;
        string sourceContext = DescribeSource(loadedCrop.Source);

        if (string.IsNullOrWhiteSpace(crop.Id))
            result.Errors.Add($"Crop definition in {sourceContext} has a blank Id.");

        if (string.IsNullOrWhiteSpace(crop.SeedItemId))
            result.Errors.Add($"Crop '{crop.Id}' in {sourceContext} has a blank SeedItemId.");

        if (string.IsNullOrWhiteSpace(crop.HarvestItemId))
            result.Errors.Add($"Crop '{crop.Id}' in {sourceContext} has a blank HarvestItemId.");

        if (crop.GrowthDays <= 0)
            result.Errors.Add($"Crop '{crop.Id}' in {sourceContext} has invalid GrowthDays {crop.GrowthDays}.");

        if (!string.IsNullOrWhiteSpace(crop.SeedItemId) && !registry.ItemsById.ContainsKey(crop.SeedItemId))
            result.Errors.Add($"Crop '{crop.Id}' in {sourceContext} references missing SeedItemId '{crop.SeedItemId}'.");

        if (!string.IsNullOrWhiteSpace(crop.HarvestItemId) && !registry.ItemsById.ContainsKey(crop.HarvestItemId))
            result.Errors.Add($"Crop '{crop.Id}' in {sourceContext} references missing HarvestItemId '{crop.HarvestItemId}'.");
    }

    private static void ValidateTileProducerDefinition(
        LoadedDefinition<TileProducerDefinition> loadedTileProducer,
        ContentRegistry registry,
        ContentValidationResult result)
    {
        TileProducerDefinition tileProducer = loadedTileProducer.Definition;
        string sourceContext = DescribeSource(loadedTileProducer.Source);

        if (string.IsNullOrWhiteSpace(tileProducer.Id))
            result.Errors.Add($"Tile producer definition in {sourceContext} has a blank Id.");

        if (string.IsNullOrWhiteSpace(tileProducer.StarterItemId))
            result.Errors.Add($"Tile producer '{tileProducer.Id}' in {sourceContext} has a blank StarterItemId.");

        if (string.IsNullOrWhiteSpace(tileProducer.OutputItemId))
            result.Errors.Add($"Tile producer '{tileProducer.Id}' in {sourceContext} has a blank OutputItemId.");

        if (tileProducer.ProductionDays <= 0)
            result.Errors.Add($"Tile producer '{tileProducer.Id}' in {sourceContext} has invalid ProductionDays {tileProducer.ProductionDays}.");

        if (!string.IsNullOrWhiteSpace(tileProducer.StarterItemId) && !registry.ItemsById.ContainsKey(tileProducer.StarterItemId))
            result.Errors.Add($"Tile producer '{tileProducer.Id}' in {sourceContext} references missing StarterItemId '{tileProducer.StarterItemId}'.");

        if (!string.IsNullOrWhiteSpace(tileProducer.OutputItemId) && !registry.ItemsById.ContainsKey(tileProducer.OutputItemId))
            result.Errors.Add($"Tile producer '{tileProducer.Id}' in {sourceContext} references missing OutputItemId '{tileProducer.OutputItemId}'.");
    }

    private static void ValidateDuplicateStarterItemMappings(ContentRegistry registry, ContentValidationResult result)
    {
        var duplicateGroups = registry.LoadedTileProducers
            .Where(loaded => !string.IsNullOrWhiteSpace(loaded.Definition.StarterItemId))
            .GroupBy(loaded => loaded.Definition.StarterItemId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateGroup in duplicateGroups)
        {
            LoadedDefinition<TileProducerDefinition> first = duplicateGroup.First();
            LoadedDefinition<TileProducerDefinition> duplicate = duplicateGroup.Skip(1).First();
            result.Errors.Add(
                $"Duplicate tile producer starter item '{duplicateGroup.Key}'. First seen in {DescribeSource(first.Source)}. Duplicate found in {DescribeSource(duplicate.Source)}.");
        }
    }

    private static void ValidateDuplicateOutputItemMappings(ContentRegistry registry, ContentValidationResult result)
    {
        var duplicateGroups = registry.LoadedTileProducers
            .Where(loaded => !string.IsNullOrWhiteSpace(loaded.Definition.OutputItemId))
            .GroupBy(loaded => loaded.Definition.OutputItemId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var duplicateGroup in duplicateGroups)
        {
            LoadedDefinition<TileProducerDefinition> first = duplicateGroup.First();
            LoadedDefinition<TileProducerDefinition> duplicate = duplicateGroup.Skip(1).First();
            result.Errors.Add(
                $"Duplicate tile producer output item '{duplicateGroup.Key}'. First seen in {DescribeSource(first.Source)}. Duplicate found in {DescribeSource(duplicate.Source)}.");
        }
    }

    private static void ValidateMachine(LoadedDefinition<MachineDefinition> loadedMachine, ContentValidationResult result)
    {
        MachineDefinition machine = loadedMachine.Definition;
        string sourceContext = DescribeSource(loadedMachine.Source);

        if (string.IsNullOrWhiteSpace(machine.Id))
            result.Errors.Add($"Machine definition in {sourceContext} has a blank Id.");

        if (string.IsNullOrWhiteSpace(machine.Name))
            result.Errors.Add($"Machine '{machine.Id}' in {sourceContext} has a blank Name.");

        if (string.IsNullOrWhiteSpace(machine.Type))
            result.Errors.Add($"Machine '{machine.Id}' in {sourceContext} has a blank Type.");
        else if (!string.Equals(machine.Type, "processor_machine", StringComparison.OrdinalIgnoreCase))
            result.Errors.Add($"Machine '{machine.Id}' in {sourceContext} has invalid Type '{machine.Type}'.");
    }

    private static void ValidateProcessingRecipe(
        LoadedDefinition<ProcessingRecipeDefinition> loadedProcessingRecipe,
        ContentRegistry registry,
        ContentValidationResult result)
    {
        ProcessingRecipeDefinition processingRecipe = loadedProcessingRecipe.Definition;
        string sourceContext = DescribeSource(loadedProcessingRecipe.Source);

        if (string.IsNullOrWhiteSpace(processingRecipe.Id))
            result.Errors.Add($"Processing recipe definition in {sourceContext} has a blank Id.");

        if (string.IsNullOrWhiteSpace(processingRecipe.MachineId))
            result.Errors.Add($"Processing recipe '{processingRecipe.Id}' in {sourceContext} has a blank MachineId.");
        else if (!registry.MachinesById.ContainsKey(processingRecipe.MachineId))
            result.Errors.Add($"Processing recipe '{processingRecipe.Id}' in {sourceContext} references missing MachineId '{processingRecipe.MachineId}'.");

        if (string.IsNullOrWhiteSpace(processingRecipe.InputItemId))
            result.Errors.Add($"Processing recipe '{processingRecipe.Id}' in {sourceContext} has a blank InputItemId.");
        else if (!registry.ItemsById.ContainsKey(processingRecipe.InputItemId))
            result.Errors.Add($"Processing recipe '{processingRecipe.Id}' in {sourceContext} references missing InputItemId '{processingRecipe.InputItemId}'.");

        if (string.IsNullOrWhiteSpace(processingRecipe.OutputItemId))
            result.Errors.Add($"Processing recipe '{processingRecipe.Id}' in {sourceContext} has a blank OutputItemId.");
        else if (!registry.ItemsById.ContainsKey(processingRecipe.OutputItemId))
            result.Errors.Add($"Processing recipe '{processingRecipe.Id}' in {sourceContext} references missing OutputItemId '{processingRecipe.OutputItemId}'.");

        if (processingRecipe.InputQuantity <= 0)
            result.Errors.Add($"Processing recipe '{processingRecipe.Id}' in {sourceContext} has invalid InputQuantity {processingRecipe.InputQuantity}.");

        if (processingRecipe.OutputQuantity <= 0)
            result.Errors.Add($"Processing recipe '{processingRecipe.Id}' in {sourceContext} has invalid OutputQuantity {processingRecipe.OutputQuantity}.");

        if (processingRecipe.ProcessingDays <= 0)
            result.Errors.Add($"Processing recipe '{processingRecipe.Id}' in {sourceContext} has invalid ProcessingDays {processingRecipe.ProcessingDays}.");
    }

    private static void ValidateMarketOffer(
        LoadedDefinition<MarketOfferDefinition> loadedMarketOffer,
        ContentRegistry registry,
        ContentValidationResult result)
    {
        MarketOfferDefinition marketOffer = loadedMarketOffer.Definition;
        string sourceContext = DescribeSource(loadedMarketOffer.Source);

        if (string.IsNullOrWhiteSpace(marketOffer.Id))
            result.Errors.Add($"Market offer definition in {sourceContext} has a blank Id.");

        if (string.IsNullOrWhiteSpace(marketOffer.ItemId))
            result.Errors.Add($"Market offer '{marketOffer.Id}' in {sourceContext} has a blank ItemId.");
        else if (!registry.ItemsById.ContainsKey(marketOffer.ItemId))
            result.Errors.Add($"Market offer '{marketOffer.Id}' in {sourceContext} references missing ItemId '{marketOffer.ItemId}'.");

        if (marketOffer.Quantity <= 0)
            result.Errors.Add($"Market offer '{marketOffer.Id}' in {sourceContext} has invalid Quantity {marketOffer.Quantity}.");

        if (marketOffer.Price <= 0)
            result.Errors.Add($"Market offer '{marketOffer.Id}' in {sourceContext} has invalid Price {marketOffer.Price}.");

        if (string.IsNullOrWhiteSpace(marketOffer.Category))
            result.Errors.Add($"Market offer '{marketOffer.Id}' in {sourceContext} has a blank Category.");
    }

    private static void ValidateRestorationSite(
        LoadedDefinition<RestorationSiteDefinition> loadedRestorationSite,
        ContentValidationResult result)
    {
        RestorationSiteDefinition restorationSite = loadedRestorationSite.Definition;
        string sourceContext = DescribeSource(loadedRestorationSite.Source);

        if (string.IsNullOrWhiteSpace(restorationSite.Id))
            result.Errors.Add($"Restoration site definition in {sourceContext} has a blank Id.");

        if (string.IsNullOrWhiteSpace(restorationSite.Name))
            result.Errors.Add($"Restoration site '{restorationSite.Id}' in {sourceContext} has a blank Name.");

        if (string.IsNullOrWhiteSpace(restorationSite.Description))
            result.Errors.Add($"Restoration site '{restorationSite.Id}' in {sourceContext} has a blank Description.");

        if (restorationSite.Cost <= 0)
            result.Errors.Add($"Restoration site '{restorationSite.Id}' in {sourceContext} has invalid Cost {restorationSite.Cost}.");

        if (string.IsNullOrWhiteSpace(restorationSite.RewardType))
        {
            result.Errors.Add($"Restoration site '{restorationSite.Id}' in {sourceContext} has a blank RewardType.");
        }
        else if (!string.Equals(restorationSite.RewardType, "unlock_expansion_level", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add($"Restoration site '{restorationSite.Id}' in {sourceContext} has invalid RewardType '{restorationSite.RewardType}'.");
        }

        if (restorationSite.RewardValue <= 0)
            result.Errors.Add($"Restoration site '{restorationSite.Id}' in {sourceContext} has invalid RewardValue {restorationSite.RewardValue}.");

        if (string.IsNullOrWhiteSpace(restorationSite.RewardDescription))
            result.Errors.Add($"Restoration site '{restorationSite.Id}' in {sourceContext} has a blank RewardDescription.");
    }

    private static string DescribeSource(DefinitionSource source)
    {
        return $"{source.FilePath} (module '{source.ModuleName}')";
    }
}
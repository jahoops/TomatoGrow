using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow.GameContent;

public sealed class ContentModuleLoader
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ContentRegistry LoadFromModules(string contentModulesPath, bool includeTestPacks = false)
    {
        string[] moduleDirectories = GetModuleDirectories(contentModulesPath, includeTestPacks);
        return LoadFromModuleDirectories(contentModulesPath, moduleDirectories);
    }

    public ContentRegistry LoadFromModules(string contentModulesPath, IEnumerable<string> moduleIds)
    {
        if (moduleIds is null)
            throw new ArgumentNullException(nameof(moduleIds));

        if (!Directory.Exists(contentModulesPath))
        {
            Console.WriteLine($"Content modules directory not found at {contentModulesPath}");
            return new ContentRegistry(
                new List<LoadedDefinition<ItemDefinition>>(),
                new List<LoadedDefinition<CropDefinition>>(),
                new List<LoadedDefinition<TileProducerDefinition>>(),
                new List<LoadedDefinition<MachineDefinition>>(),
                new List<LoadedDefinition<ProcessingRecipeDefinition>>(),
                new List<LoadedDefinition<MarketOfferDefinition>>(),
                new List<LoadedDefinition<RestorationSiteDefinition>>());
        }

        string[] requestedModuleIds = moduleIds
            .Where(moduleId => !string.IsNullOrWhiteSpace(moduleId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string[] moduleDirectories = requestedModuleIds
            .Select(moduleId => Path.Combine(contentModulesPath, moduleId))
            .ToArray();

        return LoadFromModuleDirectories(contentModulesPath, moduleDirectories);
    }

    private ContentRegistry LoadFromModuleDirectories(string contentModulesPath, IReadOnlyList<string> moduleDirectories)
    {
        List<LoadedDefinition<ItemDefinition>> items = [];
        List<LoadedDefinition<CropDefinition>> crops = [];
        List<LoadedDefinition<TileProducerDefinition>> tileProducers = [];
        List<LoadedDefinition<MachineDefinition>> machines = [];
        List<LoadedDefinition<ProcessingRecipeDefinition>> processingRecipes = [];
        List<LoadedDefinition<MarketOfferDefinition>> marketOffers = [];
        List<LoadedDefinition<RestorationSiteDefinition>> restorationSites = [];

        foreach (string moduleDirectory in moduleDirectories)
        {
            string moduleName = Path.GetFileName(moduleDirectory);
            if (!Directory.Exists(moduleDirectory))
                continue;

            Console.WriteLine($"Loading content module '{moduleName}'...");

            items.AddRange(LoadDefinitions<ItemDefinition>(
                contentModulesPath,
                moduleName,
                Path.Combine(moduleDirectory, "items.json"),
                "item"));

            crops.AddRange(LoadDefinitions<CropDefinition>(
                contentModulesPath,
                moduleName,
                Path.Combine(moduleDirectory, "crops.json"),
                "crop"));

            tileProducers.AddRange(LoadDefinitions<TileProducerDefinition>(
                contentModulesPath,
                moduleName,
                Path.Combine(moduleDirectory, "tile_producers.json"),
                "tile_producer"));

            machines.AddRange(LoadDefinitions<MachineDefinition>(
                contentModulesPath,
                moduleName,
                Path.Combine(moduleDirectory, "machines.json"),
                "machine"));

            processingRecipes.AddRange(LoadDefinitions<ProcessingRecipeDefinition>(
                contentModulesPath,
                moduleName,
                Path.Combine(moduleDirectory, "processing_recipes.json"),
                "processing_recipe"));

            marketOffers.AddRange(LoadDefinitions<MarketOfferDefinition>(
                contentModulesPath,
                moduleName,
                Path.Combine(moduleDirectory, "market_offers.json"),
                "market_offer"));

            restorationSites.AddRange(LoadDefinitions<RestorationSiteDefinition>(
                contentModulesPath,
                moduleName,
                Path.Combine(moduleDirectory, "restoration_sites.json"),
                "restoration_site"));
        }

        return new ContentRegistry(items, crops, tileProducers, machines, processingRecipes, marketOffers, restorationSites);
    }

    private static string[] GetModuleDirectories(string contentModulesPath, bool includeTestPacks)
    {
        if (!Directory.Exists(contentModulesPath))
            return [];

        return Directory
            .GetDirectories(contentModulesPath)
            .Where(path => includeTestPacks || !IsTestPackModule(Path.GetFileName(path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsTestPackModule(string moduleName)
    {
        return !string.IsNullOrWhiteSpace(moduleName)
            && moduleName.EndsWith("TestPack", StringComparison.OrdinalIgnoreCase);
    }

    private List<LoadedDefinition<TDefinition>> LoadDefinitions<TDefinition>(
        string contentModulesPath,
        string moduleName,
        string filePath,
        string definitionType)
    {
        if (!File.Exists(filePath))
            return [];

        string json = File.ReadAllText(filePath);
        List<TDefinition> definitions = JsonSerializer.Deserialize<List<TDefinition>>(json, _jsonOptions) ?? [];

        string displayPath = BuildDisplayPath(contentModulesPath, filePath);
        List<LoadedDefinition<TDefinition>> loadedDefinitions = new(definitions.Count);

        foreach (TDefinition definition in definitions)
        {
            string definitionId = GetDefinitionId(definition);
            loadedDefinitions.Add(new LoadedDefinition<TDefinition>(
                definition,
                new DefinitionSource(moduleName, displayPath, definitionType, definitionId)));
        }

        return loadedDefinitions;
    }

    private static string BuildDisplayPath(string contentModulesPath, string filePath)
    {
        string rootName = Path.GetFileName(contentModulesPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string relativePath = Path.GetRelativePath(contentModulesPath, filePath);
        return Path.Combine(rootName, relativePath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string GetDefinitionId<TDefinition>(TDefinition definition)
    {
        if (definition is ItemDefinition item)
            return item.Id ?? string.Empty;

        if (definition is CropDefinition crop)
            return crop.Id ?? string.Empty;

        if (definition is TileProducerDefinition tileProducer)
            return tileProducer.Id ?? string.Empty;

        if (definition is MachineDefinition machine)
            return machine.Id ?? string.Empty;

        if (definition is ProcessingRecipeDefinition processingRecipe)
            return processingRecipe.Id ?? string.Empty;

        if (definition is MarketOfferDefinition marketOffer)
            return marketOffer.Id ?? string.Empty;

        if (definition is RestorationSiteDefinition restorationSite)
            return restorationSite.Id ?? string.Empty;

        return string.Empty;
    }
}
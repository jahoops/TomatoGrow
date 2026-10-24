using System;
using System.Collections.Generic;
using System.Linq;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public static class ThemePackRuntimeDiagnostics
{
    public static string[] GetThemePackSummaryLines(TurnipTestState state, ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(registry);

        string[] moduleIds = GetLoadedModuleIds(registry).ToArray();
        List<string> lines =
        [
            "THEME PACK",
            $"Theme: {state.ThemeVocabulary.ThemeName} ({state.ThemeVocabulary.ThemeId})",
            $"Interface: {state.InterfacePack.Name} ({state.InterfacePack.Id})",
            $"Modules: {moduleIds.Length} | {(moduleIds.Length == 0 ? "<unknown>" : string.Join(", ", moduleIds))}",
            $"Starters: {registry.GetStarterItemIds().Count} | Tile Producers: {registry.TileProducers.Count} | Machines: {registry.Machines.Count}",
            $"Recipes: {registry.ProcessingRecipes.Count} | Market Offers: {registry.MarketOffers.Count} | Restoration Sites: {registry.RestorationSites.Count}"
        ];

        IReadOnlyList<string> warnings = GetWarnings(registry);
        if (warnings.Count > 0)
        {
            lines.Add("Warnings:");
            lines.AddRange(warnings.Select(warning => $"- {warning}"));
        }

        return lines.ToArray();
    }

    public static string[] Format(TurnipTestState state, ContentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(registry);

        List<string> lines =
        [
            "THEME PACK RUNTIME DIAGNOSTICS",
            "PRESENTATION",
            $"Theme vocabulary: {state.ThemeVocabulary.ThemeId} ({state.ThemeVocabulary.ThemeName})",
            $"Interface pack: {state.InterfacePack.Id} ({state.InterfacePack.Name})",
            string.Empty,
            "CONTENT SUMMARY",
            $"Loaded modules: {FormatLoadedModules(registry)}",
            $"Items: {registry.Items.Count}",
            $"Starter items: {registry.GetStarterItemIds().Count}",
            $"Tile producers: {registry.TileProducers.Count}",
            $"Machines: {registry.Machines.Count}",
            $"Processing recipes: {registry.ProcessingRecipes.Count}",
            $"Market offers: {registry.MarketOffers.Count}",
            $"Restoration sites: {registry.RestorationSites.Count}",
            string.Empty,
            "STARTER ITEMS"
        ];

        lines.AddRange(FormatStarterItems(registry));
        lines.Add(string.Empty);
        lines.Add("TILE PRODUCERS");
        lines.AddRange(FormatTileProducers(registry));
        lines.Add(string.Empty);
        lines.Add("MACHINES");
        lines.AddRange(FormatMachines(registry));
        lines.Add(string.Empty);
        lines.Add("PROCESSING RECIPES");
        lines.AddRange(FormatProcessingRecipes(registry));
        lines.Add(string.Empty);
        lines.Add("RESTORATION SITES");
        lines.AddRange(FormatRestorationSites(registry));

        IReadOnlyList<string> warnings = GetWarnings(registry);
        if (warnings.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("WARNINGS");
            lines.AddRange(warnings);
        }

        return lines.ToArray();
    }

    private static string FormatLoadedModules(ContentRegistry registry)
    {
        string[] moduleIds = GetLoadedModuleIds(registry).ToArray();
        return moduleIds.Length == 0 ? "<unknown>" : string.Join(", ", moduleIds);
    }

    private static IReadOnlyList<string> FormatStarterItems(ContentRegistry registry)
    {
        IReadOnlyList<string> starterItemIds = registry.GetStarterItemIds();
        if (starterItemIds.Count == 0)
            return ["None."];

        return starterItemIds
            .Select(starterItemId => $"{starterItemId}: {registry.GetItem(starterItemId).Name}")
            .ToArray();
    }

    private static IReadOnlyList<string> FormatTileProducers(ContentRegistry registry)
    {
        if (registry.TileProducers.Count == 0)
            return ["None."];

        return registry.TileProducers
            .OrderBy(tileProducer => tileProducer.Id, StringComparer.OrdinalIgnoreCase)
            .Select(tileProducer =>
            {
                ItemDefinition starterItem = registry.GetItem(tileProducer.StarterItemId);
                ItemDefinition outputItem = registry.GetItem(tileProducer.OutputItemId);
                return $"{tileProducer.Id}: {starterItem.Name} -> {outputItem.Name}, {tileProducer.ProductionDays} days";
            })
            .ToArray();
    }

    private static IReadOnlyList<string> FormatMachines(ContentRegistry registry)
    {
        if (registry.Machines.Count == 0)
            return ["None."];

        return registry.Machines
            .OrderBy(machine => machine.Id, StringComparer.OrdinalIgnoreCase)
            .Select(machine => $"{machine.Id}: {machine.Name}")
            .ToArray();
    }

    private static IReadOnlyList<string> FormatProcessingRecipes(ContentRegistry registry)
    {
        if (registry.ProcessingRecipes.Count == 0)
            return ["None."];

        return registry.ProcessingRecipes
            .OrderBy(recipe => recipe.Id, StringComparer.OrdinalIgnoreCase)
            .Select(recipe =>
            {
                MachineDefinition machine = registry.GetMachine(recipe.MachineId);
                ItemDefinition inputItem = registry.GetItem(recipe.InputItemId);
                ItemDefinition outputItem = registry.GetItem(recipe.OutputItemId);
                return $"{recipe.Id}: {machine.Name}, {recipe.InputQuantity} {inputItem.Name} -> {recipe.OutputQuantity} {outputItem.Name}, {recipe.ProcessingDays} days";
            })
            .ToArray();
    }

    private static IReadOnlyList<string> FormatRestorationSites(ContentRegistry registry)
    {
        if (registry.RestorationSites.Count == 0)
            return ["None."];

        return registry.RestorationSites
            .OrderBy(site => site.Id, StringComparer.OrdinalIgnoreCase)
            .Select(site => $"{site.Id}: {site.Name}, ${site.Cost}, {site.RewardDescription}")
            .ToArray();
    }

    private static IReadOnlyList<string> GetWarnings(ContentRegistry registry)
    {
        List<string> warnings = [];

        if (registry.GetStarterItemIds().Count == 0)
            warnings.Add("No starter items found.");

        if (registry.TileProducers.Count == 0)
            warnings.Add("No tile producers found.");

        if (registry.Machines.Count == 0)
            warnings.Add("No machines found.");

        foreach (MachineDefinition machine in registry.Machines.OrderBy(machine => machine.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (registry.GetProcessingRecipesForMachine(machine.Id).Count == 0)
                warnings.Add($"Machine '{machine.Id}' has no recipes.");
        }

        if (warnings.Count > 0)
            warnings.Add("Content validation is assumed OK for referenced ids.");

        return warnings;
    }

    private static IEnumerable<string> GetLoadedModuleIds(ContentRegistry registry)
    {
        return registry.LoadedItems.Select(loaded => loaded.Source.ModuleName)
            .Concat(registry.LoadedCrops.Select(loaded => loaded.Source.ModuleName))
            .Concat(registry.LoadedTileProducerDefinitions.Select(loaded => loaded.Source.ModuleName))
            .Concat(registry.LoadedMachines.Select(loaded => loaded.Source.ModuleName))
            .Concat(registry.LoadedProcessingRecipes.Select(loaded => loaded.Source.ModuleName))
            .Concat(registry.LoadedMarketOffers.Select(loaded => loaded.Source.ModuleName))
            .Concat(registry.LoadedRestorationSites.Select(loaded => loaded.Source.ModuleName))
            .Where(moduleName => !string.IsNullOrWhiteSpace(moduleName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(moduleName => moduleName, StringComparer.OrdinalIgnoreCase);
    }
}
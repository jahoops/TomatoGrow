using System;
using System.Collections.Generic;
using System.Linq;

namespace TomatoGrow;

public sealed class ThemePackCatalog
{
    public const string TomatoGrowDefaultManifestId = "tomatogrow_default";
    public const string MiningDemoManifestId = "mining_demo";
    public const string FarmingDefaultVocabularyId = "farming_default";
    public const string MiningDemoVocabularyId = "mining_demo";
    public const string FarmingDefaultInterfacePackId = "farming_default";
    public const string MiningDemoInterfacePackId = "mining_demo";

    private readonly IReadOnlyDictionary<string, ThemePackManifest> _manifestsById;
    private readonly IReadOnlyDictionary<string, ThemeVocabulary> _vocabulariesById;
    private readonly IReadOnlyDictionary<string, InterfacePack> _interfacePacksById;

    public ThemePackCatalog(
        IEnumerable<ThemePackManifest> manifests = null,
        IEnumerable<KeyValuePair<string, ThemeVocabulary>> vocabularies = null,
        IEnumerable<KeyValuePair<string, InterfacePack>> interfacePacks = null)
    {
        _manifestsById = (manifests ?? GetBuiltInManifests())
            .ToDictionary(manifest => manifest.Id, StringComparer.OrdinalIgnoreCase);
        _vocabulariesById = (vocabularies ?? GetBuiltInVocabularies())
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        _interfacePacksById = (interfacePacks ?? GetBuiltInInterfacePacks())
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
    }

    public ThemePackManifest GetManifest(string id)
    {
        if (!_manifestsById.TryGetValue(id, out ThemePackManifest manifest))
            throw new KeyNotFoundException($"Unknown theme pack manifest '{id}'.");

        return manifest;
    }

    public ThemePackManifest GetDefaultManifest()
    {
        ThemePackManifest manifest = _manifestsById.Values.SingleOrDefault(candidate => candidate.IsDefault);
        if (manifest is null)
            throw new InvalidOperationException("No default theme pack manifest is configured.");

        return manifest;
    }

    public ThemeVocabulary GetVocabularyForManifest(ThemePackManifest manifest)
    {
        if (!_vocabulariesById.TryGetValue(manifest.ThemeVocabularyId, out ThemeVocabulary vocabulary))
        {
            throw new InvalidOperationException(
                $"Manifest '{manifest.Id}' references unknown theme vocabulary '{manifest.ThemeVocabularyId}'.");
        }

        return vocabulary;
    }

    public InterfacePack GetInterfacePackForManifest(ThemePackManifest manifest)
    {
        if (!_interfacePacksById.TryGetValue(manifest.InterfacePackId, out InterfacePack interfacePack))
        {
            throw new InvalidOperationException(
                $"Manifest '{manifest.Id}' references unknown interface pack '{manifest.InterfacePackId}'.");
        }

        return interfacePack;
    }

    public IReadOnlyList<string> GetContentModuleIdsForManifest(ThemePackManifest manifest)
    {
        return manifest.ContentModuleIds
            .Where(moduleId => !string.IsNullOrWhiteSpace(moduleId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<ThemePackManifest> GetBuiltInManifests()
    {
        return
        [
            new ThemePackManifest
            {
                Id = TomatoGrowDefaultManifestId,
                Name = "TomatoGrow",
                Description = "Default TomatoGrow farming setup with the base crop, potato, and fermentation content modules.",
                ContentModuleIds = ["BaseGame", "FermentationPack", "PotatoPack"],
                ThemeVocabularyId = FarmingDefaultVocabularyId,
                InterfacePackId = FarmingDefaultInterfacePackId,
                IsDefault = true,
                Notes = "This remains the real game setup used by default startup in v1."
            },
            new ThemePackManifest
            {
                Id = MiningDemoManifestId,
                Name = "Mining Demo",
                Description = "Focused mining-flavored proof setup built from the opt-in MiningTestPack content module.",
                ContentModuleIds = ["BaseGame", "MiningTestPack"],
                ThemeVocabularyId = MiningDemoVocabularyId,
                InterfacePackId = MiningDemoInterfacePackId,
                Notes = "This is a focused demo manifest in v1. It currently includes BaseGame for shared restoration/setup assumptions and is not a full standalone replacement for TomatoGrow startup."
            }
        ];
    }

    private static IReadOnlyList<KeyValuePair<string, ThemeVocabulary>> GetBuiltInVocabularies()
    {
        return
        [
            new KeyValuePair<string, ThemeVocabulary>(FarmingDefaultVocabularyId, ThemeVocabulary.FarmingDefault),
            new KeyValuePair<string, ThemeVocabulary>(MiningDemoVocabularyId, ThemeVocabulary.MiningDemo)
        ];
    }

    private static IReadOnlyList<KeyValuePair<string, InterfacePack>> GetBuiltInInterfacePacks()
    {
        return
        [
            new KeyValuePair<string, InterfacePack>(FarmingDefaultInterfacePackId, InterfacePack.FarmingDefault),
            new KeyValuePair<string, InterfacePack>(MiningDemoInterfacePackId, InterfacePack.MiningDemo)
        ];
    }
}
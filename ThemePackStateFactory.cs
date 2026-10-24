using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TomatoGrow.GameContent;

namespace TomatoGrow;

public sealed class ThemePackStateFactory
{
    private readonly string _contentModulesPath;
    private readonly ThemePackCatalog _catalog;
    private readonly ContentModuleLoader _contentModuleLoader;
    private readonly ContentValidator _contentValidator;

    public ThemePackStateFactory(
        string contentModulesPath,
        ThemePackCatalog catalog = null,
        ContentModuleLoader contentModuleLoader = null,
        ContentValidator contentValidator = null)
    {
        _contentModulesPath = contentModulesPath ?? throw new ArgumentNullException(nameof(contentModulesPath));
        _catalog = catalog ?? new ThemePackCatalog();
        _contentModuleLoader = contentModuleLoader ?? new ContentModuleLoader();
        _contentValidator = contentValidator ?? new ContentValidator();
    }

    public GameStateFactoryResult CreateState(string manifestId, GameBalanceConfig balanceConfig = null)
    {
        ThemePackManifest manifest;
        try
        {
            manifest = _catalog.GetManifest(manifestId);
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException)
        {
            return GameStateFactoryResult.CreateFailure(exception.Message);
        }

        return CreateState(manifest, balanceConfig);
    }

    public GameStateFactoryResult CreateState(ThemePackManifest manifest, GameBalanceConfig balanceConfig = null)
    {
        List<string> errors = [];

        if (manifest is null)
        {
            errors.Add("Theme pack manifest must be provided.");
            return GameStateFactoryResult.CreateFailure(errors);
        }

        IReadOnlyList<string> moduleIds = _catalog.GetContentModuleIdsForManifest(manifest);
        foreach (string moduleId in moduleIds)
        {
            string modulePath = Path.Combine(_contentModulesPath, moduleId);
            if (!Directory.Exists(modulePath))
                errors.Add($"Manifest '{manifest.Id}' references missing content module '{moduleId}' at '{modulePath}'.");
        }

        ThemeVocabulary vocabulary = null;
        InterfacePack interfacePack = null;

        try
        {
            vocabulary = _catalog.GetVocabularyForManifest(manifest);
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }

        try
        {
            interfacePack = _catalog.GetInterfacePackForManifest(manifest);
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
        }

        if (errors.Count > 0)
            return GameStateFactoryResult.CreateFailure(errors);

        ContentRegistry registry = _contentModuleLoader.LoadFromModules(_contentModulesPath, moduleIds);
        ContentValidationResult validationResult = _contentValidator.Validate(registry);
        if (!validationResult.IsValid)
        {
            errors.AddRange(validationResult.Errors.Select(error => $"Manifest '{manifest.Id}' content validation failed: {error}"));
            return GameStateFactoryResult.CreateFailure(errors);
        }

        TurnipTestState state = new(
            registry,
            balanceConfig: balanceConfig,
            themeVocabulary: vocabulary,
            interfacePack: interfacePack);

        return GameStateFactoryResult.CreateSuccess(state, registry, manifest);
    }
}
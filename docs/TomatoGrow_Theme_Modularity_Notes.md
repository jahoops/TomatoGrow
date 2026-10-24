# TomatoGrow Theme Modularity Notes

## Purpose

TomatoGrow remains the main game. The goal of theme modularity is not to turn the project into a generic engine overnight. The goal is to separate a few presentation layers carefully so the same mechanics can be presented with different interface language and simple visual cues.

## Current Separation

- ThemeVocabulary = player-facing words such as Farm/Mine, Plot/Claim, Water/Power, Harvest/Extract.
- InterfacePack = symbols, short labels, and simple presentation colors used by the debug UI.
- Content modules = gameplay nouns and economy data such as items, crops, machines, recipes, market offers, and restoration sites.
- ThemePackManifest = setup metadata that groups content modules, ThemeVocabulary, and InterfacePack into one coherent built-in presentation preset.
- Engine = mechanics, progression, simulation rules, save/load, autoplay, validation, and rendering flow.

## Theme Pack Manifest v1

Theme packs are now a small setup-time layer, not a runtime switching system.

- `ThemePackManifest` groups content module ids, a vocabulary id, and an interface pack id.
- `ThemePackCatalog` is code-defined in v1 and resolves built-in manifests to the existing `ThemeVocabulary` and `InterfacePack` instances.
- `ThemePackStateFactory` is the standard v1 setup path for creating a validated `TurnipTestState` from a manifest id.
- manifests are deterministic metadata for tests, setup, and documentation.
- manifests do not change gameplay rules, controls, balance, save format, or content schemas.
- manifests are not written into save files in v1.
- no runtime theme picker or launcher flow exists yet in v1.

Built-in manifests in v1:

- `tomatogrow_default` = TomatoGrowDefault with `BaseGame`, `FermentationPack`, `PotatoPack`, `farming_default` vocabulary, and `farming_default` interface pack.
- `mining_demo` = MiningDemo with `BaseGame`, `MiningTestPack`, `mining_demo` vocabulary, and `mining_demo` interface pack.

`TomatoGrowDefault` remains the real game setup. `Game1` startup behavior is intentionally unchanged in v1.

`MiningDemo` is still a proof/demo setup, not a fully standalone alternate game. It currently includes `BaseGame` for shared restoration/setup assumptions while using mining presentation and mining content for the focused demo path.

## Manifest-Based State Factory v1

Manifest-based state creation is now centralized.

- `ThemePackStateFactory` resolves a manifest by id.
- it loads only the listed content modules.
- it validates the resulting content registry before state creation.
- it resolves the manifest's vocabulary and interface pack.
- it returns a `GameStateFactoryResult` with `Success`, `State`, `ContentRegistry`, and explicit `Errors`.

This keeps manifest setup deterministic and testable while leaving default game startup behavior stable.

In v1, manifests and the factory are setup-time metadata only. They are still not serialized into save files.

## Interface Pack v1 Boundary

InterfacePack is display-only in v1.

- It does not change gameplay behavior.
- It does not change controls.
- It does not change save format.
- It does not change content schemas.
- It does not change balance.
- It does not rename internal classes.
- It does not add sprites, animation, sound, or a UI framework.

The current high-value use is the farm grid and related tile-display helpers.

- tile fill colors
- selected border color
- ready-stage emphasis color
- short tile state labels
- simple tile symbols
- selected tile marker

## Generic Stat / Report Labels v1

Stats and reports now have a display-only terminology layer.

- `GameStats` storage remains unchanged and keeps the existing compatibility-oriented field names such as seeds, crops, watering, harvesting, and bot action counters.
- player-facing labels for the Stats screen, Farm Computer bot summary, Progression Report, and baseline report can now be generated from `ThemeVocabulary`.
- `ThemeVocabulary.FarmingDefault` keeps natural farming wording where practical.
- alternate vocabularies such as `ThemeVocabulary.MiningDemo` can present the same underlying counters with marker / vein / power / extract wording.
- formatting does not mutate game state, gameplay rules, controls, balance, content schemas, or save format.

This is intentionally a presentation seam, not a data migration. A future internal stats rename can wait until there is clear evidence that the added churn is worth it.

## Generic Bot Role Aliases v1

Bot roles now have a small generic alias layer.

- the existing classes remain `PlanterBot`, `WateringBot`, and `HarvesterBot` for compatibility.
- generic roles now exist for `Starter`, `Maintainer`, and `Collector`.
- `BotRoleFormatter` maps those roles to display names, action verbs, keys, and costs.
- `TurnipTestState` now exposes small role-based aliases for unlock/run state without replacing the existing bot-specific methods.
- `ThemeVocabulary` remains the source of player-facing bot names, so farming still reads as Planter / Watering / Harvester while mining reads as Claim / Power / Extractor.

This keeps the role concept reusable without forcing a risky internal class rename. A future rename remains optional, not required.

## Generic Grid / Tile Helpers v1

Grid and tile terminology now has a small neutral helper seam.

- the existing runtime classes remain `FarmGrid`, `FarmPlot`, and `PlantedCrop` for compatibility.
- generic helper vocabulary is now `ProductionGridView`, `ProductionTileView`, and active tile producer terminology.
- `TurnipTestState` now exposes `GetSelectedTile()` and `GetProductionGrid()` as neutral aliases alongside the older farming-named accessors.
- this is a read-only terminology seam, not a gameplay rewrite.

Conceptual mapping:

- `FarmGrid` = production grid
- `FarmPlot` = production tile
- `PlantedCrop` = active producer instance

This keeps new code free to talk about grids, tiles, and producers without forcing a broad internal rename. Future internal renames remain optional and should wait until more proof themes justify the churn.

## Production Area Container Model v1

`ProductionArea` is now the generic concept for a container of production tiles.

- it is a small read-only wrapper around an existing production grid view
- it currently exposes identity, name, area type, unlocked state, display order, interface token, and the wrapped `ProductionGridView`
- the default TomatoGrow farm is represented as one built-in `ProductionArea` with id `outdoor_garden`
- `outdoor_garden` wraps the existing `FarmGrid`; gameplay still acts directly on `FarmGrid` exactly as before
- future facilities such as Greenhouse should first be modeled as another `ProductionArea`, not as a hardcoded building interior or separate screen flow
- structures/facilities should first be treated as simple containers/effects before any later decision about deeper mechanics

This keeps future non-farm spaces aligned with the same start / maintain / collect loop while avoiding premature special cases.

## Theme Pack Runtime Diagnostics v1

Runtime diagnostics now provide a small read-only summary of the currently loaded modular setup.

- `ThemePackRuntimeDiagnostics` reports the active `ThemeVocabulary` and `InterfacePack` identities.
- it summarizes loaded content modules, starter items, tile producers, machines, recipes, market offers, and restoration sites.
- a compact summary is now exposed on the existing Help screen so the active modular setup is visible during development without adding a new screen type.
- it is development/adaptation infrastructure for verifying that a runtime state is assembled the way a theme pack expects.
- it does not add runtime theme switching, a theme picker, or save-time theme state.
- the detailed report and the Help summary remain read-only display/reporting helpers.

This gives future forks and proof packs a deterministic way to confirm that content, vocabulary, and interface layers are aligned without changing gameplay or startup behavior.

## Visual Growth Feedback v1

Before real art, TomatoGrow now leans on simple visual tokens, growth bars, and emphasized ready states to create visible progress in the text-and-shape UI.

- visual accomplishment matters for motivation, even in a low-fi presentation
- the farm grid should show growing progress at a glance instead of reading like a raw debug table
- `ThemeVocabulary` still owns words while `InterfacePack` owns the compact visual tokens and emphasis cues
- this is presentation-only and should not change gameplay rules, save data, controls, or balance

Project pacing note:

- the roadmap should keep mixing engine/tooling milestones with visible game-progress milestones so the project continues to feel rewarding to work on

## Why This Step Exists

ThemeVocabulary proved that wording can be separated from mechanics. InterfacePack is the next safe step: a small presentation layer that makes farming and mining-style demos look different at a glance without changing how the simulation works.

This keeps the architecture incremental.

- content stays data-driven
- words stay configurable
- presentation tokens stay configurable
- mechanics stay stable

## Mining Test Content Pack v1

`ContentModules/MiningTestPack` is a tiny content-only proof pack.

- It is not a separate game mode.
- It is not loaded by default.
- It reuses the existing seed -> grow -> harvest -> process -> sell loop with mining-flavored nouns and display names.
- It proves that content modules, ThemeVocabulary, and InterfacePack can combine to present a mining-style slice without changing the main TomatoGrow rules.

The current tests prove three different things:

- mining items, crops, machines, recipes, and market offers can load and validate when test packs are included explicitly
- production reports, market previews, and formatter output can describe mining content through the existing generic content/reporting paths
- processor machine runtime instances now come from loaded machine content, so mining-only runtime tests can use smelter and forge through the existing machine loop

## Generic Processor Machine Instances v1

Processor machine runtime instances are now created from loaded machine definitions instead of being hardcoded to `fermenter` and `aging_barrel`.

- each loaded machine definition creates one runtime `ProcessorMachineInstance`
- the runtime list uses deterministic content order
- default TomatoGrow content still creates Fermenter and Aging Barrel
- `MiningTestPack` runtime now creates Smelter and Forge
- autoplay, save/load, invariant validation, activity logging, and machine screens all operate on the generic runtime machine list

## Generic Tile Producer Terminology v1

The engine still uses farming-heavy class names internally in many places. This step adds a small helper terminology layer instead of attempting a risky broad rename.

- `collect` is the neutral engine verb for harvest, mine, gather, or collect finished outputs
- `maintain` is the neutral engine verb for water, power, feed, or operate
- `starter item` is the neutral term for seed, marker, or other tile-starting input
- `output item` is the neutral term for crop, ore, egg, milk, or similar result

Current implementation notes:

- `CropDefinition` is still the underlying content type, but helper views now expose starter item, output item, and production days terminology
- `FarmPlot` is still the underlying runtime tile type, but helper views now expose maintained and ready-to-collect terminology
- `TurnipTestState` still keeps farming-named methods for compatibility, but selected-tile orchestration can now route through neutral `start`, `maintain`, and `collect` aliases
- `Game1` input wrappers and autoplay manual fallback now use the neutral aliases internally while preserving the existing P/W/H controls and gameplay results
- player-facing wording still comes from `ThemeVocabulary`, so farming stays farming and `MiningDemo` stays power/extract where appropriate

This is the safe migration path. A broader internal rename should wait until multiple non-farming themes prove that the additional churn is warranted.

## Generic Tile Producer Content Schema v1

`crops.json` remains supported and unchanged for farming content.

- BaseGame, PotatoPack, and FermentationPack still define crops through `crops.json`
- those crop definitions are projected into generic `TileProducerDefinition` entries at load time
- existing crop APIs still work for farming content, including `CropsById` and `TryGetCropBySeedItemId`

`tile_producers.json` is the neutral schema for new themes.

- `TileProducerDefinition` uses `id`, `starterItemId`, `outputItemId`, and `productionDays`
- non-farming content such as `MiningTestPack` can now describe markers -> ore production without pretending to be a crop module
- the live start/maintain/collect loop, save validation, runtime invariant checking, and autoplay producer discovery now resolve against generic tile producers

Compatibility notes:

- `CropDefinition` remains for compatibility and farming-oriented code paths
- save format is unchanged; persisted plot ids still use the existing `CropDefinitionId` field name and now validate against the generic producer registry underneath
- starter item discovery now comes from `tile_producers.json` and crop-projected producer mappings rather than `ItemDefinition.Type`
- `MiningTestPack` marker items can now use a neutral `marker` item type while the existing starter-selection, buy, autoplay, save/load, and validator paths continue to work through producer-driven starter lookup

## Future Direction

A cautious sequence still makes sense:

1. Theme vocabulary
2. Interface packs
3. Theme pack manifests
4. JSON manifest files if the setup layer proves durable
5. Launcher/theme selection only after a real setup use case exists
6. Tiny standalone Mining Demo setup if restoration/startup assumptions are separated cleanly
7. Visual symbol/color mappings per item or machine where proven useful
8. Only later consider internal renames or assembly splits if the modularity work has already proven durable

Other seams still visible today:

- internal `Farm*` and `Crop*` naming remains farming-specific
- starter and maintenance actions still use farming-shaped mechanics even when the display vocabulary changes
- some action/help text still assumes farming verbs outside the ThemeVocabulary layer

## What This Is Not

- not a skinning framework
- not a sprite system
- not a dynamic theme switcher
- not a content-pack rewrite
- not evidence that TomatoGrow should stop being a farming game

The project should keep using small, testable seams instead of a giant speculative refactor.

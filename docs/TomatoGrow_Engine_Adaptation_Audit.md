# TomatoGrow Engine Adaptation Audit

## Purpose

TomatoGrow remains the main game.

`MiningDemo` and `MiningTestPack` exist to prove adaptation seams, not to replace TomatoGrow or to justify a premature universal engine. The current approach is to reduce unnecessary coupling step by step, keep proven TomatoGrow behavior stable, and only generalize the parts that already need to support a second calm production/automation theme.

The goal of this audit is to clarify three things:

- what is already reusable enough to support adjacent themes
- what is still intentionally TomatoGrow or farming specific
- what should be decoupled only through small, behavior-preserving seams rather than a broad rename or platform rewrite

## Already Reusable / Engine-Like Systems

The following systems are already reasonably theme-neutral or are on a practical path to reuse without broad churn:

- Content modules loaded from `ContentModules`
- `ItemDefinition` and item-driven buy/sell/value behavior
- `TileProducerDefinition` and `tile_producers.json`
- generic starter item discovery through `ContentRegistry.GetStarterItemIds()` and `IsStarterItem()`
- generic `StartSelectedTileProducer`, `MaintainSelectedTile`, and `CollectSelectedTile` aliases
- content-driven processor machine runtime instances
- processing recipes and processing economics
- shared storage and accessible-inventory logic
- market offers
- market opportunities
- activity ledger / activity log
- save/load validation flow
- runtime invariant validation in `GameStateValidator`
- deterministic autoplay smoke-test coverage
- progression simulation / production report support
- `ThemeVocabulary`
- `InterfacePack`
- `ThemePackManifest`
- `ThemePackStateFactory`

These systems are not fully generic in naming, but they already separate content, presentation, validation, and deterministic simulation strongly enough to support proof themes like mining.

## Still Farming / TomatoGrow-Specific Seams

Several seams remain clearly farming- or TomatoGrow-specific. Most are acceptable for now because they preserve compatibility and do not yet block the current proof themes.

High-value remaining seams:

- runtime owner and type names such as `TurnipTestState`, `FarmGrid`, `FarmPlot`, `FarmRegion`, `CropDefinition`, and `PlantedCrop`
- bot class names and unlock names such as `PlanterBot`, `WateringBot`, and `HarvesterBot`
- default startup still constructs `TurnipTestState` directly in `Game1` instead of routing through `ThemePackStateFactory`
- hardcoded restoration defaults in `TurnipTestState`, especially `old_junk_pile`, farm-expansion text, and cleanup assumptions
- convenience properties such as `Fermenter` and `AgingBarrel`
- farming-oriented stat and reporting names that still refer to seeds, crops, watering, and harvesting
- save DTO field names that still refer to crop or seed concepts for compatibility
- some direct runtime messages remain farming-shaped even when the display layer is mining-themed, for example action return messages like `Watered plot 0,0.` or `Harvested ...`
- content module names such as `BaseGame` and `FermentationPack` still reflect TomatoGrow’s current content structure
- UI screen ownership and layout still revolve around the TomatoGrow farm shell in `Game1`

Additional observations from the audit search:

- engine-adjacent code no longer shows the old `ItemDefinition.Type == "seed"` policy path as a major remaining coupling seam; starter discovery is already routed through tile producers
- the biggest remaining coupling is now naming and default orchestration, not starter classification
- `MiningDemo` still relies on `BaseGame` for shared restoration/setup assumptions, which is a reasonable compatibility choice in v1 but not a fully standalone alternate game setup

## Coupling We Should Intentionally Keep For Now

Some coupling is useful and should remain explicit at this stage.

- TomatoGrow’s default farming content and economy
- the name `TomatoGrow`
- current controls and input model
- the current farming default presentation
- existing class names unless they block a real next seam
- save compatibility field names and existing serialized structure
- tests that explicitly verify TomatoGrow behavior, balance assumptions, and compatibility behavior
- the direct `Game1` startup path, unless routing it through the factory becomes an obviously zero-risk cleanup

Keeping these stable protects the real game while allowing reuse to emerge only where the code already proves it is needed.

## Recommended Low-Risk Next Refactors

The next steps should stay small, local, and testable.

1. Add generic stat aliases or reporting labels for starter / producer / collect concepts while keeping current stat properties for compatibility.
2. Add bot-role aliases such as starter / maintainer / collector bot alongside `PlanterBot`, `WateringBot`, and `HarvesterBot`.
3. Add more generic production-grid helper names around `FarmGrid` and `FarmPlot` rather than renaming those types broadly.
4. Consider routing default startup through `ThemePackStateFactory` only if the change is demonstrably low risk and leaves startup behavior identical.
5. Continue moving remaining player-facing UI strings through `ThemeVocabulary` where they are still clearly farming-shaped.
6. Revisit internal class or file renames only after additional proof themes show that naming churn would pay for itself.

## Red-Zone Refactors To Avoid Now

The following changes are high-risk and low-value at this stage:

- renaming the whole codebase to remove every farming term
- splitting the solution into multiple assemblies prematurely
- adding a runtime theme picker or launcher flow
- introducing a plugin system
- introducing a scripting system
- adding arbitrary behavior callbacks that make content definitions dynamic in uncontrolled ways
- adding unrelated systems such as conveyors, pathfinding, or factory-automation mechanics just to appear more engine-like
- broadly rewriting the save schema to remove farming-shaped names

These would add churn faster than they would add proven reuse.

## Future Adaptation Path

The likely path remains incremental:

- TomatoGrow continues as the main shipped game shape.
- `MiningTestPack` continues as proof content.
- future related games can reuse the current core by supplying:
  - content modules
  - a `ThemeVocabulary`
  - an `InterfacePack`
  - a `ThemePackManifest`
  - a `ThemePackStateFactory` setup path
  - if needed later, a small custom startup wrapper around the same core systems

That path keeps the architecture honest. Reuse comes from proven seams that already support a second theme, not from speculative “engine” work divorced from TomatoGrow’s real needs.
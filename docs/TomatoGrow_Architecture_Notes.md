# TomatoGrow Architecture Notes

## Overview

TomatoGrow uses a small, explicit architecture built around one gameplay state owner, deterministic text UI formatting, data-driven content, and a large regression test suite. The project favors direct action methods and validated data over deep abstraction layers.

## Core Runtime Shape

- `TurnipTestState` is the gameplay source of truth. It owns farm state, inventory, storage, bots, machines, market opportunities, activity entries, save DTO creation, save application, and gameplay-facing actions.
- `TurnipTestState` also owns code-level defaults such as `GameBalanceConfig` and the current `ThemeVocabulary`, but those values are runtime configuration only and are not serialized into saves.
- `Game1` is the MonoGame shell. It owns input routing, update timing, screen switching, pause handling, save/load prompts, and rendering orchestration.
- `DebugUiTextFormatter` is the deterministic text formatting layer for dashboard panels, sub-screens, economics displays, help text, and market opportunity summaries.
- `AutoPlayTestController` is a deterministic full-loop smoke test controller that drives the live game using public actions.

## JSON Content Modules

Game content is loaded from JSON module folders under `ContentModules`.

- `ContentModuleLoader` reads module files.
- `ContentRegistry` provides resolved lookup tables and normalized collections.
- `ContentValidator` verifies ids, references, duplicates, and required fields before gameplay uses the content.

The current content model includes:

- items
- crops
- tile producers
- machines
- processing recipes
- market offers

This keeps balancing and content growth mostly data-driven.

## Action-Method Pattern

UI input, autoplay, and most tests should call gameplay action methods instead of mutating state directly.

Examples include:

- planting, watering, harvesting, selling, and advancing the day
- buying seeds and market offers
- unlocking bots
- starting and collecting processor machines
- depositing to and withdrawing from shared storage

This pattern keeps messages, stats, activity logging, and gameplay rules aligned in one place.

## Screen And Navigation Pattern

The debug UI uses explicit screen modes in `DebugUiState`.

- Farm is the primary action screen.
- Inventory, Storage, Market, Computer, Activity Log, Machines, Stats, and Help are separate read-heavy screens.
- Escape returns from sub-screens to Farm, and exits only when already on Farm.
- Gameplay continues updating on sub-screens unless the game is paused.

`DebugUiTextFormatter` supplies screen-specific line builders so the rendered UI remains deterministic and easy to test.

## Display Vocabulary Boundary

Player-facing text now has a small display-label layer via `ThemeVocabulary`.

- `ThemeVocabulary` is display-only. It exists to reduce hardcoded farming terms in UI text without renaming internal gameplay classes, save data, or JSON content schemas.
- Default gameplay still uses the farming vocabulary, so current behavior and terminology stay stable unless a caller explicitly swaps the vocabulary.
- `ThemeVocabulary.MiningDemo` exists as a built-in proof preset only. It does not add mining mechanics, mining content, new items, new recipes, or a theme-switching feature.
- Formatter and rendering surfaces are the preferred integration points for vocabulary changes.
- The purpose of `MiningDemo` is to prove that UI language can separate from engine behavior. A future tiny `MiningTestPack` could be explored later, but it is intentionally out of scope for the current project shape.
- Future non-farming themes should be proven with small test/demo slices before any broader engine renaming is considered.

`InterfacePack` is the neighboring display layer for simple visual presentation tokens.

- `InterfacePack` is also display-only in v1. It covers simple grid symbols, short state labels, and color choices without changing gameplay rules or saved state.
- `TurnipTestState` currently owns both `ThemeVocabulary` and `InterfacePack` so tests and rendering can use one deterministic presentation pairing.
- Content modules still own gameplay nouns and economy data; `ThemeVocabulary` owns words; `InterfacePack` owns simple presentation tokens.

## Processor-Machine Pattern

Machines follow a generic processor pattern:

- machine definition in content
- recipe definition in content
- input consumed from inventory or shared storage
- time passes in days
- output becomes collectable later

This pattern currently supports the fermenter and aging barrel while keeping room for more processing chains without rewriting the simulation model.

## Generic Tile-Producer Terminology Layer

The runtime still uses farming-oriented types such as `FarmPlot`, `CropDefinition`, and farming-named action methods. That is intentional for now.

A small helper layer now provides neutral producer/tile terminology without changing gameplay behavior, save data, or content schemas.

- `collect` is the neutral engine verb for harvest, mine, gather, or collect output
- `maintain` is the neutral engine verb for water, power, feed, or operate
- `starter item` maps to the existing seed-item concept
- `output item` maps to the existing harvest-item concept

The action layer now also has neutral aliases for the selected-tile loop.

- `StartSelectedTileProducer`, `MaintainSelectedTile`, and `CollectSelectedTile` are the preferred neutral orchestration actions
- existing farming-named methods remain as compatibility wrappers so controls, tests, save/load behavior, and external call sites do not break
- `Game1` input routing and autoplay manual fallback can use the neutral aliases without changing player-facing text
- `ThemeVocabulary` still owns displayed verbs, so the engine can route through neutral actions while UI keeps showing Water, Harvest, Power, or Extract as appropriate

This layer exists so future themes can adapt the current simulation model safely. It should remain a thin wrapper until multiple themes justify broader internal renaming.

## Production Area Container Model

The engine now has a small generic `ProductionArea` model for things that contain production tiles.

- `ProductionArea` is intentionally read-only or mostly read-only in v1
- the current `FarmGrid` is exposed as the default `outdoor_garden` production area
- `TurnipTestState` can now return all production areas, the default production area, and the unlocked production areas
- this is a wrapper/adaptor layer, not a gameplay rewrite
- no second active grid, greenhouse mechanics, interiors, controls, or save schema changes were introduced in this step

Design intent:

- a future Greenhouse should be another production-area container with different presentation or unlock rules
- it should not start life as a hardcoded structure or interior simulation
- future facilities should first be modeled as simple containers of production tiles plus optional effects

## Generic Tile-Producer Content Schema

`crops.json` remains the supported farming-era content schema. Existing TomatoGrow modules still load through `CropDefinition`, old crop lookups still work, and no save schema changed in v1.

`tile_producers.json` is the new neutral schema for non-farming themes.

- `TileProducerDefinition` carries `Id`, `StarterItemId`, `OutputItemId`, and `ProductionDays`
- `ContentRegistry.TileProducersById` is the engine-facing lookup used by generic producer gameplay, save validation, runtime validation, and autoplay producer discovery
- crop entries are projected into equivalent tile producers automatically so old farming content participates in the generic producer path without duplication
- explicit tile producers do not create `CropDefinition` compatibility entries in v1 unless a real crop also exists; old crop APIs remain compatibility surfaces for farming content
- starter item discovery now comes from tile-producer mappings, not `ItemDefinition.Type == "seed"`
- non-farming starter items such as mining markers can therefore use neutral item types while still participating in buying, save validation, runtime validation, autoplay, and sellability protection

## Storage And Computer Overview

Shared storage is a global buffer between direct farm output and later use.

- sellable goods can be deposited and withdrawn
- storage contributes to accessible item counts for processing
- the Farm Computer shows combined player and storage inventory, sellable value, market summary, machine summary, bot status, farm summary, save/load status, and recent activity

The computer screen is the high-level overview surface for the whole farm.

## Activity Ledger

The activity ledger records notable runtime actions such as sales, machine starts, machine collections, market opportunity transitions, and save/load outcomes. It is bounded, visible both on its own screen and in compact form on the Farm Computer, and validated as part of runtime state checking.

## Market Opportunities

Market opportunities are modeled as positive, temporary sell-price bonuses.

- only one active opportunity is currently supported
- selling and value previews use effective sell price when applicable
- opportunities are persisted in saves and validated on load
- the market, computer, and dashboard surfaces expose the active opportunity
- autoplay responds modestly by favoring compatible seeds and recipes without becoming a full optimizer

## Save / Load Validation

Save/load is DTO-based and validation-first.

- gameplay creates a `SaveGameState`
- save JSON is written through `SaveGameFileService`
- `GameSaveController` owns save/load status and user-facing messages
- save validation runs before apply
- invalid saves do not partially mutate live state
- load resets UI back to Farm, unpaused, with autoplay disabled

## Runtime Invariant Checker

`GameStateValidator` verifies live state consistency across:

- farm plots and planted crops
- selected seed and inventory counts
- storage counts
- machines and machine payloads
- processed output stats
- activity log entries
- market opportunity state

This provides a second line of defense beyond content validation and save validation.

## Autoplay As Smoke Test

Autoplay is intentionally deterministic and conservative.

- it protects seed safety first
- it performs the same public actions a player would
- it buys market inputs conservatively when that helps prove a production chain
- it now responds modestly to market opportunities by preferring supportive seeds and matching recipes
- it is not intended to be a smart planner or economic optimizer

## Design Boundaries

- prefer small, local changes over broad rewrites
- avoid speculative architecture
- keep runtime behavior readable in code and in UI text
- expand via validated content and focused tests whenever possible
- avoid premature internal renames when a display-only vocabulary layer is sufficient

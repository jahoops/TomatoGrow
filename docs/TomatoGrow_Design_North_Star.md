# TomatoGrow Design North Star

## Project Identity

TomatoGrow is a calm, data-driven farming automation game. It is built around a readable systems loop where the player starts with direct farm chores, then gradually hands those chores off to automation, expansion, processing, and live farm overview tools.

TomatoGrow remains the primary game identity. At the same time, the codebase should evolve carefully toward a reusable calm production/automation engine where theme shifts can be explored first through player-facing vocabulary and small content experiments rather than giant internal renames.

The intended core loop is:

manual chores -> automation -> expansion -> processing -> overview

Money is fuel, not the main goal. The game uses money to unlock more capacity, better flow, and clearer production choices, but the softer goals matter more: expansion, automation, optimization, and later restoration and beautification.

## Core Principles

- Manual chores come first so later automation is legible and earned.
- New systems should stay data-driven, deterministic, and easy to test.
- The player should build reliable production flow, not perform high-action inputs.
- Capacity is progress: more plots, more bots, more machines, more storage, better overview.
- Debug-friendly presentation is acceptable while the core simulation is still evolving.

## Tone And Pressure Model

TomatoGrow is designed around opportunity without pressure.

- Market opportunities are positive bonuses, not punishments.
- Temporary bonuses should invite action without creating stress.
- The game should remain calm even when it is asking the player to notice a profitable moment.

TomatoGrow should not include:

- combat
- spoilage
- raids
- enemies
- crop destruction
- disease
- nuisance byproducts
- pressure timers

## Automation Philosophy

- Bots automate chores the player already understands.
- Autoplay exists as a deterministic full-loop smoke test, not as a fantasy AI worker layer.
- Automation should reduce repetition without hiding the production chain.
- The game should avoid complex scripting, conveyors, pathfinding, and robot-routing puzzles.
- Robot movement can stay abstract for a long time.

## Content And Scope Philosophy

- The engine defines verbs.
- JSON content modules define nouns, recipes, offers, and tuning.
- New content should mostly come from validated data rather than special-case code.
- Processor chains should stay understandable from the debug UI and tests.
- Avoid premature graphics work while the systems model is still tightening.
- Display vocabulary should stay decoupled from internal model names until a new theme is proven worth deeper structural change.

## Soft Goals


## Things To Avoid


## Playable Visual Farm UI

Visual accomplishment matters for motivation. Before real art exists, the game should still show progress through large simple visual states, readable growth bars, strong ready emphasis, and explicit action panels.

Text reports remain useful for debugging and systems clarity, but the farm view should become increasingly visual and playable rather than asking the player to parse tiny status text.

Important actions should not be hidden behind memorized function keys when the main play screen can surface them directly.

Before mouse UI exists, TomatoGrow should use large visual keyboard-action buttons and command panels so the player can see what to press without scanning tiny help text.

Machines should also be visible actors on the main farm screen. The farm view should show which processors exist, whether they are idle, working, or ready, what they are producing, and what the next useful machine action is without forcing a screen switch.

The immediate goal is to reduce key memorization and tiny-text scanning while keeping the implementation simple, deterministic, and testable.

Mouse clicking, hit-testing, hover states, and a fuller UI framework are intentionally deferred until the command model is clearer.

TomatoGrow should stay compact, legible, and extensible. The game is not trying to become a combat game, a life-sim sprawl, or a factory-programming sandbox before the calm farm automation identity is fully proven.
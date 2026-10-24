# TomatoGrow Balance Notes

## Purpose

Balance Configuration v1 centralizes code-owned tuning values in `GameBalanceConfig` so progression changes are deliberate and reviewable.

Use `GameBalanceConfig.Default` for normal gameplay.
Use `GameBalanceConfig.Default with { ... }` in tests or focused simulations when you need a cheaper or faster scenario.

## What Lives In GameBalanceConfig

These values are currently code-owned and should be tuned in `GameBalanceConfig`:

- Bot unlock costs
  - `PlanterBotCost`
  - `WateringBotCost`
  - `HarvesterBotCost`
- Farm expansion costs
  - `FirstExpansionCost`
  - `SecondExpansionCost`
- Autoplay defaults
  - `AutoPlayTargetMoney`
  - `AutoPlayMaxDays`
  - `AutoPlayMaxSteps`
  - `AutoPlayNoActionLimit`
- Activity and report limits
  - `ActivityLogMaxEntries`
  - `ProgressionSimulationDefaultTargetMoney`
  - `ProgressionSimulationDefaultMaxDays`
  - `ProgressionSimulationDefaultMaxSteps`
- Simulation-only market opportunity defaults
  - `MarketOpportunityDefaultDurationDays`
  - `MarketOpportunityDefaultMultiplier`
- Positive market opportunity generator defaults
  - `MarketOpportunityGenerationIntervalDays`
  - `MarketOpportunityGeneratedDurationDays`
  - `MarketOpportunityGeneratedMultiplier`

## What Stays In JSON Content Modules

These values remain content-driven and should stay in the content modules:

- Item buy and sell prices in `items.json`
- Crop growth timing and harvest mapping in `crops.json`
- Machine definitions in `machines.json`
- Processing durations and recipe IO in `processing_recipes.json`
- Outside Supply Market offers in `market_offers.json`
- Restoration cleanup costs and rewards in `restoration_sites.json`

This split is intentional.
Item, crop, machine, market, and restoration data describe game content.
`GameBalanceConfig` only centralizes engine-side tuning that was previously scattered through code.

## Market Opportunity Generator v1

The runtime market opportunity generator is positive-only.

- it creates occasional tempting boosts for one sellable item.
- it does not create penalties, shortages, spoilage, or pressure events.
- ignoring a generated opportunity has no direct punishment.
- candidate items are deterministic and content-driven: processing outputs first, then sellable producer outputs if needed.
- generated opportunities are surfaced on the Farm screen and Farm Computer so the current bonus is visible without opening extra tooling.
- visibility is informational only; these are not warning timers and do not punish the player for waiting them out.
- tuning should be adjusted through `GameBalanceConfig` and checked with the Progression Report or focused tests.

## Current Ownership Notes

- Cleanup cost is not duplicated in `GameBalanceConfig` because restoration site cost already belongs to restoration content.
- Save/load does not serialize `GameBalanceConfig`.
  Saves remain game-state data, not balance snapshots.
- If farm expansion costs are ever moved to content, remove them from `GameBalanceConfig` instead of mirroring the values in two places.

## How To Tune Safely

1. Change one or two values in `GameBalanceConfig`.
2. Run the Progression Report.
3. Check unlock timing, expansion timing, money flow, and stop reason.
4. Re-run tests before keeping the change.

Do not tune by guessing from a single play feel.
Use the Progression Report to validate whether a balance change improves or harms the loop.

## Example

To make a test scenario cheaper:

```csharp
var config = GameBalanceConfig.Default with
{
    PlanterBotCost = 1,
    FirstExpansionCost = 5,
    AutoPlayTargetMoney = 100
};
```

Pass that config into `TurnipTestState` or `ProgressionSimulationRunner` when you want a fast scenario without changing the normal default game balance.

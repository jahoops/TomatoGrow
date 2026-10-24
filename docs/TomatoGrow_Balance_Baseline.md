# TomatoGrow Balance Baseline

Milestone: Balance Baseline Snapshot v1
Date: May 25, 2026

## Default config values summary

- Planter Bot cost: 60
- Watering Bot cost: 90
- Harvester Bot cost: 120
- First expansion cost: 150
- Second expansion cost: 300
- Autoplay target money: 3000
- Autoplay max days: 365
- Autoplay max steps: 5000
- Autoplay no-action limit: 6
- Progression simulation target money: 3000
- Progression simulation max days: 365
- Progression simulation max steps: 10000

## Baseline progression summary

Current default balance reaches the target money cleanly with no invariant errors.

- Stop reason: Reached $3000.
- Days simulated: 60
- Steps run: 627
- Final money: $3072
- Potential money: $3232
- Invariant status: OK
- Stalled: no

## Important milestone days

- First crop planted: Day 1
- First crop sold: Day 5
- First seed bought: Day 5
- Planter Bot unlocked: Day 6
- First machine run: Day 7
- First machine output collected: Day 9
- First market purchase: Day 11
- First expansion: Day 14
- First processed good sold: Day 14
- First aged good sold: Day 14
- Watering Bot unlocked: Day 15
- Harvester Bot unlocked: Day 18
- Old Junk Pile cleared: Day 24
- Final 5x3 expansion: Day 26

## Economy summary

- Final money: $3072
- Potential money at stop: $3232
- Market money spent: $350
- Market purchases: 2
- Machine value added: +$1150

## Automation summary

- Bots unlocked: 3
- Expansions completed: 2
- Cleanup sites cleared: 1
- The baseline reaches all three bot unlocks before the run ends.
- The baseline clears Old Junk Pile and completes the final expansion.

## Processing summary

- First machine run begins on Day 7.
- First machine output is collected on Day 9.
- First processed good sale happens on Day 14.
- First aged good sale also happens on Day 14.
- Total machine value added in the baseline run is +$1150.

## Interpretation notes

Current baseline reaches all three bots, clears Old Junk Pile, expands the farm to 5x3, uses processors, uses the market, and ends without invariant errors. This is the pre-tuning reference for future balance changes.

Do not tune by guessing from feel alone. Change a small number of config values, rerun the baseline snapshot, rerun the Progression Report, and compare the new curve against this reference before keeping the change.

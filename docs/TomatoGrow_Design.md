docs/TomatoGrow_Design_North_Star.mdTomatoGrow Design North Star

TomatoGrow is a small, calm, data-driven farming automation game about turning manual chores into systems, then using those systems to expand into new productive areas.

The player begins by doing simple farming tasks manually:

plant → water → advance days → harvest → sell

Then the player gradually unlocks simple region-based bots that automate those same chores:

manual planting → Planter Bot
manual watering → Watering Bot
manual harvesting → Harvester Bot

The game should not be story-heavy, combat-based, rushed, or full of annoyance mechanics. It should feel more like a calm automation sandbox than a traditional farming RPG.

Core Design Principles

Money is fuel, not the main goal.
Money should let the player buy seeds, unlock bots, clear/expand land, and later build machines. The emotional reward should be expansion, automation, and optimization.

Manual first, automation second.
Every automated chore should first exist as a simple manual action. The player understands the chore, then earns the ability to remove it.

Expansion should create new possibilities, not just more squares.
New areas should eventually unlock new mechanics, constraints, or production choices: better soil, robot bays, greenhouse zones, machine areas, special crop zones, etc.

Keep robots simple.
Bots should not be programmable at first. They should have:

one job
one region
simple conditions
clear output

No scripting, pathfinding, movement, water-refill puzzles, or complex inventory logistics until there is a very strong reason.

Keep graphics debug-simple until the engine is fun.
Use rectangles, colors, text, dashboards, and selected-cell detail panels. Avoid real art, sprites, sounds, animations, mouse UI, or drag/drop until the systems are stable.

Use JSON modules for content.
Crops, seeds, items, and eventually machines/recipes should come from validated content packs. The engine defines verbs; JSON defines nouns and wiring.

Code: grow, plant, water, harvest, sell, process
JSON: turnip, potato, grape, fermenter, wine
Near-Term Roadmap
012 Seed Buying
- Buy selected seed using money.
- Keep it simple; no full shop UI yet.

013 Expand Farm Grid
- Spend money to expand beyond 3x3.
- Expansion should feel like the first major reward.

014 Stats Panel
- Show useful totals: days passed, money earned, crops harvested, bot actions, sell value, automation efficiency.

015 Simple Area Unlock / Clear Junk
- Add locked/blocked areas that can be cleared with money.
- Make expansion visible and satisfying.

016 Bot Region Assignment
- Let bots operate on specific player-defined regions.
- Still no pathfinding or scripting.

017 Processor Machines
- Generic input + time + output machines.
- Example: grape → fermenter → wine.
Things to Avoid
No combat.
No story-heavy structure.
No NPC schedules or romance systems.
No complex robot scripting.
No pathfinding.
No water-refill puzzle for bots.
No premature sprites/sounds/animations.
No large UI framework.
No scope explosion into Stardew Valley.
Current Project Identity

TomatoGrow is a compact, testable, module-driven farming automation prototype where the player earns the right to stop doing chores manually.
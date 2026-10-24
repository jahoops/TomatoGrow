# TomatoGrow Testing Strategy

## Purpose

TomatoGrow is being built through small, frequent milestones. The test strategy exists to make that pace safe by catching regressions early while the project keeps layering new systems onto the same core simulation.

## Main Testing Layers

### Unit Tests For Systems

Focused unit tests cover the core gameplay surfaces and supporting services, including:

- farming actions
- bots
- processor machines and aging
- storage and selling
- content loading and validation
- save/load behavior
- UI text formatting
- runtime invariant validation
- market opportunity behavior

These tests keep small changes local and make data or rule regressions obvious.

### Scenario Tests

Scenario-style tests verify that multi-step flows still work when several systems interact. These are especially valuable for:

- production chains
- machine usage from inventory and storage
- market purchasing for processing
- screen-visible summaries
- longer progression loops

### Autoplay Smoke Tests

Autoplay serves as a deterministic full-loop smoke test.

- it exercises the live game through public actions
- it proves that a fresh run can still progress
- it catches interaction bugs across farming, bots, expansion, processing, storage, market use, and selling
- it now also proves modest response to positive market opportunities

Because autoplay uses the same action surface as the player, it is a strong integration test for the whole project.

### Save / Load Round Trips

Save/load tests verify that:

- state can be serialized and restored correctly
- invalid save data is rejected before mutation
- content ids referenced in saves are validated
- new persistent systems, such as market opportunities, keep working after round trips

### Content Validation

Content tests verify that JSON-driven definitions remain safe to load.

- ids must exist and be unique
- references must resolve correctly
- required fields must be present
- new modules can be added without breaking the registry contract

This protects the data-driven architecture from silent content drift.

### Runtime Invariant Validation

The runtime invariant checker provides a second layer of protection beyond unit tests.

It verifies that live state remains internally consistent across inventory, plots, machines, activity log entries, and market opportunity state. Tests ensure the validator catches known invalid conditions.

## Why This Matters

TomatoGrow is growing by incremental milestones rather than large rewrites. That only works if the project can safely absorb constant change.

The current strategy supports fast incremental development by making it practical to:

- add a feature
- extend content or UI formatting
- run focused tests
- confirm the full suite still passes
- move on without destabilizing older milestones

In short, tests are what let the project stay small, ambitious, and maintainable at the same time.
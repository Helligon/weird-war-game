---
id: SUP
title: Supply
status: decided
phase: unassigned
tags: [campaign, economy, battle-modifier]
related: ["[[resources]]", "[[upkeep-and-desertion]]", "[[deployment]]", "[[map-and-locations]]"]
updated: 2026-11-21
---

# Supply

## Summary
Each Brigade carries SP up to its **supply capacity**. SP is spent to move. A Brigade's remaining SP, relative to its capacity, sets its **supply state**, and the state changes how it fights. Resupply only happens on or next to a Supply ST, FOB or HQ, or by Card.

## Rules
### Capacity and states
- **SUP-001** (`decided`): Each Brigade MUST have a supply capacity (its supply train) (P-SUP-00).
- **SUP-002** (`decided`): Supply state MUST be measured relative to capacity, so bigger Brigades need more SP to stay effective.
- **SUP-003** (`decided`): Supply states MUST be:

| State | Condition | Effect in battle |
|---|---|---|
| Well Supplied | SP > P-SUP-01 of capacity | Bonus to morale and reload speed (P-SUP-05) |
| Supplied | between thresholds | None |
| Low | SP < P-SUP-02 of capacity | Small penalty, e.g. slower reloads (P-SUP-06) |
| Overextended | SP < 0 on entering battle | Morale penalty (P-SUP-07) **and** loses choice of deployment (see [[deployment]]) |

- **SUP-004** (`decided`): The UI MUST clearly show each Brigade's supply state.

### Movement
- **SUP-005** (`decided`): A Brigade MUST spend SP to move. It can move as far as its SP allows.
- **SUP-015** (`decided`): A Brigade MUST only spend SP from its own supply, never directly from the reserve (see UPK-007).
- **SUP-006** (`decided`): A Brigade MUST NOT move into negative SP **unless that move ends in a battle**.
- **SUP-007** (`decided`): A Brigade whose move into battle takes its SP below zero MUST be marked **Overextended** for that battle.
- **SUP-008** (`decided`): **Design target:** a Brigade at full capacity MUST be able to move a standard distance, fight, and move the same distance back with SP left over, so it is not penalised every time it attacks.
- **SUP-009** (`proposed`): Placeholder budget to meet SUP-008: standard march ≈ 30% of capacity (P-SUP-03), battle ≈ 20% (P-SUP-04). Full → march (~70%, Supplied) → battle (~50%) → march back (~20%, Low, not Overextended).
- **SUP-010** (`proposed`): A battle SHOULD consume SP (ammunition) (P-SUP-04).
- **SUP-011** (`proposed`): Terrain SHOULD change movement cost (mud and no-man's-land more expensive, rail cheaper).

### Resupply
- **SUP-012** (`decided`): A Brigade MUST only receive SP from the reserve when near a Supply ST, FOB or HQ (P-SUP-08), **or** through the Resupply Card, which works anywhere.
- **SUP-013** (`decided`): Supply STs MUST be the resupply points on the map (there is no separate "supply depot"). They start neutral and MUST be captured before use.
- **SUP-016** (`decided`): A **5-Section** Brigade at full capacity MUST be able to move 10 hexes and fight one battle (P-SUP-00). This will be tuned once map scale and SP values are known.

### Brigade size and Section supply profiles
- **SUP-019** (`decided`): Larger Brigades MUST have a higher maximum capacity **and** a higher SP cost per hex. Smaller Brigades MUST have both lower.
- **SUP-020** (`decided`): Section types MAY carry supply modifiers. Examples: Sections with fewer Units (e.g. Snipers) cost less supply; undead don't need feeding in the traditional sense, so they use less.
- **SUP-021** (`decided`): Model: each Section type has a supply profile (capacity contribution, consumption per hex) (P-SUP-09). A Brigade's capacity is the sum of its Sections' contributions, and its cost per hex is the sum of their consumption. This produces SUP-019 and SUP-020 automatically.

### Undead
- **SUP-024** (`decided`): Brigades MUST NOT have a general hexes-per-turn cap; SP is the limit. Only Sections with the **Slow** tag (see [[unit-tags]]) impose a movement cap (P-SUP-10).
- **SUP-025** (`decided`): A Brigade MUST move as fast and as far as its slowest Section.
- **SUP-022** (`decided`): Undead Sections (Thralls/Ghouls, Vampires) MUST use supply more slowly. Thralls/Ghouls MUST have the Slow tag (P-SUP-10); Vampires MUST NOT. Individual unit designs are to come later. Example flavour: an all-undead Brigade has high capacity and doesn't need feeding, so it lasts a long time, but it moves slowly.
- **SUP-023** (`decided`): Undead Sections MUST suffer losses ("rot away") when their Brigade has gone more than P-SUP-12 consecutive turns without a battle (P-SUP-11). Fighting a real-time battle counts as feeding and resets the count; bombardment does not (BOM-020).
- **SUP-017** (`decided`): "Near" a Supply ST, FOB or HQ MUST mean the same hex or an adjacent hex (P-SUP-08).
- **SUP-018** (`decided`): Resupplying MUST cost nothing extra (no action, move or fee beyond the SP transferred). It is limited only by being near a Supply ST, FOB or HQ.
- **SUP-014** (`decided`, reinstated by DEC-065): The Field depot Card MUST place a full Supply ST where a Brigade stands (it earns SP and is a resupply point). It is permanent and capturable. The Card is **single use** and MUST be removed from the deck after playing.
- **SUP-026** (`decided`): A Supply ST from the Field depot Card MUST behave like any other Supply ST: it generates SP and resupplies Brigades.

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-SUP-00 | Supply capacity of a **5-Section** Brigade | Enough for 10 hexes of movement + 1 battle (to be tuned once map scale is known) | decided |
| P-SUP-09 | Supply profile per Section type (capacity contribution, consumption per hex) | – | open |
| P-SUP-10 | Slow movement cap (hexes per turn) | – | open |
| P-SUP-11 | Undead rot losses per turn | – | open |
| P-SUP-12 | Undead grace period (turns without battle before rotting) | 2 | decided |
| P-SUP-01 | Well Supplied threshold | 75% | decided |
| P-SUP-02 | Low threshold | 25% | decided |
| P-SUP-03 | Standard march cost | ~30% (re-derive from hex costs) | proposed |
| P-SUP-04 | Battle SP cost | ~20% | proposed |
| P-SUP-05 | Well Supplied bonus (morale, reload) | – | open |
| P-SUP-06 | Low penalty | – | open |
| P-SUP-07 | Overextended morale penalty | – | open |
| P-SUP-08 | Resupply range around Supply ST/FOB/HQ | Same or adjacent hex | decided |

## Interactions
- [[deployment]]: Overextended deployment patterns, and reinforcement timing by supply state.
- [[battle-initiation]]: proposed rule that Overextended Brigades cannot retreat.
- [[upkeep-and-desertion]]: ongoing SP drain.
- [[cards]]: Resupply and Gain SP Cards.

## Edge cases
- A Brigade at exactly 0 SP is Low, not Overextended (Overextended requires < 0).
- A Brigade cannot go below 0 on a move that does not end in a battle (SUP-006).

## Open questions
- Q-SUP-01 (answered → SUP-016): Capacity = 10 hexes of movement + 1 battle, to be tuned later.
- Q-SUP-05 (answered → SUP-019, SUP-020): Yes. Bigger Brigades have more capacity and higher cost per hex, and Section types have modifiers.
- Q-SUP-06 (answered → SUP-024): No general cap; only Slow-tagged Sections.
- Q-SUP-07 (answered → SUP-022): Thralls/Ghouls and Vampires.
- Q-SUP-08 (answered → SUP-023): They rot unless they fought a battle that turn.
- Q-SUP-09 (answered → SUP-023): 2-turn grace period, tunable.
- Q-SUP-02 (answered → SUP-017): The same hex or an adjacent hex.
- Q-SUP-03 (answered → SUP-012): Yes.
- Q-SUP-04 (answered → SUP-018): Free; limited only by proximity.

## Out of scope
- Supply drain per minute of battle as an anti-stalemate tool: **deferred** (see [[future-ideas]]).

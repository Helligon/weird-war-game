---
id: DEP
title: Deployment and reinforcements
status: decided
phase: unassigned
tags: [battle]
related: ["[[supply]]", "[[battle-maps]]"]
updated: 2026-11-21
---

# Deployment and reinforcements

## Summary
Normally a player chooses where its forces are placed. Overextended Brigades are given a deployment pattern instead. A second Brigade can join partway through as reinforcements, with timing set by its supply state.

## Rules
### Normal deployment
- **DEP-001** (`decided`): A Brigade that is not Overextended MUST let its player choose where its Sections are placed, within its deployment zone.
- **DEP-009** (`decided`): Each side's deployment zone MUST be a strip along its own map edge, about 15–20% of the map's depth (P-DEP-06).

### Overextended deployment patterns
- **DEP-002** (`decided`): An Overextended Brigade MUST NOT choose where it deploys. Instead, it MUST be placed in one of these predetermined patterns:

| Pattern | Description |
|---|---|
| Caught on the march | Strung out in a column along a road. Front and rear exposed. |
| Scattered | Placed randomly, isolated, and must regroup under fire. |
| Bunched | Packed into one tight cluster, vulnerable to artillery and gas. |
| Split | Halved on two sides of an obstacle (town, river, road). |

- **DEP-003** (`decided`): The pattern MUST be chosen at random, weighted by the battle-map terrain (hybrid). Suggested weights: road → Caught on the march; town or river → Split; open ground → Scattered; woods or trench → Bunched (P-DEP-01).

### Reinforcements
- **DEP-004** (`decided`): A friendly Brigade adjacent to the battle MAY join partway through as reinforcements.
- **DEP-005** (`decided`): At most 2 Brigades per side MAY take part in one battle (P-DEP-02).
- **DEP-006** (`decided`, revised 2026-11-23): **All** reinforcements MUST arrive after a timer and appear on the map edge. Supply state sets the timer:

| Supply state | Timer |
|---|---|
| Well Supplied / Supplied | Short (P-DEP-05) |
| Low | Longer (P-DEP-03) |
| Overextended | Longest (P-DEP-04) |

- ~~DEP-008~~: replaced by DEP-006 (revised). Reinforcements never choose their deployment (DEC-074).
- **DEP-007** (`decided`): Reinforcements MUST enter from the map edge matching where they are on the campaign map relative to the battle hex.

## Parameters
| ID | Name | Status |
|---|---|---|
| P-DEP-01 | Terrain → pattern weights | open |
| P-DEP-02 | Max Brigades per side (= 2) | decided |
| P-DEP-03 | Low-supply reinforcement timer | open |
| P-DEP-04 | Overextended reinforcement timer (> P-DEP-03) | open |
| P-DEP-05 | Well Supplied / Supplied reinforcement timer (short) | open |
| P-DEP-06 | Deployment-zone depth | 15–20% of map depth (decided) |

## Open questions
- Q-DEP-01 (answered → DEP-006, DEP-008): Supplied reinforcements choose where they deploy, the same as Well Supplied.
- Q-DEP-02 (answered → DEP-006): A short timer; they appear on the map edge.
- Q-DEP-03 (answered → DEP-009, BMP-007): A strip of 15–20% along your own edge.

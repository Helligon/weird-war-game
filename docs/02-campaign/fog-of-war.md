---
id: FOW
title: Fog of war
status: decided
phase: unassigned
tags: [campaign, map]
related: ["[[map-and-locations]]", "[[cards]]", "[[standard-roster]]"]
updated: 2026-11-22
---

# Fog of war

## Summary
The campaign map has fog of war. Terrain is always visible; STs and enemy Brigades are hidden unless within sight. Each Section type has a sight range of 1–3 hexes.

## Rules
- **FOW-001** (`decided`): The campaign map MUST have fog of war.
- **FOW-002** (`decided`): Fog of war MUST NOT hide terrain.
- **FOW-003** (`decided`): Fog of war MUST hide STs and enemy Brigades outside the player's sight.
- **FOW-004** (`decided`): Each Section type MUST have a sight value of 1–3 hexes (P-FOW-01).
- **FOW-005** (`decided`): The Recon Card MUST reveal a chosen hex and all its neighbours (see [[cards]]).
- **FOW-006** (`decided`): A Brigade's sight MUST be the best sight value among its Sections.
- **FOW-007** (`decided`): Owned locations MUST give sight of the hexes around them: HQ 3, FOB 2, Major ST 2, Command ST 1, Supply ST 1 (P-FOW-03).
- **FOW-008** (`decided`): The Recon reveal MUST last for the rest of the current turn and through the enemy's next turn, then end.
- **FOW-009** (`decided`): Once spotted, an ST, FOB or HQ MUST stay shown on the map with its last-known owner.
- **FOW-010** (`decided`): FOBs and enemy HQs MUST be hidden by fog of war, like STs.
- **FOW-011** (`decided`): Real-time battles MUST NOT have fog of war.

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-FOW-01 | Sight per Section type | 1–3 hexes | decided (range), open (per type) |
| P-FOW-02 | Recon reveal duration | Current turn + the enemy's next turn | decided |
| P-FOW-03 | Location sight radius | HQ 3, FOB 2, Major ST 2, Command ST 1, Supply ST 1 | decided |

## Open questions
- Q-FOW-01 (answered → FOW-006): The best of its Sections.
- Q-FOW-02 (answered → FOW-007): HQ 3, FOB 2, ST 2.
- Q-FOW-09 (answered → FOW-007): Sight by location type; Supply and Command STs 1, Major ST 2.
- Q-FOW-07 (answered → FOW-007): 2 hexes.
- Q-FOW-03 (answered → FOW-008): This turn and the enemy's next turn.
- Q-FOW-04 (answered → FOW-009): It stays, with its last-known owner.
- Q-FOW-05 (answered → FOW-011): No.
- Q-FOW-06 (answered → FOW-010): Yes.
- Q-FOW-08 (answered → FOW-009): Yes.

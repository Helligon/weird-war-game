---
id: RES
title: Resources: SP and CP
status: decided
phase: unassigned
tags: [campaign, economy]
related: ["[[supply]]", "[[cards]]", "[[map-and-locations]]"]
updated: 2026-11-21
---

# Resources: SP and CP

## Summary
Two currencies. **Supply Points (SP)** are logistics: movement, upkeep, forming Brigades. **Command Points (CP)** are command: spent on [[cards]].

## Rules
### Supply Points
- **RES-001** (`decided`): A Faction MUST hold SP in a central **reserve**.
- **RES-002** (`decided`): SP income each turn MUST equal a standard minimum (P-RES-01) plus bonuses for held STs.
- **RES-003** (`decided`): SP in the reserve MAY be distributed to Brigades (subject to [[supply#Resupply]]) or used to form a new Brigade (see [[brigades]]).
- **RES-004** (`decided`): Brigades MUST spend SP to move (see [[supply]]).

### Command Points
- **RES-005** (`decided`): CP income each turn MUST equal a standard minimum (P-RES-02) plus bonuses for control of certain STs.
- **RES-006** (`decided`): CP MUST be spendable on Cards.
- **RES-007** (`decided`): A Card MAY convert CP into SP (a "gain SP" Card).
- **RES-008** (`decided`): Conversion MUST be one-way only (CP → SP). SP MUST NOT be converted to CP.
- **RES-009** (`proposed`): CP SHOULD have a cap or decay to prevent hoarding (P-RES-03).

### Strategic Target types
- **RES-010** (`decided`): STs MUST be typed: Supply STs give SP, Command STs give CP, Major STs give both (see MAP-022).

### Balance
- **RES-011** (`decided`): The guaranteed minimum income MUST exist so that a losing player can recover. It MUST come from the HQ (MAP-024).
- **RES-012** (`proposed`): ST bonuses SHOULD diminish as a Faction holds more STs, to limit snowballing.

## Parameters
| ID | Name | Status |
|---|---|---|
| P-RES-01 | Base SP income per turn | open |
| P-RES-02 | Base CP income per turn | open |
| P-RES-03 | CP cap / decay | proposed |
| P-RES-04 | SP / CP bonus per ST (by type) | open |

## Interactions
- [[upkeep-and-desertion]]: upkeep is paid in SP.
- [[unit-tags]]: Strange units spend SP too; CP is for Cards only (TAG-011).

## Open questions
- Q-RES-01 (answered → RES-008): No.

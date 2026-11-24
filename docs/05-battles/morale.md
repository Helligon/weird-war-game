---
id: MOR
title: Morale
status: decided
phase: unassigned
tags: [battle, campaign, forces]
related: ["[[supply]]", "[[victory-and-routing]]", "[[player-character]]", "[[officers]]", "[[brigades]]"]
updated: 2026-11-21
---

# Morale

## Summary
Morale is a percentage. Each Brigade has a base morale that events on the map change. Each Section starts every battle at its Brigade's base and then has its own morale during the battle. Below a threshold, a Section has a growing chance to rout.

## Rules
### Values
- **MOR-001** (`decided`): Morale MUST be a percentage (0–100%).
- **MOR-002** (`decided`): Each Brigade MUST have a base morale value (P-MOR-01).
- **MOR-003** (`decided`): Each Section MUST have its own morale value during battle.
- **MOR-004** (`decided`): At the start of each battle, every Section's morale MUST be set to its Brigade's base morale.

### What changes morale
- **MOR-005** (`decided`): Events MUST change morale both in battle (Section morale) and on the campaign map (Brigade base morale) (P-MOR-05).
- **MOR-006** (`decided`): Supply state, officer and hero deaths, and overextension MUST be among the things that change morale. They are not the only ones.
  - Well Supplied → morale bonus (SUP-003).
  - Overextended → morale penalty (SUP-003).
  - All officers and heroes dead → large morale penalty (PCH-005).

### Routing
- **MOR-007** (`decided`): A Section whose morale is below the rout threshold (P-MOR-02) MUST have a chance to rout. At or above the threshold, the chance MUST be 0%.
- **MOR-008** (`decided`): The chance to rout MUST grow as morale falls towards zero. Formula: **chance = 100 − 5 × morale** (in %) when morale is below 20%, otherwise 0% (P-MOR-03).

| Section morale | Chance to rout |
|---|---|
| 45% | 0% |
| 20% | 0% |
| 19% | 5% |
| 10% | 50% |
| 4% | 80% |
| 0% | 100% |

- **MOR-010** (`decided`): The rout chance MUST be rolled only when a morale-changing event affects the Section (e.g. taking casualties, officer death), not on a timer.
- **MOR-011** (`decided`): The rout formula and threshold are expected to be tuned and MUST be data-driven (P-MOR-02, P-MOR-03).
- **MOR-012** (`decided`): A routing Section MUST be able to rally if its morale climbs back above the rout threshold (P-MOR-02). It needs to reach cover to recover morale. Being near a friendly officer or hero SHOULD help it recover.
- **MOR-013** (`decided`): A Section's morale MUST naturally recover over time, up to that Section's current maximum (P-MOR-06).
- **MOR-014** (`decided`): A Section's current maximum MUST be the morale it started the battle with (its Brigade base). Recovery MUST only happen while the Section is not taking fire.
- **MOR-015** (`decided`): After a battle, the Brigade's base morale MUST rise a little if it won and fall if it lost (P-MOR-07).
- **MOR-016** (`decided`): Morale MUST be capped at 100%.
- **MOR-009** (`decided`): A Section that routs MUST behave as described in [[victory-and-routing]] (VIC-002, VIC-003).

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-MOR-01 | Default Brigade base morale | – | open |
| P-MOR-02 | Rout threshold | 20% | decided |
| P-MOR-03 | Rout chance formula | 100 − 5 × morale | decided |
| P-MOR-04 | When the rout chance is rolled | On each morale-changing event | decided |
| P-MOR-05 | Morale change per event (catalogue) | – | open |
| P-MOR-06 | Natural morale recovery rate | – | open |
| P-MOR-07 | Base morale change after a win / loss | – | open |

## Edge cases
- A Section's morale is clamped between 0% and 100% (Q-MOR-04).
- The formula reaches exactly 0% at the threshold (20%), so it joins the "no chance" zone smoothly.

## Open questions
- Q-MOR-01 (answered → MOR-010): Rolled on each morale-changing event.
- Q-MOR-02 (answered → MOR-015): Win up, lose down.
- Q-MOR-03 (answered → MOR-012): Yes, by reaching cover and recovering morale.
- Q-MOR-06 (answered → MOR-014): Yes to both.
- Q-MOR-04 (answered → MOR-016): Capped at 100%.
- Q-MOR-05: The event catalogue: what raises and lowers morale on the map and in battle, and by how much?

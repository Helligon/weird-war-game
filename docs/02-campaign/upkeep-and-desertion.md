---
id: UPK
title: Upkeep and desertion
status: decided
phase: unassigned
tags: [campaign, economy]
related: ["[[supply]]", "[[map-and-locations]]", "[[hierarchy]]"]
updated: 2026-11-26
---

# Upkeep and desertion

## Summary
Brigades inside their Faction's territory pay no upkeep. Outside it, they pay a small SP upkeep from their own supply at the end of each turn. A Brigade with no SP at the end of its turn suffers desertion.

## Rules
- **UPK-001** (`decided`): A Brigade inside its Faction's territory perimeter MUST pay no upkeep (P-UPK-01 = 0).
- **UPK-008** (`decided`): Upkeep and desertion MUST be resolved at the end of the owner's turn.
- **UPK-010** (`decided`): Desertion MUST apply every turn a Brigade ends at 0 SP, and MAY destroy a Section completely (0 health).
- **UPK-011** (`decided`): Outside its territory, a Brigade's upkeep MUST be a whole number of SP per turn set by its title (e.g. Infantry Brigade 1 SP/turn, Artillery Brigade 2 SP/turn), with possible modifiers (P-UPK-02). Inside its territory there MUST be no upkeep (UPK-001).
- **UPK-012** (`decided`): The UI MUST show an upkeep projection next to the SP counter at all times, highlighting any Brigade that will suffer desertion this turn.
- **UPK-009** (`decided`): A Brigade with no SP (0 or below) at the end of its owner's turn MUST suffer desertion.
- **UPK-007** (`decided`): A Brigade MUST pay upkeep from its **own** SP. It MUST NEVER be paid directly from the Faction's reserve. (A Brigade only ever spends SP from its own supply; the reserve is refilled into Brigades through resupply, see [[supply#Resupply]].)
- **UPK-002** (`decided`): A Brigade outside its Faction's territory perimeter (past its furthest forward ST) MUST pay a small SP upkeep at the end of each turn (P-UPK-02).
- **UPK-003** (`decided`, revised by DEC-104): Upkeep territory (the "perimeter" in UPK-001, UPK-002 and P-UPK-01 and P-UPK-02) MUST mean the Faction's territory as MAP-027 defines it (see [[map-and-locations]]). It MUST NOT be calculated by joining captured points. To be revisited when upkeep uses territory (MAP-016).
- **UPK-004** (`decided`): If paying upkeep leaves a Brigade with no SP (or it can't pay at all), the Brigade MUST suffer **desertion** (see also UPK-009).
- **UPK-005** (`decided`): Desertion MUST reduce the health of **each Section** in the Brigade by a random amount within a range centred on ~15% (P-UPK-03).
- **UPK-006** (`decided`): Because Section size is derived from health, desertion MUST visibly reduce the number of Units in a Section (see [[hierarchy]]).

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-UPK-01 | Upkeep inside perimeter | 0 | decided |
| P-UPK-02 | Upkeep outside perimeter | whole SP per turn by title (e.g. Infantry 1, Artillery 2) | decided (examples), open (full table) |
| P-UPK-03 | Desertion health loss range | ~15% (bounds TBD) | decided (centre), open (bounds) |

## Interactions
- [[supply]]: upkeep drains SP.
- [[unit-tags]]: Strange units pay upkeep in SP like everyone else (TAG-011).
- [[supply#Undead]]: undead use less SP but rot when not in battle.
- [[standard-roster#Medic]]: medics might reduce desertion (proposed).

## Edge cases
- A Section at very low health might be destroyed by desertion. Q-UPK-02.

## Open questions
- Q-UPK-01 (answered → UPK-007): Source of upkeep: the Brigade's own SP, never the reserve.
- Q-UPK-04 (answered → UPK-001, UPK-002): No upkeep inside your territory; a small cost outside; desertion at 0 SP.
- Q-UPK-02 (answered → UPK-010): It can destroy a Section.
- Q-UPK-03 (answered → UPK-010): Every turn at 0 SP.
- Q-UPK-05 (answered → UPK-011): Only outside your territory.

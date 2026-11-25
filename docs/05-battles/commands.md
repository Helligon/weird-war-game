---
id: CMD
title: Commands and tactical view
status: decided
phase: unassigned
tags: [battle, controls]
related: ["[[default-ai-behaviour]]", "[[player-character]]", "[[hierarchy]]"]
updated: 2026-11-25
---

# Commands and tactical view

## Summary
The player gives orders to Sections from a tactical view. The command set is deliberately small and simple.

## Rules
### Tactical view
- **CMD-001** (`decided`): The player MUST enter a tactical view to give commands.
- **CMD-002** (`decided`): In single-player, the tactical view MUST pause the battle.
- **CMD-003** (`decided`): In multiplayer, the tactical view MUST NOT pause the battle (it stays live).
- **CMD-004** (`decided`): While the player is in the tactical view, their character MUST keep fighting under AI control (see [[player-character]]).

### Commands
- **CMD-005** (`decided`) **Move:** the player selects a Section and gives it 1 or more waypoints, up to P-CMD-01. A Move with no waypoints MUST be rejected (DEC-097). A waypoint next to cover MUST make the Section take that cover on arrival.
- **CMD-006** (`decided`) **Stop:** a queued final order meaning "hold this ground". The Section MUST carry out its waypoints first, then hold its final position. With no waypoints, it MUST stop on the spot.
- **CMD-007** (`decided`) **Clear waypoints:** the player MUST be able to clear a Section's waypoints explicitly. This is the only way to cancel queued movement.
- **CMD-008** (`decided`) **Target:** the player selects an enemy Section for a Section to attack. Moving to waypoints and stopping MUST take priority over targeting.
- **CMD-009** (`decided`) **Target ground:** Target MUST also accept an area of ground, for artillery and mortars.
- **CMD-010** (`decided`) **Protect:** the player selects an allied Section to guard. The protecting Section MUST stay with it (surround it or move in front) and engage the nearest enemy it can reach without leaving its side.
- **CMD-011** (`decided`, revised by DEC-096) **Fall back:** the Section MUST clear its queued waypoints, move towards its own side's map edge in good order, and hold there. This is not a rout.
- **CMD-012** (`decided`, DEC-094): Without a Stop at the end of its orders, a Section reaching its last waypoint MUST return to its default behaviour (see [[default-ai-behaviour]]).

### Selection
- **CMD-018** (`decided`): Officers and heroes the player is not controlling MAY be given orders in the tactical view, with the same commands as Sections.
- **CMD-019** (`decided`): Sections MAY fire while moving, at reduced accuracy (P-CMD-03). Heavy and Artillery Sections MUST NOT fire while moving.
- **CMD-020** (`decided`, DEC-096): A Move given to a Section that already has queued waypoints MUST add its waypoints to the queue, up to P-CMD-01 in total. A Move that would go over P-CMD-01 MUST be rejected.
- **CMD-016** (`decided`): Commands MUST be given to one Section at a time. There is no group selection. A command that names a Section that does not exist, or targets one, MUST be rejected (DEC-097).
- **CMD-017** (`decided`): Design assumption: a side has at most about 10 Sections in a battle (2 Brigades × 5 Sections; P-CMD-02), plus up to 2 officers, 2 heroes and a Garrison Section (MAP-051), so group selection isn't needed. (The +1 Section bonus, BRG-022, can take this to 12.)

### Abilities
- **CMD-013** (`decided`): Sections MUST NOT have abilities that the player triggers.
- **CMD-014** (`decided`): Sections MAY have passive or automatic abilities (e.g. a Medic auto-heal, a creature's fear aura, Sappers laying mines automatically).

### Formations
- **CMD-015** (`decided`): There MUST be no formations (may change later; see [[future-ideas]]).

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-CMD-01 | Max queued waypoints per Section | 2 | decided |
| P-CMD-02 | Max Sections per side in battle | 10 | decided |
| P-CMD-03 | Accuracy penalty while moving | – | open |

## Open questions
- Q-CMD-01 (answered → CMD-016): No. Single-Section orders only.
- Q-CMD-02 (answered → CMD-018): Yes.
- Q-CMD-03 (answered → CMD-019): Yes, at reduced accuracy; Heavy and Artillery can't.

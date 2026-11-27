---
id: PCH
title: Player character in battle
status: decided
phase: unassigned
tags: [battle, controls, third-person]
related: ["[[officers]]", "[[heroes]]", "[[commands]]"]
updated: 2026-11-27
---

# Player character in battle

## Summary
In battle, the player fights in third person as an officer or hero, and switches to the tactical view to give orders.

## Rules
- **PCH-001** (`decided`): The player MUST control an officer or hero in third person.
- **PCH-002** (`decided`): At the start of a battle, the player MUST choose which of their officers or heroes in the battle to play as.
- **PCH-003** (`decided`): When the controlled character dies, control MUST move to another living officer or hero on the player's side.
- **PCH-004** (`decided`): The player MUST NOT be able to directly control a single Unit inside a Section (may change; see [[future-ideas]]).
- **PCH-005** (`decided`): If all of a side's officers and heroes die, that side's remaining troops MUST suffer a large morale penalty (P-PCH-01).
- **PCH-006** (`decided`, DEC-094): If no officer or hero is left, the player SHOULD stay in the tactical view only.
- **PCH-009** (`decided`, DEC-096): The player's third-person actions (move, aim, fire) are player actions, so they MUST be commands (DLV-013) and MUST be recorded in the battle's command log.
- **PCH-010** (`decided`, DEC-116): Production third-person controls MUST start from the Spike 2 scheme: WASD movement, mouse-look aiming and a click to fire.
- **PCH-011** (`decided`, DEC-116): Switching between the third-person view and the tactical view MUST be a sweeping camera move, up from behind the Officer to the tactical view and back, not a cut.
- **PCH-012** (`decided`, DEC-116): Third-person camera turning MUST be eased, not linear. It should feel like a person turning, not a tank turret.
- **PCH-013** (`deferred`, DEC-116): Height and vertical camera movement come with 3D, later.
- **PCH-014** (`decided`, DEC-116): An Officer under AI control MUST be weaker than the same Officer played by the player (see DAI-007).
- **PCH-008** (`decided`): Officers MUST NOT have active abilities.
- **PCH-007** (`decided`): Heroes MUST fight with special weapons and gear. Active abilities are deferred (HER-010, HER-011).

## Parameters
| ID | Name | Status |
|---|---|---|
| P-PCH-01 | Leaderless morale penalty | open |

## Open questions
- Q-PCH-01 (answered → PCH-010 to PCH-014, CMD-021 to CMD-025): Third-person controls, camera and weapon handling. Start production from the Spike 2 scheme; eased camera turning, a sweeping view switch, and weapon rules (range, rate of fire, spread, moving penalty, sprint) are decided (DEC-116).
- Q-PCH-02 (answered → PCH-008): No.

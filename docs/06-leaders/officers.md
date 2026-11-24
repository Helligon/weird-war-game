---
id: OFF
title: Officers
status: decided
phase: unassigned
tags: [leaders, progression]
related: ["[[aro-progression]]", "[[officer-veterancy]]", "[[heroes]]", "[[player-character]]", "[[multiplayer]]"]
updated: 2026-11-21
---

# Officers

## Summary
Every Brigade is led by an Officer, a separate Unit the player can play as. Officers come from a collection unlocked through achievements, carry up to 3 flavourful bonuses, and can be wounded or (in Iron Officers mode) killed permanently.

## Rules
### Collection
- **OFF-001** (`decided`): Each Brigade MUST be led by one Officer.
- **OFF-002** (`decided`): There MUST be a collection of officers with different bonuses and abilities.
- **OFF-003** (`decided`): Officers MUST be unlockable through in-game achievements.
- **OFF-004** (`decided`): In multiplayer, all officers MUST be available. Players choose a roster to bring into battle (P-OFF-03).

### Bonuses
- **OFF-005** (`decided`): An officer MUST hold at most 3 different bonuses (P-OFF-01).
- **OFF-006** (`decided`): When all bonus slots are full, a new bonus MUST either upgrade an existing bonus or replace one.
- **OFF-007** (`decided`): Officer bonuses MUST be flavourful and named (examples: "Shock Troops: −5% enemy defences", "Brutal Tactics: −5% enemy morale").
- **OFF-008** (`proposed`): Some bonuses SHOULD affect the campaign map (e.g. "Quartermaster: −10% upkeep", "Forced March: +1 move before going Overextended").

### Death, wounds and replacement
- **OFF-009** (`decided`): The campaign MUST have an **Iron Officers** setting: when on, officers and heroes who die in battle are permanently dead and cannot be used for the rest of the game. An ARO who dies loses their progress (ARO-009).
- **OFF-010** (`decided`): In standard mode (Iron Officers off), **every** officer who falls in battle, AROs included, MUST be **wounded** and out of action for a number of turns (P-OFF-02), then recover.
- **OFF-011** (`decided`): If no named officer is available to lead a Brigade, the player MUST be able to pick a **Generic Officer** (basic stats, no bonuses) **or** promote a Unit to **ARO** (see [[aro-progression]]).

- **OFF-017** (`decided`): A wounded officer whose Brigade was wiped out MUST return to the available roster after a 3-turn cooldown (P-OFF-04).
- **OFF-018** (`decided`): When an officer is wounded but their Brigade survives, the player MUST pick a different officer from their roster or collection to lead it as **acting officer**.
- **OFF-019** (`decided`): When the wounded officer recovers, the player MUST choose to either reinstate them (the acting officer returns to the roster) or keep the acting officer (the recovered officer returns to the roster).
- **OFF-020** (`decided`): A wounded officer MUST be out of action for 3 turns (P-OFF-02).
- **OFF-021** (`decided`): If no officer is available to act, the player MUST use a Generic Officer or create an ARO (OFF-011).
- **OFF-014** (`decided`): If an officer's whole Brigade is wiped out, the officer MUST be wounded (standard mode) or killed (Iron Officers mode).
- **OFF-015** (`decided`): Officers MUST NOT move between Brigades. The officer *is* the Brigade's leader; Sections move between Brigades instead (BRG-016).
- **OFF-016** (`decided`): Achievement unlocks (officers and heroes) MUST carry over between campaigns.

### In battle
- **OFF-012** (`decided`): An officer MUST be a separate Unit, not part of a Section.
- **OFF-013** (`decided`): The player MAY play as the officer (see [[player-character]]).

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-OFF-01 | Bonus slots | 3 | decided |
| P-OFF-02 | Wounded duration (turns) | 3 | decided |
| P-OFF-03 | Multiplayer officer roster size | – | open |
| P-OFF-04 | Roster cooldown after Brigade wiped out | 3 turns | decided |

## Open questions
- Q-OFF-05 (answered → OFF-017): Returns to the roster after 3 turns.
- Q-OFF-06 (answered → OFF-018, OFF-019): An acting officer from the roster; on recovery the player picks which officer keeps command.
- Q-OFF-07 (answered → OFF-020): 3 turns.
- Q-OFF-08 (answered → OFF-021): Generic Officer or ARO.
- Q-OFF-01: Achievement list and which officers each unlocks.
- Q-OFF-02 (answered → OFF-014): Wounded or killed, by mode.
- Q-OFF-03 (answered → OFF-016): Yes.
- Q-OFF-04 (answered → OFF-015): No; Sections move instead.

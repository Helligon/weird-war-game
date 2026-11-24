---
id: BOM
title: Bombardment
status: decided
phase: unassigned
tags: [battle, campaign]
related: ["[[battle-initiation]]", "[[map-and-locations]]", "[[vehicles]]", "[[brigades]]", "[[fog-of-war]]", "[[morale]]"]
updated: 2026-11-22
---

# Bombardment

## Summary
A second type of combat besides the standard battle. A bombardment is **one-sided**: the attacker fires at a target from range and the target cannot fire back. It is calculated instantly on the campaign map. Any Brigade with Artillery can bombard, and so can HQs. FOBs cannot bombard. HQs and FOBs have health (their garrison): bombarding an FOB to 0 levels it, and bombarding an HQ to 0 wins the game.

## Rules
### General
- **BOM-001** (`decided`): There MUST be two types of combat: the **standard battle** ([[battle-initiation]]) and the **bombardment**.
- **BOM-002** (`decided`): In a bombardment, the target MUST NOT be able to fire back.
- **BOM-006** (`decided`): A bombardment MUST be calculated instantly on the campaign map, with a short shelling animation over the target hex. No real-time battle is played.
- **BOM-007** (`decided`): The target MUST be visible to the attacker, either in line of sight (within the player's sight, [[fog-of-war]]) or revealed by Recon.

### Who can bombard
- **BOM-003** (`decided`): Artillery and HQs MUST be able to bombard a target several hexes away (P-BOM-01). FOBs MUST NOT bombard; they can only attack adjacent enemies (MAP-036).
- **BOM-008** (`decided`): Any Brigade with at least one Artillery Section MAY bombard. Bombardment strength MUST increase with each extra Artillery Section.
- **BOM-009** (`decided`): Range MUST depend on artillery type (e.g. mortars 2 hexes, field guns 3) (P-BOM-01).

### Cost and timing
- **BOM-010** (`decided`): Bombarding MUST cost the Brigade SP (shells), less than a battle (P-BOM-03).
- **BOM-011** (`decided`): Bombarding MUST end that Brigade's movement for the turn. The Brigade MAY move into position before firing.
- **BOM-012** (`decided`): An HQ MAY bombard once, then MUST wait a 1-turn cooldown (reloading) before firing again, so at most every other turn (P-BOM-04). Its shells MUST come from the reserve.

### Targets and effects
- **BOM-013** (`decided`): Valid targets MUST be enemy Brigades, HQs and FOBs. Supply, Command and Major STs MUST NOT be targets (there is no garrison to hit).
- **BOM-014** (`decided`): A Brigade on an ST MUST take less bombardment damage; on a Major ST, less still (MAP-029) (P-BOM-05).
- **BOM-015** (`decided`): Bombardment MUST reduce the target's health **and** lower its Brigade base morale, so the effect carries into its next battle ([[morale]]) (P-BOM-02).
- **BOM-017** (`decided`): Bombardment MAY wipe out a Brigade.
- **BOM-016** (`decided`): Bombardment MUST reduce an enemy HQ's or FOB's health (MAP-032). An FOB at 0 health becomes a Levelled FOB (MAP-010). An HQ at 0 health loses its owner the game (MAP-004). (Replaces the earlier rule "bombard before capturing".)
- **BOM-019** (`decided`): Bombarding an HQ or FOB with an enemy Brigade on it MUST hit both. The Brigade gets its usual defence bonus, so it takes reduced damage.
- **BOM-021** (`decided`): Brigades MAY also assault an adjacent HQ or FOB in a one-sided auto-resolved attack, but do little damage without heavy weaponry (MAP-035).
- **BOM-022** (`decided`): The **Continuous Shelling** Card MUST reload an Artillery Brigade, an HQ or a Bomber Squadron, letting it fire again (ignores the cooldown; see [[cards]]).
- **BOM-023** (`decided`): A Brigade MUST bombard at most once per turn. Continuous Shelling gives it a second shot that turn.
- **BOM-024** (`decided`): Every bombardment, including the second shot, MUST cost SP (BOM-010).
- **BOM-020** (`decided`): Bombardment MUST NOT count as feeding for undead (SUP-023); only real battles do.
- ~~BOM-018~~: withdrawn. STs are not bombardment targets (BOM-013).

### Air (deferred)
- **BOM-004** (`deferred`): Bomber air units MAY bomb an adjacent hex in a one-sided attack.
- **BOM-005** (`deferred`): Fighter air Brigades MAY attack other air Brigades.

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-BOM-01 | Bombardment range by source | mortar 2, field gun 3 (examples) | decided (examples), open (full list) |
| P-BOM-02 | Damage and morale effect | – | open |
| P-BOM-03 | SP cost per bombardment | less than a battle | decided (principle), open (value) |
| P-BOM-04 | HQ bombardment cooldown | 1 turn | decided |
| P-BOM-05 | Damage reduction on ST / Major ST | – | open |
| P-BOM-06 | HQ bombardment range | – | open |

## Open questions
- Q-BOM-01 (answered → BOM-006): Calculated instantly, with an animation.
- Q-BOM-02 (answered → BOM-008, BOM-009): Any Brigade with Artillery; range by type.
- Q-BOM-03 (answered → BOM-010, BOM-011): Costs SP; ends movement.
- Q-BOM-04 (answered → BOM-012): Once, then a 1-turn cooldown.
- Q-BOM-05 (answered → BOM-013, BOM-014, BOM-016): Brigades, plus HQs and FOBs before capture.
- Q-BOM-06 (answered → BOM-015): Lowers health and morale.
- Q-BOM-07 (answered → BOM-016): Bombard until 0 health; no direct capture.
- Q-BOM-08 (answered → MAP-032, BOM-016): HQs and FOBs have health (garrison).
- Q-BOM-09 (answered → BOM-013): STs aren't targets.
- Q-BOM-10 (answered → BOM-019): Both; the Brigade gets reduced damage.
- Q-BOM-11 (answered → BOM-020): Only real battles.
- Q-BOM-12 (answered → BOM-003): Moot; FOBs don't bombard.
- Q-BOM-13 (answered → BOM-023, BOM-024): Once per turn; Continuous Shelling gives a second, also paid in SP.
- Q-BOM-14 (answered → BOM-003): No. FOBs only attack adjacent enemies.

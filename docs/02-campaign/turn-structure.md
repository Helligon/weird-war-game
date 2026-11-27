---
id: TRN
title: Turn structure
status: decided
phase: unassigned
tags: [campaign]
related: ["[[resources]]", "[[supply]]", "[[upkeep-and-desertion]]", "[[battle-initiation]]"]
updated: 2026-11-27
---

# Turn structure

## Summary
How a campaign turn flows. Players take alternating turns; each turn runs through the phases below.

## Rules
- **TRN-001** (`decided`): Each turn, a Faction MUST receive SP and CP income (see [[resources]]).
- **TRN-002** (`decided`): During a turn, a player MAY distribute SP from the reserve to Brigades, form Brigades, play Cards and move Brigades.
- **TRN-003** (`decided`, revised by DEC-104): **Contact** MUST happen when a moving Brigade enters a hex next to an enemy Brigade. Contact MUST start a battle (see [[battle-initiation]]).
- **TRN-004** (`decided`): Brigades MUST pay upkeep each turn (see [[upkeep-and-desertion]]).
- **TRN-006** (`decided`): The campaign map MUST be turn-based. Battles MUST be real-time.
- **TRN-007** (`decided`): Campaign turns MUST alternate: each player takes a full turn in sequence.
- **TRN-008** (`decided`, revised by DEC-104): A battle MUST start as soon as contact happens (TRN-003) during its owner's turn. The moving Brigade MUST stop in the hex where contact happened, and the rest of its path MUST NOT be walked. The battle starts with both Brigades on their own hexes. If the hex where contact would stop the move is occupied by a friendly Brigade, the move MUST be rejected and the player MUST choose another path (TRN-020).
- **TRN-009** (`decided`, revised by DEC-104): A Brigade MUST capture an ST by staying on it for a turn: it ends its owner's turn on the ST and is still there at the start of its Faction's next turn. The capture MUST be completed only by the **same** Brigade that started it. An HQ MUST NOT be captured by standing on it. An HQ falls when its health reaches 0, however that happens (MAP-004, BOM-016, MAP-035).
### Battles over an ST being captured or held
- **TRN-010** (`decided`): If a Brigade on an ST is attacked, the battle MUST be fought as usual.
- **TRN-011** (`decided`): If the attacker wins, the defender MUST retreat and forfeit the hex. The attacker MUST take the hex and begin capturing the ST (TRN-009).
- **TRN-012** (`decided`): If the defender wins, it MUST stay on the hex, and any capture in progress MUST carry on uninterrupted (it completes at the start of the defender's next turn).
### After a battle
- **TRN-013** (`decided`): A defeated defender MUST retreat towards its Faction's nearest HQ or FOB.
- **TRN-017** (`decided`): A retreat MUST move the Brigade back exactly 1 hex and MUST NOT cost SP.
- **TRN-018** (`decided`): A retreat MUST only go to a passable adjacent hex that is **towards** the nearest friendly HQ or FOB. A Brigade can never retreat away from its bases towards the map edge.
- **TRN-019** (`proposed`, deferred to the battle-results Feature by DEC-104): "Towards" SHOULD mean an adjacent hex whose hex distance to the nearest friendly HQ/FOB is less than the current hex's. If several qualify, the Brigade's owner chooses.
- **TRN-020** (`decided`, revised by DEC-104): Two Brigades MUST NOT share a hex, friendly or not. A move MUST NOT end on an occupied hex. A moving Brigade MAY pass through a hex occupied by a friendly Brigade, as long as the move does not end there. If that hex is next to an enemy Brigade, contact (TRN-008) would stop the move on an occupied hex, so the move MUST be rejected.
- **TRN-021** (`decided`): Exception: a retreating Brigade MAY pass through a hex occupied by a friendly Brigade. It moves 1 extra hex beyond it (still towards the nearest HQ/FOB, still free).
- **TRN-022** (`proposed`, deferred to the battle-results Feature by DEC-104): If the extra hex is also friendly-occupied, the Brigade SHOULD keep moving hex by hex until it reaches a free valid hex. If it reaches an impassable hex first, it is destroyed (TRN-014).
- **TRN-023** (`decided`, revised by DEC-104): A hex MUST be impassable if it is impassable terrain, the map edge, or occupied by an enemy Brigade. An impassable hex MUST block the whole path through it, not only a move that ends on it.
- **TRN-024** (`decided`): Impassable terrain for ground Brigades MUST include mountains, sea and rivers.
- **TRN-025** (`proposed` by Alex, tentative): Marsh MAY also be impassable.
- **TRN-026** (`proposed`): A river hex with a bridge SHOULD be passable (implied by the future idea of blowing bridges).
- **TRN-027** (`proposed`): A Brigade SHOULD only be able to enter a hex that **every** one of its Sections can enter (in line with SUP-025, slowest Section).
- **TRN-028** (`decided`): Enemy Brigades MUST block air movement, just as they block ground movement. An air Brigade MAY only enter an enemy-occupied hex to attack it.
- **TRN-014** (`decided`): A defeated Brigade with no valid retreat hex (TRN-018) MUST be destroyed.
- **TRN-015** (`decided`, revised by DEC-110): A defeated attacker MUST stay on the hex it attacked from: the hex where contact stopped it (TRN-008), or the hex it stood on for an attack from an adjacent hex (TRN-036). It does not move.
- **TRN-016** (`decided`): An ST or FOB MUST give its owner no income while an enemy Brigade is on its hex.
- **TRN-005** (`decided`): Each player's turn MUST run in this order:
  1. **Start of turn:** resolve captures (TRN-009); count down timers (wounded officers, roster cooldowns).
  2. **Income:** SP to the reserve, CP to the player. STs and FOBs with an enemy Brigade on them pay nothing (TRN-016). Income comes from STs and FOBs; the HQ is exempt and always pays (MAP-024, RES-011).
  3. **Draw:** draw 1 Card; if holding 9+, discard to 8 (CRD-005, CRD-014).
  4. **Actions**, in any order: resupply, form Brigades, move Sections between Brigades, play Cards, move and attack (battles happen immediately), bombard.
  5. **End of turn:** upkeep and desertion (see [[upkeep-and-desertion]]); undead rot check (SUP-023).

```mermaid
flowchart LR
    A[Start: captures, timers] --> B[Income]
    B --> C[Draw Card]
    C --> D[Actions: any order]
    D --> E[End: upkeep/desertion, undead rot]
```

- **TRN-029** (`decided`): SP MUST be deducted immediately as a Brigade moves.
- **TRN-030** (`decided`): A Brigade's movement MAY be split across the Actions phase (e.g. move, resupply, move again). SP is the only limit.
- **TRN-031** (`decided`): A Brigade that has fought a battle MUST NOT move again that turn.
- **TRN-032** (`decided`): A Brigade formed this turn MAY move straight away.
- **TRN-033** (`decided`): The first player MUST be chosen at random. The second player MUST start with 1 extra Card (P-TRN-01).
- **TRN-034** (`decided`, DEC-104): The first player MUST receive income on turn 1, like every later turn (TRN-005).
- **TRN-036** (`decided`, DEC-110): A move order that targets an adjacent enemy hex MUST start a battle at once. The attacker walks 0 hexes and stays where it is. The attack MUST cost P-TRN-02 SP, so a Brigade with less SP than that MUST NOT be able to attack this way.
- **TRN-035** (`decided`, DEC-104): The turn number MUST count Faction turns, not rounds: each Faction's turn adds 1, so turn 1 is the first player's and turn 2 is the second player's.

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-TRN-01 | Extra starting Cards for the second player | 1 | decided |
| P-TRN-02 | SP cost of attacking an adjacent enemy hex (TRN-036) | 1 (placeholder) | proposed |

## Open questions
- Q-TRN-05 (answered → TRN-017, TRN-018): 1 hex, free, only towards the nearest HQ/FOB.
- Q-TRN-06 (answered → TRN-020, TRN-021): No sharing; a retreating Brigade passes through a friendly one, moving 1 extra hex.
- Q-TRN-07 (answered → TRN-023): Terrain, the map edge, enemy Brigades.
- Q-TRN-08 (answered → TRN-024): Mountains, sea, rivers; marsh maybe.
- Q-TRN-09 (answered → TRN-028): No; enemies block air travel unless the air Brigade is attacking them.
- Q-TRN-01 (answered → TRN-005): Phase order decided; upkeep at the end of the turn.
- Q-TRN-04 (answered → TRN-010 to TRN-012): Fought as usual; the winner holds the hex.
- Q-TRN-02 (answered → TRN-007, TRN-008): Alternating turns; battles happen immediately.
- Q-TRN-03 (answered → TRN-009): Stay on it for a turn.

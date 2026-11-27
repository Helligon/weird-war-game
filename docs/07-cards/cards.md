---
id: CRD
title: Cards
status: decided
phase: unassigned
tags: [cards]
related: ["[[resources]]", "[[supply]]", "[[brigades]]", "[[map-and-locations]]", "[[fog-of-war]]", "[[heroes]]"]
updated: 2026-11-27
---

# Cards

## Summary
Cards are one-off effects played on the campaign map by paying CP. They work like a deck and hand: you start with 5, draw 1 per turn, and can hold up to 8. A Card's rarity comes from how many copies are in the deck and its CP cost, not from cooldowns or play limits. There's a shared core of Cards plus faction-only Cards.

## Rules
### Deck and hand
- **CRD-003** (`decided`): Cards MUST use a deck-and-hand model.
- **CRD-004** (`decided`): Each player MUST start the game with 5 Cards in hand (P-CRD-01).
- **CRD-005** (`decided`): Each player MUST draw 1 Card per turn (P-CRD-02).
- **CRD-006** (`decided`): The hand limit MUST be 8 (P-CRD-03). Whenever a hand reaches 9 or more Cards, the player MUST discard down to 8.
- **CRD-007** (`decided`): A "Draw 2" Card MUST exist.
- **CRD-014** (`decided`): A player with a full hand MUST still draw, then discard down to 8.
- **CRD-015** (`decided`): When the deck runs out, the discard pile MUST be shuffled back in to form a new deck.
- **CRD-016** (`decided`): In the campaign, the player's deck MUST contain every Card they have unlocked (this may change).
- **CRD-018** (`decided`): The player who goes second MUST start with 1 extra Card (TRN-033).
- **CRD-017** (`decided`): Card unlocks MUST carry over between campaigns.

### Playing Cards
- **CRD-001** (`decided`): Playing a Card MUST cost CP. A Card MAY also cost SP if its text says so.
- **CRD-008** (`decided`): Cards MUST only be played on the campaign map, never during a battle.
- **CRD-009** (`decided`): There MUST be no limit on Cards played per turn and no cooldowns. Rarity comes from the number of copies in the deck and from CP cost.
- **CRD-010** (`decided`): A Card MAY have play conditions, e.g. "only on a Brigade on a Supply ST/HQ/FOB" or "only on a Brigade that has been in battle this turn".

- **CRD-019** (`decided`, DEC-114): The **Bridgelayer** Card MUST place a bridge (MAP-054) on a single river hex. That hex MUST have a passable land hex on each side.

### Card pools
- **CRD-011** (`decided`): There MUST be a shared core of Cards available to every Faction, plus faction-only Cards (e.g. turning enemies into Thralls for the Central Powers; calling in a seconded Section for the Entente).
- **CRD-012** (`decided`): Cards MUST be unlocked through the campaign. Completing the campaign MUST unlock all Cards.
- **CRD-013** (`decided`): In multiplayer, each player MUST build a deck before the match, within a minimum and maximum size (P-CRD-04, P-CRD-05).

## Card list
| Card | Effect | Cost | Pool | Status |
|---|---|---|---|---|
| Gain SP | Converts CP into SP (into the reserve) | CP | Core | decided |
| Resupply | Resupplies a Brigade **anywhere**, ignoring the usual need to be near a Supply ST, HQ or FOB | CP | Core | decided |
| Heal Brigade | Heals a Brigade anywhere by 50% of its **maximum** health (not above full) | CP | Core | decided |
| Field depot | Places a full Supply ST (earns SP, resupply point, capturable) where a Brigade stands. **Single use: removed from the deck after playing** | CP | Core | decided |
| New FOB | Places an FOB on any hex within your territory (no ST needed). **Single use: removed from the deck after playing** | CP (standard) | Core | decided |
| Random small Brigade | A small random Brigade, no re-rolls | CP | Core | decided |
| Extra Section | +1 to a Brigade's Section cap | CP | Core | decided |
| Starting Brigade boost | Improves the starting Brigade | CP | Core | decided |
| Choose a hero | The player chooses a hero from their unlocked heroes, who joins any Brigade **immediately**, wherever it is; costs more each time (HER-018) | CP | Core | decided |
| Draw 2 | Draw 2 Cards | CP | Core | decided |
| Forced march | Moves a Brigade 3 extra hexes on top of normal movement, free of SP, ignoring the Slow cap. Cannot be used to move into a battle (P-CRD-06) | CP | Core | decided |
| Rousing Speech | + morale for one Brigade (P-CRD-07) | CP | Core | decided |
| Sabotage | Pauses an enemy ST's income for one turn | CP | Core | decided |
| Dig in | A Brigade gets defences on its current hex, as if it were an ST | CP | Core | decided |
| Recon | Reveals a hex and all its neighbours through fog of war (see [[fog-of-war]]) | CP | Core | decided |
| Hasty Repairs | Restores 25% of an HQ's or FOB's health | CP | Core | decided |
| Fortified Position | Restores 50% of an HQ's or FOB's health | CP | Core | decided |
| Continuous Shelling | Reloads an Artillery Brigade, an HQ or a Bomber Squadron (deferred with air units), letting it fire again. The extra shot still costs SP | CP | Core | decided |
| Free re-rolls | Gives a number of free re-rolls when forming a Brigade (P-BRG-08) | CP | Core | decided |
| Call In The Big Guns | Form a Brigade by **choosing** its Sections (working title) | CP | Core | decided |
| Bridgelayer | Places a bridge (MAP-054) on a single river hex that has a passable land hex on each side | CP | – | decided |
| Named Brigade Cards | Call in a special named Brigade with its own bonuses: Barnum's Brigade, Haig's Heroes, Australian Light Brigade, Helsing's Hunters, 5th Kaiservolk, Grand Duke's Guard, The Red Baron, Billy's Crack Shots, Angels of Mannequin Hill (working titles) | CP | Faction-specific? | decided |
| Turn enemies | Converts routed enemy infantry into Thralls | CP | Central Powers | proposed |
| Seconded Section | Calls in a Section from the other Entente Faction | CP | Entente | proposed |
| Defector | Gives a defected enemy Section | CP | – | proposed |

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-CRD-01 | Starting hand size | 5 | decided |
| P-CRD-02 | Cards drawn per turn | 1 | decided |
| P-CRD-03 | Hand limit | 8 | decided |
| P-CRD-04 | Multiplayer deck minimum | – | open |
| P-CRD-05 | Multiplayer deck maximum | – | open |
| P-CRD-06 | Forced march distance | 3 hexes | decided |
| P-CRD-07 | Rousing Speech morale bonus | – | open |
| P-CRD-08 | CP cost and copies per Card | – | open |

## Open questions
- Q-CRD-01 (answered → CRD-003 to CRD-006): Deck and hand.
- Q-CRD-02 (answered → CRD-009): No limits or cooldowns; rarity comes from copies and CP.
- Q-CRD-03 (answered → CRD-011): Shared core plus faction Cards.
- Q-CRD-04 (answered → Card list): Heal Brigade works anywhere, for 50%.
- Q-CRD-05 (answered → CRD-008): Campaign map only.
- Q-CRD-06 (answered → Card list): 50% of maximum health.
- Q-CRD-07 (answered → CRD-014): Draw, then discard.
- Q-CRD-08 (answered → CRD-016): Every unlocked Card (may change).
- Q-CRD-09 (answered → CRD-015): Shuffle the discards back in.
- Q-CRD-10 (answered → CRD-017): Yes.
- Q-CRD-11 (answered → Card list): 3 hexes.
- Q-CRD-12 (answered → Card list): Free, on top of normal movement, ignores Slow, can't be used to start a battle.

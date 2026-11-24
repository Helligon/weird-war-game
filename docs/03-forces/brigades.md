---
id: BRG
title: Brigades
status: decided
phase: unassigned
tags: [forces, campaign]
related: ["[[hierarchy]]", "[[unit-tags]]", "[[officers]]", "[[cards]]", "[[map-and-locations]]"]
updated: 2026-11-21
---

# Brigades

## Summary
A Brigade is the army piece on the campaign map. You start with a random Brigade, form more at FOBs by buying Sections with SP, and are rewarded for building Brigades of similar Sections.

## Rules
### Starting Brigade
- **BRG-001** (`decided`): Each Faction MUST start with one Brigade of **3 random Sections from the standard roster** (P-BRG-05), led by a Generic Officer or an unlocked officer of the player's choice.
- **BRG-002** (`decided`): The player MAY re-roll individual Sections in the starting Brigade.
- **BRG-003** (`decided`): Re-rolls MUST cost SP or CP. Which currency is to be decided by playtesting (P-BRG-03). Each re-roll MUST cost more than the last; there is no hard limit.
- **BRG-024** (`decided`): A Card MUST give a number of free re-rolls when forming a Brigade (P-BRG-08).
- **BRG-004** (`decided`): Cards MAY improve the quality of the starting Brigade.

### Forming new Brigades
- **BRG-005** (`decided`): A new Brigade MAY be formed at an FOB or the HQ by spending SP.
- ~~BRG-006~~ (revised by DEC-077): Forming a Brigade is **not** shopping.
- **BRG-031** (`decided`): A Brigade formed normally (at the HQ or an FOB, BRG-005) MUST get a **random** selection of Sections (re-rolls apply, BRG-003).
- **BRG-034** (`decided`): Each Section MUST have a fixed SP cost, so a re-roll never changes the price (P-BRG-02).
- **BRG-035** (`proposed`): The player SHOULD choose how many Sections (3–5) the random Brigade has, and pay for that many.
- **BRG-032** (`decided`): The **Call In The Big Guns** Card (working title) MUST let the player form a Brigade by choosing its Sections.
- **BRG-033** (`decided`): **Named Brigade** Cards MUST "call in" a special pre-built Brigade with its own identity and bonuses. Working titles: Barnum's Brigade, Haig's Heroes, Australian Light Brigade, Helsing's Hunters, 5th Kaiservolk, Grand Duke's Guard, The Red Baron, Billy's Crack Shots, Angels of Mannequin Hill.
- **BRG-007** (`decided`): Each new Brigade MUST be assigned an Officer from the player's available officers (see [[officers]]).
- **BRG-008** (`decided`): A Card MAY create a random small Brigade with no re-rolls (see [[cards]]).
- **BRG-025** (`decided`): A new Brigade MUST start with a small amount of supply (P-BRG-07). Topping it up is a normal resupply.
- **BRG-026** (`decided`): A new Brigade MUST appear on the FOB/HQ hex, or on a free adjacent hex if that hex is occupied. If there is no free hex, the Brigade cannot be formed there.
- **BRG-027** (`decided`): All standard-roster Sections MUST be available from the start. Faction-specific Sections MUST be unlocked through the campaign (which ones and when: to be set during unit and campaign design).
- **BRG-028** (`decided`): There MUST be no hard cap on the number of Brigades per Faction (SP cost and available officers limit it; may be revisited).
- **BRG-029** (`decided`): There MUST be no limits on which Sections can go together in a Brigade.
- **BRG-009** (`decided`): Sections from any nation within the same Faction MAY be mixed in one Brigade.

- **BRG-016** (`decided`): Sections MAY be moved between Brigades. Officers stay with their Brigade (OFF-015).
- **BRG-017** (`decided`): Sections MUST only move between Brigades on adjacent hexes, and moving MUST NOT cost SP.
- **BRG-019** (`decided`): If a Brigade's maximum capacity drops, it MUST keep any SP above the new maximum (**overflow**). Once its SP falls below the new maximum, it MUST NOT be resupplied above it again.
- **BRG-020** (`decided`): The 3-Section minimum MUST apply only when forming a new Brigade. An existing Brigade MAY drop below 3 Sections (through losses or transfers).
- **BRG-021** (`decided`): 5 Sections MUST be the hard maximum per Brigade, including through transfers (P-BRG-01).
- **BRG-022** (`decided`): A Card and/or an officer bonus MAY raise a Brigade's maximum by 1 (P-BRG-06).
- **BRG-023** (`decided`): A Brigade left with 0 Sections MUST be disbanded, and its officer MUST return to the roster.
- **BRG-018** (`decided`): Sections MUST NOT hold SP. They draw on the SP of the Brigade they belong to, which is owned by that Brigade's officer. A moving Section takes no SP with it.

### Brigade type bonuses
- **BRG-010** (`decided`): The game MUST encourage Brigades of similar Sections (e.g. Artillery Brigade, Infantry Brigade, Armoured Brigade).
- **BRG-011** (`decided`): A Brigade with several Sections of the same class MUST get a class bonus. Example: several Artillery Sections → accuracy bonus.
- **BRG-012** (`decided`, revised by DEC-078): Brigade **titles replace bonus tiers**. A Brigade gets its title's bonus (BRG-030). The **major** bonus MUST only apply with 4 or more Sections of the same type (P-BRG-04). Majors are reachable with one Section of something else, e.g. an Artillery Brigade with a Heavy infantry, cavalry or Sapper Section for protection or repairs.
- ~~BRG-013~~ (withdrawn by DEC-078): Support Sections take up slots and count like any other Section. A Brigade that is mostly Support Sections is a **Support Brigade**.
- **BRG-014** (`decided`, examples; full list later): Example bonuses:

| Class | Minor | Major |
|---|---|---|
| Artillery | Accuracy (decided example) | + Range |
| Infantry | Cheaper upkeep / faster on map | + Morale holding |
| Heavy | Damage resistance | + Counts as heavy weaponry vs HQ |

- **BRG-015** (`decided`): Brigade type MUST affect campaign-map traits (e.g. Artillery Brigades slower and hungrier; Infantry Brigades cheap and flexible). This comes from Section supply profiles (SUP-021).

### Brigade titles
- **BRG-030** (`decided`): Brigades MUST get a title from their composition:
  - **[Type] Brigade** (e.g. Artillery Brigade) when **more than half** its Sections are that type.
  - **Combined Brigade** when it has 3 or more different types of Section, or an equal split of 2 types.
  - **Support Brigade** when more than half its Sections are Support.
  - **Strange Brigade** only with **4 or more Strange Sections** (deliberately special; not the more-than-half rule, TAG-004).
  - Support Sections MUST count the same as any other Section when working out titles.
  - Each title MUST have its own bonuses (to be finalised later). A full list of titles is to come later.

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-BRG-01 | Sections per Brigade | 3–5 when formed; max 5 | decided |
| P-BRG-02 | SP cost per Section type | – | open |
| P-BRG-03 | Re-roll cost and currency | SP or CP | open (playtest) |
| P-BRG-04 | Major bonus threshold | 4 Sections of the same type | decided |
| P-BRG-05 | Starting Brigade size | 3 Sections | decided |
| P-BRG-07 | Starting supply of a new Brigade | small | decided (principle), open (value) |
| P-BRG-08 | Free re-rolls from the Card | – | open |
| P-BRG-06 | Bonus Section cap (Card or officer bonus) | +1 | decided |

## Interactions
- [[unit-tags]]: class and origin tags drive type bonuses.
- [[supply]]: capacity may scale with Brigade size.

## Open questions
- Q-BRG-01 (answered → BRG-025 to BRG-029): Forming rules.
- Q-BRG-02 (answered → BRG-016): Sections can move between Brigades.
- Q-BRG-06 (answered → BRG-017, BRG-018): Adjacent, free; Sections hold no SP.
- Q-BRG-07 (answered → BRG-019, BRG-020): Overflow is kept until spent; the minimum of 3 applies only at formation.
- Q-BRG-08 (answered → BRG-021, BRG-022): 5 is the maximum; a Card or officer bonus can add 1.
- Q-BRG-09 (answered → BRG-023): Disbanded; the officer returns to the roster.
- Q-BRG-03 (answered → BRG-028): No hard cap.
- Q-BRG-04 (answered → BRG-029): None.
- Q-BRG-05 (answered → BRG-003): Rising cost, no hard limit.
- Q-BRG-10 (answered → BRG-031 to BRG-033): Normal forming is random; Call In The Big Guns lets you choose; named Brigade Cards exist.
- Q-BRG-12 (answered → BRG-005): HQs and FOBs only.
- Q-BRG-13 (answered → BRG-034): Fixed cost per Section. (Choosing the number of Sections is proposed in BRG-035.)
- Q-BRG-11 (answered → BRG-030): Titles from majority or mix, each with its own bonuses.
- Q-BRG-14 (answered → BRG-012): Titles replace tiers; major bonus only at 4 of the same type.
- Q-BRG-15 (answered → BRG-030): Strange needs 4+; Support counts normally; Support Brigade exists.

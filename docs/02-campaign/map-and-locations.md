---
id: MAP
title: Map and locations
status: decided
phase: unassigned
tags: [campaign, map]
related: ["[[supply]]", "[[upkeep-and-desertion]]", "[[brigades]]", "[[battle-maps]]", "[[fog-of-war]]", "[[resources]]", "[[map-editor]]"]
updated: 2026-11-25
---

# Map and locations

## Summary
The campaign is played on fixed, hand-made hex maps (about 50 wide by 30 tall to start, landscape), built with a simple in-house [[map-editor]]. The map holds Strategic Targets (STs) of three types, FOBs and each Faction's HQ. Supply STs double as resupply points. Territory spreads around the locations a Faction owns. Each side starts with only its HQ and starting Brigade; everything else has to be captured.

## Rules
### Map
- **MAP-017** (`decided`): The campaign map MUST use a hex grid.
- **MAP-018** (`decided`): Maps MUST be fixed and hand-made at first. Procedural generation is deferred ([[future-ideas]]).
- **MAP-019** (`decided`, revised by DEC-093): The starting map size MUST be about 50×30 hexes, 50 wide by 30 tall, in landscape for desktop screens (P-MAP-04), to be reviewed in playtesting. Prototypes MAY use smaller maps.
- **MAP-020** (`decided`): Maps MUST be made with the in-house map editor ([[map-editor]]).

### Starting positions
- **MAP-021** (`decided`): Each side MUST start with only its HQ and its starting Brigade. All STs and FOBs MUST be captured or built.

### Strategic Targets
- **MAP-001** (`decided`): STs MUST be capturable and MUST give income bonuses while held (see [[resources]]).
- **MAP-002** (`decided`): Battles at an ST MUST give the defender defences (see [[battle-maps]]).
- **MAP-022** (`decided`): STs MUST come in three types:

| Type | Income | Examples |
|---|---|---|
| Supply | SP; **also a resupply point** (SUP-013) | Rail depot, factory, farm |
| Command | CP | Telegraph station, airship dock, villa |
| Major | SP + CP | City, castle, bunker system, zeppelin port (a large port for airships) |

### HQ
- **MAP-003** (`decided`): Each Faction MUST have exactly one HQ.
- **MAP-004** (`decided`): A Faction whose HQ is reduced to 0 health MUST lose the game.
- **MAP-032** (`decided`): HQs and FOBs MUST have health points, representing their stationed garrison (P-MAP-09).
- **MAP-005** (`decided`): HQs and FOBs MUST be fortified. Only heavy weaponry does proper damage to them; other attacks do very little.
- **MAP-006** (`decided`): HQs and FOBs MUST have a stationed garrison (their health, MAP-032) that fights back by attacking adjacent enemies (MAP-036).
- **MAP-023** (`decided`): The HQ MUST be able to do everything: form Brigades, resupply, bombard and defend itself.
- **MAP-024** (`decided`): The HQ MUST generate SP and CP every turn, so a Faction always has some income (this is the guaranteed minimum, RES-011).

### Assaulting and defending bases
- **MAP-035** (`decided`): A Brigade adjacent to an enemy HQ or FOB MAY **assault** it. An assault MUST be an auto-resolved, one-sided attack: the HQ/FOB cannot fire back during it. A Brigade without heavy weaponry MUST do very little damage (P-MAP-10).
- **MAP-036** (`decided`): An HQ or FOB MAY attack any enemy Brigade adjacent to it in a one-sided attack, so it can badly hurt a Brigade at its gates.
- **MAP-042** (`decided`): "Heavy weaponry" MUST mean any Section with the Heavy or Artillery class tag.
- **MAP-045** (`decided`): FOBs MUST NOT bombard. They are forward logistics and local defence; long-range fire comes from the HQ and Artillery Brigades (BOM-003).
- **MAP-044** (`decided`): An HQ or FOB MUST attack adjacent enemies at most once per turn.
- **MAP-043** (`decided`): An HQ's or FOB's attack on adjacent enemies MUST be triggered by the player, and MUST NOT have a reload (separate from the bombardment cooldown, BOM-012).
- **MAP-037** (`decided`): An assault MUST use up the Brigade's turn (no further movement or actions for that Brigade) and MUST cost SP (P-MAP-13).

### Repair and healing
- **MAP-038** (`decided`): HQs and FOBs MUST recover health over time (P-MAP-11).
- **MAP-039** (`decided`): A Brigade on a friendly HQ or FOB hex MUST recover health over time (P-MAP-12).
- **MAP-040** (`decided`): The **Hasty Repairs** Card MUST restore 25% of an HQ's or FOB's health. The **Fortified Position** Card MUST restore 50%.

### FOBs
- **MAP-007** (`decided`): A Faction MAY have any number of FOBs. There is no hard limit.
- **MAP-008** (`decided`): An FOB MUST act as a resupply point **and** as a place where new Brigades can be formed.
- **MAP-009** (`decided`): When a Faction loses an FOB, the Faction that destroyed it MUST receive SP and CP worth about half the cost of building one (P-MAP-01).
- **MAP-034** (`proposed`): The FOB bonus SHOULD be paid at the moment the FOB is levelled (health reaches 0).
- **MAP-010** (`decided`): An FOB reduced to 0 health MUST become a **Levelled FOB**: a neutral Supply ST that can then be captured as normal. FOBs MUST NOT be captured directly.
- **MAP-011** (`decided`): The New FOB Card MUST place an FOB on any hex within the player's territory; no ST is needed. It costs a standard (non-rising) CP amount and MUST be removed from the deck after use (see [[cards]]).
- **MAP-012** (`decided`): An owned Supply ST MAY be upgraded into an FOB for a standard SP cost plus an CP cost that rises with each upgrade (P-MAP-03). The FOB is ready immediately, and no Brigade is needed on the hex.
- **MAP-025** (`decided`): The rising CP cost MUST be what limits the number of FOBs.
- **MAP-026** (`decided`): In the campaign, capturing certain FOBs MAY free imprisoned officers, who join the player's collection.
- **MAP-041** (`decided`): **Any** levelled FOB MUST become a neutral Supply ST, including one placed off an ST by the New FOB Card. A badly placed FOB can end up helping the enemy.
- **MAP-014** (`decided`): A destroyed FOB MUST be called a **Levelled FOB** (working name).

### Supply STs as resupply points
- **MAP-015** (`decided`): There MUST be no separate "supply depot" location. Supply STs generate SP **and** are where Brigades resupply (on or adjacent). All start neutral, except those placed by the single-use Field depot Card (SUP-014).

### Garrisons
- **MAP-046** (`decided`): The only ST upgrade MUST be the Supply ST → FOB upgrade (MAP-012).
- **MAP-047** (`decided`): A player MAY place a **Garrison Section** on an owned ST. It is a special Section with basic stats whose job is to slow down attackers.
- **MAP-048** (`decided`): When an enemy Brigade tries to move onto a garrisoned ST hex, an **auto-resolved battle** MUST happen. If the attacker wins, it moves onto the hex. If it loses, it stays where it was and MAY attack again next turn.
- **MAP-049** (`decided`): A Garrison Section MUST cost SP to place and MUST NOT cost upkeep (P-MAP-15).
- **MAP-050** (`decided`): A garrison beaten by an attacker MUST be destroyed. Attacking a garrison MUST use up the attacking Brigade's turn and cost SP, like an assault (MAP-037).
- **MAP-051** (`decided`): A garrison MAY share its hex with a friendly Brigade (it is not a Brigade). If that Brigade is attacked, the garrison MUST fight as an extra Section on the defender's side.
- **MAP-052** (`decided`): Any owned ST MAY be garrisoned. An ST that changes hands MUST lose its garrison.

### Major ST defences
- **MAP-029** (`decided`): Major STs MUST have stronger defences against bombardment than other STs (P-MAP-07).
- **MAP-030** (`decided`): A Brigade defending a Major ST MUST get a larger morale and defence bonus than at other STs (P-MAP-08).

### Territory
- **MAP-016** (`decided`): A Faction's territory MUST be the hexes around the locations it owns.
- **MAP-027** (`decided`): Each owned location MUST claim territory in a radius equal to its sight radius minus 1, with a minimum of 1 (P-MAP-02):

| Location | Sight (P-FOW-03) | Territory radius |
|---|---|---|
| HQ | 3 | 2 |
| FOB | 2 | 1 |
| Major ST | 2 | 1 |
| Command ST | 1 | 1 |
| Supply ST | 1 | 1 |

- **MAP-028** (`proposed`): Only owned locations SHOULD create territory, not Brigades.
- **MAP-031** (`decided`): **Every** FOB MUST generate SP each turn, whether it was upgraded from a Supply ST or placed by the New FOB Card (P-MAP-14). An upgraded Supply ST keeps the same amount for now (may be increased later).
- **MAP-013** (`decided`): Supply ST→FOB upgrades are kept in check by their rising CP cost (MAP-025).

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-MAP-01 | FOB-capture bonus | ~50% of build cost, in SP + CP | decided |
| P-MAP-02 | Territory radius | sight − 1, min 1 | decided |
| P-MAP-03 | Supply ST→FOB upgrade cost | standard SP + rising CP | decided (values open) |
| P-MAP-04 | Starting map size | ~50×30 hexes (50 wide, 30 tall) | decided |
| P-MAP-05 | HQ income per turn (SP, CP) | – | open |
| P-MAP-06 | Income per ST type | – | open |
| P-MAP-07 | Major ST bombardment defence | – | open |
| P-MAP-08 | Defender morale/defence bonus by ST type | – | open |
| P-MAP-09 | HQ and FOB health | – | open |
| P-MAP-10 | Assault damage (normal vs heavy weaponry) | very low / proper | decided (principle), open (values) |
| P-MAP-11 | HQ/FOB health recovery per turn | – | open |
| P-MAP-12 | Brigade healing on a friendly HQ/FOB per turn | – | open |
| P-MAP-13 | SP cost of an assault | – | open |
| P-MAP-14 | FOB SP income per turn | = Supply ST income | proposed |
| P-MAP-15 | Garrison Section SP cost | – | open |

## Open questions
- Q-MAP-01 (answered → MAP-017): Hex grid.
- Q-MAP-02 (answered → MAP-012): Standard SP + rising CP; ready immediately; no Brigade needed; no limit.
- Q-MAP-03 (answered → MAP-009, MAP-026): Half the build cost in SP + CP; imprisoned officers freed in the campaign.
- Q-MAP-04 (answered → MAP-023, MAP-024): The HQ does everything and provides base income.
- Q-MAP-05 (answered → MAP-027): Territory radius = sight − 1, min 1.
- Q-MAP-06 (answered → MAP-018, MAP-019, MAP-021): Fixed maps, ~30×50, start with HQ + Brigade.
- Q-MAP-07 (answered → MAP-015, SUP-013): Supply STs are the supply depots; the term "supply depot" is retired.
- Q-MAP-08 (answered → MAP-011): The Card has a standard cost and is single use, placing an FOB anywhere in your territory.
- Q-MAP-09 (answered → MAP-029, MAP-030): Yes.
- Q-MAP-10 (answered → MAP-031): Yes, unchanged for now.
- Q-MAP-12 (answered → MAP-035, MAP-036): Adjacent one-sided assault (weak without heavy weaponry); bases attack adjacent enemies.
- Q-MAP-15 (answered → MAP-042): Heavy or Artillery tag.
- Q-MAP-16 (answered → MAP-043): Player-triggered, no reload.
- Q-MAP-17 (answered → MAP-044): Once per turn.
- Q-MAP-18 (answered → MAP-037): Yes, it costs SP.
- Q-MAP-13 (answered → MAP-038 to MAP-040): Recovers over time; Brigades heal on friendly bases; repair Cards.
- Q-MAP-14 (answered → MAP-041): Yes.
- Q-MAP-19 (answered → MAP-050): Destroyed; uses the turn; costs SP.
- Q-MAP-20 (answered → MAP-051): Yes; it fights as an extra Section.
- Q-MAP-21 (answered → MAP-052): Any owned ST; lost on capture.
- Q-MAP-11 (answered → SUP-014 withdrawn, then reinstated by DEC-065): The Field depot Card was dropped, then brought back as a single-use Card.

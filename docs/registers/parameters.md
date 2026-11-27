---
id: REG-P
title: Parameters register
status: decided
tags: [register]
updated: 2026-11-27
---

# Parameters register

Every tunable value in one place, generated from the **Parameters** tables in the feature notes (the note is the source of truth). Values marked `open` or `proposed` MUST be implemented as configurable data with placeholders, never hard-coded.

| ID | Name | Value | Status | Note |
|---|---|---|---|---|
| P-FOW-01 | Sight per Section type | 1–3 hexes | decided (range), open (per type) | [[fog-of-war]] |
| P-FOW-02 | Recon reveal duration | Current turn + the enemy's next turn | decided | [[fog-of-war]] |
| P-FOW-03 | Location sight radius (per-type format, TEC-017; keys in the order `hq`, `fob`, `major`, `command`, `supply`) | hq=3 fob=2 major=2 command=1 supply=1 | decided | [[fog-of-war]] |
| P-MAP-01 | FOB-capture bonus | ~50% of build cost, in SP + CP | decided | [[map-and-locations]] |
| P-MAP-02 | Territory radius: sight minus `minus`, at least `min` (per-type format, TEC-017; keys in the order `minus`, `min`) | minus=1 min=1 | decided | [[map-and-locations]] |
| P-MAP-03 | Supply ST→FOB upgrade cost | standard SP + rising CP | decided (values open) | [[map-and-locations]] |
| P-MAP-04 | Starting map size | ~50×30 hexes (50 wide, 30 tall) | decided | [[map-and-locations]] |
| P-MAP-07 | Major ST bombardment defence | – | open | [[map-and-locations]] |
| P-MAP-08 | Defender morale/defence bonus by ST type | – | open | [[map-and-locations]] |
| P-MAP-09 | HQ and FOB health | – | open | [[map-and-locations]] |
| P-MAP-10 | Assault damage (normal vs heavy weaponry) | very low / proper | decided (principle), open (values) | [[map-and-locations]] |
| P-MAP-11 | HQ/FOB health recovery per turn | – | open | [[map-and-locations]] |
| P-MAP-12 | Brigade healing on a friendly HQ/FOB per turn | – | open | [[map-and-locations]] |
| P-MAP-13 | SP cost of an assault | – | open | [[map-and-locations]] |
| P-MAP-14 | FOB SP income per turn | = Supply ST income | proposed | [[map-and-locations]] |
| P-MAP-15 | Garrison Section SP cost | – | open | [[map-and-locations]] |
| P-RES-01 | Base SP income per turn | – | open | [[resources]] |
| P-RES-02 | Base CP income per turn | – | open | [[resources]] |
| P-RES-03 | CP cap / decay | – | proposed | [[resources]] |
| P-RES-04 | SP / CP bonus per ST: one bonus for all ST types for now; MAY be split per type later using the per-type format (TEC-017) | – | open | [[resources]] |
| P-RES-05 | Starting SP reserve | 5 | decided | [[resources]] |
| P-RES-06 | Starting CP | 0 | decided | [[resources]] |
| P-SUP-00 | Supply capacity of a **5-Section** Brigade | Enough for 10 hexes of movement + 1 battle (to be tuned once map scale is known) | decided | [[supply]] |
| P-SUP-09 | Supply profile per Section type (capacity contribution, consumption per hex) | – | open | [[supply]] |
| P-SUP-10 | Slow movement cap (hexes per turn) | 2 (placeholder) | open | [[supply]] |
| P-SUP-11 | Undead rot losses per turn | – | open | [[supply]] |
| P-SUP-12 | Undead grace period (turns without battle before rotting) | 2 | decided | [[supply]] |
| P-SUP-01 | Well Supplied threshold | 75% | decided | [[supply]] |
| P-SUP-02 | Overextended threshold (fraction of capacity) | 25% | decided | [[supply]] |
| P-SUP-03 | Standard march cost | ~30% (re-derive from hex costs) | proposed | [[supply]] |
| P-SUP-04 | Battle SP cost | ~20% | proposed | [[supply]] |
| P-SUP-05 | Well Supplied bonus (morale, reload) | – | open | [[supply]] |
| P-SUP-07 | Overextended morale penalty | – | open | [[supply]] |
| P-SUP-08 | Resupply range around the HQ and owned Supply STs, Major STs and FOBs | Same or adjacent hex | decided | [[supply]] |
| P-TRN-01 | Extra starting Cards for the second player | 1 | decided | [[turn-structure]] |
| P-TRN-02 | SP cost of attacking an adjacent enemy hex (TRN-036) | 1 (placeholder) | proposed | [[turn-structure]] |
| P-UPK-01 | Upkeep inside perimeter | 0 | decided | [[upkeep-and-desertion]] |
| P-UPK-02 | Upkeep outside perimeter | whole SP per turn by title (e.g. Infantry 1, Artillery 2) | decided (examples), open (full table) | [[upkeep-and-desertion]] |
| P-UPK-03 | Desertion health loss range | ~15% (bounds TBD) | decided (centre), open (bounds) | [[upkeep-and-desertion]] |
| P-BRG-01 | Sections per Brigade | 3–5 when formed; max 5 | decided | [[brigades]] |
| P-BRG-02 | SP cost per Section type | – | open | [[brigades]] |
| P-BRG-03 | Re-roll cost and currency | SP or CP | open (playtest) | [[brigades]] |
| P-BRG-04 | Major bonus threshold | 4 Sections of the same type | decided | [[brigades]] |
| P-BRG-05 | Starting Brigade size | 3 Sections | decided | [[brigades]] |
| P-BRG-07 | Starting supply of a new Brigade | small | decided (principle), open (value) | [[brigades]] |
| P-BRG-09 | Starting SP of each Faction's starting Brigade | 90 | decided | [[brigades]] |
| P-BRG-08 | Free re-rolls from the Card | – | open | [[brigades]] |
| P-BRG-06 | Bonus Section cap (Card or officer bonus) | +1 | decided | [[brigades]] |
| P-HIE-01 | Max Units per Section, by type | – | open | [[hierarchy]] |
| P-HIE-02 | Health per Unit, by type | – | open | [[hierarchy]] |
| P-TAG-01 | Threshold for "Strange Brigade" | – | 4 Sections (decided) | [[unit-tags]] |
| P-VET-01 | Number of ranks | 3 | proposed | [[veterancy]] |
| P-VET-02 | XP / battles per rank | – | open | [[veterancy]] |
| P-VET-03 | Trait pool and magnitudes | – | open | [[veterancy]] |
| P-VET-04 | Veterancy chance (winners / losers) | – | open | [[veterancy]] |
| P-BIN-01 | Retreat SP cost | – | proposed | [[battle-initiation]] |
| P-BIN-02 | Retreat morale penalty | – | proposed | [[battle-initiation]] |
| P-BMP-01 | Infantry time to cross the map | 60–90 s | decided | [[battle-maps]] |
| P-BOM-01 | Bombardment range by source | mortar 2, field gun 3 (examples) | decided (examples), open (full list) | [[bombardment]] |
| P-BOM-02 | Damage and morale effect | – | open | [[bombardment]] |
| P-BOM-03 | SP cost per bombardment | less than a battle | decided (principle), open (value) | [[bombardment]] |
| P-BOM-04 | HQ bombardment cooldown | 1 turn | decided | [[bombardment]] |
| P-BOM-05 | Damage reduction on ST / Major ST | – | open | [[bombardment]] |
| P-BOM-06 | HQ bombardment range | – | open | [[bombardment]] |
| P-CMD-01 | Max queued waypoints per Section | 2 | decided | [[commands]] |
| P-CMD-02 | Max Sections per side in battle | 10 | decided | [[commands]] |
| P-CMD-03 | Accuracy penalty while moving: the share of accuracy lost, so hit chance = accuracy × (1 − P-CMD-03) (DEC-098) | – | open | [[commands]] |
| P-CMD-04 | Next-to-cover distance: a waypoint this close to a cover object's edge counts as next to it (CMD-005, DEC-100) | 3 m (Spike 2 placeholder) | proposed | [[commands]] |
| P-DAI-01 | Nearby cover distance (DAI-001) | 25 m (Spike 2 placeholder) | proposed | [[default-ai-behaviour]] |
| P-DAI-02 | Protect distance: how far in front of the protected Section an uncontrolled officer stands (DAI-005, DEC-102) | 6 m (Spike 2 placeholder) | proposed | [[default-ai-behaviour]] |
| P-DAI-03 | Time between an uncontrolled officer's shots (DAI-005, DEC-102) | 2 s (Spike 2 placeholder) | proposed | [[default-ai-behaviour]] |
| P-DAI-04 | Hit chance of an uncontrolled officer's shot, rolled on the battle's seeded random source (DAI-005, DEC-102) | 0.5 (Spike 2 placeholder) | proposed | [[default-ai-behaviour]] |
| P-DEP-01 | Terrain → pattern weights | – | open | [[deployment]] |
| P-DEP-02 | Max Brigades per side (= 2) | – | decided | [[deployment]] |
| P-DEP-03 | Overextended reinforcement timer (longer than P-DEP-05; DEC-111 removed the separate Low tier and the longest-timer row; its number, 04, is not reused) | – | open | [[deployment]] |
| P-DEP-05 | Well Supplied / Supplied reinforcement timer (short) | – | open | [[deployment]] |
| P-DEP-06 | Deployment-zone depth | – | 15–20% of map depth (decided) | [[deployment]] |
| P-MOR-01 | Default Brigade base morale | – | open | [[morale]] |
| P-MOR-02 | Rout threshold | 20% | decided | [[morale]] |
| P-MOR-03 | Rout chance formula | 100 − 5 × morale | decided | [[morale]] |
| P-MOR-04 | When the rout chance is rolled | On each morale-changing event | decided | [[morale]] |
| P-MOR-05 | Morale change per event (catalogue) | – | open | [[morale]] |
| P-MOR-06 | Natural morale recovery rate | – | open | [[morale]] |
| P-MOR-07 | Base morale change after a win / loss | – | open | [[morale]] |
| P-PCH-01 | Leaderless morale penalty | – | open | [[player-character]] |
| P-VIC-01 | Escaped-rout health and morale penalty | – | open | [[victory-and-routing]] |
| P-VIC-02 | Rout threshold (see P-MOR-02) | – | decided | [[victory-and-routing]] |
| P-ARO-01 | Battles as ARO | 5 | decided | [[aro-progression]] |
| P-ARO-02 | Bonus options per victory | 3–4 | decided | [[aro-progression]] |
| P-ARO-03 | Lesson learned per loss | +1–2% to several stats | decided | [[aro-progression]] |
| P-ARO-04 | Upgraded-bonus chance by difficulty | – | open | [[aro-progression]] |
| P-HER-01 | Max heroes per Brigade | 1 | decided | [[heroes]] |
| P-HER-02 | Max heroes per Faction | 3 | decided | [[heroes]] |
| P-HER-04 | Hero Card cost escalation | – | open | [[heroes]] |
| P-HER-03 | Multiplayer hero roster size | 5 | decided | [[heroes]] |
| P-OVT-01 | Veterancy bonus cap | +20% | decided | [[officer-veterancy]] |
| P-OVT-02 | Officer veterancy ranks and bonus list | – | open | [[officer-veterancy]] |
| P-OFF-01 | Bonus slots | 3 | decided | [[officers]] |
| P-OFF-02 | Wounded duration (turns) | 3 | decided | [[officers]] |
| P-OFF-03 | Multiplayer officer roster size | – | open | [[officers]] |
| P-OFF-04 | Roster cooldown after Brigade wiped out | 3 turns | decided | [[officers]] |
| P-CRD-01 | Starting hand size | 5 | decided | [[cards]] |
| P-CRD-02 | Cards drawn per turn | 1 | decided | [[cards]] |
| P-CRD-03 | Hand limit | 8 | decided | [[cards]] |
| P-CRD-04 | Multiplayer deck minimum | – | open | [[cards]] |
| P-CRD-05 | Multiplayer deck maximum | – | open | [[cards]] |
| P-CRD-06 | Forced march distance | 3 hexes | decided | [[cards]] |
| P-CRD-07 | Rousing Speech morale bonus | – | open | [[cards]] |
| P-CRD-08 | CP cost and copies per Card | – | open | [[cards]] |
| P-MUL-01 | Players (lite) | 2 | decided | [[multiplayer]] |

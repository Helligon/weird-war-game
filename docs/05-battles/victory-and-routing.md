---
id: VIC
title: Victory and routing
status: decided
phase: unassigned
tags: [battle]
related: ["[[commands]]", "[[veterancy]]", "[[cross-faction-units]]"]
updated: 2026-11-21
---

# Victory and routing

## Summary
A battle is won by destroying or routing every enemy Unit. There is no time limit.

## Rules
- **VIC-001** (`decided`): A side MUST win when every enemy Unit is destroyed or routed.
- **VIC-002** (`decided`): Routing Units MUST try to leave the map by their own side's edge, or die trying.
- **VIC-003** (`decided`): Routed Units that escape MUST survive with low health and a morale penalty (P-VIC-01). They are not destroyed.
- **VIC-004** (`decided`): Battles MUST have no time limit (the only timer is for reinforcements; see [[deployment]]).
- **VIC-005** (`proposed`): Pursuing routing Units SHOULD be worthwhile, especially to stop veterans escaping.

## Parameters
| ID | Name | Status |
|---|---|---|
| P-VIC-01 | Escaped-rout health and morale penalty | open |
| P-VIC-02 | Rout threshold (see P-MOR-02) | decided |

## Open questions
- Q-VIC-01 (answered → [[morale]]): Morale model and rout chance.
- Q-VIC-02 (answered → MOR-012): Yes.
- Q-VIC-03 (answered → TRN-010 to TRN-016): Defenders retreat towards the nearest HQ/FOB, attackers fall back, and a Brigade that can't retreat is destroyed.

## Out of scope
- Anti-stalemate supply drain: **deferred** ([[future-ideas]]).

---
id: DAI
title: Default Section behaviour
status: decided
phase: unassigned
tags: [battle, ai]
related: ["[[commands]]", "[[ai-opponents]]"]
updated: 2026-11-26
---

# Default Section behaviour

## Summary
With no orders, Sections behave very simply, so that players are encouraged to give orders.

## Rules
- **DAI-001** (`decided`, revised by DEC-099): A Section with no orders MUST move to engage the enemy and take nearby cover. Nearby cover means cover within P-DAI-01. A Section MUST NOT back away to reach cover, and leaves cover only to close to firing range.
- **DAI-002** (`decided`): Default behaviour MUST stay deliberately simple, to encourage the player to give orders.
- **DAI-003** (`decided`): A Section that starts in, or is in, defences (e.g. trenches at an ST) MUST hold its position by default.
- **DAI-004** (`decided`): A Section given Stop MUST hold its position, and SHOULD fire at enemies in range.
- **DAI-006** (`decided`, revised by DEC-096): By default, a Section MUST target the nearest enemy in range, preferring one that is attacking it. Enemy officers and heroes count as enemies.
- **DAI-005** (`decided`, DEC-094): By default, an officer or hero that the player is not controlling SHOULD Protect the nearest friendly Section.

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-DAI-01 | Nearby cover distance (DAI-001) | 25 m (Spike 2 placeholder) | proposed |
| P-DAI-02 | Protect distance: how far in front of the protected Section an uncontrolled officer stands (DAI-005, DEC-102) | 6 m (Spike 2 placeholder) | proposed |
| P-DAI-03 | Time between an uncontrolled officer's shots (DAI-005, DEC-102) | 2 s (Spike 2 placeholder) | proposed |
| P-DAI-04 | Hit chance of an uncontrolled officer's shot, rolled on the battle's seeded random source (DAI-005, DEC-102) | 0.5 (Spike 2 placeholder) | proposed |

## Open questions
- Q-DAI-01 (answered → DAI-006): Nearest in range, preferring attackers.

---
id: TEC
title: Platform and technology
status: decided
phase: unassigned
tags: [foundation, tech]
related: ["[[multiplayer]]", "[[map-editor]]"]
updated: 2026-11-24
---

# Platform and technology

## Summary
The game will be built in **Unity** and published on **itch.io**. Alex is new to game engines (his experience is simple browser games), so early work should include learning spikes and small prototypes.

## Rules
- **TEC-001** (`decided`, tentative): The game engine SHOULD be **Unity**.
- **TEC-002** (`decided`): The game MUST be publishable on **itch.io**.
- **TEC-003** (`proposed`): The primary itch.io build SHOULD be a **downloadable desktop build** (Windows, then Mac and Linux). A **browser (Web) build** is a stretch goal.
  - Reason: Unity's Web platform has limits. Browsers don't allow direct socket access, so browser multiplayer needs WebSockets through a server or relay, and threading is limited. A downloadable build avoids these.
- **TEC-004** (`proposed`): Multiplayer SHOULD use a relay service (e.g. Unity Relay) so that one player can host without port forwarding, and so a future browser build can connect over secure WebSockets.
- **TEC-005** (`proposed`): The first technical work SHOULD be throwaway prototypes (spikes): (1) a hex campaign map with movement; (2) a third-person + tactical-view battle with 2 Sections; (3) two players connecting through the relay.
- **TEC-007** (`decided`): A browser build is **not essential**. If made, it MAY be single-player only.
- **TEC-008** (`decided`): Multiplayer MUST shape the architecture from the start (e.g. game state and player actions designed so they can be sent over a network), even though multiplayer features are built much later (MUL-014).
- **TEC-009** (`decided`): Development MAY be done on a Mac (Unity's editor supports Apple silicon, M1 or later).
- **TEC-010** (`decided`): Windows builds MUST be tested on Alex's Windows PC (GTX 1080 Ti).
- **TEC-011** (`proposed`): That PC SHOULD also serve as a reference machine for performance targets.
- **TEC-012** (`decided`): The code MUST be split into a plain C# simulation core and a Unity presentation layer (DLV-012).
- **TEC-006** (`decided`): The art style SHOULD stay simple-looking (VIS-006), which helps performance, especially for a browser build.

## Open questions
- Q-TEC-01 (answered → TEC-007): Not essential; possibly single-player only.
- Q-TEC-02 (deferred → after first prototype): Confirm Unity.
- Q-TEC-03 (answered → TEC-010): On Alex's Windows PC.

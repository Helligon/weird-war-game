---
id: TEC
title: Platform and technology
status: decided
phase: unassigned
tags: [foundation, tech]
related: ["[[multiplayer]]", "[[map-editor]]"]
updated: 2026-11-26
---

# Platform and technology

## Summary
The game will be built in **Unity** and published on **itch.io**. Alex is new to game engines (his experience is simple browser games), so early work should include learning spikes and small prototypes.

## Rules
- **TEC-001** (`decided`, confirmed by DEC-095): The game engine MUST be **Unity**. Spike 1 confirmed it (see [[spike-1-learnings]]).
- **TEC-002** (`decided`): The game MUST be publishable on **itch.io**.
- **TEC-003** (`proposed`): The primary itch.io build SHOULD be a **downloadable desktop build** (Windows, then Mac and Linux). A **browser (Web) build** is a stretch goal.
  - Reason: Unity's Web platform has limits. Browsers don't allow direct socket access, so browser multiplayer needs WebSockets through a server or relay, and threading is limited. A downloadable build avoids these.
- **TEC-004** (`proposed`): Multiplayer SHOULD use a relay service (e.g. Unity Relay) so that one player can host without port forwarding, and so a future browser build can connect over secure WebSockets.
- **TEC-005** (`decided`, revised by DEC-086 and DEC-094): The first technical work MUST be throwaway prototypes (spikes): (1) a hex campaign map with movement, which also checks that the core is deterministic (the same commands give the same state); (2) a third-person + tactical-view battle with 2 Sections: the player's officer with 1 friendly Section against 1 enemy Section. Spike 2 MUST record its commands as a log; replaying a battle from that log is a stretch goal, as evidence for Q-MUL-05. Morale and rout are out of scope for Spike 2. Each spike has its own milestone (M0.1, M0.2). The relay spike (two players connecting) is deferred until multiplayer work begins (MUL-011, MUL-014).
- **TEC-007** (`decided`): A browser build is **not essential**. If made, it MAY be single-player only.
- **TEC-008** (`decided`): Multiplayer MUST shape the architecture from the start (e.g. game state and player actions designed so they can be sent over a network), even though multiplayer features are built much later (MUL-014).
- **TEC-009** (`decided`): Development MAY be done on a Mac (Unity's editor supports Apple silicon, M1 or later).
- **TEC-010** (`decided`): Windows builds MUST be tested on Alex's Windows PC (GTX 1080 Ti).
- **TEC-011** (`proposed`): That PC SHOULD also serve as a reference machine for performance targets.
- **TEC-012** (`decided`): The code MUST be split into a plain C# simulation core and a Unity presentation layer (DLV-012).
- **TEC-006** (`decided`): The art style SHOULD stay simple-looking (VIS-006), which helps performance, especially for a browser build.
- **TEC-013** (`decided`): Spike code MUST live in a `prototypes/` folder in this repo and is merged to `main` like any other work. It is throwaway: production code MUST NOT build on it (DEC-087).
- **TEC-016** (`decided`, DEC-103): Production code MUST live in top-level `src/`: the simulation core in `src/WeirdWar.Core` (netstandard2.1, no Unity references, DLV-012) and its tests in `src/WeirdWar.Core.Tests`, with its own CI job. Nothing in `src/` MAY reference `prototypes/` (TEC-013). Whether Unity loads the core as a DLL or a local package is decided in the first Unity Feature.
- **TEC-017** (`decided`, DEC-104): A parameter MAY hold one value per type. It MUST be written as `key=number` pairs separated by single spaces, with the keys in a fixed order that the parameter's row documents (for example P-FOW-03: `hq=3 fob=2 major=2 command=1 supply=1`). The same format MAY name parts of a value instead of types (P-MAP-02: `minus=1 min=1`).
- **TEC-018** (`decided`, DEC-104): Percentages MUST be stored in data as fractions (0.75, not 75%).
- **TEC-019** (`decided`, DEC-104): Simulation state that is hashed (TEC-008) MUST hold no floating-point numbers. It MUST use integers or decimals.
- **TEC-014** (`decided`): The repo MUST use Git LFS for binary assets, using Unity's standard `.gitattributes` setup (DEC-087).
- **TEC-015** (`decided`, revised by DEC-092): The Unity editor MUST be an LTS (long-term support) release. The project uses **6000.3.29f1** (Unity 6.3 LTS). The exact version is recorded in the project's `ProjectSettings/ProjectVersion.txt`.

## Open questions
- Q-TEC-01 (answered → TEC-007): Not essential; possibly single-player only.
- Q-TEC-02 (answered → TEC-001): Unity confirmed after Spike 1 (DEC-095).
- Q-TEC-03 (answered → TEC-010): On Alex's Windows PC.

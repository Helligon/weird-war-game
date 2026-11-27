---
id: MUL
title: Multiplayer
status: decided
phase: unassigned
tags: [modes, multiplayer]
related: ["[[battle-initiation]]", "[[officers]]", "[[commands]]"]
updated: 2026-11-27
---

# Multiplayer

## Summary
The long-term goal is to play against other humans and against bots with adjustable difficulty. It starts as a 2-player "lite" version.

## Rules
- **MUL-001** (`decided`): Long-term goal: players MUST be able to play against other humans and against bots with adjustable difficulty.
- **MUL-002** (`decided`): The first version of multiplayer MUST be a 2-player "lite" mode (P-MUL-01).
- **MUL-003** (`deferred`): Several players MAY share a Faction (e.g. a Germany player and an Austria player).
- **MUL-004** (`decided`): All officers MUST be available in multiplayer, with a roster chosen for battle (OFF-004).
- **MUL-008** (`decided`): Each player picks a roster of 5 heroes before the match (HER-017).
- **MUL-009** (`decided`): There MUST be no turn timer for now.
- **MUL-010** (`decided`): Any two Factions MAY face each other, including mirror matches.
- **MUL-011** (`decided`): Multiplayer MUST use one player hosting (peer-to-peer style), joined by private invite or code. A technical spike MUST come first, because real-time battles need careful syncing (see [[platform-and-tech]]).
- **MUL-012** (`decided`): There MUST be no matchmaking or ranked play. The audience is wargamers who play mostly single-player, or against a friend at an arranged time.
- **MUL-013** (`deferred`): Multi-session play (ideally asynchronous, like play-by-email) is for later, alongside larger maps. **For now, a 2-player game MUST be played in a single session.**
- **MUL-014** (`decided`): Multiplayer MUST be considered throughout development (architecture ready for it) but MUST NOT be built until much later.
- **MUL-015** (`decided`, DEC-117): Production battles MUST be built as a deterministic, command-driven simulation: one game clock, fixed steps, a step-numbered command log and a seeded random source, as in Spike 2. The network model on top of it is decided later (Q-MUL-10).
- **MUL-005** (`decided`): Auto-resolve MUST require consent from both players (BIN-004).
- **MUL-006** (`decided`): The tactical view MUST NOT pause in multiplayer (CMD-003).
- **MUL-007** (`deferred`): With more than 2 players, there MUST be a solution for players waiting during battles they aren't in. Options: auto-resolve, spectate, control neutral or garrison forces, or simultaneous turns with queued battles.

## Parameters
| ID | Name | Value | Status |
|---|---|---|---|
| P-MUL-01 | Players (lite) | 2 | decided |

## Open questions
- Q-MUL-01 (answered → MUL-011): One player hosts; spike first.
- Q-MUL-02 (answered → MUL-010): Any pairing, including mirrors.
- Q-MUL-03 (answered → MUL-012): Private invites only, no matchmaking.
- Q-MUL-04 (answered → MUL-013): Single session for now; asynchronous play later.
- Q-MUL-05 (answered → MUL-015): A deterministic, command-driven battle is viable; build production battles that way. The network model is decided later (Q-MUL-10).
- Q-MUL-06: What number type should battle state use when it is hashed or compared across machines: integers, fixed-point, or floats on one simulating machine (in the spirit of TEC-019)? To answer before production battle work.
- Q-MUL-07: Should the command log header be self-contained, with the seed, the map and the data version in it? To answer before production battle work.
- Q-MUL-08: Should the command log carry a per-step (or periodic) state hash, so a silent divergence is found?
- Q-MUL-09: Should a battle hash be checked across platforms (Mac and Linux CI), as Spike 1 did for the campaign?
- Q-MUL-10: Which network model sits on top of the deterministic battle: a server that runs the simulation while clients send commands, or lockstep? Decided later.

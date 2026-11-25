---
id: MUL
title: Multiplayer
status: decided
phase: unassigned
tags: [modes, multiplayer]
related: ["[[battle-initiation]]", "[[officers]]", "[[commands]]"]
updated: 2026-11-25
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
- Q-MUL-05: How will real-time battles be networked (e.g. a deterministic simulation driven by commands, or Unity netcode syncing state)? MUST be answered before production battle work starts, so battles aren't rebuilt later (DEC-087).

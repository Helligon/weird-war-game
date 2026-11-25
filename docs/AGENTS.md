---
id: AGENTS
title: Agent instructions
status: decided
updated: 2026-11-25
tags: [meta]
---

# Agent instructions

These docs are the **single source of truth** for the game design of *Weird War* (working title). They are written for humans (via Obsidian) and for AI agents that turn design into tickets and plan delivery order.

## Reading order

1. [[glossary]]: learn the terms first. Terms are used with exactly these meanings.
2. [[vision]] and [[setting]]: the intent behind every rule.
3. Feature notes, folder by folder (`02-campaign` → `09-vehicles`).
4. Registers: [[parameters]], [[open-questions]], [[decision-log]], [[future-ideas]].

## Status meanings

Every rule carries a status. **Rule-level status is authoritative**; the note-level `status` property only summarises the note's overall maturity.

| Status | Meaning | Agent action |
|---|---|---|
| `decided` | Confirmed by the design owner (Alex). | Ticket it. |
| `proposed` | Suggested (by Claude, or tentatively by Alex) but **not confirmed**. | **Do not ticket.** Raise it for confirmation, and clarify its details with the design owner before it can become `decided`. |
| `open` | Known gap; not yet discussed. | Do not ticket. Raise it as a blocker if a decided rule depends on it. |
| `deferred` | Explicitly postponed ("later"). | Do not ticket now. |

> [!important] Proposed features
> A `proposed` item MUST be confirmed by the design owner, and its details MUST be clarified in conversation, before it is promoted to `decided`. This is deliberate: it gives the design owner room to reshape the idea.

## Rule format

- Rules have stable IDs: `PREFIX-NNN` (e.g. `SUP-004`). IDs are never reused or renumbered. A removed rule is marked `~~struck~~` with a note.
- Rules use RFC 2119 keywords: **MUST**, **MUST NOT**, **SHOULD**, **MAY**.
- Numbers do not live in rules. Rules reference parameters (`P-SUP-01`) defined in [[parameters]].
- Each note has the same sections: Summary → Rules → Parameters → Interactions → Edge cases → Open questions → Out of scope.

## Ticketing conventions

- Every Feature has a three-character code and every Story an ID like `SP2-38` (DLV-019). Titles start with that code or ID.
- One ticket per coherent cluster of `decided` rules (typically one note section).
- Every ticket MUST cite the rule IDs it implements and the parameter IDs it uses.
- Acceptance criteria MUST be derived from the cited rules (one or more criteria per rule).
- Parameters marked `open` or `proposed` SHOULD be implemented as data-driven/configurable values with a placeholder, never hard-coded.
- If a decided rule depends on an `open` question, create the ticket but mark it **blocked** and link the question ID from [[open-questions]].
- Stubs (`status: open` notes) produce no tickets except, optionally, a spike/discovery ticket.

## Sequencing

`phase` is `unassigned` throughout. A planning agent proposes the delivery order; the design owner approves it. Items stated by the design owner as "later" are marked `deferred`.

## Sequencing constraints from the design owner
- Start with the prototype spikes in [[platform-and-tech]] (TEC-005) before building production systems. The design owner is new to game engines.
- Multiplayer features are built **much later**, but every system MUST be designed to be multiplayer-ready (TEC-008, MUL-014).
- Air and Sea Brigades (air and naval vehicles, bombers, fighters, gunships) MUST be designed and built **after** ground units are finished (DEC-049).

## Delivery
The agent roster, workflow, branching and Definitions of Ready and Done are in [[pipeline]].

## Changing these docs

- Record any significant design change in [[decision-log]].
- Update the `updated` property on any note you change.
- New notes MUST use [[feature-template]].

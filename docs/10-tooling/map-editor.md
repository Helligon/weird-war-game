---
id: MED
title: Map editor
status: decided
phase: unassigned
tags: [tooling]
related: ["[[map-and-locations]]"]
updated: 2026-11-22
---

# Map editor

## Summary
A very simple, internal map editor that Alex uses to build the campaign maps. It doesn't need to be production quality for now.

## Rules
- **MED-001** (`decided`): There MUST be a simple map editor for creating hex campaign maps.
- **MED-002** (`decided`): It MUST be good enough for Alex to build the campaign maps. It does not need production polish for now.
- **MED-003** (`proposed`): Minimum features:
  - Create a map of a given size (default ~30×50, P-MAP-04).
  - Paint terrain per hex (including impassable terrain: mountains, sea, rivers; marsh maybe).
  - Place locations: HQ starts, STs by type (Supply / Command / Major), bridges.
  - Save and load to a data file the game reads.
- **MED-004** (`decided`): The editor MUST be a separate tool for now, and MUST export maps as JSON (or a similarly simple format) that the game imports.
- **MED-005** (`decided`): The editor MUST be a map editor only for now. Campaign events are to be added later.

## Open questions
- Q-MED-01 (answered → MED-004): A separate tool that exports JSON.
- Q-MED-02 (answered → MED-005): Map only; events later.

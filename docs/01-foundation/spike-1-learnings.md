---
id: SPK
title: "Spike 1 learning note: what I learned / is Unity OK?"
status: decided
phase: unassigned
tags: [prototype, learning]
related: ["[[platform-and-tech]]", "[[pipeline]]", "[[map-and-locations]]", "[[decision-log]]"]
updated: 2026-11-25
---

# Spike 1 learning note: what I learned / is Unity OK?

## Summary
Spike 1 (Feature #6, milestone M0.1, TEC-005) built a hex campaign map in Unity, driven by a plain C# simulation core, with hot-seat movement and turns and a determinism check. It answers Q-TEC-02: **Unity is confirmed** (DEC-095, TEC-001). The code under `prototypes/spike-1-hex-map/` stays throwaway (TEC-013); what carries forward is the lessons below, not the code.

## What worked
- **Plain C# core plus a Unity presentation layer (TEC-012, DLV-012).** The core has no Unity references and is tested with `dotnet test` alone (130 tests). Unity loads it as a prebuilt DLL (`dotnet build core -c Release -o unity/Assets/Plugins/WeirdWarCore`) and only draws state and turns clicks into commands (35 EditMode tests).
- **Every player action is a command (DLV-013).** Select, move and End Turn all go through `GameState.Apply`, which returns an accepted or rejected result with a reason. The UI holds no rules, so the same rules are tested once, in the core.
- **Determinism (TEC-008).** Canonical JSON serialisation of the state plus a SHA256 hash gives the same golden hash on the Mac and on Linux CI. Replaying a pinned command fixture reproduces it. Formatting is culture-invariant, and nothing relies on `GetHashCode` or `Dictionary`/`HashSet` order. Tests prove a single changed command or hex changes the hash.
- **The pipeline.** Each Story had its own branch and PR, a reviewer pass, and a manual check by Alex. Mutation checks (breaking the code on purpose) confirmed that review fixes catch the bugs they target.

## What was harder than expected
- **Unity versions.** The first project was made on a non-LTS editor (6000.6.10f1) and then moved to 6.3 LTS (6000.3.29f1, DEC-092). Unity cannot open a project in an older editor, so it was recreated from the template. That dropped the VS Code and Rider integration packages, which had to be restored. Pick the LTS version before creating a project.
- **Files Unity writes on open.** Unity 6.3 writes `ProjectSettings/SceneTemplateSettings.json` and `PackageManagerSettings.asset` when the project or editor GUI first opens. Commit them, or every checkout shows untracked files.
- **Scenes.** Scenes are plain-text YAML and stay out of LFS (TEC-014). Rebuilding a scene from an editor script rewrites its internal IDs, so only regenerate a scene when it really changes.
- **Batch mode versus the open editor.** Command-line tests cannot run while the editor has the same project open, so agents run them on a copy.
- **Editor UI for a beginner.** The Game view's Scale slider hid the overlay panel. Manual test scripts for Alex must spell out editor basics (Game tab, Scale 1x, Free Aspect, the build step in a terminal).
- **Map shape.** A portrait 30×50 map felt wrong on a desktop screen, which led to the landscape rule (MAP-019, DEC-093).

## Answer to Q-TEC-02
Unity is confirmed as the engine (TEC-001, DEC-095). The core/presentation split, the plugin build step and the determinism harness all worked on Unity 6.3 LTS, and Alex could run and check the spike without prior Unity experience.

## Carry forward (as lessons, not code)
- Keep the core/presentation split, commands with accepted/rejected results, and the canonical state hash.
- Choose the editor version first, and commit Unity's generated settings files.
- Keep manual test scripts in PRs, written for a non-expert.
- Revisit the DLL build step (or a local package) for the production project.

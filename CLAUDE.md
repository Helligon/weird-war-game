# CLAUDE.md

*Weird War*: an alternate-history WW1 strategy game, built with an AI-assisted delivery pipeline. Alex is the design owner and approves every merge to `main`.

## Source of truth
- The design lives in `docs/`, an Obsidian vault. **Read `docs/AGENTS.md` first.** It covers status meanings, rule format and ticketing conventions.
- The delivery process, the agent roster and the architecture rules are in `docs/11-delivery/pipeline.md`.
- Every piece of work traces back to a rule ID: rule → Story issue → branch/PR → test named after the rule.

## Hard rules
- Only `decided` rules become work. Do not build `proposed`, `open` or `deferred` items. Raise them with Alex instead.
- No agent merges into `main` (DLV-016), except a docs-only PR (DLV-020): every changed file under `docs/`, none in `docs/11-delivery/` or `docs/AGENTS.md`, and `Doc checks` passing. Alex merges Feature branches himself (DLV-007). Agents MAY merge PRs into any other branch, e.g. a Story PR into its Feature branch (DLV-017). A hook in `.claude/settings.json` enforces this.
- Never edit `docs/registers/open-questions.md` or `parameters.md` by hand. They are generated.
- Docs MAY be changed without asking when Alex has already discussed the change. Raise anything completely new with him first (DLV-009). Update a note's `updated` date when you change it. Record significant design changes in `docs/registers/decision-log.md`.
- `main` is protected: every change goes through a PR with a passing `Doc checks` job, and Alex merges.
- Simulation core in plain C# with no Unity dependency. Every player action is a command (DLV-012, DLV-013).

## Commands
```
python3 scripts/check_docs.py              # validate the vault (also runs in CI)
python3 scripts/generate_registers.py      # regenerate the registers after editing notes
python3 -m unittest discover -s scripts    # tests for the doc scripts
```

## Agents (`.claude/agents/`)
| Agent | Use for |
|---|---|
| `design-steward` | Checking the docs and regenerating the registers |
| `planner` | Proposing Features and their order. Creates issues only after Alex approves |
| `ticket-writer` | Writing the Stories for one approved Feature |
| `developer` | Implementing one Story on its own branch, with a PR into the Feature branch |
| `reviewer` | Reviewing a PR against its rules, as PR comments only |

All agents run on demand from Claude Code for now. Running them automatically in GitHub Actions is an open question (Q-DLV-01, Q-DLV-02).

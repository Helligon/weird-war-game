# Weird War (working title)

An alternate-history WW1 strategy game with steampunk weaponry and fantastical creatures. Lead your Faction's Brigades across a turn-based hex campaign map, then take command in real-time third-person battles.

This project is also an experiment in an **AI-assisted software development lifecycle**: design decisions live in a structured Obsidian vault with stable rule IDs, and AI agents turn them into GitHub Issues, code, tests and reviews, with a human approving every merge.

- **Design docs:** [`docs/`](docs/00-index.md) (open as an Obsidian vault). Start with [`docs/AGENTS.md`](docs/AGENTS.md).
- **Delivery pipeline:** [`docs/11-delivery/pipeline.md`](docs/11-delivery/pipeline.md)
- **Engine:** Unity (tentative), with a plain C# simulation core.
- **Target platform:** itch.io.

## Regenerating the registers
```
python3 scripts/generate_registers.py
```

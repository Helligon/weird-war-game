# Prototypes

Everything under `prototypes/` is **throwaway** spike code (TEC-013). It exists to answer a question, not to ship.

**Production code must not build on it.** Do not reference, copy or depend on anything here from production projects. If a spike produces something worth keeping, rewrite it properly under the production source tree, with tests, from the rules in `docs/`.

| Spike | Folder |
|---|---|
| Spike 1: hex map | `spike-1-hex-map/` (plain C# `core`, Unity project in `unity/`) |
| Spike 2: third-person battle | `spike-2-battle/` (plain C# `core`, Unity project in `unity/`; throwaway) |

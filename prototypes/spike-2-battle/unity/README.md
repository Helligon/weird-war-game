# Spike 2 Unity project (presentation only)

Unity shows the battle and sends commands to the core. It holds no game rules that the core owns (DLV-012, DLV-013). The core is plain C# in `../core`.

## Before opening the project (and after any change in `../core`)

Build the core into the project's plugin folder. Run this from `prototypes/spike-2-battle`:

```
dotnet build core -c Release -o unity/Assets/Plugins/WeirdWarBattleCore
```

The DLL is git-ignored, so it is never committed (`*.dll` would go to LFS and go stale). Without it Unity reports missing `WeirdWar.Spike2.Battle.Core`.

## Scene

Open `Assets/Scenes/Battle.unity` (an empty scene for now) and press Play.

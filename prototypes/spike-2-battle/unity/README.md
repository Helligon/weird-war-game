# Spike 2 Unity project (presentation only)

Unity shows the battle and sends commands to the core. It holds no game rules that the core owns (DLV-012, DLV-013). The core is plain C# in `../core`.

## Before opening the project (and after any change in `../core`)

Build the core into the project's plugin folder. Run this from `prototypes/spike-2-battle`:

```
dotnet build core -c Release -o unity/Assets/Plugins/WeirdWarBattleCore
```

The DLL is git-ignored, so it is never committed (`*.dll` would go to LFS and go stale). Without it Unity reports missing `WeirdWar.Spike2.Battle.Core`.

## Scene

Open `Assets/Scenes/Battle.unity` and press Play. The scene is made by the menu item *Weird War > Build Battle scene*.

The map and the Riflemen speed are read at run time from `../data/map.json` and `../data/stats.json` (metres; one metre is one Unity unit). Blue strip = west edge, red strip = east edge, green discs = cover, tan = open ground.

EditMode tests: Window > General > Test Runner, or `Unity -batchmode -nographics -projectPath unity -runTests -testPlatform EditMode -testResults results.xml`.

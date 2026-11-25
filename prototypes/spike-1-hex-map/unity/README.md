# Spike 1 Unity project (presentation only)

Unity shows the core's state and holds no game rules (DLV-012). The core is plain C# in `../core`.

## Before opening the project (and after any change in `../core`)

Build the core into the project's plugin folder. Run this from `prototypes/spike-1-hex-map`:

```
dotnet build core -c Release -o unity/Assets/Plugins/WeirdWarCore
```

The DLL is git-ignored, so it is never committed (`*.dll` would go to LFS and go stale). Without it Unity reports missing `WeirdWar.Spike1.Core`.

## Scene and tests

- Open `Assets/Scenes/HexMap.unity` and press Play. The scene is made by the menu item *Weird War > Build HexMap scene*.
- The map is read from `../maps/sample.json` and `sample-scenario.json` at run time.
- Camera: WASD or arrow keys pan, right or middle mouse drag pans, scroll zooms to the cursor. The view centre is clamped to the map plus a margin.
- EditMode tests: Window > General > Test Runner, or from the command line:
  `Unity -batchmode -nographics -projectPath unity -runTests -testPlatform EditMode -testResults results.xml`

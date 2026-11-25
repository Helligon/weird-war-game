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

Sections, Units, the Officer and the Fire buttons are drawn by `BattleStateView` (one square per Unit; yellow square = Officer). Stats and parameters come from `../data/stats.json` and `../data/parameters.json`.

The map and the Riflemen speed are read at run time from `../data/map.json` and `../data/stats.json` (metres; one metre is one Unity unit). Blue strip = west edge, red strip = east edge, green discs = cover, tan = open ground.

Third-person Officer (PCH-001): press Play and click once in the Game view to give the game your mouse. W/S walk forward/back along the aim, A/D step sideways, the mouse turns the aim (the camera sits behind and above the Officer), left click fires the stand-in hitscan rifle, Esc frees the mouse (to use the Debug buttons; a click on the top-left panel does not take it back, a click elsewhere does). Every input becomes a plain command (`OfficerMoveCommand`, `OfficerAimCommand`, `OfficerFireCommand`) made by the core's `OfficerInputMapper` and applied with `BattleState.ApplyOfficerCommand`; applied commands are listed in `BattleState.CommandLog` (PCH-009, DLV-013). It uses Unity's existing legacy Input Manager, so no package was added.

EditMode tests: Window > General > Test Runner, or `Unity -batchmode -nographics -projectPath unity -runTests -testPlatform EditMode -testResults results.xml`.

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

Sections, Units and the Officer are drawn by `BattleStateView` (one square per Unit; yellow square = Officer). Stats and parameters come from `../data/stats.json` and `../data/parameters.json`.

The map and the Riflemen speed are read at run time from `../data/map.json` and `../data/stats.json` (metres; one metre is one Unity unit). Blue strip = west edge, red strip = east edge, green discs = cover, tan = open ground.

Third-person Officer (PCH-001): press Play and click once in the Game view to give the game your mouse. The Officer only moves while the battle is running, so click "Run battle" (top-left panel) first. W/S walk forward/back along the aim, A/D step sideways, the mouse turns the aim (the camera sits behind and above the Officer), left click fires the stand-in hitscan rifle, Esc frees the mouse (a click on the top-left panel does not take it back, a click elsewhere does). The Pause and Speed (x1/x5/x20) buttons apply to the Officer too, because only `BattleState.Step()` moves anything (0.1 battle seconds per step). Every input becomes a plain command (`OfficerMoveCommand` sets the walking direction, `OfficerAimCommand`, `OfficerFireCommand`), made by the core's `OfficerInputMapper` only when the input changes, and applied with `BattleState.ApplyCommand`. Applied commands are listed with their step number in `BattleState.CommandLog` and replay with `BattleState.Replay` (PCH-009, DLV-013, TEC-008). It uses Unity's existing legacy Input Manager, so no package was added.

EditMode tests: Window > General > Test Runner, or `Unity -batchmode -nographics -projectPath unity -runTests -testPlatform EditMode -testResults results.xml`.

Tactical view (SP2-41): press Tab to enter or leave it. Entering pauses the battle (CMD-002) and gives a top-down camera and the command panel on the right; leaving resumes the battle and returns the third-person camera. Section commands are only possible inside it (CMD-001). Click a blue Section to select it (one at a time, CMD-016), click the map to place up to 2 waypoints (queued ones count, P-CMD-01), then "Confirm Move". Click a red Section to Target it. Stop, Clear waypoints and Fall back are buttons. Right click clears the selection. There is no ability trigger (CMD-013). The rules live in the core's `TacticalView`; `TacticalController` only reads input and draws. Every order is a command applied with `BattleState.ApplyCommand`. If you press "Run battle" while still in the tactical view, the Officer takes no player input and is flagged `TacticalView.OfficerUnderAi` (its AI behaviour is SP2-42).

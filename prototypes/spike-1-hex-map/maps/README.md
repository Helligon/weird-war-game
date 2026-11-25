# Hand-written maps (spike 1)

Stand-in for the map editor (DEC-087, exception to MAP-020). `sample.json` is the 30 columns x 50 rows spike map (MAP-019, P-MAP-04).

## Format

```json
{
  "width": 30,
  "height": 50,
  "default": "plain",
  "terrain": {
    "sea":      [[0,0], [1,0]],
    "mountain": [[20,10]],
    "river":    [[14,0], [14,1]]
  }
}
```

- `width` and `height`: columns and rows (read from the file, not hard-coded).
- `default`: terrain of every cell not listed.
- `terrain`: optional. Each key is a terrain type, each value a list of `[column, row]` cells. Columns and rows count from 0.
- Terrain types: `plain` (the only passable one), `mountain`, `sea`, `river` (TRN-024).
- Layout is flat-top, odd-q: odd columns sit half a hex lower.
- Loading fails with an error naming the cell if a terrain type is unknown, a cell is outside the size, or a cell is listed twice.

## Sample map

- Sea in the top-left and bottom-right corners.
- A river down column 14 from row 0 to row 34. A whole column of hexes blocks crossing, so tokens must go around its south end (rows 35 to 49).
- Three small mountain clusters.

## Scenario file (`sample-scenario.json`)

A separate file from the map, so one map can host several starts. Stand-ins from DEC-087.

```json
{
  "firstPlayer": 1,
  "startingBudget": 10,
  "tokens": { "1": [5, 20], "2": [24, 30] }
}
```

- `firstPlayer`: 1 or 2. Stand-in for the random first player (TRN-033).
- `startingBudget`: movement budget each token has at the start of its owner's turn (stand-in for SP; 10 comes from P-SUP-00's "10 hexes of movement").
- `tokens`: start `[column, row]` for Player 1 and Player 2.
- A new game is rejected (`SetupException`) if a token is off the map or on an impassable hex, both tokens share a hex, or they are adjacent.

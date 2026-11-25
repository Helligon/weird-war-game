# Hand-written maps (spike 1)

Stand-in for the map editor (DEC-087, exception to MAP-020). `sample.json` is the 20 columns x 12 rows landscape spike map (MAP-019 as revised by DEC-093: the real starting map is about 50 x 30, prototypes may be smaller).

## Format

```json
{
  "width": 20,
  "height": 12,
  "default": "plain",
  "terrain": {
    "sea":      [[0,0], [1,0]],
    "mountain": [[20,10]],
    "river":    [[9,0], [9,1]]
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

- Sea in the top-left and bottom-right corners (3 hexes each).
- A river down column 9 from row 0 to row 9. A whole column of hexes blocks crossing, so tokens must go around its south end: the gap is the two plain hexes (9,10) and (9,11), the only way across.
- Three small mountain clusters: (4,1), (5,1), (4,2); (14,8), (15,8), (14,9); (3,9), (4,9).

## Scenario file (`sample-scenario.json`)

A separate file from the map, so one map can host several starts. Stand-ins from DEC-087.

```json
{
  "firstPlayer": 1,
  "startingBudget": 10,
  "movementCostPerHex": 1,
  "tokens": { "1": [3, 4], "2": [16, 5] }
}
```

- `firstPlayer`: 1 or 2. Stand-in for the random first player (TRN-033).
- `startingBudget`: movement budget each token has at the start of its owner's turn (stand-in for SP; 10 comes from P-SUP-00's "10 hexes of movement").
- `movementCostPerHex`: required, whole number from 1 to 10000. There is no default. Budget spent for each hex a token enters, the same for every passable hex. P-SUP-09 (per-hex SP cost) is open, so this is data and not a constant, so the file must set it. A Move that costs more than the budget left is rejected.
- `tokens`: start `[column, row]` for Player 1 and Player 2.
- A new game is rejected (`SetupException`) if a token is off the map or on an impassable hex, both tokens share a hex, or they are adjacent.

## Moving (`MoveCommand`)

A `MoveCommand` holds the issuing player, the token's owner and the target hex. It moves the token one hex and spends `movementCostPerHex` at once. It is rejected, with the state unchanged, if the player is not the current player or does not own the token, the target is off the map or impassable, not adjacent, holds the other token, or is next to it ("contact: battles not in spike", DEC-087), or the budget left is less than the cost.

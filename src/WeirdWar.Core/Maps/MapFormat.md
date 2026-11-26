# Map JSON format (version 1)

Defined by agents under MED-006 (DEC-103) until the map editor is designed (MED-003). Imported by
`MapImporter.Parse` (MED-004): any tool or hand-written file that follows this page works.
The file is read with the core's own JSON reader (`WeirdWar.Core.Serialization.Json`).

## Example

```json
{
  "version": 1,
  "width": 6,
  "height": 4,
  "terrain": {
    "mountain": [[1, 1], [2, 1]],
    "sea": [[0, 0]],
    "river": [[4, 0], [4, 1], [4, 2], [4, 3]]
  }
}
```

## Fields

| Key | Type | Required | Meaning |
|---|---|---|---|
| `version` | whole number | yes | Format version. Must be `1`. A different number is an error. |
| `width` | whole number | yes | Hexes across (columns), 1 to 1000. |
| `height` | whole number | yes | Hexes down (rows), 1 to 1000. |
| `terrain` | object | no | Terrain type name to a list of `[column, row]` hexes. |

Any other top-level key is an error (it is never silently ignored). The next Story (CC1-68) adds
locations and the starting setup as new keys and raises the version.

The size is not fixed by the importer. The design target is 50 wide by 30 tall, landscape (MAP-019,
P-MAP-04); smaller maps are fine. The 1000 limit per side is only a safety check.

## Terrain types

Every hex not listed is `open`.

| Type | Passable for ground Brigades | Movement cost |
|---|---|---|
| `open` | yes | 1 step, the same on every open hex |
| `mountain` | no (TRN-024) | none |
| `sea` | no (TRN-024) | none |
| `river` | no (TRN-024) | none |

`open` may also be listed explicitly. Terrain costs for other types (SUP-011, TRN-025, TRN-026) are not
decided and are not part of this format.

## Coordinates

Hexes are `[column, row]`, both counted from 0, column 0 and row 0 at the top left. The layout is
flat-top hexes in the "odd-q" arrangement: odd columns sit half a hex lower. The core converts to axial
coordinates (`HexCoord`) for distance and neighbours. A hex has 6 neighbours, fewer at the map edge
(MAP-017). Each hex may be listed at most once across all terrain lists.

## Errors

Import throws `MapFormatException` with a message naming the problem, and returns no map. Cases:
invalid JSON (with line and column), top level not an object, unknown key, missing or non-numeric
`version`/`width`/`height`, unsupported version, size out of range, unknown terrain type, a bad hex
entry, a hex outside the grid, a hex listed twice.

## Determinism

The same file always gives the same map (MAP-018). Nothing is generated or randomised.

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
  },
  "factions": ["east", "west"],
  "locations": [
    {"id": "hq-east", "type": "hq", "at": [5, 2], "faction": "east"},
    {"id": "hq-west", "type": "hq", "at": [0, 2], "faction": "west"},
    {"id": "supply-1", "type": "supply", "at": [2, 0]},
    {"id": "command-1", "type": "command", "at": [3, 3]},
    {"id": "major-1", "type": "major", "at": [3, 2]}
  ],
  "setup": {
    "east": {"brigade": {"sections": ["infantry", "infantry", "artillery"]}},
    "west": {"brigade": {"sections": ["infantry", "infantry", "recon"]}}
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
| `factions` | list of text | no | Exactly two Faction ids, or leave out for a terrain-only map (no game can start on it). |
| `locations` | list of objects | no | HQs and STs, see below. |
| `setup` | object | with `factions` | The Sections of each Faction's starting Brigade, see below. |

Any other top-level key is an error (it is never silently ignored). The format stays on `version: 1` and
is extended in place until a map is handed to the design owner to edit; after that, incompatible changes
bump the version. Ids (Factions, locations, Section types) use letters, digits, `-` and `_` only.

The JSON is strict: no comments, no trailing commas, each key appears once (so a terrain type cannot be
split over two lists), and sizes and coordinates are whole numbers (`1.0` is rejected).

Sample maps to start from: `src/WeirdWar.Core.Tests/data/maps/sample-20x12.json` and `sample-50x30.json`.

The size is not fixed by the importer. The design target is 50 wide by 30 tall, landscape (MAP-019,
P-MAP-04); smaller maps are fine. The 1000 limit per side is only a safety check.

## Terrain types

Every hex not listed is `open`.

| Type | Passable for ground Brigades | Movement cost |
|---|---|---|
| `open` | yes | terrain factor 1, the same on every open hex (SP per hex comes from the Brigade, SUP-021) |
| `mountain` | no (TRN-024) | none |
| `sea` | no (TRN-024) | none |
| `river` | no (TRN-024) | none |

`open` may also be listed explicitly. Terrain costs for other types (SUP-011, TRN-025, TRN-026) are not
decided and are not part of this format.

## Locations (MAP-003, MAP-015, MAP-022)

Each entry in `locations` has `id` (unique), `type`, `at` (`[column, row]`, on the grid) and, for an HQ only,
`faction`. Any other key is an error.

| Type | Meaning |
|---|---|
| `hq` | A Faction's HQ. Needs `faction`. Each Faction has exactly one HQ: zero or two is an error. |
| `supply` | Supply ST. There is no separate "supply depot" type. |
| `command` | Command ST. |
| `major` | Major ST. |

Any other type name is an error. An ST must not have a `faction`: every ST starts neutral and nothing
else is owned at the start but the HQs (MAP-021). FOBs are built in play and are never in a map file.

## Starting setup (MAP-021)

`setup` has one entry per Faction, with `brigade`, which holds `sections`, a list of 1 to 5 Section type ids
(BRG-021). The Sections are named, not rolled. `setup` carries no numbers: the starting SP reserve, the starting CP
and the starting Brigade's SP come from P-RES-05, P-RES-06 and P-BRG-09 in `Data/parameters.txt` (RES-013, BRG-036),
and a map cannot override them. A `reserve` or `sp` key is an unknown key and is rejected. Section type ids are checked against
the Section type data (`Data/section-types.json`) when a game starts, not at import. The list must then have exactly P-BRG-05 entries (3), and the Brigade starts on its Faction's HQ hex.

## Coordinates

Hexes are `[column, row]`, both counted from 0, column 0 and row 0 at the top left. The layout is
flat-top hexes in the "odd-q" arrangement: odd columns sit half a hex lower. The core converts to axial
coordinates (`HexCoord`) for distance and neighbours. A hex has 6 neighbours, fewer at the map edge
(MAP-017). Each hex may be listed at most once, within a list or across lists.

Neighbours of `[column, row]`, as `[column, row]` offsets:

| Column | Neighbours |
|---|---|
| even | `[+1,-1]` `[+1,0]` `[0,+1]` `[-1,0]` `[-1,-1]` `[0,-1]` |
| odd | `[+1,0]` `[+1,+1]` `[0,+1]` `[-1,+1]` `[-1,0]` `[0,-1]` |

For example `[4,5]` (even) touches `[5,4]` `[5,5]` `[4,6]` `[3,5]` `[3,4]` `[4,4]`, and `[5,5]` (odd)
touches `[6,5]` `[6,6]` `[5,6]` `[4,6]` `[4,5]` `[5,4]`.

## Errors

Import throws `MapFormatException` with a message naming the problem, and returns no map. Cases:
invalid JSON (with line and column), top level not an object, unknown key, missing or non-numeric
`version`/`width`/`height`, unsupported version, size out of range, unknown terrain type, a bad hex
entry, a hex outside the grid, a hex listed twice, a Faction count other than two, a Faction without
exactly one HQ, an unknown location type, a duplicate location id, a location outside the grid, an ST with
a faction, and a missing or unknown entry in `setup`.

## Determinism

The same file always gives the same map (MAP-018). Nothing is generated or randomised.

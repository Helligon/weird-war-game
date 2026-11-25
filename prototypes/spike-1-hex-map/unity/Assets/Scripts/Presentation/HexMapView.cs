using UnityEngine;
using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Presentation
{
    /// <summary>
    /// Draws a core <see cref="HexMap"/> as one generated mesh with a flat colour per hex (MAP-017, VIS-006).
    /// Read-only: it never changes the map.
    /// </summary>
    public sealed class HexMapView : MonoBehaviour
    {
        /// <summary>Hex size scale; drawn hexes are shrunk a little so the grid lines show.</summary>
        public const float HexSize = 1f;
        public const float DrawScale = 0.94f;

        public int TileCount { get; private set; }

        /// <summary>Centre of every drawn tile, indexed row * width + column (same order as the core's cells).</summary>
        public Vector3[] TileCentres { get; private set; } = new Vector3[0];

        public Bounds WorldBounds { get; private set; }

        public void Build(HexMap map)
        {
            var count = map.Width * map.Height;
            var vertices = new Vector3[count * 7];
            var colours = new Color[count * 7];
            var triangles = new int[count * 18];
            TileCentres = new Vector3[count];

            for (var row = 0; row < map.Height; row++)
            {
                for (var col = 0; col < map.Width; col++)
                {
                    var tile = row * map.Width + col;
                    var hex = HexCoord.FromOffset(new OffsetCoord(col, row));
                    var centre = HexLayout.HexToWorld(hex, HexSize);
                    var colour = TerrainColours.For(map.TerrainAt(hex));
                    TileCentres[tile] = centre;

                    var v = tile * 7;
                    vertices[v] = centre;
                    colours[v] = colour;
                    for (var i = 0; i < 6; i++)
                    {
                        vertices[v + 1 + i] = centre + HexLayout.Corner(i, HexSize * DrawScale);
                        colours[v + 1 + i] = colour;
                    }

                    var t = tile * 18;
                    for (var i = 0; i < 6; i++)
                    {
                        // Clockwise as seen from the camera (looking along +Z), so the faces point at it.
                        triangles[t + i * 3] = v;
                        triangles[t + i * 3 + 1] = v + 1 + (i + 1) % 6;
                        triangles[t + i * 3 + 2] = v + 1 + i;
                    }
                }
            }

            var mesh = new Mesh { name = "HexMap", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.colors = colours;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            if (!TryGetComponent<MeshFilter>(out var filter)) { filter = gameObject.AddComponent<MeshFilter>(); }
            if (!TryGetComponent<MeshRenderer>(out var meshRenderer)) { meshRenderer = gameObject.AddComponent<MeshRenderer>(); }
            filter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default")) { name = "HexMapFlat" };

            TileCount = count;
            WorldBounds = mesh.bounds;
        }
    }
}

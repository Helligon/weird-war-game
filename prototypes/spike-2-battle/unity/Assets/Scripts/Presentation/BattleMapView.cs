using System.Collections.Generic;
using UnityEngine;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Presentation
{
    /// <summary>
    /// Draws the core's <see cref="BattleMap"/> in flat greybox colours (BMP-004, BMP-005): open ground, a blue west edge,
    /// a red east edge and green cover discs. Read-only: it never changes the map. One metre is one Unity unit.
    /// </summary>
    public sealed class BattleMapView : MonoBehaviour
    {
        public static readonly Color GroundColour = new Color(0.55f, 0.52f, 0.42f);
        public static readonly Color WestEdgeColour = new Color(0.15f, 0.35f, 0.9f);
        public static readonly Color EastEdgeColour = new Color(0.85f, 0.15f, 0.15f);
        public static readonly Color CoverColour = new Color(0.1f, 0.4f, 0.15f);

        /// <summary>How wide the coloured edge strips are drawn, in metres (inside the map).</summary>
        public const float EdgeStripWidth = 3f;
        private const int DiscSegments = 24;

        public Bounds WorldBounds { get; private set; }
        public int CoverCount { get; private set; }
        public Transform Ground { get; private set; }
        public Transform WestEdge { get; private set; }
        public Transform EastEdge { get; private set; }
        public IReadOnlyList<Transform> CoverObjects => _cover;

        private readonly List<Transform> _cover = new List<Transform>();

        public void Build(BattleMap map)
        {
            for (var i = transform.childCount - 1; i >= 0; i--) { DestroyImmediate(transform.GetChild(i).gameObject); }
            _cover.Clear();

            var w = (float)map.Width;
            var h = (float)map.Height;
            Ground = Layer("Ground", 0f, GroundColour, Rect(0f, 0f, w, h));
            WestEdge = Layer("West edge", -0.1f, WestEdgeColour, Rect(0f, 0f, EdgeStripWidth, h));
            EastEdge = Layer("East edge", -0.1f, EastEdgeColour, Rect(w - EdgeStripWidth, 0f, w, h));

            foreach (var c in map.CoverObjects)
            {
                var disc = Layer("Cover " + c.Id, -0.2f, CoverColour, Disc((float)c.Position.X, (float)c.Position.Y, (float)c.Radius));
                _cover.Add(disc);
            }
            CoverCount = _cover.Count;
            WorldBounds = new Bounds(new Vector3(w / 2f, h / 2f, 0f), new Vector3(w, h, 0f));
        }

        private Transform Layer(string name, float z, Color colour, (Vector3[] vertices, int[] triangles) shape)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            var mesh = new Mesh { name = name, vertices = shape.vertices, triangles = shape.triangles };
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var material = new Material(Shader.Find("Sprites/Default")) { name = name, color = colour };
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }

        // Triangles are clockwise as seen from the camera (looking along +Z), so the faces point at it.
        private static (Vector3[], int[]) Rect(float x0, float y0, float x1, float y1) =>
            (new[] { new Vector3(x0, y0), new Vector3(x0, y1), new Vector3(x1, y1), new Vector3(x1, y0) },
             new[] { 0, 1, 2, 0, 2, 3 });

        private static (Vector3[], int[]) Disc(float cx, float cy, float r)
        {
            var vertices = new Vector3[DiscSegments + 1];
            var triangles = new int[DiscSegments * 3];
            vertices[0] = new Vector3(cx, cy);
            for (var i = 0; i < DiscSegments; i++)
            {
                var a = i * Mathf.PI * 2f / DiscSegments;
                vertices[i + 1] = new Vector3(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % DiscSegments + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            return (vertices, triangles);
        }
    }
}

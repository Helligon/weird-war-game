using UnityEngine;
using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Presentation
{
    /// <summary>
    /// Draws both Brigade tokens as flat coloured shapes (VIS-006: no art) and a highlight ring behind the selected one.
    /// Read-only: <see cref="Render"/> redraws from a core state and never changes it.
    /// </summary>
    public sealed class TokenView : MonoBehaviour
    {
        public static readonly Color Player1Colour = new Color(0.85f, 0.15f, 0.15f);
        public static readonly Color Player2Colour = new Color(0.15f, 0.25f, 0.85f);
        public static readonly Color HighlightColour = new Color(1f, 0.92f, 0.1f);

        private const float TokenRadius = 0.6f;
        private const float RingRadius = 0.85f;
        private const float TokenZ = -1f; // nearer the camera than the map at z 0

        private Transform _p1, _p2, _ring;

        public static Color ColourOf(PlayerId player) => player == PlayerId.Player1 ? Player1Colour : Player2Colour;

        /// <summary>World position the token of this player is drawn at (or null if not built yet).</summary>
        public Vector3? PositionOf(PlayerId player)
        {
            var t = player == PlayerId.Player1 ? _p1 : _p2;
            return t == null ? (Vector3?)null : t.position;
        }

        public bool RingVisible => _ring != null && _ring.gameObject.activeSelf;
        public Vector3 RingPosition => _ring.position;

        public void Render(GameState state, PlayerId? selected)
        {
            if (_p1 == null)
            {
                _ring = Shape("SelectionRing", 24, RingRadius, HighlightColour, -0.5f);
                _p1 = Shape("Token P1", 24, TokenRadius, Player1Colour, TokenZ); // circle
                _p2 = Shape("Token P2", 4, TokenRadius * 1.2f, Player2Colour, TokenZ); // diamond, so shape differs too
            }
            _p1.position = Place(state.TokenOf(PlayerId.Player1).Position);
            _p2.position = Place(state.TokenOf(PlayerId.Player2).Position);
            _ring.gameObject.SetActive(selected.HasValue);
            if (selected.HasValue)
            {
                var p = Place(state.TokenOf(selected.Value).Position);
                _ring.position = new Vector3(p.x, p.y, -0.5f);
            }
        }

        private static Vector3 Place(HexCoord hex)
        {
            var w = HexLayout.HexToWorld(hex, HexMapView.HexSize);
            return new Vector3(w.x, w.y, TokenZ);
        }

        private Transform Shape(string name, int sides, float radius, Color colour, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var vertices = new Vector3[sides + 1];
            var triangles = new int[sides * 3];
            for (var i = 0; i < sides; i++)
            {
                var a = Mathf.PI * 2f * i / sides;
                vertices[i + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = 1 + (i + 1) % sides;
                triangles[i * 3 + 2] = 1 + i;
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var material = new Material(Shader.Find("Sprites/Default")) { color = colour };
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.transform.position = new Vector3(0f, 0f, z);
            return go.transform;
        }
    }
}

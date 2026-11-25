using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Presentation
{
    /// <summary>
    /// Draws the core's Sections (one square per Unit, HIE-004) and the Officer (a yellow square, HIE-006), and shows
    /// health and Unit counts. Read-only apart from the demo "Fire" button, which asks the core to resolve a volley
    /// (placeholder fire; the seed is the volley number, so the result is repeatable, TEC-008). One metre is one Unity unit.
    /// </summary>
    public sealed class BattleStateView : MonoBehaviour
    {
        public static readonly Color PlayerColour = new Color(0.4f, 0.65f, 1f);
        public static readonly Color EnemyColour = new Color(1f, 0.45f, 0.4f);
        public static readonly Color OfficerColour = new Color(1f, 0.9f, 0.2f);

        private const int Columns = 4;
        private const float Spacing = 1.3f;
        private const float Size = 1f;

        /// <summary>The top-left panel with the stats and the Debug buttons, in screen pixels from the top-left corner.</summary>
        public static readonly Rect DebugPanel = new Rect(10, 10, 520, 145);

        private BattleState _state;
        private BattleBootstrap _bootstrap;
        private readonly Dictionary<int, List<GameObject>> _unitSquares = new Dictionary<int, List<GameObject>>();
        private readonly List<GameObject> _officerSquares = new List<GameObject>();
        private Transform _root;
        private int _volley;
        private Mesh _squareMesh;
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();
        private string _lastFire = "No shots fired yet.";

        /// <summary>The number of Unit squares currently drawn for a Section.</summary>
        public int SquaresFor(SectionId id) => _unitSquares.TryGetValue(id.Value, out var l) ? l.Count : 0;
        public int OfficerSquares => _officerSquares.Count;

        public void Show(BattleState state, BattleBootstrap bootstrap)
        {
            _state = state;
            _bootstrap = bootstrap;
            _volley = 0;
            _lastFire = "No shots fired yet.";
            if (_root != null) { DestroyImmediate(_root.gameObject); }
            ReleaseShared();
            _unitSquares.Clear();
            _officerSquares.Clear();
            _root = new GameObject("Sections").transform;
            _root.SetParent(transform, false);
            _root.localPosition = new Vector3(0f, 0f, -1f);
            foreach (var o in state.Officers)
            {
                var square = Square("Officer", new Vector3((float)o.Position.X, (float)o.Position.Y, 0f), OfficerColour);
                // A thin dark bar from the centre shows where the rifle points (PCH-001).
                var bar = Square("Rifle", new Vector3(1f, 0f, -0.1f), Color.black);
                bar.transform.SetParent(square.transform, false);
                bar.transform.localPosition = new Vector3(1f, 0f, -0.1f);
                bar.transform.localScale = new Vector3(2f, 0.2f, 1f);
                _officerSquares.Add(square);
            }
            Refresh();
        }

        // Read-only: the squares follow the core's Officer state every frame.
        private void LateUpdate()
        {
            if (_state == null) { return; }
            for (var i = 0; i < _officerSquares.Count && i < _state.Officers.Count; i++)
            {
                var o = _state.Officers[i];
                _officerSquares[i].transform.localPosition = new Vector3((float)o.Position.X, (float)o.Position.Y, 0f);
                _officerSquares[i].transform.localRotation = Quaternion.Euler(0f, 0f, (float)(o.AimRadians * Mathf.Rad2Deg));
            }
        }

        /// <summary>Redraws one square per remaining Unit, so losing health visibly removes Units.</summary>
        public void Refresh()
        {
            foreach (var section in _state.Sections)
            {
                if (_unitSquares.TryGetValue(section.Id.Value, out var old)) { foreach (var g in old) { DestroyImmediate(g); } }
                var list = new List<GameObject>();
                var colour = section.Side == Side.Player ? PlayerColour : EnemyColour;
                var rows = (section.MaxUnits + Columns - 1) / Columns;
                for (var i = 0; i < section.UnitCount; i++)
                {
                    var x = (float)section.Position.X + (i % Columns - (Columns - 1) / 2f) * Spacing;
                    var y = (float)section.Position.Y + ((rows - 1) / 2f - i / Columns) * Spacing;
                    list.Add(Square("Unit", new Vector3(x, y, 0f), colour));
                }
                _unitSquares[section.Id.Value] = list;
            }
        }

        private GameObject Square(string name, Vector3 position, Color colour)
        {
            if (_squareMesh == null)
            {
                var h = Size / 2f;
                _squareMesh = new Mesh
                {
                    name = "Unit square",
                    vertices = new[] { new Vector3(-h, -h), new Vector3(-h, h), new Vector3(h, h), new Vector3(h, -h) },
                    triangles = new[] { 0, 1, 2, 0, 2, 3 },
                };
            }
            if (!_materials.TryGetValue(colour, out var material))
            {
                material = new Material(Shader.Find("Sprites/Default")) { color = colour };
                _materials[colour] = material;
            }
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = position;
            go.AddComponent<MeshFilter>().sharedMesh = _squareMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        // One mesh and one material per colour are shared by every square; destroy them with the view.
        private void ReleaseShared()
        {
            if (_squareMesh != null) { DestroyImmediate(_squareMesh); _squareMesh = null; }
            foreach (var m in _materials.Values) { if (m != null) { DestroyImmediate(m); } }
            _materials.Clear();
        }

        private void OnDestroy() { ReleaseShared(); }

        private void OnGUI()
        {
            if (_state == null) { return; }
            var sb = new StringBuilder();
            foreach (var s in _state.Sections)
            {
                sb.AppendLine(s.Side + " Riflemen: " + s.UnitCount + " of " + s.MaxUnits + " Units, health " + s.Health
                    + " of " + s.MaxUnits * s.HealthPerUnit + (s.IsDestroyed ? " (destroyed)" : ""));
            }
            foreach (var o in _state.Officers) { sb.AppendLine(o.Side + " Officer: health " + o.Health + " (not in a Section), at " + o.Position.X.ToString("0") + ", " + o.Position.Y.ToString("0")); }
            sb.AppendLine("Hit chance standing still: " + _state.HitChance(false).ToString("0.00")
                + ", moving: " + _state.HitChance(true).ToString("0.00"));
            sb.AppendLine(_lastFire);

            GUI.Box(new Rect(10, 10, 520, 110), GUIContent.none);
            GUI.Label(new Rect(18, 14, 505, 100), sb.ToString());
            // TODO(#40, DLV-013): these Debug buttons change battle state outside a command. Remove them, or route them
            // through a command, once Story #40 applies TargetCommand (CMD-008).
            if (GUI.Button(new Rect(10, 125, 270, 28), "Debug: fire player Section at enemy")) { Fire(false); }
            if (GUI.Button(new Rect(285, 125, 130, 28), "Debug: fire moving")) { Fire(true); }
            if (GUI.Button(new Rect(420, 125, 110, 28), "Debug: reset")) { _bootstrap.Load(); }
        }

        private void Fire(bool moving)
        {
            var target = _state.Find(BattleState.EnemySectionId);
            var before = target.Health;
            _volley++;
            var r = _state.Fire(BattleState.PlayerSectionId, target.Id, moving);
            var what = moving ? " (moving)" : "";
            switch (r.Outcome)
            {
                case FireOutcome.Fired:
                    _lastFire = "Volley " + _volley + what + ": " + r.Hits + " hits of " + r.Shots + " shots, " + (before - target.Health) + " damage";
                    break;
                case FireOutcome.OutOfRange: _lastFire = "Volley " + _volley + what + ": target out of range"; break;
                case FireOutcome.TargetDestroyed: _lastFire = "Volley " + _volley + what + ": target already destroyed"; break;
                default: _lastFire = "Volley " + _volley + what + ": shooter destroyed"; break;
            }
            Refresh();
        }
    }
}

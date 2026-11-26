using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Presentation
{
    /// <summary>
    /// Draws the core's Sections (one square per Unit, HIE-004) and the Officer (a yellow square, HIE-006), and shows
    /// health and Unit counts. It changes nothing itself. Section orders come from the tactical view (<see cref="TacticalController"/>),
    /// which applies them with <see cref="BattleState.ApplyCommand"/> (DLV-013). One metre is one Unity unit.
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
        public static readonly Rect DebugPanel = new Rect(10, 10, 520, 165);

        private BattleState _state;
        private BattleBootstrap _bootstrap;
        private readonly Dictionary<int, List<GameObject>> _unitSquares = new Dictionary<int, List<GameObject>>();
        private readonly List<GameObject> _officerSquares = new List<GameObject>();
        private Transform _root;
        private Mesh _squareMesh;
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();
        // Default behaviour (DAI-001..006) runs in fixed core steps (BattleState.StepSeconds) so the battle does not depend on the frame rate.
        private static readonly int[] SpeedChoices = { 1, 5, 20 };
        private int _speedIndex;
        private double _carry;
        /// <summary>Whether the battle is running. The tactical view owns the flag (CMD-002): entering it pauses, leaving it resumes.</summary>
        public bool Running => _bootstrap != null && _bootstrap.Tactical != null && _bootstrap.Tactical.Running;
        /// <summary>The battle speed choice: 1, 5 or 20 battle seconds per real second. It applies to everything the core steps, the Officer too.</summary>
        public int SpeedMultiplier => SpeedChoices[_speedIndex];
        public void SetRunning(bool running) { _bootstrap.Tactical.SetRunning(running); }
        public void CycleSpeed() { _speedIndex = (_speedIndex + 1) % SpeedChoices.Length; }

        /// <summary>The number of Unit squares currently drawn for a Section.</summary>
        public int SquaresFor(SectionId id) => _unitSquares.TryGetValue(id.Value, out var l) ? l.Count : 0;
        public int OfficerSquares => _officerSquares.Count;

        public void Show(BattleState state, BattleBootstrap bootstrap)
        {
            _state = state;
            _bootstrap = bootstrap;
            _carry = 0;
            if (_root != null) { DestroyImmediate(_root.gameObject); }
            ReleaseShared();
            _unitSquares.Clear();
            _officerSquares.Clear();
            _root = new GameObject("Sections").transform;
            _root.SetParent(transform, false);
            _root.localPosition = new Vector3(0f, 0f, -1f);
            foreach (var o in state.Officers)
            {
                var square = Square("Officer", new Vector3((float)o.Position.X, (float)o.Position.Y, 0f), OfficerColour, BattleMapView.OrderOfficer);
                // A thin dark bar from the centre shows where the rifle points (PCH-001).
                var bar = Square("Rifle", new Vector3(1f, 0f, -0.1f), Color.black, BattleMapView.OrderRifle);
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
                _officerSquares[i].SetActive(!o.IsDestroyed); // PCH-006: a dead Officer is no longer drawn
                _officerSquares[i].transform.localPosition = new Vector3((float)o.Position.X, (float)o.Position.Y, 0f);
                _officerSquares[i].transform.localRotation = Quaternion.Euler(0f, 0f, (float)(o.AimRadians * Mathf.Rad2Deg));
            }
        }

        /// <summary>Runs the battle for the given real seconds (times the speed choice), in fixed core steps (BattleState.StepSeconds), then redraws.</summary>
        public void Advance(double realSeconds)
        {
            _carry += realSeconds * SpeedChoices[_speedIndex];
            var stepped = false;
            while (_carry >= BattleState.StepSeconds) { _state.Step(); _carry -= BattleState.StepSeconds; stepped = true; }
            if (stepped) { Refresh(); }
        }

        /// <summary>One frame: the battle advances only while running (not paused).</summary>
        public void Frame(double realSeconds)
        {
            if (_state != null && Running) { Advance(realSeconds); }
        }

        private void Update() { Frame(Time.deltaTime); }

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
                    list.Add(Square("Unit", new Vector3(x, y, 0f), colour, BattleMapView.OrderUnits));
                }
                _unitSquares[section.Id.Value] = list;
            }
        }

        private GameObject Square(string name, Vector3 position, Color colour, int order)
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
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
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
                    + " of " + s.MaxUnits * s.HealthPerUnit + (s.IsDestroyed ? " (destroyed)" : "") + " | " + OrdersText(s));
            }
            foreach (var o in _state.Officers) { sb.AppendLine(o.Side + " Officer: health " + o.Health + " (not in a Section), at " + o.Position.X.ToString("0") + ", " + o.Position.Y.ToString("0")); }
            sb.AppendLine("Hit chance standing still: " + _state.HitChance(false).ToString("0.00")
                + ", moving: " + _state.HitChance(true).ToString("0.00"));
            sb.AppendLine("View: " + ViewText());

            GUI.Box(DebugPanel, GUIContent.none);
            GUI.Label(new Rect(18, 14, 505, 110), sb.ToString());
            // Default behaviour: Sections with no orders move, take cover and fire on their own. Time is not a player action.
            if (GUI.Button(new Rect(10, 130, 270, 28), Running ? "Pause battle" : "Run battle (default behaviour)")) { SetRunning(!Running); }
            if (GUI.Button(new Rect(285, 130, 130, 28), "Speed x" + SpeedChoices[_speedIndex])) { CycleSpeed(); }
            if (GUI.Button(new Rect(420, 130, 110, 28), "Reset battle")) { _bootstrap.Load(); return; }
        }

        private string ViewText()
        {
            var t = _bootstrap.Tactical;
            if (!t.Active) return "third person (Tab: tactical view)";
            return "TACTICAL" + (t.Running ? ", running, Officer under AI control" : ", paused");
        }

        private static string OrdersText(Section s)
        {
            var parts = new List<string>();
            if (s.Waypoints.Count > 0) parts.Add(s.Waypoints.Count + " waypoint(s)" + (s.StopAtEnd ? " then Stop" : ""));
            if (s.IsHolding) parts.Add("holding");
            if (s.OrderedTarget.HasValue) parts.Add("target " + s.OrderedTarget.Value);
            parts.Add("at " + s.Position.X.ToString("0") + ", " + s.Position.Y.ToString("0"));
            return string.Join(", ", parts);
        }
    }
}

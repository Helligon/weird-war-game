using System.Collections.Generic;
using UnityEngine;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Presentation
{
    /// <summary>
    /// The tactical view (CMD-001): a top-down camera and the command UI. Tab enters and leaves it. All the rules live in the
    /// core's <see cref="TacticalView"/>; this class only reads the mouse and keyboard, draws, and calls it. It never changes the
    /// battle itself: every order is a command applied by <see cref="TacticalView"/> through <see cref="BattleState.ApplyCommand"/>
    /// (DLV-013). Entering pauses the battle by stopping the clock (<see cref="BattleStateView.Frame"/> checks the flag), and
    /// creates no command (CMD-002). Uses Unity's legacy Input Manager, so no package was added.
    /// </summary>
    public sealed class TacticalController : MonoBehaviour
    {
        /// <summary>The key that enters and leaves the tactical view.</summary>
        public KeyCode ToggleKey = KeyCode.Tab;
        /// <summary>The camera to move. When empty, the scene's main camera.</summary>
        public Camera TargetCamera;

        private static readonly Color SelectionColour = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color QueuedColour = new Color(0.4f, 0.9f, 1f, 0.9f);
        private static readonly Color PlannedColour = new Color(1f, 1f, 1f, 0.95f);

        private BattleBootstrap _bootstrap;
        private Camera _camera;
        private Transform _overlay;
        private Mesh _quadMesh;
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();
        private GameObject _selection;
        private LineRenderer _queuedLine;
        private LineRenderer _plannedLine;
        private readonly List<GameObject> _markers = new List<GameObject>();

        /// <summary>The right-hand command panel, in screen pixels from the top-left corner.</summary>
        public static Rect Panel => new Rect(Screen.width - 270, 10, 260, 330);

        public bool Active => _bootstrap != null && _bootstrap.Tactical != null && _bootstrap.Tactical.Active;

        public void Attach(BattleBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
            _camera = TargetCamera != null ? TargetCamera : Camera.main;
            ReleaseOverlay();
            _overlay = new GameObject("Tactical overlay").transform;
            _overlay.SetParent(transform, false);
            _overlay.localPosition = new Vector3(0f, 0f, -1.5f);
            _selection = Quad("Selection", SelectionColour, BattleMapView.OrderSelection);
            _queuedLine = Line("Queued path", QueuedColour);
            _plannedLine = Line("Planned path", PlannedColour);
            _overlay.gameObject.SetActive(false);
        }

        /// <summary>Tab: enter the tactical view (pauses the battle) or leave it (resumes).</summary>
        public void Toggle()
        {
            var t = _bootstrap.Tactical;
            if (!t.Active)
            {
                t.Enter();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _camera = TargetCamera != null ? TargetCamera : Camera.main;
                if (_camera != null) { BattleBootstrap.FitCamera(_camera, _bootstrap.View.WorldBounds); _camera.transform.rotation = Quaternion.identity; }
            }
            else
            {
                t.Exit();
                if (_bootstrap.Officer != null) { _bootstrap.Officer.UseThirdPersonCamera(); }
                else if (_camera != null) { BattleBootstrap.FitCamera(_camera, _bootstrap.View.WorldBounds); _camera.transform.rotation = Quaternion.identity; }
            }
            _overlay.gameObject.SetActive(t.Active);
        }

        /// <summary>A left click at a point of the map (metres): selects, targets or places a waypoint, as the core decides.</summary>
        public bool HandleClick(Vector2 world)
        {
            return _bootstrap.Tactical.Click(new MapPoint(world.x, world.y));
        }

        private void Update()
        {
            if (_bootstrap == null || _bootstrap.State == null) { return; }
            if (Input.GetKeyDown(ToggleKey)) { Toggle(); return; }
            if (!Active || _camera == null) { return; }
            if (Input.GetMouseButtonDown(0) && !OverPanel(Input.mousePosition))
            {
                var p = _camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -_camera.transform.position.z));
                HandleClick(new Vector2(p.x, p.y));
            }
            if (Input.GetMouseButtonDown(1) && !OverPanel(Input.mousePosition)) { _bootstrap.Tactical.Deselect(); }
        }

        /// <summary>True when a screen position (pixels from the bottom-left, as Input.mousePosition) is on one of the panels.</summary>
        public static bool OverPanel(Vector2 mousePosition)
        {
            var p = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            return Panel.Contains(p) || BattleStateView.DebugPanel.Contains(p);
        }

        private void LateUpdate()
        {
            if (!Active) { return; }
            var t = _bootstrap.Tactical;
            var selected = t.Selected;
            _selection.SetActive(selected.HasValue);
            var queuedPoints = new List<Vector3>();
            var plannedPoints = new List<Vector3>();
            if (selected.HasValue)
            {
                var s = _bootstrap.State.Find(selected.Value);
                var start = new Vector3((float)s.Position.X, (float)s.Position.Y, 0f);
                _selection.transform.localPosition = start;
                _selection.transform.localScale = new Vector3(7f, 4.5f, 1f);
                queuedPoints.Add(start);
                foreach (var w in s.Waypoints) { queuedPoints.Add(new Vector3((float)w.X, (float)w.Y, 0f)); }
                plannedPoints.Add(queuedPoints[queuedPoints.Count - 1]);
                foreach (var w in t.PendingWaypoints) { plannedPoints.Add(new Vector3(w.Q, w.R, 0f)); }
            }
            SetLine(_queuedLine, queuedPoints);
            SetLine(_plannedLine, plannedPoints);
            // One marker per queued waypoint (blue) and per planned waypoint (white).
            var markerPositions = new List<(Vector3, Color)>();
            for (var i = 1; i < queuedPoints.Count; i++) { markerPositions.Add((queuedPoints[i], QueuedColour)); }
            for (var i = 1; i < plannedPoints.Count; i++) { markerPositions.Add((plannedPoints[i], PlannedColour)); }
            while (_markers.Count < markerPositions.Count) { _markers.Add(Quad("Waypoint marker", Color.white, BattleMapView.OrderWaypoints)); }
            for (var i = 0; i < _markers.Count; i++)
            {
                var on = i < markerPositions.Count;
                _markers[i].SetActive(on);
                if (!on) { continue; }
                _markers[i].transform.localPosition = markerPositions[i].Item1;
                _markers[i].transform.localScale = new Vector3(1.6f, 1.6f, 1f);
                _markers[i].GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(markerPositions[i].Item2);
            }
        }

        private static void SetLine(LineRenderer line, List<Vector3> points)
        {
            line.enabled = points.Count >= 2;
            line.positionCount = points.Count;
            for (var i = 0; i < points.Count; i++) { line.SetPosition(i, points[i]); }
        }

        private Material MaterialFor(Color colour)
        {
            if (!_materials.TryGetValue(colour, out var m))
            {
                m = new Material(Shader.Find("Sprites/Default")) { color = colour };
                _materials[colour] = m;
            }
            return m;
        }

        private GameObject Quad(string name, Color colour, int order)
        {
            if (_quadMesh == null)
            {
                _quadMesh = new Mesh
                {
                    name = "Tactical quad",
                    vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f), new Vector3(0.5f, -0.5f) },
                    triangles = new[] { 0, 1, 2, 0, 2, 3 },
                };
            }
            var go = new GameObject(name);
            go.transform.SetParent(_overlay, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quadMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = MaterialFor(colour);
            r.sortingOrder = order;
            return go;
        }

        private LineRenderer Line(string name, Color colour)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(_overlay, false);
            line.useWorldSpace = false;
            line.sharedMaterial = MaterialFor(colour);
            line.startColor = line.endColor = colour;
            line.sortingOrder = BattleMapView.OrderWaypoints;
            line.startWidth = line.endWidth = 0.4f;
            line.positionCount = 0;
            return line;
        }

        private void ReleaseOverlay()
        {
            if (_overlay != null) { DestroyImmediate(_overlay.gameObject); }
            _markers.Clear();
            if (_quadMesh != null) { DestroyImmediate(_quadMesh); _quadMesh = null; }
            foreach (var m in _materials.Values) { if (m != null) { DestroyImmediate(m); } }
            _materials.Clear();
        }

        private void OnDestroy() { ReleaseOverlay(); }

        // CMD-001: the command panel is drawn only inside the tactical view. There is no ability button (CMD-013).
        private void OnGUI()
        {
            if (!Active) { return; }
            var t = _bootstrap.Tactical;
            var selected = t.Selected;
            GUI.Box(Panel, GUIContent.none);
            var x = Panel.x + 8;
            var w = Panel.width - 16;
            var y = Panel.y + 6;
            GUI.Label(new Rect(x, y, w, 20), "TACTICAL VIEW  (" + ToggleKey + " to leave)");
            y += 22;
            string what;
            if (selected.HasValue)
            {
                var s = _bootstrap.State.Find(selected.Value);
                what = "Selected: " + selected.Value + " (" + s.UnitCount + " of " + s.MaxUnits + " Units)\n"
                    + "Waypoints: " + s.Waypoints.Count + " queued, " + t.PendingWaypoints.Count + " planned, " + t.WaypointsLeft
                    + " left (limit " + _bootstrap.State.Parameters.MaxWaypoints + ", P-CMD-01)";
            }
            else { what = "Click a blue Section to select it.\nRight click clears the selection."; }
            GUI.Label(new Rect(x, y, w, 52), what);
            y += 56;
            GUI.Label(new Rect(x, y, w, 36), "Click the map: place a waypoint.\nClick a red Section: Target it.");
            y += 40;
            if (GUI.Button(new Rect(x, y, w, 28), "Confirm Move (" + t.PendingWaypoints.Count + " waypoint(s))")) { t.ConfirmMove(); }
            y += 32;
            if (GUI.Button(new Rect(x, y, w, 28), "Cancel planned waypoints")) { t.CancelPending(); }
            y += 32;
            if (GUI.Button(new Rect(x, y, w, 28), "Stop (hold)")) { t.Stop(); }
            y += 32;
            if (GUI.Button(new Rect(x, y, w, 28), "Clear waypoints")) { t.ClearWaypoints(); }
            y += 32;
            if (GUI.Button(new Rect(x, y, w, 28), "Fall back")) { t.FallBack(); }
            y += 32;
            GUI.Label(new Rect(x, y, w, 40), t.Message);
        }
    }
}

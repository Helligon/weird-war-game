using System.Collections.Generic;
using UnityEngine;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Presentation
{
    /// <summary>
    /// Third-person control of the player's Officer (PCH-001) and its camera. Uses Unity's existing legacy Input Manager
    /// (WASD, mouse, left button). Input is turned into plain command objects by the core's <see cref="OfficerInputMapper"/>
    /// and applied with <see cref="BattleState.ApplyOfficerCommand"/> (PCH-009, DLV-013): this class never sets the Officer's
    /// position or aim itself. There are only move, aim and fire inputs (PCH-008), and nothing here addresses a Section or Unit (PCH-004).
    /// </summary>
    public sealed class OfficerController : MonoBehaviour
    {
        /// <summary>Radians of turn per unit of mouse movement.</summary>
        public float LookSensitivity = 0.03f;
        /// <summary>How far behind the Officer the camera sits, in metres.</summary>
        public float CameraDistance = 9f;
        /// <summary>How far above the ground the camera sits, in metres.</summary>
        public float CameraHeight = 6f;
        /// <summary>How far ahead of the Officer the camera looks, in metres.</summary>
        public float LookAhead = 8f;
        public float FieldOfView = 65f;
        /// <summary>The camera to move. When empty, the scene's main camera.</summary>
        public Camera TargetCamera;

        private BattleBootstrap _bootstrap;
        private Camera _camera;
        private string _lastShot = "No shots fired yet.";
        private LineRenderer _tracer;
        private float _tracerUntil;
        private Material _tracerMaterial;
        private readonly List<IBattleCommand> _submitted = new List<IBattleCommand>();

        /// <summary>Every command this controller created and submitted (for tests and debugging).</summary>
        public IReadOnlyList<IBattleCommand> Submitted => _submitted;
        public string LastShot => _lastShot;

        public void Attach(BattleBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
            _camera = TargetCamera != null ? TargetCamera : Camera.main;
            if (_camera != null)
            {
                _camera.orthographic = false;
                _camera.fieldOfView = FieldOfView;
                _camera.nearClipPlane = 0.1f;
                _camera.farClipPlane = 400f;
                PlaceCamera();
            }
        }

        /// <summary>Applies one command to the battle (never touches the Officer directly) and shows what a shot did.</summary>
        public OfficerCommandResult Submit(IBattleCommand command)
        {
            _submitted.Add(command);
            var result = _bootstrap.State.ApplyOfficerCommand(command);
            if (command is OfficerFireCommand && result.IsAccepted && result.Shot.HasValue) { ShowShot(result.Shot.Value); }
            return result;
        }

        private void Update()
        {
            if (_bootstrap == null || _bootstrap.State == null || _bootstrap.State.Officers.Count == 0) { return; }
            if (Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            var locked = Cursor.lockState == CursorLockMode.Locked;
            if (!locked)
            {
                // The first click gives the game the mouse. It is not a shot.
                if (Input.GetMouseButtonDown(0)) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
                return;
            }

            var forward = (Input.GetKey(KeyCode.W) ? 1.0 : 0.0) - (Input.GetKey(KeyCode.S) ? 1.0 : 0.0);
            var strafe = (Input.GetKey(KeyCode.D) ? 1.0 : 0.0) - (Input.GetKey(KeyCode.A) ? 1.0 : 0.0);
            // Moving the mouse right turns the Officer clockwise, which is a negative angle (0 is east, positive is towards north).
            var look = -Input.GetAxisRaw("Mouse X") * LookSensitivity;
            var fire = Input.GetMouseButtonDown(0);
            var commands = OfficerInputMapper.Map(0, _bootstrap.State.Officers[0].AimRadians, forward, strafe, look, fire, Time.deltaTime);
            foreach (var c in commands) { Submit(c); }
        }

        private void LateUpdate()
        {
            if (_camera == null || _bootstrap == null || _bootstrap.State == null || _bootstrap.State.Officers.Count == 0) { return; }
            PlaceCamera();
            if (_tracer != null && Time.time > _tracerUntil) { _tracer.enabled = false; }
        }

        // The map lies in Unity's XY plane and "up" is towards the camera, which is the -Z side. The camera sits behind the
        // Officer (against the aim) and above, looks slightly ahead, and keeps the aim direction pointing up the screen.
        private void PlaceCamera()
        {
            var o = _bootstrap.State.Officers[0];
            var aim = new Vector3(Mathf.Cos((float)o.AimRadians), Mathf.Sin((float)o.AimRadians), 0f);
            var pos = new Vector3((float)o.Position.X, (float)o.Position.Y, 0f);
            _camera.transform.position = pos - aim * CameraDistance + new Vector3(0f, 0f, -CameraHeight);
            _camera.transform.rotation = Quaternion.LookRotation((pos + aim * LookAhead) - _camera.transform.position, new Vector3(0f, 0f, -1f));
        }

        private void ShowShot(OfficerShot shot)
        {
            var o = _bootstrap.State.Officers[0];
            var aim = new Vector3(Mathf.Cos((float)o.AimRadians), Mathf.Sin((float)o.AimRadians), 0f);
            var from = new Vector3((float)o.Position.X, (float)o.Position.Y, -1.5f);
            var to = from + aim * (float)shot.Distance;
            if (_tracer == null)
            {
                _tracerMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(1f, 0.95f, 0.5f) };
                _tracer = new GameObject("Rifle tracer").AddComponent<LineRenderer>();
                _tracer.transform.SetParent(transform, false);
                _tracer.sharedMaterial = _tracerMaterial;
                _tracer.positionCount = 2;
                _tracer.startWidth = _tracer.endWidth = 0.15f;
            }
            _tracer.SetPosition(0, from);
            _tracer.SetPosition(1, to);
            _tracer.enabled = true;
            _tracerUntil = Time.time + 0.12f;
            _lastShot = shot.Hit ? "Hit at " + shot.Distance.ToString("0") + " m, " + shot.Damage + " damage" : "Miss";
            _bootstrap.StateView.Refresh();
        }

        private void OnDestroy()
        {
            if (_tracerMaterial != null) { DestroyImmediate(_tracerMaterial); }
        }

        private void OnGUI()
        {
            if (_bootstrap == null) { return; }
            var y = Screen.height - 70f;
            GUI.Box(new Rect(10, y, 520, 60), GUIContent.none);
            GUI.Label(new Rect(18, y + 3, 505, 56),
                "WASD move, mouse look, left click fire, Esc frees the mouse (click to take it back)\n"
                + "Rifle: " + _lastShot);
        }
    }
}

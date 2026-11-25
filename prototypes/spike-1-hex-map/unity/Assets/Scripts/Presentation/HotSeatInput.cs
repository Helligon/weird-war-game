using UnityEngine;
using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Presentation
{
    /// <summary>
    /// Hot-seat input and overlay (IMGUI). Turns left clicks into <see cref="GameController.ClickHex"/>, the End Turn
    /// button into <see cref="GameController.EndTurn"/>, shows the controller's state, and redraws the tokens after every
    /// input. It holds no rules and never edits the game state (DLV-012, DLV-013).
    /// </summary>
    public sealed class HotSeatInput : MonoBehaviour
    {
        private static readonly Rect Panel = new Rect(10, 10, 300, 170);

        private GameController _controller;
        private TokenView _tokens;
        private HexMap _map;
        private GUIStyle _label, _warning;

        public void Init(GameController controller, TokenView tokens)
        {
            _controller = controller;
            _tokens = tokens;
            _map = controller.State.Map;
            _tokens.Render(_controller.State, _controller.Selected);
        }

        private void Update()
        {
            if (_controller == null || !Input.GetMouseButtonDown(0)) return;
            var screen = Input.mousePosition;
            if (Panel.Contains(new Vector2(screen.x, Screen.height - screen.y))) return; // click was on the overlay

            var cam = Camera.main;
            if (cam == null) return;
            var world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            var hex = HexLayout.WorldToHex(world, HexMapView.HexSize);
            if (!_map.Contains(hex)) return;
            _controller.ClickHex(hex);
            _tokens.Render(_controller.State, _controller.Selected);
        }

        private void OnGUI()
        {
            if (_controller == null) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
                _warning = new GUIStyle(_label) { wordWrap = true, normal = { textColor = new Color(1f, 0.6f, 0.4f) } };
            }
            GUI.Box(Panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(Panel.x + 8, Panel.y + 6, Panel.width - 16, Panel.height - 12));
            var current = _controller.CurrentPlayer;
            var colour = _label.normal.textColor;
            _label.normal.textColor = TokenView.ColourOf(current) == TokenView.Player2Colour ? new Color(0.5f, 0.65f, 1f) : new Color(1f, 0.45f, 0.45f);
            GUILayout.Label("Current player: " + (current == PlayerId.Player1 ? "Player 1" : "Player 2"), _label);
            _label.normal.textColor = colour;
            var st = _controller.State;
            GUILayout.Label("Budget: P1 " + st.TokenOf(PlayerId.Player1).Budget + "   P2 " + st.TokenOf(PlayerId.Player2).Budget, _label);
            var budget = _controller.SelectedBudget;
            GUILayout.Label(budget.HasValue ? "Selected token budget: " + budget.Value : "No token selected", _label);
            if (_controller.LastRejection != null) GUILayout.Label("Rejected: " + _controller.LastRejection, _warning);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("End Turn", GUILayout.Height(28)))
            {
                _controller.EndTurn();
                _tokens.Render(_controller.State, _controller.Selected);
            }
            GUILayout.EndArea();
        }
    }
}

using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WeirdWar.Spike2.Battle.Core;
using WeirdWar.Spike2.Presentation;

namespace WeirdWar.Spike2.Tests
{
    public class TacticalControllerTests
    {
        private GameObject _go;
        private GameObject _camGo;
        private BattleBootstrap _boot;

        [SetUp]
        public void SetUp()
        {
            _camGo = new GameObject("test-camera") { tag = "MainCamera" };
            _camGo.AddComponent<Camera>();
            _go = new GameObject("test-battle");
            _boot = _go.AddComponent<BattleBootstrap>();
            _boot.Load();
            _boot.Officer.TargetCamera = _camGo.GetComponent<Camera>();
            _boot.Officer.Attach(_boot);
            _boot.TacticalUi.TargetCamera = _camGo.GetComponent<Camera>();
            _boot.TacticalUi.Attach(_boot);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_camGo);
        }

        // Unity calls LateUpdate every frame; edit-mode tests call it by name.
        private static void LateUpdate(MonoBehaviour behaviour)
        {
            var late = behaviour.GetType().GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(late);
            late.Invoke(behaviour, null);
        }

        [Test]
        public void CMD_002_entering_the_tactical_view_pauses_the_battle_and_leaving_resumes_it()
        {
            _boot.StateView.SetRunning(true);
            _boot.StateView.Frame(1.05);
            var steps = _boot.State.StepCount;
            Assert.Greater(steps, 0);

            _boot.TacticalUi.Toggle();
            var sectionX = _boot.State.Find(BattleState.PlayerSectionId).Position.X;
            _boot.StateView.Frame(5.0);
            Assert.IsFalse(_boot.StateView.Running);
            Assert.AreEqual(steps, _boot.State.StepCount);
            Assert.AreEqual(sectionX, _boot.State.Find(BattleState.PlayerSectionId).Position.X);
            Assert.AreEqual(0, _boot.State.CommandLog.Count, "pausing must not create commands");

            _boot.TacticalUi.Toggle();
            Assert.IsTrue(_boot.StateView.Running);
            _boot.StateView.Frame(1.05);
            Assert.Greater(_boot.State.StepCount, steps);
        }

        [Test]
        public void CMD_004_resuming_inside_the_tactical_view_puts_the_officer_under_ai_and_off_player_input()
        {
            _boot.TacticalUi.Toggle();
            _boot.StateView.SetRunning(true);
            Assert.IsTrue(_boot.Tactical.OfficerUnderAi);
            Assert.IsFalse(_boot.Tactical.OfficerAcceptsInput);
            _boot.StateView.Frame(1.05);
            Assert.Greater(_boot.State.StepCount, 0); // the battle runs while the player is still in the view
        }

        // The core's damage method is internal, so the Officer is killed the way it would be in play: it walks at the enemy Section.
        private void KillOfficer()
        {
            _boot.State.ApplyCommand(new OfficerMoveCommand(0, 1, 0));
            for (var i = 0; i < 5000 && !_boot.State.Officers[0].IsDestroyed; i++) { _boot.State.Step(); }
            Assert.IsTrue(_boot.State.Officers[0].IsDestroyed);
        }

        [Test]
        public void CMD_004_resuming_hands_the_officer_to_the_core_ai_by_a_logged_command_and_leaving_takes_it_back()
        {
            _boot.TacticalUi.Toggle();
            _boot.StateView.SetRunning(true);
            Assert.IsTrue(_boot.State.Officers[0].UnderAi);
            Assert.IsTrue(_boot.State.CommandLog.Any(l => l.Command is OfficerControlCommand c && c.UnderAi));

            _boot.TacticalUi.Toggle();
            Assert.IsFalse(_boot.State.Officers[0].UnderAi);
            Assert.IsTrue(_boot.Tactical.OfficerAcceptsInput);
        }

        [Test]
        public void PCH_006_the_officer_dying_in_third_person_switches_to_the_tactical_view_paused()
        {
            Assert.IsFalse(_boot.Tactical.Active);
            Assert.IsFalse(_boot.TacticalUi.CheckOfficerLost());
            KillOfficer();

            Assert.IsTrue(_boot.TacticalUi.CheckOfficerLost());

            Assert.IsTrue(_boot.Tactical.Active);
            Assert.IsFalse(_boot.StateView.Running);
            Assert.IsTrue(_camGo.GetComponent<Camera>().orthographic);
            Assert.IsTrue(_go.transform.Find("Tactical overlay").gameObject.activeSelf);
        }

        [Test]
        public void PCH_006_with_no_officer_left_the_player_cannot_go_back_to_third_person()
        {
            _boot.TacticalUi.Toggle();
            KillOfficer();

            _boot.TacticalUi.Toggle();

            Assert.IsTrue(_boot.Tactical.Active);
            Assert.IsTrue(_camGo.GetComponent<Camera>().orthographic);
            Assert.IsFalse(_boot.Tactical.OfficerAcceptsInput);
        }

        [Test]
        public void CMD_001_the_tactical_camera_is_top_down_and_the_third_person_camera_returns()
        {
            var cam = _camGo.GetComponent<Camera>();
            _boot.TacticalUi.Toggle();
            Assert.IsTrue(cam.orthographic);
            Assert.AreEqual(Quaternion.identity, cam.transform.rotation);
            // A late update must not pull the camera back to the Officer while in the tactical view.
            LateUpdate(_go.GetComponent<OfficerController>());
            Assert.IsTrue(cam.orthographic);

            _boot.TacticalUi.Toggle();
            Assert.IsFalse(cam.orthographic);
        }

        [Test]
        public void CMD_016_clicking_a_section_selects_it_and_HIE_007_it_is_a_section()
        {
            _boot.TacticalUi.Toggle();
            var p = _boot.State.Find(BattleState.PlayerSectionId).Position;

            Assert.IsTrue(_boot.TacticalUi.HandleClick(new Vector2((float)p.X + 2f, (float)p.Y)));

            Assert.AreEqual(BattleState.PlayerSectionId, _boot.Tactical.Selected);
        }

        [Test]
        public void CMD_005_clicking_two_waypoints_and_confirming_issues_one_move_through_the_core()
        {
            _boot.TacticalUi.Toggle();
            var p = _boot.State.Find(BattleState.PlayerSectionId).Position;
            _boot.TacticalUi.HandleClick(new Vector2((float)p.X, (float)p.Y));
            _boot.TacticalUi.HandleClick(new Vector2(30f, 20f));
            _boot.TacticalUi.HandleClick(new Vector2(40f, 25f));
            Assert.IsFalse(_boot.TacticalUi.HandleClick(new Vector2(50f, 25f)), "CMD-020: a third waypoint is refused");
            Assert.AreEqual(0, _boot.State.CommandLog.Count);

            Assert.IsTrue(_boot.Tactical.ConfirmMove(), _boot.Tactical.Message);

            Assert.AreEqual(1, _boot.State.CommandLog.Count);
            Assert.AreEqual(2, _boot.State.Find(BattleState.PlayerSectionId).Waypoints.Count);
        }

        [Test]
        public void CMD_001_no_section_command_is_possible_outside_the_tactical_view()
        {
            Assert.IsFalse(_boot.TacticalUi.HandleClick(new Vector2(30f, 20f)));
            Assert.IsFalse(_boot.Tactical.Stop());
            Assert.AreEqual(0, _boot.State.CommandLog.Count);
        }

        [Test]
        public void CMD_013_the_tactical_controls_have_no_ability_trigger()
        {
            Assert.IsFalse(TacticalView.Controls.Any(c => c.ToLowerInvariant().Contains("abilit")));
        }

        [Test]
        public void PCH_001_the_tactical_overlay_renderers_have_fixed_draw_orders()
        {
            _boot.TacticalUi.Toggle();
            var p = _boot.State.Find(BattleState.PlayerSectionId).Position;
            _boot.TacticalUi.HandleClick(new Vector2((float)p.X, (float)p.Y));
            _boot.TacticalUi.HandleClick(new Vector2(30f, 20f));
            LateUpdate(_go.GetComponent<TacticalController>());

            var overlay = _go.transform.Find("Tactical overlay");
            var selection = overlay.Find("Selection").GetComponent<MeshRenderer>();
            var marker = overlay.GetComponentsInChildren<MeshRenderer>(true).First(r => r.name == "Waypoint marker");
            Assert.AreEqual(BattleMapView.OrderSelection, selection.sortingOrder);
            Assert.AreEqual(BattleMapView.OrderWaypoints, marker.sortingOrder);
            foreach (var line in overlay.GetComponentsInChildren<LineRenderer>(true)) { Assert.AreEqual(BattleMapView.OrderWaypoints, line.sortingOrder); }
            Assert.Greater(BattleMapView.OrderUnits, BattleMapView.OrderSelection);
            Assert.Greater(BattleMapView.OrderSelection, BattleMapView.OrderCover);
            Assert.Greater(BattleMapView.OrderWaypoints, BattleMapView.OrderRifle);
            Assert.Greater(BattleMapView.OrderTracer, BattleMapView.OrderWaypoints);
        }
    }
}

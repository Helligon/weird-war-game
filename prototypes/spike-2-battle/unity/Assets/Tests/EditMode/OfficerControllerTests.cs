using NUnit.Framework;
using UnityEngine;
using WeirdWar.Spike2.Battle.Core;
using WeirdWar.Spike2.Presentation;

namespace WeirdWar.Spike2.Tests
{
    public class OfficerControllerTests
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
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_camGo);
        }

        [Test]
        public void PCH_001_the_camera_is_perspective_behind_and_above_the_officer()
        {
            var cam = _camGo.GetComponent<Camera>();
            _boot.Officer.TargetCamera = cam; // another scene may already have a main camera
            _boot.Officer.Attach(_boot);
            var officer = _boot.State.Officers[0];
            Assert.IsFalse(cam.orthographic);
            // The Officer faces east (+X), so "behind" is to the west (smaller X), and "above" is towards -Z.
            Assert.Less(cam.transform.position.x, (float)officer.Position.X);
            Assert.Less(cam.transform.position.z, 0f);
        }

        [Test]
        public void PCH_009_input_is_applied_as_commands_and_logged_in_the_battle()
        {
            var before = _boot.State.Officers[0].Position.X;
            Assert.IsTrue(_boot.Officer.Submit(new OfficerMoveCommand(0, 1, 0, 0.5)).IsAccepted);
            Assert.IsTrue(_boot.Officer.Submit(new OfficerAimCommand(0, 0.5)).IsAccepted);
            Assert.IsTrue(_boot.Officer.Submit(new OfficerFireCommand(0)).IsAccepted);
            Assert.Greater(_boot.State.Officers[0].Position.X, before);
            Assert.AreEqual(0.5, _boot.State.Officers[0].AimRadians);
            Assert.AreEqual(3, _boot.State.CommandLog.Count);
            Assert.AreEqual(3, _boot.Officer.Submitted.Count);
        }

        [Test]
        public void PCH_001_the_officer_square_and_the_camera_follow_the_core_state()
        {
            var cam = _camGo.GetComponent<Camera>();
            _boot.Officer.TargetCamera = cam;
            _boot.Officer.Attach(_boot);
            var start = _boot.State.Officers[0].Position;
            var camStart = cam.transform.position;

            _boot.Officer.Submit(new OfficerAimCommand(0, Mathf.PI / 2f));
            _boot.Officer.Submit(new OfficerMoveCommand(0, 0, 1, 2));
            var officer = _boot.State.Officers[0];
            Assert.AreEqual(start.Y + 8.0, officer.Position.Y, 1e-4); // 2 s at the stats speed of 4 m/s

            // Unity calls LateUpdate every frame; the edit-mode test calls it by name, so it fails if either is removed.
            Frame(_boot.StateView);
            Frame(_boot.Officer);

            var square = _boot.StateView.transform.Find("Sections/Officer");
            Assert.IsNotNull(square);
            Assert.AreEqual((float)officer.Position.X, square.localPosition.x, 1e-3f);
            Assert.AreEqual((float)officer.Position.Y, square.localPosition.y, 1e-3f);
            Assert.AreEqual(90f, square.localEulerAngles.z, 1e-2f);

            // The camera moved with the Officer, sits behind it (south, as it now faces north) and above, and looks at it.
            Assert.AreEqual((float)officer.Position.X, cam.transform.position.x, 1e-3f);
            Assert.AreEqual((float)officer.Position.Y - _boot.Officer.CameraDistance, cam.transform.position.y, 1e-3f);
            Assert.AreNotEqual(camStart.y, cam.transform.position.y);
            Assert.Less(cam.transform.position.z, 0f);
            var toOfficer = new Vector3((float)officer.Position.X, (float)officer.Position.Y, 0f) - cam.transform.position;
            Assert.Greater(Vector3.Dot(cam.transform.forward, toOfficer.normalized), 0.9f);
        }

        private static void Frame(MonoBehaviour behaviour)
        {
            var late = behaviour.GetType().GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(late, behaviour.GetType().Name + " must follow the state in LateUpdate");
            late.Invoke(behaviour, null);
        }

        [Test]
        public void PCH_009_a_rejected_command_is_not_recorded_and_a_reset_clears_the_record()
        {
            Assert.IsFalse(_boot.Officer.Submit(new OfficerMoveCommand(0, 1, 0, 0)).IsAccepted);
            Assert.AreEqual(0, _boot.Officer.Submitted.Count);
            _boot.Officer.Submit(new OfficerAimCommand(0, 0.2));
            Assert.AreEqual(1, _boot.Officer.Submitted.Count);
            _boot.Load();
            Assert.AreEqual(0, _boot.Officer.Submitted.Count);
        }

        [Test]
        public void PCH_001_a_click_on_the_debug_panel_does_not_take_the_mouse_back()
        {
            Assert.IsTrue(OfficerController.IsOverDebugPanel(new Vector2(20f, Screen.height - 20f)));
            Assert.IsFalse(OfficerController.IsOverDebugPanel(new Vector2(20f, Screen.height - 400f)));
        }

        [Test]
        public void PCH_008_the_controller_has_no_ability_members()
        {
            foreach (var m in typeof(OfficerController).GetMethods())
            {
                StringAssert.DoesNotContain("Ability", m.Name);
                StringAssert.DoesNotContain("Special", m.Name);
            }
        }
    }
}

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
        public void PCH_001_the_officer_square_follows_the_core_state()
        {
            _boot.Officer.Submit(new OfficerMoveCommand(0, 0, 1, 1));
            Assert.AreEqual(1, _boot.StateView.OfficerSquares);
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

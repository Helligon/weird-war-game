using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WeirdWar.Spike2.Battle.Core;
using WeirdWar.Spike2.Presentation;

namespace WeirdWar.Spike2.Tests
{
    public class BattleStateViewTests
    {
        private GameObject _go;
        private BattleBootstrap _boot;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("test-battle");
            _boot = _go.AddComponent<BattleBootstrap>();
            _boot.Load();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void PCH_001_every_layer_has_a_fixed_draw_order_so_cover_and_squares_stay_above_the_ground()
        {
            // Transparent layers are sorted by camera distance unless a sorting order is set, which made
            // cover and squares flicker or vanish under the third-person camera. Every layer needs a fixed order.
            var renderers = _go.GetComponentsInChildren<MeshRenderer>();
            int[] Orders(System.Func<string, bool> match) => renderers.Where(r => match(r.name)).Select(r => r.sortingOrder).ToArray();
            var ground = Orders(n => n == "Ground");
            var edges = Orders(n => n == "West edge" || n == "East edge");
            var cover = Orders(n => n.StartsWith("Cover"));
            var squares = Orders(n => n == "Unit" || n == "Officer");
            var rifle = Orders(n => n == "Rifle");
            Assert.AreEqual(1, ground.Length);
            Assert.AreEqual(2, edges.Length);
            Assert.AreEqual(_boot.Map.CoverObjects.Count, cover.Length);
            Assert.Greater(squares.Length, 0);
            Assert.AreEqual(1, rifle.Length);
            Assert.Greater(edges.Min(), ground.Max());
            Assert.Greater(cover.Min(), edges.Max());
            Assert.Greater(squares.Min(), cover.Max());
            Assert.Greater(rifle.Min(), squares.Max());
            Assert.Greater(BattleMapView.OrderTracer, rifle.Max());
        }

        [Test]
        public void HIE_004_one_square_is_drawn_per_unit_and_one_for_the_officer()
        {
            Assert.AreEqual(_boot.Stats.RiflemenMaxUnits, _boot.StateView.SquaresFor(BattleState.PlayerSectionId));
            Assert.AreEqual(_boot.Stats.RiflemenMaxUnits, _boot.StateView.SquaresFor(BattleState.EnemySectionId));
            Assert.AreEqual(1, _boot.StateView.OfficerSquares);
        }

        [Test]
        public void HIE_004_losing_health_removes_unit_squares_when_refreshed()
        {
            var enemy = _boot.State.Find(BattleState.EnemySectionId);
            // Fire has a range now, so let the Sections walk together until they are in range.
            var player = _boot.State.Find(BattleState.PlayerSectionId);
            for (var i = 0; i < 200 && Distance(player, enemy) > _boot.Stats.RiflemenRange; i++) { _boot.StateView.Advance(1.0); }
            Assert.Greater(enemy.UnitCount, 0);
            var before = _boot.StateView.SquaresFor(enemy.Id);

            // Fire is internal to the core (DLV-013): order the player Section to attack and let the battle run until a Unit falls.
            Assert.IsTrue(_boot.State.ApplyCommand(new TargetCommand(BattleState.PlayerSectionId, enemy.Id)).IsAccepted);
            for (var i = 0; i < 600 && enemy.UnitCount == before; i++) { _boot.StateView.Advance(1.0); }
            _boot.StateView.Refresh();

            Assert.Less(enemy.UnitCount, before);
            Assert.AreEqual(enemy.UnitCount, _boot.StateView.SquaresFor(enemy.Id));
        }

        [Test]
        public void DAI_001_running_the_battle_moves_the_player_section_toward_the_enemy()
        {
            var player = _boot.State.Find(BattleState.PlayerSectionId);
            var startX = player.Position.X;

            _boot.StateView.Advance(10.0);

            Assert.Greater(player.Position.X, startX);
        }

        [Test]
        public void TEC_005_ending_the_battle_writes_the_command_log_and_shows_the_result()
        {
            var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ww-log-test-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                _boot.LogFolder = folder;
                _boot.StateView.Show(_boot.State, _boot);
                Assert.IsTrue(_boot.State.ApplyCommand(new OfficerMoveCommand(0, 1, 0)).IsAccepted);
                _boot.StateView.Frame(1.0);
                Assert.IsNull(_boot.StateView.LogPath, "no log while the battle is still going");
                Assert.AreEqual(BattleOutcome.InProgress, _boot.StateView.Outcome);

                // Let the default behaviour fight it out: the Sections walk together and shoot until one side is gone.
                _boot.StateView.SetRunning(true);
                for (var i = 0; i < 5000 && _boot.StateView.Outcome == BattleOutcome.InProgress; i++) { _boot.StateView.Frame(1.0); }

                Assert.AreNotEqual(BattleOutcome.InProgress, _boot.StateView.Outcome);
                Assert.IsNotNull(_boot.StateView.LogPath, _boot.StateView.LogError);
                Assert.IsTrue(System.IO.File.Exists(_boot.StateView.LogPath));
                StringAssert.Contains(_boot.StateView.Outcome.ToString(), _boot.StateView.LogPath);
                var read = CommandLogFile.Read(System.IO.File.ReadAllText(_boot.StateView.LogPath));
                Assert.AreEqual(_boot.State.CommandLog.Count, read.Commands.Count);
                Assert.AreEqual(_boot.State.CommandLog[0].Command, read.Commands[0].Command);
                var path = _boot.StateView.LogPath;
                // The file name has one-second resolution, so move the clock on: without the once-only guard each frame would make a new file.
                var clock = new System.DateTime(2030, 1, 1, 12, 0, 0);
                _boot.StateView.Clock = () => clock = clock.AddSeconds(5);
                _boot.StateView.Frame(0.1);
                _boot.StateView.Frame(0.1);
                Assert.AreEqual(path, _boot.StateView.LogPath);
                Assert.AreEqual(1, System.IO.Directory.GetFiles(folder).Length, "written once");
            }
            finally
            {
                if (System.IO.Directory.Exists(folder)) { System.IO.Directory.Delete(folder, true); }
            }
        }

        [Test]
        public void VIC_001_each_result_has_its_own_message()
        {
            StringAssert.Contains("VICTORY", BattleStateView.OutcomeText(BattleOutcome.PlayerWon));
            StringAssert.Contains("DEFEAT", BattleStateView.OutcomeText(BattleOutcome.EnemyWon));
            StringAssert.Contains("DRAW", BattleStateView.OutcomeText(BattleOutcome.Draw));
            Assert.AreEqual("", BattleStateView.OutcomeText(BattleOutcome.InProgress));
        }

        private static double Distance(Section a, Section b)
        {
            var dx = a.Position.X - b.Position.X;
            var dy = a.Position.Y - b.Position.Y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}

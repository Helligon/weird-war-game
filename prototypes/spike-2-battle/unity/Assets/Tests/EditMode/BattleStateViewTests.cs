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

            for (var i = 0; i < 20 && enemy.UnitCount == before; i++)
            {
                _boot.State.Fire(BattleState.PlayerSectionId, enemy.Id);
            }
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

        private static double Distance(Section a, Section b)
        {
            var dx = a.Position.X - b.Position.X;
            var dy = a.Position.Y - b.Position.Y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}

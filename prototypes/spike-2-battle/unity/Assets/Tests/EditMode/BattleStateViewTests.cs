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
            var before = _boot.StateView.SquaresFor(enemy.Id);

            for (ulong seed = 1; seed < 20 && enemy.UnitCount == before; seed++)
            {
                _boot.State.Fire(BattleState.PlayerSectionId, enemy.Id, new SeededRandom(seed));
            }
            _boot.StateView.Refresh();

            Assert.Less(enemy.UnitCount, before);
            Assert.AreEqual(enemy.UnitCount, _boot.StateView.SquaresFor(enemy.Id));
        }
    }
}

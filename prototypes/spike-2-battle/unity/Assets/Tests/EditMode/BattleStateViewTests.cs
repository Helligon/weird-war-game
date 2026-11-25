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

        [Test]
        public void DLV_013_the_order_buttons_apply_logged_commands_through_the_core()
        {
            var player = _boot.State.Find(BattleState.PlayerSectionId);
            var result = _boot.StateView.Give(OrderFor.MoveBy(_boot.State, player.Id, 10, 0));

            Assert.IsTrue(result.IsAccepted, result.Reason);
            Assert.AreEqual(1, _boot.State.CommandLog.Count);
            Assert.AreEqual(1, player.Waypoints.Count);
            Assert.AreEqual(player.Position.X + 10, player.Waypoints[0].X, 1e-9);
        }

        [Test]
        public void CMD_020_a_third_waypoint_is_rejected_and_the_view_says_why()
        {
            var id = BattleState.PlayerSectionId;
            _boot.StateView.Give(OrderFor.MoveBy(_boot.State, id, 10, 0));
            _boot.StateView.Give(OrderFor.MoveBy(_boot.State, id, 10, 0));
            var third = _boot.StateView.Give(OrderFor.MoveBy(_boot.State, id, 10, 0));

            Assert.IsFalse(third.IsAccepted);
            StringAssert.Contains("REJECTED", _boot.StateView.LastOrder);
            Assert.AreEqual(2, _boot.State.CommandLog.Count);
        }

        [Test]
        public void CMD_005_move_beside_cover_makes_a_waypoint_next_to_cover()
        {
            var id = BattleState.PlayerSectionId;
            var result = _boot.StateView.Give(OrderFor.MoveBesideCover(_boot.State, id));

            Assert.IsTrue(result.IsAccepted, result.Reason);
            var wp = _boot.State.Find(id).Waypoints[0];
            var near = false;
            foreach (var c in _boot.State.Map.CoverObjects)
            {
                var d = System.Math.Sqrt((wp.X - c.Position.X) * (wp.X - c.Position.X) + (wp.Y - c.Position.Y) * (wp.Y - c.Position.Y)) - c.Radius;
                if (d <= _boot.Stats.CoverAdjacencyDistance) { near = true; }
            }
            Assert.IsTrue(near);
        }

        private static double Distance(Section a, Section b)
        {
            var dx = a.Position.X - b.Position.X;
            var dy = a.Position.Y - b.Position.Y;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}

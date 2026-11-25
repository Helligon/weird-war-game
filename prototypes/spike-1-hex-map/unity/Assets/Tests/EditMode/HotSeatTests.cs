using NUnit.Framework;
using UnityEngine;
using WeirdWar.Spike1.Core;
using WeirdWar.Spike1.Presentation;

namespace WeirdWar.Spike1.Tests
{
    public class HotSeatTests
    {
        // Sample scenario: Player 1 starts at offset (3,4), Player 2 at (16,5), budget 10, cost 1, Player 1 first.
        private GameController _c;
        private HexCoord _p1;
        private HexCoord _p2;

        [SetUp]
        public void SetUp()
        {
            _c = new GameController(HexMapBootstrap.LoadState("../maps", "sample.json", "sample-scenario.json"));
            _p1 = _c.State.TokenOf(PlayerId.Player1).Position;
            _p2 = _c.State.TokenOf(PlayerId.Player2).Position;
        }

        // A passable neighbour of the token that is not next to (or on) the enemy token.
        private HexCoord FreeNeighbour(PlayerId who)
        {
            var s = _c.State;
            foreach (var n in s.TokenOf(who).Position.Neighbours())
            {
                var enemy = s.TokenOf(who == PlayerId.Player1 ? PlayerId.Player2 : PlayerId.Player1).Position;
                if (s.Map.Contains(n) && s.Map.IsPassable(n) && n.DistanceTo(enemy) > 1) return n;
            }
            Assert.Fail("no free neighbour");
            return default;
        }

        [Test]
        public void MAP_017_world_to_hex_round_trips_for_several_hexes()
        {
            foreach (var (col, row) in new[] { (0, 0), (1, 0), (0, 1), (3, 4), (16, 5), (19, 11), (7, 3) })
            {
                var hex = HexCoord.FromOffset(new OffsetCoord(col, row));
                Assert.AreEqual(hex, HexLayout.WorldToHex(HexLayout.HexToWorld(hex)), "col " + col + " row " + row);
            }
        }

        [Test]
        public void MAP_017_world_to_hex_resolves_points_near_the_centre_but_not_in_the_next_hex()
        {
            var hex = HexCoord.FromOffset(new OffsetCoord(10, 10));
            var centre = HexLayout.HexToWorld(hex);
            foreach (var corner in new[] { 0, 1, 2, 3, 4, 5 })
            {
                Assert.AreEqual(hex, HexLayout.WorldToHex(centre + HexLayout.Corner(corner) * 0.8f), "corner " + corner);
            }
            foreach (var n in hex.Neighbours())
            {
                // The border between two hexes is half way between their centres.
                var there = HexLayout.HexToWorld(n);
                Assert.AreEqual(hex, HexLayout.WorldToHex(Vector3.Lerp(centre, there, 0.45f)), "0.45 of the way to " + n);
                Assert.AreEqual(n, HexLayout.WorldToHex(Vector3.Lerp(centre, there, 0.55f)), "0.55 of the way to " + n);
            }
        }

        [Test]
        public void TRN_007_clicking_the_current_players_own_token_selects_it()
        {
            _c.ClickHex(_p1);
            Assert.AreEqual(PlayerId.Player1, _c.Selected);
        }

        [Test]
        public void TRN_007_clicking_the_other_players_token_does_not_select_it()
        {
            _c.ClickHex(_p2);
            Assert.IsNull(_c.Selected);
        }

        [Test]
        public void TRN_007_clicking_the_other_players_token_keeps_the_current_selection()
        {
            _c.ClickHex(_p1);
            _c.ClickHex(_p2);
            Assert.AreEqual(PlayerId.Player1, _c.Selected);
        }

        [Test]
        public void SUP_005_clicking_an_empty_hex_with_nothing_selected_sends_no_command()
        {
            var before = _c.State;
            _c.ClickHex(FreeNeighbour(PlayerId.Player1));
            Assert.AreSame(before, _c.State);
            Assert.IsNull(_c.LastRejection);
        }

        [Test]
        public void SUP_005_accepted_move_updates_position_and_drops_budget_by_the_cost()
        {
            var target = FreeNeighbour(PlayerId.Player1);
            _c.ClickHex(_p1);
            Assert.AreEqual(10, _c.SelectedBudget);
            _c.ClickHex(target);
            Assert.AreEqual(target, _c.State.TokenOf(PlayerId.Player1).Position);
            Assert.AreEqual(10 - _c.State.Config.MovementCostPerHex, _c.SelectedBudget);
            Assert.IsNull(_c.LastRejection);
            Assert.AreEqual(PlayerId.Player1, _c.Selected, "token stays selected for the next step");
        }

        [Test]
        public void SUP_005_rejected_move_leaves_the_state_untouched_and_shows_the_reason()
        {
            var before = _c.State;
            _c.ClickHex(_p1);
            var far = HexCoord.FromOffset(new OffsetCoord(3, 8)); // not adjacent
            _c.ClickHex(far);
            Assert.AreSame(before, _c.State);
            Assert.AreEqual(_p1, _c.State.TokenOf(PlayerId.Player1).Position);
            Assert.AreEqual(10, _c.State.TokenOf(PlayerId.Player1).Budget);
            StringAssert.Contains("not next to", _c.LastRejection);
        }

        [TestCase(TerrainType.Mountain)]
        [TestCase(TerrainType.Sea)]
        [TestCase(TerrainType.River)]
        public void SUP_005_moving_onto_impassable_terrain_is_rejected_and_shows_the_core_reason(TerrainType terrain)
        {
            var map = _c.State.Map;
            var enemy = _p2;
            HexCoord blocked = default, start = default;
            var found = false;
            for (var row = 0; row < map.Height && !found; row++)
            {
                for (var col = 0; col < map.Width && !found; col++)
                {
                    var hex = HexCoord.FromOffset(new OffsetCoord(col, row));
                    if (map.TerrainAt(hex) != terrain) continue;
                    foreach (var n in hex.Neighbours())
                    {
                        if (map.Contains(n) && map.IsPassable(n) && n.DistanceTo(enemy) > 2) { blocked = hex; start = n; found = true; break; }
                    }
                }
            }
            Assert.IsTrue(found, "sample map has no " + terrain + " next to a passable hex");
            var config = new GameConfig(PlayerId.Player1, 10, start.ToOffset(), enemy.ToOffset(), 1);
            var c = new GameController(GameState.NewGame(map, config));
            var before = c.State;
            c.ClickHex(start);
            c.ClickHex(blocked);
            Assert.AreSame(before, c.State);
            Assert.AreEqual(start, c.State.TokenOf(PlayerId.Player1).Position);
            Assert.AreEqual(10, c.State.TokenOf(PlayerId.Player1).Budget);
            StringAssert.Contains("impassable " + terrain, c.LastRejection);
        }

        [Test]
        public void SUP_005_moving_into_the_other_token_is_rejected_by_the_core_with_its_reason()
        {
            _c.ClickHex(_p1);
            _c.ClickHex(_p2);
            Assert.AreEqual(PlayerId.Player1, _c.Selected);
            StringAssert.Contains("occupied", _c.LastRejection);
        }

        [Test]
        public void SUP_005_moving_next_to_the_enemy_shows_the_contact_rejection()
        {
            // Two tokens two hexes apart: the hex between them is next to both.
            var map = HexMapBootstrap.LoadState("../maps", "sample.json", "sample-scenario.json").Map;
            var config = new GameConfig(PlayerId.Player1, 10, new OffsetCoord(3, 4), new OffsetCoord(3, 6), 1);
            var c = new GameController(GameState.NewGame(map, config));
            c.ClickHex(c.State.TokenOf(PlayerId.Player1).Position);
            c.ClickHex(HexCoord.FromOffset(new OffsetCoord(3, 5)));
            Assert.AreEqual("contact: battles not in spike", c.LastRejection);
        }

        [Test]
        public void SUP_005_not_enough_budget_rejects_and_shows_the_reason()
        {
            var map = HexMapBootstrap.LoadState("../maps", "sample.json", "sample-scenario.json").Map;
            var config = new GameConfig(PlayerId.Player1, 1, new OffsetCoord(3, 4), new OffsetCoord(16, 5), 1);
            var c = new GameController(GameState.NewGame(map, config));
            c.ClickHex(c.State.TokenOf(PlayerId.Player1).Position);
            var first = HexCoord.FromOffset(new OffsetCoord(3, 4));
            foreach (var n in first.Neighbours()) { if (map.Contains(n) && map.IsPassable(n)) { c.ClickHex(n); break; } }
            Assert.AreEqual(0, c.SelectedBudget);
            var before = c.State;
            foreach (var n in c.State.TokenOf(PlayerId.Player1).Position.Neighbours()) { if (map.Contains(n) && map.IsPassable(n)) { c.ClickHex(n); break; } }
            Assert.AreSame(before, c.State);
            StringAssert.Contains("Not enough movement budget", c.LastRejection);
        }

        [Test]
        public void TRN_007_end_turn_switches_the_current_player_and_refills_their_budget()
        {
            _c.ClickHex(_p1);
            _c.ClickHex(FreeNeighbour(PlayerId.Player1));
            _c.EndTurn(); // P2's turn; P2's budget is full
            Assert.AreEqual(PlayerId.Player2, _c.CurrentPlayer);
            Assert.AreEqual(_c.State.Config.StartingBudget, _c.State.TokenOf(PlayerId.Player2).Budget);
            _c.ClickHex(_c.State.TokenOf(PlayerId.Player2).Position);
            Assert.AreEqual(PlayerId.Player2, _c.Selected);
            Assert.AreEqual(10, _c.SelectedBudget);
            _c.EndTurn(); // back to P1, whose budget was spent and is refilled
            Assert.AreEqual(PlayerId.Player1, _c.CurrentPlayer);
            Assert.AreEqual(10, _c.State.TokenOf(PlayerId.Player1).Budget);
        }

        [Test]
        public void TRN_007_end_turn_clears_the_selection_and_the_old_rejection()
        {
            _c.ClickHex(_p1);
            _c.ClickHex(HexCoord.FromOffset(new OffsetCoord(3, 8)));
            Assert.IsNotNull(_c.LastRejection);
            _c.EndTurn();
            Assert.IsNull(_c.Selected);
            Assert.IsNull(_c.LastRejection);
        }

        [Test]
        public void TRN_007_player_two_cannot_be_selected_or_moved_on_player_ones_turn_but_can_on_their_own()
        {
            _c.ClickHex(_p2);
            Assert.IsNull(_c.Selected);
            _c.EndTurn();
            _c.ClickHex(_p1); // now the other player's token
            Assert.IsNull(_c.Selected);
        }

        [Test]
        public void DLV_013_state_only_changes_through_apply_and_a_rejection_keeps_the_same_state_object()
        {
            var original = _c.State;
            _c.ClickHex(_p1);
            Assert.AreSame(original, _c.State, "selecting sends no command");
            _c.ClickHex(HexCoord.FromOffset(new OffsetCoord(3, 8)));
            Assert.AreSame(original, _c.State, "a rejected command leaves the same state object");
            _c.ClickHex(FreeNeighbour(PlayerId.Player1));
            Assert.AreNotSame(original, _c.State, "an accepted command gives a new state");
            Assert.AreEqual(_p1, original.TokenOf(PlayerId.Player1).Position, "the old state is not mutated");
            Assert.AreEqual(10, original.TokenOf(PlayerId.Player1).Budget);
        }

        [Test]
        public void DLV_012_controller_does_not_expose_a_way_to_set_state_or_budget()
        {
            foreach (var p in typeof(GameController).GetProperties())
            {
                var setter = p.GetSetMethod(true);
                Assert.IsTrue(setter == null || setter.IsPrivate, p.Name + " must not have a public setter");
            }
        }

        [Test]
        public void DLV_012_controller_public_methods_are_only_click_hex_and_end_turn()
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (var m in typeof(GameController).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            {
                if (!m.IsSpecialName) names.Add(m.Name); // special names are property getters and setters
            }
            names.Sort();
            CollectionAssert.AreEqual(new[] { "ClickHex", "EndTurn" }, names);
        }

        [Test]
        public void DLV_013_token_view_re_renders_from_state_with_tokens_on_their_hexes_with_distinct_colours_and_a_ring_on_the_selected_one()
        {
            var go = new GameObject("test-tokens");
            try
            {
                var view = go.AddComponent<TokenView>();
                view.Render(_c.State, null);
                Assert.IsFalse(view.RingVisible);
                Assert.AreEqual(0f, Vector2.Distance(HexLayout.HexToWorld(_p1), view.PositionOf(PlayerId.Player1).Value), 1e-4f);
                Assert.AreEqual(0f, Vector2.Distance(HexLayout.HexToWorld(_p2), view.PositionOf(PlayerId.Player2).Value), 1e-4f);
                Assert.AreNotEqual(TokenView.ColourOf(PlayerId.Player1), TokenView.ColourOf(PlayerId.Player2));

                _c.ClickHex(_p1);
                view.Render(_c.State, _c.Selected);
                Assert.IsTrue(view.RingVisible);
                Assert.AreEqual(0f, Vector2.Distance(HexLayout.HexToWorld(_p1), view.RingPosition), 1e-4f);

                var target = FreeNeighbour(PlayerId.Player1);
                _c.ClickHex(target);
                view.Render(_c.State, _c.Selected);
                Assert.AreEqual(0f, Vector2.Distance(HexLayout.HexToWorld(target), view.PositionOf(PlayerId.Player1).Value), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

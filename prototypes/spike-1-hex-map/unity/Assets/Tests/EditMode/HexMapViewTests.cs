using System.IO;
using NUnit.Framework;
using UnityEngine;
using WeirdWar.Spike1.Core;
using WeirdWar.Spike1.Presentation;

namespace WeirdWar.Spike1.Tests
{
    public class HexMapViewTests
    {
        private GameObject _go;
        private HexMapView _view;
        private HexMap _map;

        [SetUp]
        public void SetUp()
        {
            _map = HexMapBootstrap.LoadState("../maps", "sample.json", "sample-scenario.json").Map;
            _go = new GameObject("test-map");
            _view = _go.AddComponent<HexMapView>();
            _view.Build(_map);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void MAP_019_grid_has_30_columns_by_50_rows_of_tiles()
        {
            Assert.AreEqual(30, _map.Width);
            Assert.AreEqual(50, _map.Height);
            Assert.AreEqual(30 * 50, _view.TileCount);
            Assert.AreEqual(30 * 50, _view.TileCentres.Length);
        }

        [Test]
        public void MAP_017_tile_positions_match_core_flat_top_odd_q_layout()
        {
            for (var row = 0; row < _map.Height; row++)
            {
                for (var col = 0; col < _map.Width; col++)
                {
                    var expected = HexLayout.OffsetToWorld(col, row);
                    Assert.AreEqual(0f, Vector3.Distance(expected, _view.TileCentres[row * _map.Width + col]), 1e-4f);
                }
            }
        }

        [Test]
        public void MAP_017_odd_columns_sit_half_a_hex_lower_and_columns_are_1_5_apart()
        {
            var even = HexLayout.OffsetToWorld(2, 5);
            var odd = HexLayout.OffsetToWorld(3, 5);
            var height = Mathf.Sqrt(3f);
            Assert.AreEqual(1.5f, odd.x - even.x, 1e-4f);
            Assert.AreEqual(height / 2f, even.y - odd.y, 1e-4f); // lower = smaller y
            Assert.AreEqual(height, even.y - HexLayout.OffsetToWorld(2, 6).y, 1e-4f); // next row is below
        }

        [Test]
        public void MAP_017_adjacent_hexes_are_one_hex_width_apart_on_screen()
        {
            var hex = HexCoord.FromOffset(new OffsetCoord(10, 20));
            var centre = HexLayout.HexToWorld(hex);
            foreach (var n in hex.Neighbours())
            {
                Assert.AreEqual(Mathf.Sqrt(3f), Vector3.Distance(centre, HexLayout.HexToWorld(n)), 1e-4f);
            }
        }

        [Test]
        public void MAP_017_terrain_colours_differ_for_plain_mountain_sea_and_river()
        {
            var colours = new[]
            {
                TerrainColours.For(TerrainType.Plain), TerrainColours.For(TerrainType.Mountain),
                TerrainColours.For(TerrainType.Sea), TerrainColours.For(TerrainType.River),
            };
            for (var i = 0; i < colours.Length; i++)
            {
                for (var j = i + 1; j < colours.Length; j++)
                {
                    Assert.AreNotEqual(colours[i], colours[j], "terrain " + i + " and " + j);
                }
            }
        }

        [Test]
        public void MAP_017_every_tile_vertex_colour_is_its_terrains_colour()
        {
            var mesh = _go.GetComponent<MeshFilter>().sharedMesh;
            var colours = mesh.colors;
            for (var row = 0; row < _map.Height; row++)
            {
                for (var col = 0; col < _map.Width; col++)
                {
                    var hex = HexCoord.FromOffset(new OffsetCoord(col, row));
                    var expected = TerrainColours.For(_map.TerrainAt(hex));
                    Assert.AreEqual(expected, colours[(row * _map.Width + col) * 7]);
                }
            }
        }

        [Test]
        public void MAP_017_sample_map_uses_all_four_terrain_types()
        {
            var seen = new System.Collections.Generic.HashSet<TerrainType>();
            for (var row = 0; row < _map.Height; row++)
            {
                for (var col = 0; col < _map.Width; col++)
                {
                    seen.Add(_map.TerrainAt(HexCoord.FromOffset(new OffsetCoord(col, row))));
                }
            }
            Assert.AreEqual(4, seen.Count);
        }

        [Test]
        public void TEC_012_presentation_assembly_has_no_game_rule_dependencies_beyond_the_core()
        {
            // The core must not reference UnityEngine (DLV-012); check the loaded core assembly directly.
            foreach (var name in typeof(HexMap).Assembly.GetReferencedAssemblies())
            {
                StringAssert.DoesNotContain("UnityEngine", name.Name);
                StringAssert.DoesNotContain("UnityEditor", name.Name);
            }
        }
    }
}

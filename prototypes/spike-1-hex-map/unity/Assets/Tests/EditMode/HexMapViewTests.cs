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
        public void MAP_019_grid_has_20_columns_by_12_rows_of_tiles_in_landscape()
        {
            Assert.AreEqual(20, _map.Width);
            Assert.AreEqual(12, _map.Height);
            Assert.AreEqual(20 * 12, _view.TileCount);
            Assert.AreEqual(20 * 12, _view.TileCentres.Length);
            Assert.Greater(_view.WorldBounds.size.x, _view.WorldBounds.size.y, "landscape");
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
        public void MAP_017_tile_positions_match_hard_coded_expected_values()
        {
            var h = Mathf.Sqrt(3f);
            AssertPosition(new Vector3(0f, 0f, 0f), 0, 0);
            AssertPosition(new Vector3(1.5f, -h / 2f, 0f), 1, 0);
            AssertPosition(new Vector3(0f, -h, 0f), 0, 1);
            AssertPosition(new Vector3(1.5f * 19f, -h * (11f + 0.5f), 0f), 19, 11);
        }

        private void AssertPosition(Vector3 expected, int col, int row)
        {
            Assert.AreEqual(0f, Vector3.Distance(expected, _view.TileCentres[row * _map.Width + col]), 1e-3f, "col " + col + " row " + row);
        }

        [TestCase(16f / 9f)]
        [TestCase(4f / 3f)]
        [TestCase(0.4f)]
        public void MAP_019_camera_can_zoom_out_to_show_the_whole_map(float aspect)
        {
            var camGo = new GameObject("test-cam");
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.aspect = aspect;
                var controller = camGo.AddComponent<MapCameraController>();
                controller.SetMapBounds(_view.WorldBounds);
                controller.ZoomOutFully();

                var halfH = cam.orthographicSize;
                var halfW = halfH * aspect;
                var view = new Bounds(camGo.transform.position, new Vector3(halfW * 2f, halfH * 2f, 100f));
                var map = _view.WorldBounds;
                Assert.LessOrEqual(view.min.x, map.min.x, "left edge");
                Assert.GreaterOrEqual(view.max.x, map.max.x, "right edge");
                Assert.LessOrEqual(view.min.y, map.min.y, "bottom edge");
                Assert.GreaterOrEqual(view.max.y, map.max.y, "top edge");
            }
            finally
            {
                Object.DestroyImmediate(camGo);
            }
        }

        [TestCase(16f / 9f)]
        [TestCase(0.4f)]
        public void MAP_019_max_zoom_out_is_derived_from_the_map_bounds_and_the_aspect_ratio(float aspect)
        {
            var camGo = new GameObject("test-cam");
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.aspect = aspect;
                var controller = camGo.AddComponent<MapCameraController>();
                controller.SetMapBounds(_view.WorldBounds);
                controller.ZoomOutFully();

                var e = _view.WorldBounds.extents;
                var expected = Mathf.Max(controller.MinSize, e.y + controller.FitMargin, (e.x + controller.FitMargin) / aspect);
                Assert.AreEqual(expected, controller.MaxSize, 1e-4f);
                Assert.AreEqual(expected, cam.orthographicSize, 1e-4f);
                // Landscape map in a landscape window: the height decides, so the map fills the height of the screen and leaves spare width.
                if (aspect > 1f) Assert.AreEqual(e.y + controller.FitMargin, cam.orthographicSize, 1e-4f);
                // Independent of the formula above: in a narrow window the width decides, so the view is exactly as
                // wide as the map plus the margin on each side, and the map's bounds sit inside the view.
                if (aspect < 1f)
                {
                    Assert.AreEqual(e.x + controller.FitMargin, cam.orthographicSize * aspect, 1e-3f);
                    Assert.LessOrEqual(_view.WorldBounds.max.y - _view.WorldBounds.min.y, cam.orthographicSize * 2f);
                    Assert.LessOrEqual(_view.WorldBounds.max.x, camGo.transform.position.x + cam.orthographicSize * aspect);
                }
            }
            finally
            {
                Object.DestroyImmediate(camGo);
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
        public void TEC_012_core_assembly_references_no_unity_assemblies()
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

using NUnit.Framework;
using UnityEngine;
using WeirdWar.Spike2.Battle.Core;
using WeirdWar.Spike2.Presentation;

namespace WeirdWar.Spike2.Tests
{
    public class BattleMapViewTests
    {
        private GameObject _go;
        private BattleMapView _view;
        private BattleMap _map;

        [SetUp]
        public void SetUp()
        {
            (_map, _) = BattleBootstrap.LoadData("../data", "map.json", "stats.json");
            _go = new GameObject("test-battle-map");
            _view = _go.AddComponent<BattleMapView>();
            _view.Build(_map);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private static Color ColourOf(Transform t) => t.GetComponent<MeshRenderer>().sharedMaterial.color;

        [Test]
        public void BMP_004_both_edges_are_drawn_and_differ_from_open_ground_and_each_other()
        {
            var ground = ColourOf(_view.Ground);
            var west = ColourOf(_view.WestEdge);
            var east = ColourOf(_view.EastEdge);

            Assert.AreNotEqual(ground, west);
            Assert.AreNotEqual(ground, east);
            Assert.AreNotEqual(west, east);
            Assert.Less(_view.WestEdge.GetComponent<MeshFilter>().sharedMesh.bounds.center.x, _map.Width / 2);
            Assert.Greater(_view.EastEdge.GetComponent<MeshFilter>().sharedMesh.bounds.center.x, _map.Width / 2);
        }

        [Test]
        public void BMP_005_every_cover_object_is_drawn_in_a_colour_unlike_open_ground()
        {
            Assert.AreEqual(_map.CoverObjects.Count, _view.CoverCount);
            Assert.Greater(_view.CoverCount, 0);
            foreach (var cover in _view.CoverObjects)
            {
                Assert.AreNotEqual(ColourOf(_view.Ground), ColourOf(cover));
            }
        }

        [Test]
        public void BMP_010_view_bounds_match_the_map_size()
        {
            Assert.AreEqual((float)_map.Width, _view.WorldBounds.size.x, 0.001f);
            Assert.AreEqual((float)_map.Height, _view.WorldBounds.size.y, 0.001f);
        }
    }
}

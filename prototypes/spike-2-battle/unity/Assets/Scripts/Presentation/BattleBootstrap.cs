using System.IO;
using UnityEngine;
using WeirdWar.Spike2.Battle.Core;

namespace WeirdWar.Spike2.Presentation
{
    /// <summary>
    /// Scene entry point. Loads the map and stats data files through the core, draws the map and fits the camera to it.
    /// Unity only reads core state here (DLV-012).
    /// </summary>
    public sealed class BattleBootstrap : MonoBehaviour
    {
        /// <summary>Folder holding the JSON data files, relative to the Unity project folder.</summary>
        public string DataFolder = "../data";
        public string MapFile = "map.json";
        public string StatsFile = "stats.json";
        public string ParametersFile = "parameters.json";
        /// <summary>Seed for the battle's random source (TEC-008). Fixed, so a Reset repeats the same battle.</summary>
        public ulong Seed = 1;

        public BattleMap Map { get; private set; }
        public UnitStats Stats { get; private set; }
        public BattleParameters Parameters { get; private set; }
        public BattleState State { get; private set; }
        public BattleStateView StateView { get; private set; }
        public BattleMapView View { get; private set; }

        private void Start()
        {
            Load();
        }

        public void Load()
        {
            (Map, Stats) = LoadData(DataFolder, MapFile, StatsFile);

            if (!TryGetComponent<BattleMapView>(out var view)) { view = gameObject.AddComponent<BattleMapView>(); }
            View = view;
            View.Build(Map);

            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", DataFolder));
            Parameters = BattleParameters.Load(File.ReadAllText(Path.Combine(root, ParametersFile)));
            State = BattleState.SetUp(Map, Stats, Parameters, Seed);
            if (!TryGetComponent<BattleStateView>(out var stateView)) { stateView = gameObject.AddComponent<BattleStateView>(); }
            StateView = stateView;
            StateView.Show(State, this);

            var cam = Camera.main;
            if (cam != null) { FitCamera(cam, View.WorldBounds); }
        }

        public static (BattleMap, UnitStats) LoadData(string dataFolder, string mapFile, string statsFile)
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", dataFolder));
            var map = BattleMapLoader.Load(File.ReadAllText(Path.Combine(root, mapFile)));
            var stats = UnitStats.Load(File.ReadAllText(Path.Combine(root, statsFile)));
            return (map, stats);
        }

        /// <summary>Centres the orthographic camera on the map and zooms so the whole map shows, with a small margin.</summary>
        public static void FitCamera(Camera cam, Bounds map, float margin = 3f)
        {
            var forHeight = map.extents.y + margin;
            var forWidth = (map.extents.x + margin) / Mathf.Max(cam.aspect, 0.01f);
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(forHeight, forWidth);
            cam.transform.position = new Vector3(map.center.x, map.center.y, -10f);
        }
    }
}

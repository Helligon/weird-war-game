using System.IO;
using UnityEngine;
using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Presentation
{
    /// <summary>
    /// Scene entry point. Loads the sample map and scenario through the core, builds the game state and draws the map.
    /// The Unity layer only reads core state (DLV-012).
    /// </summary>
    public sealed class HexMapBootstrap : MonoBehaviour
    {
        /// <summary>Folder holding the sample JSON files, relative to the Unity project folder.</summary>
        public string MapsFolder = "../maps";
        public string MapFile = "sample.json";
        public string ScenarioFile = "sample-scenario.json";

        public GameState State { get; private set; }
        public HexMapView View { get; private set; }

        private void Start()
        {
            Load();
        }

        public void Load()
        {
            State = LoadState(MapsFolder, MapFile, ScenarioFile);

            if (!TryGetComponent<HexMapView>(out var view)) { view = gameObject.AddComponent<HexMapView>(); }
            View = view;
            View.Build(State.Map);

            var cam = Camera.main;
            if (cam != null)
            {
                if (!cam.TryGetComponent<MapCameraController>(out var controller)) { controller = cam.gameObject.AddComponent<MapCameraController>(); }
                controller.SetMapBounds(View.WorldBounds);
                controller.FitToMap();
            }
        }

        public static GameState LoadState(string mapsFolder, string mapFile, string scenarioFile)
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", mapsFolder));
            var map = MapLoader.Load(File.ReadAllText(Path.Combine(root, mapFile)));
            var config = GameConfigLoader.Load(File.ReadAllText(Path.Combine(root, scenarioFile)));
            return GameState.NewGame(map, config);
        }
    }
}

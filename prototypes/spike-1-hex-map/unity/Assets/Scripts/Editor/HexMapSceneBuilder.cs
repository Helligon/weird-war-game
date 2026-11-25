using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WeirdWar.Spike1.Presentation;

namespace WeirdWar.Spike1.Editor
{
    /// <summary>Creates Assets/Scenes/HexMap.unity and adds it to the build settings. Re-runnable.</summary>
    public static class HexMapSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/HexMap.unity";

        [MenuItem("Weird War/Build HexMap scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camObject.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 26f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            camObject.transform.position = new Vector3(0f, 0f, -10f);
            camObject.AddComponent<MapCameraController>();

            var mapObject = new GameObject("HexMap");
            mapObject.AddComponent<HexMapBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WeirdWar.Spike2.Presentation;

namespace WeirdWar.Spike2.Editor
{
    /// <summary>Creates Assets/Scenes/Battle.unity and adds it to the build settings. Re-runnable.</summary>
    public static class BattleSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Battle.unity";

        [MenuItem("Weird War/Build Battle scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camObject.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 36f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            camObject.transform.position = new Vector3(50f, 30f, -10f);

            var mapObject = new GameObject("BattleMap");
            mapObject.AddComponent<BattleBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }
}

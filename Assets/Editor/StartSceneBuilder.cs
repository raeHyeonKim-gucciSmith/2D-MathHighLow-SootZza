using System.Collections.Generic;
using System.Linq;
using MathHighLow.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathHighLow.EditorTools
{
    [InitializeOnLoad]
    public static class StartSceneBuilder
    {
        private const string StartScenePath = "Assets/Scenes/StartScene.unity";
        private const string GameScenePath = "Assets/Scenes/MathHighLowGame.unity";

        static StartSceneBuilder()
        {
            EditorApplication.delayCall += BuildMissingStartScene;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += BuildMissingStartScene;
            }
        }

        private static void BuildMissingStartScene()
        {
            if (!EditorApplication.isCompiling &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                !System.IO.File.Exists(StartScenePath))
            {
                BuildStartScene();
            }
        }

        [MenuItem("Math High Low/Build Start Scene")]
        public static void BuildStartScene()
        {
            Scene previouslyActiveScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);

            GameObject cameraObject = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            GameObject controllerObject = new GameObject("StartSceneController");
            StartSceneController controller =
                controllerObject.AddComponent<StartSceneController>();
            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/startScene.png");
            controller.ConfigureForEditor(background);

            EditorSceneManager.SaveScene(scene, StartScenePath);
            EditorSceneManager.CloseScene(scene, true);
            if (previouslyActiveScene.IsValid() && previouslyActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previouslyActiveScene);
            }

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(StartScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
            scenes.AddRange(EditorBuildSettings.scenes.Where(existing =>
                existing.path != StartScenePath && existing.path != GameScenePath));
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log("StartScene created and placed first in Build Settings.");
        }
    }
}

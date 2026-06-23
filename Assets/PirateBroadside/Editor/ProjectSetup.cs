using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PirateBroadside.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/PirateBroadside/Scenes/Main.unity";
        private const string TitleArtPath = "Assets/PirateBroadside/Resources/TitleArt.png";
        private const string SmokeTestKey = "PirateBroadside.SmokeTest";
        private static int smokeFrames;

        static ProjectSetup()
        {
            if (EditorPrefs.GetBool(SmokeTestKey, false))
            {
                EditorApplication.update -= SmokeUpdate;
                EditorApplication.update += SmokeUpdate;
            }
        }

        [MenuItem("Pirate Broadside/Configure Project")]
        public static void ConfigureProject()
        {
            Directory.CreateDirectory("Assets/PirateBroadside/Scenes");
            ConfigureTitleArt();
            ConfigurePlayer();
            CreateMainScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Pirate Broadside project configured.");
        }

        [MenuItem("Pirate Broadside/Build WebGL")]
        public static void BuildWebGL()
        {
            ConfigureProject();
            var outputPath = Path.GetFullPath("Build/WebGL");
            Directory.CreateDirectory(outputPath);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"WebGL build failed: {report.summary.result}, {report.summary.totalErrors} errors");
            }

            Debug.Log($"WebGL build completed: {outputPath} ({report.summary.totalSize} bytes)");
        }

        public static void RunSmokeTest()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            smokeFrames = 0;
            EditorPrefs.SetBool(SmokeTestKey, true);
            EditorApplication.update += SmokeUpdate;
            EditorApplication.isPlaying = true;
        }

        private static void SmokeUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            smokeFrames++;
            if (smokeFrames == 8)
            {
                var game = PirateGame.Instance;
                if (game == null)
                {
                    FailSmokeTest("PirateGame was not created.");
                    return;
                }
                game.BeginBattle();
            }

            if (smokeFrames < 24)
            {
                return;
            }

            try
            {
                var game = PirateGame.Instance;
                var pirates = UnityEngine.Object.FindObjectsByType<EnemyShip>(FindObjectsSortMode.None);
                var camera = Camera.main;
                var hud = UnityEngine.Object.FindFirstObjectByType<BattleHud>();
                var galleon = Resources.Load<GameObject>("Models/StylizedGalleon");
                var shipRenderers = game?.Player == null ? 0 : game.Player.GetComponentsInChildren<Renderer>().Length;
                if (game == null || game.Player == null || pirates.Length != 3 || camera == null || hud == null || galleon == null || shipRenderers < 20)
                {
                    FailSmokeTest($"Runtime objects missing: game={game != null}, player={game?.Player != null}, pirates={pirates.Length}, camera={camera != null}, hud={hud != null}, galleon={galleon != null}, renderers={shipRenderers}");
                    return;
                }

                Debug.Log($"PIRATE_BROADSIDE_SMOKE_OK player={game.Player.name} pirates={pirates.Length} camera={camera.name}");
                EditorPrefs.DeleteKey(SmokeTestKey);
                EditorApplication.update -= SmokeUpdate;
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                FailSmokeTest(exception.ToString());
            }
        }

        private static void FailSmokeTest(string message)
        {
            Debug.LogError("PIRATE_BROADSIDE_SMOKE_FAILED " + message);
            EditorPrefs.DeleteKey(SmokeTestKey);
            EditorApplication.update -= SmokeUpdate;
            EditorApplication.Exit(1);
        }

        private static void ConfigureTitleArt()
        {
            AssetDatabase.ImportAsset(TitleArtPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(TitleArtPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Masafykun Games";
            PlayerSettings.productName = "Pirate Broadside";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.WebGL, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "PROJECT:PirateBroadside";
        }

        private static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main";
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }
    }
}

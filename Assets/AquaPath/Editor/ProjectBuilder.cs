using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using AquaPath.Core;

namespace AquaPath.Editor
{
    public static class ProjectBuilder
    {
        public const string ScenePath = "Assets/AquaPath/Scenes/Main.unity";

        [MenuItem("Aqua Path/Build Game Assets")]
        public static void BuildAssets()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before rebuilding game assets.");
            Directory.CreateDirectory("Assets/AquaPath/Scenes");
            Directory.CreateDirectory("Assets/AquaPath/Resources/Prefabs");
            Directory.CreateDirectory("Assets/AquaPath/Resources/Levels");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            var cam = camera.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.016f, .075f, .12f);
            cam.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            new GameObject("Aqua Path Connect", typeof(AquaApp));
            foreach (Shape shape in Enum.GetValues(typeof(Shape)))
            {
                string label = shape == Shape.Tee ? "T" : shape == Shape.Source ? "Start" : shape == Shape.Target ? "End" : shape.ToString();
                var go = new GameObject("Pipe_" + label, typeof(RectTransform), typeof(PipeView));
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 180);
                var view = go.GetComponent<PipeView>();
                var field = typeof(PipeView).GetField("Shape");
                if (field != null) field.SetValue(view, shape);
                PrefabUtility.SaveAsPrefabAsset(go, "Assets/AquaPath/Resources/Prefabs/Pipe_" + label + ".prefab");
                UnityEngine.Object.DestroyImmediate(go);
            }
            var catalog = new Catalog();
            catalog.levels = new SavedLevel[30];
            for (int n = 1; n <= 30; n++)
            {
                var p = LevelFactory.Create(n);
                catalog.levels[n - 1] = new SavedLevel { level = n, size = p.Size, source = p.Source, target = p.Target, idealMoves = p.IdealMoves, cells = p.Cells, route = p.Route };
            }
            File.WriteAllText("Assets/AquaPath/Resources/Levels/Catalog.json", JsonUtility.ToJson(catalog, true));
            PlayerSettings.companyName = "Aqua Path";
            PlayerSettings.productName = "Aqua Path Connect";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.aquapath.glassconnect");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PortraitGameView();
            Debug.Log("[Aqua Build] Main scene, six pipe prefabs, 30-level catalog and Android portrait settings saved.");
        }

        [MenuItem("Aqua Path/Portrait Game View")]
        public static void PortraitGameView()
        {
            SetGameView(540, 960);
        }

        public static void SetGameView(int width, int height)
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
            var groupType = group.GetType();
            var total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            int selected = -1;
            for (int i = 0; i < total; i++)
            {
                var item = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)item.GetType().GetProperty("width").GetValue(item) == width && (int)item.GetType().GetProperty("height").GetValue(item) == height)
                    selected = i;
            }
            if (selected < 0)
            {
                var kind = asm.GetType("UnityEditor.GameViewSizeType");
                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var size = Activator.CreateInstance(sizeType, new object[] { Enum.ToObject(kind, 1), width, height, "Aqua Portrait " + width + "x" + height });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                selected = total;
            }
            var gameType = asm.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameType);
            gameType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(window, selected);
            window.Show();
            window.Focus();
        }

        [MenuItem("Aqua Path/Build Windows Player")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/AquaPathConnect.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("[Aqua Build] Windows player succeeded: " + report.summary.totalSize + " bytes.");
        }

        [Serializable] private class Catalog { public SavedLevel[] levels; }
        [Serializable] private class SavedLevel { public int level, size, source, target, idealMoves; public Cell[] cells; public int[] route; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using AquaPath.Core;

namespace AquaPath.Editor
{
    /// <summary>Editor-only acceptance exercise; actions use the same buttons as a player.</summary>
    [InitializeOnLoad]
    public static class PlayVerification
    {
        static readonly int[] Levels = { 1, 4, 8, 14, 19, 25 };
        static readonly List<string> Results = new List<string>();
        static int stage, levelIndex, cellIndex;
        static double nextAction, deadline;
        static bool running;
        static int[] initial;
        static Dictionary<string, int?> preferences;
        static string OutDir => Path.GetFullPath("Evidence");

        static PlayVerification() { EditorApplication.update += Tick; }

        [MenuItem("Aqua Path/QA/Run Play Verification")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || AquaApp.Instance == null)
                throw new InvalidOperationException("Enter Play Mode first.");
            Directory.CreateDirectory(OutDir);
            Results.Clear();
            SavePreferences();
            stage = 0; levelIndex = 0; cellIndex = 0;
            nextAction = EditorApplication.timeSinceStartup + 1;
            deadline = EditorApplication.timeSinceStartup + 300;
            running = true;
            Application.logMessageReceived += OnLog;
            AquaApp.Instance.ShowMenu();
            File.WriteAllText(Path.Combine(OutDir, "play-verification.txt"), "RUNNING\n");
        }

        [MenuItem("Aqua Path/QA/Show Level 1")]
        public static void ShowFirst() { AquaApp.Instance.ShowGame(1); }

        [MenuItem("Aqua Path/QA/Show Level 8")]
        public static void ShowEight() { AquaApp.Instance.ShowGame(8); }

        [MenuItem("Aqua Path/QA/Capture Gameplay")]
        public static void CaptureGameplay() { Capture("gameplay-current"); }

        static void Tick()
        {
            if (!running || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextAction) return;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Play verification timed out at stage " + stage);
                var app = AquaApp.Instance;
                if (app == null) return;
                switch (stage)
                {
                    case 0:
                        Check(app.ScreenName == "Menu", "Main menu starts");
                        Capture("menu");
                        Click("Play");
                        stage = 1; Wait(.5); break;
                    case 1:
                        Check(app.ScreenName == "Levels", "Play opens level selection");
                        Capture("levels");
                        Click("Level_1");
                        stage = 2; Wait(2); break;
                    case 2:
                        Check(app.Board != null && app.CurrentLevel == 1, "Level button opens puzzle");
                        Capture("level-01-start");
                        initial = Rotations(app.Board.Puzzle);
                        var index = FirstMovable(app.Board.Puzzle);
                        app.Board.Views[index].GetComponent<Button>().onClick.Invoke();
                        app.Board.Views[index].GetComponent<Button>().onClick.Invoke();
                        Check(app.Board.Puzzle.Moves == 1, "Rapid repeat tap is ignored during rotation");
                        app.Board.Restart();
                        Check(app.Board.Puzzle.Moves == 0 && Same(initial, Rotations(app.Board.Puzzle)), "Restart during rotation restores original board");
                        stage = 3; Wait(.5); break;
                    case 3:
                        Click("Pause");
                        Check(app.ScreenName == "Pause", "Pause opens");
                        int before = app.Board.Puzzle.Moves;
                        app.Board.Tap(FirstMovable(app.Board.Puzzle));
                        Check(app.Board.Puzzle.Moves == before, "Paused board rejects input");
                        app.Resume();
                        Check(app.ScreenName == "Game", "Resume returns to board");
                        stage = 4; Wait(.5); break;
                    case 4:
                        app.ShowGame(Levels[levelIndex]);
                        stage = 5; cellIndex = 0; Wait(1.2); break;
                    case 5:
                        var puzzle = app.Board.Puzzle;
                        if (puzzle.Solved) { stage = 6; Wait(.25); break; }
                        int rotateIndex = -1;
                        foreach (int i in puzzle.Route)
                        {
                            var cell = puzzle.Cells[i];
                            if (!cell.Locked && Directions.Mask(cell.Shape, cell.Rotation) != Directions.Mask(cell.Shape, cell.Solution)) { rotateIndex = i; break; }
                        }
                        if (rotateIndex < 0) throw new Exception("Stored route exhausted without reaching target");
                        app.Board.Views[rotateIndex].GetComponent<Button>().onClick.Invoke();
                        cellIndex++;
                        if (cellIndex > 250) throw new Exception("Too many rotations");
                        Wait(.25); break;
                    case 6:
                        if (app.ScreenName != "Victory") { Wait(.2); break; }
                        Check(app.Board.Views[app.Board.Puzzle.Target].Fill >= .98f, "Level " + app.CurrentLevel + ": water reaches target before victory");
                        Check(SaveStore.BestStars(app.CurrentLevel) >= 1 && SaveStore.UnlockedLevel >= Math.Min(30, app.CurrentLevel + 1), "Level " + app.CurrentLevel + ": stars and unlock saved");
                        Capture("level-" + app.CurrentLevel.ToString("00") + "-victory");
                        stage = 7; Wait(.5); break;
                    case 7:
                        int previous = app.CurrentLevel;
                        Click("Next");
                        Check(app.CurrentLevel == previous + 1 && app.ScreenName == "Game", "Next loads " + (previous + 1));
                        levelIndex++;
                        if (levelIndex < Levels.Length) stage = 4;
                        else stage = 8;
                        Wait(.5); break;
                    case 8:
                        bool music = SaveStore.MusicEnabled;
                        app.ShowMenu(); app.ShowSettings();
                        Click("Music Toggle");
                        Check(SaveStore.MusicEnabled != music, "Music setting toggles and persists");
                        app.ShowMenu(); app.ShowSettings();
                        Check(SaveStore.MusicEnabled != music, "Music setting survives screen recreation");
                        SaveStore.RecordVictory(1, 3, 3); SaveStore.RecordVictory(1, 1, 100);
                        Check(SaveStore.BestStars(1) == 3 && SaveStore.BestMoves(1) <= 3, "A worse replay never lowers saved best stars or moves");
                        app.ShowGame(8);
                        stage = 9; Wait(1); break;
                    case 9:
                        // Capture the actual board with a connected water network before popup overlay.
                        var p = app.Board.Puzzle;
                        int next = -1;
                        foreach (int i in p.Route)
                            if (!p.Cells[i].Locked && Directions.Mask(p.Cells[i].Shape, p.Cells[i].Rotation) != Directions.Mask(p.Cells[i].Shape, p.Cells[i].Solution)) { next = i; break; }
                        if (next >= 0 && !p.Solved) { app.Board.Views[next].GetComponent<Button>().onClick.Invoke(); Wait(.25); }
                        else { stage = 10; Wait(.8); }
                        break;
                    case 10:
                        Capture("glass-water-board");
                        stage = 11; Wait(.5); break;
                    case 11:
                        Finish(); break;
                }
            }
            catch (Exception e)
            {
                Results.Add("FAIL: " + e);
                Finish();
            }
        }

        static void Wait(double seconds) { nextAction = EditorApplication.timeSinceStartup + seconds; }
        static void Check(bool ok, string text)
        {
            if (!ok) throw new Exception(text);
            Results.Add("PASS: " + text); Debug.Log("[Aqua QA] PASS: " + text);
        }
        static void Click(string name)
        {
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var b in buttons) if (b.gameObject.name == name && b.gameObject.activeInHierarchy && b.interactable) { b.onClick.Invoke(); return; }
            throw new Exception("Active button not found: " + name);
        }
        static int FirstMovable(Puzzle p) { for (int i = 0; i < p.Cells.Length; i++) if (!p.Cells[i].Locked) return i; return -1; }
        static int[] Rotations(Puzzle p) { var a = new int[p.Cells.Length]; for (int i = 0; i < a.Length; i++) a[i] = p.Cells[i].Rotation; return a; }
        static bool Same(int[] a, int[] b) { for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }
        static void Capture(string name) { Directory.CreateDirectory(OutDir); ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, name + ".png"), 2); }
        static void OnLog(string text, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) Results.Add("CONSOLE ERROR: " + text); }
        static void SavePreferences()
        {
            preferences = new Dictionary<string, int?>();
            var keys = new List<string> { "SaveVersion", "UnlockedLevel", "Music", "Sfx", "Haptics" };
            for (int i = 1; i <= 30; i++) { keys.Add("Level." + i + ".Stars"); keys.Add("Level." + i + ".Moves"); }
            foreach (string key in keys) { string full = SaveStore.Prefix + key; preferences[full] = PlayerPrefs.HasKey(full) ? (int?)PlayerPrefs.GetInt(full) : null; }
        }
        static void Finish()
        {
            running = false;
            Application.logMessageReceived -= OnLog;
            bool failed = Results.Exists(s => s.StartsWith("FAIL") || s.StartsWith("CONSOLE ERROR"));
            Results.Add(failed ? "FAILED" : "ALL PLAY CHECKS PASSED");
            File.WriteAllLines(Path.Combine(OutDir, "play-verification.txt"), Results);
            if (preferences != null) foreach (var kv in preferences) { if (kv.Value.HasValue) PlayerPrefs.SetInt(kv.Key, kv.Value.Value); else PlayerPrefs.DeleteKey(kv.Key); }
            PlayerPrefs.Save();
            Time.timeScale = 1;
            if (AquaApp.Instance != null) AquaApp.Instance.ShowGame(1);
            Debug.Log("[Aqua QA] " + (failed ? "FAILED — see Evidence/play-verification.txt" : "ALL PLAY CHECKS PASSED"));
        }
    }
}

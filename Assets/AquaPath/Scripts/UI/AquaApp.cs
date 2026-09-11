using System;
using AquaPath.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AquaPath
{
    [DefaultExecutionOrder(-100)]
    public sealed class AquaApp : MonoBehaviour
    {
        public static AquaApp Instance;

        public BoardView Board;
        public int CurrentLevel;
        public string ScreenName { get; private set; }

        private Canvas canvas;
        private RectTransform safeArea;
        private GameObject screenRoot;
        private GameObject pauseOverlay;
        private GameObject settingsRoot;
        private GameObject settingsReturnRoot;
        private string settingsReturnScreen;
        private Text movesText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildShell();
        }

        private void Start()
        {
            ShowMenu();
        }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; Time.timeScale = 1; }
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (ScreenName == "Settings") CloseSettings();
            else if (ScreenName == "Game") Pause();
            else if (ScreenName == "Pause") Resume();
            else if (ScreenName == "Levels" || ScreenName == "Help") ShowMenu();
        }

        public void ShowMenu()
        {
            RectTransform root = BeginScreen("Menu", "Menu Screen");

            Text overline = AquaUI.Text("Game Type", root, "GLASS PIPE PUZZLES", 22, AquaUI.Cyan,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Fixed(overline.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -215f), new Vector2(760f, 42f));

            Text title = AquaUI.Text("Title", root, "AQUA\nPATH", 102, AquaUI.White,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Fixed(title.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -365f), new Vector2(850f, 220f));
            title.lineSpacing = .76f;
            AquaUI.AddOutline(title, new Color(0f, .9f, 1f, .16f), new Vector2(4f, -4f));

            BuildLogo(root, new Vector2(0f, -555f));

            Image menuCard = AquaUI.Image("Menu Card", root, new Color(.035f, .20f, .25f, .8f), true);
            AquaUI.Fixed(menuCard.rectTransform, new Vector2(.5f, .5f), new Vector2(0f, -235f), new Vector2(860f, 510f));
            AquaUI.AddOutline(menuCard, new Color(.3f, 1f, 1f, .12f), new Vector2(2f, -2f));

            Button play = AquaUI.Button("Play", menuCard.transform, "PLAY", ShowLevels, true, 38);
            AquaUI.Anchor(play.transform as RectTransform, new Vector2(.08f, .68f), new Vector2(.92f, .91f), Vector2.zero, Vector2.zero);
            Button help = AquaUI.Button("Help", menuCard.transform, "HOW TO PLAY", ShowHelp, false, 29);
            AquaUI.Anchor(help.transform as RectTransform, new Vector2(.08f, .39f), new Vector2(.92f, .61f), Vector2.zero, Vector2.zero);
            Button settings = AquaUI.Button("Settings", menuCard.transform, "SETTINGS", ShowSettings, false, 29);
            AquaUI.Anchor(settings.transform as RectTransform, new Vector2(.08f, .10f), new Vector2(.92f, .32f), Vector2.zero, Vector2.zero);

            Text progress = AquaUI.Text("Progress", root,
                SaveStore.UnlockedLevel >= SaveStore.LevelCount ? "ALL LEVELS OPEN" : "LEVEL " + SaveStore.UnlockedLevel + " OF " + SaveStore.LevelCount + " UNLOCKED",
                20, AquaUI.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Fixed(progress.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 82f), new Vector2(800f, 52f));
        }

        public void ShowLevels()
        {
            RectTransform root = BeginScreen("Levels", "Levels Screen");
            BuildHeader(root, "SELECT LEVEL", ShowMenu);

            Text sub = AquaUI.Text("Level Progress", root, "CONNECT EVERY CURRENT TO OPEN THE NEXT", 18,
                AquaUI.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Anchor(sub.rectTransform, new Vector2(.08f, 1f), new Vector2(.92f, 1f), new Vector2(0f, -205f), new Vector2(0f, -158f));

            RectTransform scrollRect = AquaUI.Rect("Level Scroll", root);
            AquaUI.Anchor(scrollRect, new Vector2(.055f, .04f), new Vector2(.945f, 1f), new Vector2(0f, 0f), new Vector2(0f, -225f));
            ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 34f;

            Image viewportImage = AquaUI.Image("Viewport", scrollRect, new Color(1f, 1f, 1f, .001f), true);
            AquaUI.StretchRect(viewportImage.rectTransform);
            viewportImage.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewportImage.rectTransform;

            RectTransform content = AquaUI.Rect("Level Grid", viewportImage.transform);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(424f, 145f);
            grid.spacing = new Vector2(22f, 22f);
            grid.padding = new RectOffset(6, 6, 8, 28);
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            int unlocked = SaveStore.UnlockedLevel;
            for (int level = 1; level <= SaveStore.LevelCount; level++)
                BuildLevelButton(content, level, level <= unlocked);
        }

        public void ShowGame(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 1, SaveStore.LevelCount);
            RectTransform root = BeginScreen("Game", "Game Screen");

            Text levelText = AquaUI.Text("Level Title", root, "LEVEL " + CurrentLevel.ToString("00"), 34,
                AquaUI.White, TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Fixed(levelText.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -84f), new Vector2(420f, 72f));

            Text stage = AquaUI.Text("Stage", root, "FLOW ALIGNMENT", 16, AquaUI.Cyan,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Fixed(stage.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -129f), new Vector2(400f, 38f));

            Button pause = AquaUI.Button("Pause", root, "II", Pause, false, 28);
            AquaUI.Fixed(pause.transform as RectTransform, new Vector2(0f, 1f), new Vector2(76f, -96f), new Vector2(108f, 76f));
            Button restart = AquaUI.Button("Restart", root, "RESET", RestartCurrent, false, 19);
            AquaUI.Fixed(restart.transform as RectTransform, new Vector2(1f, 1f), new Vector2(-90f, -96f), new Vector2(142f, 76f));

            Image statBar = AquaUI.Image("Stats", root, new Color(.04f, .25f, .30f, .8f), true);
            AquaUI.Fixed(statBar.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -207f), new Vector2(650f, 78f));
            movesText = AquaUI.Text("Moves", statBar.transform, "MOVES  0", 23, AquaUI.White,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.StretchRect(movesText.rectTransform);

            Image boardPanel = AquaUI.Image("Board Panel", root, new Color(.025f, .16f, .20f, .94f), true);
            AquaUI.Fixed(boardPanel.rectTransform, new Vector2(.5f, .5f), new Vector2(0f, -5f), new Vector2(964f, 964f));
            AquaUI.AddOutline(boardPanel, new Color(.25f, .95f, 1f, .18f), new Vector2(3f, -3f));
            Board = boardPanel.gameObject.AddComponent<BoardView>();
            Board.Completed += OnBoardCompleted;
            Board.Changed += UpdateMoves;
            Board.Load(CurrentLevel);

            Text hintCopy = AquaUI.Text("Hint Copy", root, CurrentLevel == 1 ? "GLOWING TILE SHOWS YOUR NEXT MOVE" : "FIND THE CONTINUOUS WATER PATH", 17,
                AquaUI.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Fixed(hintCopy.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 175f), new Vector2(820f, 42f));

            Button hint = AquaUI.Button("Hint", root, "ROTATE HINT", () => Board.Hint(), false, 25);
            AquaUI.Fixed(hint.transform as RectTransform, new Vector2(.5f, 0f), new Vector2(0f, 95f), new Vector2(440f, 82f));
            UpdateMoves();
        }

        public void ShowHelp()
        {
            RectTransform root = BeginScreen("Help", "Help Screen");
            BuildHeader(root, "HOW TO PLAY", ShowMenu);

            Image card = AquaUI.Image("Help Card", root, new Color(.035f, .20f, .25f, .86f), true);
            AquaUI.Anchor(card.rectTransform, new Vector2(.07f, .12f), new Vector2(.93f, .86f), Vector2.zero, Vector2.zero);
            AquaUI.AddOutline(card, new Color(.3f, 1f, 1f, .12f), new Vector2(2f, -2f));

            BuildHelpStep(card.transform, .81f, "1", "ROTATE THE GLASS", "Tap any unlocked pipe to turn it clockwise.");
            BuildHelpStep(card.transform, .60f, "2", "MATCH EVERY OPENING", "Water flows only where neighboring pipe ends face each other.");
            BuildHelpStep(card.transform, .39f, "3", "CONNECT IN TO OUT", "Build one continuous route and let the water reach OUT.");
            BuildHelpStep(card.transform, .18f, "*", "EARN THREE STARS", "Use fewer moves. A hint rotates the next incorrect route tile once.");

            Text tip = AquaUI.Text("Tip", root, "TIP  Restart always restores the same puzzle.", 19, AquaUI.Lime,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Fixed(tip.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 90f), new Vector2(900f, 50f));
        }

        public void ShowSettings()
        {
            if (ScreenName == "Settings") return;
            settingsReturnRoot = screenRoot;
            settingsReturnScreen = ScreenName;

            ScreenName = "Settings";
            RectTransform root = AquaUI.Stretch("Settings Screen", safeArea);
            settingsRoot = root.gameObject;
            screenRoot = settingsRoot;
            Image background = AquaUI.Image("Settings Background", root, AquaUI.Deep);
            AquaUI.StretchRect(background.rectTransform);
            BuildHeader(root, "SETTINGS", CloseSettings);

            Image card = AquaUI.Image("Settings Card", root, new Color(.035f, .20f, .25f, .88f), true);
            AquaUI.Fixed(card.rectTransform, new Vector2(.5f, .5f), new Vector2(0f, 30f), new Vector2(880f, 650f));
            AquaUI.AddOutline(card, new Color(.3f, 1f, 1f, .12f), new Vector2(2f, -2f));

            BuildToggle(card.transform, "Music Toggle", "MUSIC", "Ambient underwater soundtrack",
                () => SaveStore.MusicEnabled, value => AudioController.Instance.SetMusic(value), .73f);
            BuildToggle(card.transform, "SFX Toggle", "SOUND EFFECTS", "Turns, taps and completed routes",
                () => SaveStore.SfxEnabled, value => AudioController.Instance.SetSfx(value), .47f);
            BuildToggle(card.transform, "Haptics Toggle", "HAPTICS", "Gentle feedback on supported devices",
                () => SaveStore.HapticsEnabled, value => AudioController.Instance.SetHaptics(value), .21f);

            Text foot = AquaUI.Text("Settings Footer", root, "YOUR CHOICES ARE SAVED AUTOMATICALLY", 17, AquaUI.Muted,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Fixed(foot.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 100f), new Vector2(800f, 40f));
        }

        public void Pause()
        {
            if (ScreenName != "Game" || Board == null || Board.CompletionShown) return;
            Board.SetPaused(true);
            Time.timeScale = 0;
            ScreenName = "Pause";
            RectTransform overlay = BuildModalBackdrop("Pause Overlay");
            pauseOverlay = overlay.gameObject;

            Image card = AquaUI.Image("Pause Card", overlay, new Color(.035f, .20f, .25f, .98f), true);
            AquaUI.Fixed(card.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(820f, 700f));
            Text title = AquaUI.Text("Pause Title", card.transform, "FLOW PAUSED", 52, AquaUI.White,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Anchor(title.rectTransform, new Vector2(.08f, .76f), new Vector2(.92f, .94f), Vector2.zero, Vector2.zero);
            Text subtitle = AquaUI.Text("Pause Subtitle", card.transform, "The water will wait for you.", 22, AquaUI.Muted);
            AquaUI.Anchor(subtitle.rectTransform, new Vector2(.08f, .66f), new Vector2(.92f, .76f), Vector2.zero, Vector2.zero);

            Button resume = AquaUI.Button("Resume", card.transform, "RESUME", Resume, true, 31);
            AquaUI.Anchor(resume.transform as RectTransform, new Vector2(.08f, .47f), new Vector2(.92f, .62f), Vector2.zero, Vector2.zero);
            Button restart = AquaUI.Button("Restart", card.transform, "RESTART", RestartFromPause, false, 26);
            AquaUI.Anchor(restart.transform as RectTransform, new Vector2(.08f, .29f), new Vector2(.48f, .43f), Vector2.zero, Vector2.zero);
            Button settings = AquaUI.Button("Settings", card.transform, "SETTINGS", ShowSettings, false, 24);
            AquaUI.Anchor(settings.transform as RectTransform, new Vector2(.52f, .29f), new Vector2(.92f, .43f), Vector2.zero, Vector2.zero);
            Button levels = AquaUI.Button("Levels", card.transform, "LEVELS", ShowLevels, false, 24);
            AquaUI.Anchor(levels.transform as RectTransform, new Vector2(.08f, .11f), new Vector2(.48f, .25f), Vector2.zero, Vector2.zero);
            Button menu = AquaUI.Button("Menu", card.transform, "MENU", ShowMenu, false, 24);
            AquaUI.Anchor(menu.transform as RectTransform, new Vector2(.52f, .11f), new Vector2(.92f, .25f), Vector2.zero, Vector2.zero);
        }

        public void Resume()
        {
            if (ScreenName != "Pause") return;
            if (pauseOverlay != null) Destroy(pauseOverlay);
            pauseOverlay = null;
            ScreenName = "Game";
            Time.timeScale = 1;
            if (Board != null) Board.SetPaused(false);
        }

        public void NextLevel()
        {
            if (CurrentLevel >= SaveStore.LevelCount) ShowLevels();
            else ShowGame(CurrentLevel + 1);
        }

        private void BuildShell()
        {
            GameObject canvasObject = new GameObject("Aqua Path Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            AquaUI.BuildBackdrop(canvasObject.transform);

            safeArea = AquaUI.Stretch("Safe Area", canvasObject.transform);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            GameObject audio = new GameObject("Audio Controller", typeof(AudioController));
            audio.transform.SetParent(transform, false);
            EnsureEventSystem();
        }

        private RectTransform BeginScreen(string screenName, string objectName)
        {
            Time.timeScale = 1;
            if (settingsReturnRoot != null && settingsReturnRoot != screenRoot)
            {
                settingsReturnRoot.SetActive(false);
                Destroy(settingsReturnRoot);
            }
            if (settingsRoot != null) Destroy(settingsRoot);
            settingsRoot = null;
            settingsReturnRoot = null;
            if (screenRoot != null)
            {
                screenRoot.SetActive(false);
                Destroy(screenRoot);
            }
            pauseOverlay = null;
            Board = null;
            movesText = null;
            ScreenName = screenName;
            RectTransform root = AquaUI.Stretch(objectName, safeArea);
            screenRoot = root.gameObject;
            return root;
        }

        private void CloseSettings()
        {
            if (ScreenName != "Settings") return;
            if (settingsRoot != null) Destroy(settingsRoot);
            settingsRoot = null;
            screenRoot = settingsReturnRoot;
            if (screenRoot != null) screenRoot.SetActive(true);
            ScreenName = string.IsNullOrEmpty(settingsReturnScreen) ? "Menu" : settingsReturnScreen;
            settingsReturnRoot = null;
        }

        private void RestartCurrent()
        {
            if (Board == null) return;
            Board.Restart();
            UpdateMoves();
        }

        private void RestartFromPause()
        {
            if (pauseOverlay != null) Destroy(pauseOverlay);
            pauseOverlay = null;
            ScreenName = "Game";
            Time.timeScale = 1;
            if (Board != null) Board.SetPaused(false);
            RestartCurrent();
        }

        private void UpdateMoves()
        {
            if (movesText != null && Board != null && Board.Puzzle != null)
                movesText.text = "MOVES  " + Board.Puzzle.Moves + "     PAR  " + Board.Puzzle.IdealMoves;
        }

        private void OnBoardCompleted()
        {
            if (Board == null || Board.Puzzle == null || ScreenName != "Game") return;
            int moves = Board.Puzzle.Moves;
            int stars = ProgressRating.Stars(moves, Board.Puzzle.IdealMoves);
            SaveStore.RecordVictory(CurrentLevel, stars, moves);
            if (AudioController.Instance != null) AudioController.Instance.PlayVictory();
            ScreenName = "Victory";

            RectTransform overlay = BuildModalBackdrop("Victory Overlay");
            Image card = AquaUI.Image("Victory Card", overlay, new Color(.035f, .20f, .25f, .985f), true);
            AquaUI.Fixed(card.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(880f, 790f));
            AquaUI.AddOutline(card, new Color(.25f, 1f, 1f, .22f), new Vector2(3f, -3f));

            Text complete = AquaUI.Text("Complete", card.transform, "CURRENT COMPLETE", 20, AquaUI.Cyan,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Anchor(complete.rectTransform, new Vector2(.08f, .84f), new Vector2(.92f, .94f), Vector2.zero, Vector2.zero);
            Text title = AquaUI.Text("Victory Title", card.transform, "FLOW RESTORED", 50, AquaUI.White,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Anchor(title.rectTransform, new Vector2(.08f, .70f), new Vector2(.92f, .86f), Vector2.zero, Vector2.zero);
            Text starsText = AquaUI.Text("Stars", card.transform, StarString(stars), 74, AquaUI.Lime,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Anchor(starsText.rectTransform, new Vector2(.08f, .51f), new Vector2(.92f, .70f), Vector2.zero, Vector2.zero);
            Text stats = AquaUI.Text("Victory Stats", card.transform,
                moves + " MOVES     BEST " + SaveStore.BestMoves(CurrentLevel), 22, AquaUI.Muted,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Anchor(stats.rectTransform, new Vector2(.08f, .43f), new Vector2(.92f, .53f), Vector2.zero, Vector2.zero);

            Button next = AquaUI.Button("Next", card.transform,
                CurrentLevel < SaveStore.LevelCount ? "NEXT LEVEL" : "ALL LEVELS", NextLevel, true, 30);
            AquaUI.Anchor(next.transform as RectTransform, new Vector2(.08f, .25f), new Vector2(.92f, .40f), Vector2.zero, Vector2.zero);
            Button replay = AquaUI.Button("Replay", card.transform, "REPLAY", () => ShowGame(CurrentLevel), false, 25);
            AquaUI.Anchor(replay.transform as RectTransform, new Vector2(.08f, .08f), new Vector2(.48f, .21f), Vector2.zero, Vector2.zero);
            Button levels = AquaUI.Button("Levels", card.transform, "LEVELS", ShowLevels, false, 25);
            AquaUI.Anchor(levels.transform as RectTransform, new Vector2(.52f, .08f), new Vector2(.92f, .21f), Vector2.zero, Vector2.zero);
        }

        private void BuildLevelButton(Transform parent, int level, bool unlocked)
        {
            int captured = level;
            Button button = AquaUI.Button("Level_" + level, parent, string.Empty,
                unlocked ? (UnityEngine.Events.UnityAction)(() => ShowGame(captured)) : null, false, 24);
            button.interactable = unlocked;
            Image image = button.GetComponent<Image>();
            image.color = unlocked ? new Color(.04f, .25f, .30f, .92f) : new Color(.04f, .12f, .15f, .74f);

            Text number = AquaUI.Text("Number", button.transform, level.ToString("00"), 38,
                unlocked ? AquaUI.White : new Color(.48f, .63f, .65f), TextAnchor.MiddleLeft, FontStyle.Bold);
            AquaUI.Anchor(number.rectTransform, new Vector2(.08f, .37f), new Vector2(.42f, .9f), Vector2.zero, Vector2.zero);
            int stars = SaveStore.BestStars(level);
            Text state = AquaUI.Text("State", button.transform,
                unlocked ? (stars > 0 ? StarString(stars) : "READY") : "LOCKED",
                stars > 0 ? 22 : 17, unlocked ? (stars > 0 ? AquaUI.Lime : AquaUI.Cyan) : AquaUI.Muted,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            AquaUI.Anchor(state.rectTransform, new Vector2(.08f, .08f), new Vector2(.92f, .40f), Vector2.zero, Vector2.zero);
            Text size = AquaUI.Text("Grid Size", button.transform, GridSizeForLevel(level), 16, AquaUI.Muted,
                TextAnchor.MiddleRight, FontStyle.Bold);
            AquaUI.Anchor(size.rectTransform, new Vector2(.58f, .44f), new Vector2(.9f, .85f), Vector2.zero, Vector2.zero);
        }

        private void BuildHeader(Transform root, string titleText, UnityEngine.Events.UnityAction backAction)
        {
            Button back = AquaUI.Button("Back", root, "<", backAction, false, 34);
            AquaUI.Fixed(back.transform as RectTransform, new Vector2(0f, 1f), new Vector2(72f, -90f), new Vector2(96f, 76f));
            Text title = AquaUI.Text("Header Title", root, titleText, 38, AquaUI.White,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.Fixed(title.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -90f), new Vector2(660f, 82f));
            Image accent = AquaUI.Image("Header Accent", root, AquaUI.Cyan, true);
            AquaUI.Fixed(accent.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -143f), new Vector2(82f, 6f));
        }

        private void BuildLogo(Transform root, Vector2 position)
        {
            Image outer = AquaUI.Circle("Logo Ring", root, new Color(.15f, .95f, 1f, .14f));
            AquaUI.Fixed(outer.rectTransform, new Vector2(.5f, 1f), position, new Vector2(150f, 150f));
            Image inner = AquaUI.Circle("Logo Water", outer.transform, new Color(.13f, .88f, 1f, .72f));
            AquaUI.Anchor(inner.rectTransform, new Vector2(.19f, .19f), new Vector2(.81f, .81f), Vector2.zero, Vector2.zero);
            Image shine = AquaUI.Image("Logo Shine", outer.transform, new Color(1f, 1f, 1f, .72f), true);
            AquaUI.Anchor(shine.rectTransform, new Vector2(.31f, .63f), new Vector2(.62f, .73f), Vector2.zero, Vector2.zero);
        }

        private void BuildHelpStep(Transform parent, float centerY, string number, string heading, string body)
        {
            Image badge = AquaUI.Circle("Step " + number, parent, number == "*" ? AquaUI.Lime : AquaUI.Cyan);
            AquaUI.Anchor(badge.rectTransform, new Vector2(.065f, centerY - .055f), new Vector2(.19f, centerY + .075f), Vector2.zero, Vector2.zero);
            Text marker = AquaUI.Text("Marker", badge.transform, number, 30, AquaUI.Deep, TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.StretchRect(marker.rectTransform);
            Text title = AquaUI.Text("Heading", parent, heading, 25, AquaUI.White, TextAnchor.MiddleLeft, FontStyle.Bold);
            AquaUI.Anchor(title.rectTransform, new Vector2(.24f, centerY + .005f), new Vector2(.92f, centerY + .085f), Vector2.zero, Vector2.zero);
            Text copy = AquaUI.Text("Copy", parent, body, 19, AquaUI.Muted, TextAnchor.UpperLeft);
            AquaUI.Anchor(copy.rectTransform, new Vector2(.24f, centerY - .085f), new Vector2(.92f, centerY + .005f), Vector2.zero, Vector2.zero);
        }

        private void BuildToggle(Transform parent, string objectName, string title, string subtitle,
            Func<bool> getter, Action<bool> setter, float centerY)
        {
            Image row = AquaUI.Image(objectName + " Row", parent, new Color(1f, 1f, 1f, .045f), true);
            AquaUI.Anchor(row.rectTransform, new Vector2(.06f, centerY - .105f), new Vector2(.94f, centerY + .105f), Vector2.zero, Vector2.zero);
            Text heading = AquaUI.Text("Title", row.transform, title, 25, AquaUI.White, TextAnchor.MiddleLeft, FontStyle.Bold);
            AquaUI.Anchor(heading.rectTransform, new Vector2(.05f, .46f), new Vector2(.64f, .88f), Vector2.zero, Vector2.zero);
            Text copy = AquaUI.Text("Subtitle", row.transform, subtitle, 17, AquaUI.Muted, TextAnchor.MiddleLeft);
            AquaUI.Anchor(copy.rectTransform, new Vector2(.05f, .12f), new Vector2(.67f, .49f), Vector2.zero, Vector2.zero);

            Text state = null;
            Button toggle = AquaUI.Button(objectName, row.transform, string.Empty, () =>
            {
                setter(!getter());
                if (state != null) state.text = getter() ? "ON" : "OFF";
            }, getter(), 20);
            AquaUI.Anchor(toggle.transform as RectTransform, new Vector2(.71f, .22f), new Vector2(.94f, .78f), Vector2.zero, Vector2.zero);
            state = AquaUI.Text("State", toggle.transform, getter() ? "ON" : "OFF", 20,
                getter() ? AquaUI.Deep : AquaUI.White, TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.StretchRect(state.rectTransform);
        }

        private RectTransform BuildModalBackdrop(string name)
        {
            Image blocker = AquaUI.Image(name, screenRoot.transform, new Color(.015f, .07f, .09f, .88f));
            AquaUI.StretchRect(blocker.rectTransform);
            blocker.rectTransform.SetAsLastSibling();
            return blocker.rectTransform;
        }

        private static string StarString(int stars)
        {
            return (stars >= 1 ? "\u2605" : "\u2606") + "  " +
                   (stars >= 2 ? "\u2605" : "\u2606") + "  " +
                   (stars >= 3 ? "\u2605" : "\u2606");
        }

        private static string GridSizeForLevel(int level)
        {
            int size = level <= 7 ? 4 : level <= 18 ? 5 : level <= 24 ? 6 : 7;
            return size + " x " + size;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            events.transform.SetAsFirstSibling();
        }
    }
}

using System;
using System.Collections;
using AquaPath.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AquaPath
{
    /// <summary>Owns the interactive uGUI board and translates model state into pipe visuals.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        public Puzzle Puzzle { get; private set; }
        public PipeView[] Views { get; private set; }
        public bool InputLocked;
        public bool CompletionShown;
        public bool TutorialEnabled { get; private set; }
        private bool rotationInFlight;

        public event Action Completed;
        public event Action Changed;

        private RectTransform gridRect;
        private GridLayoutGroup grid;
        private Text inputLabel;
        private Text outputLabel;
        private GameObject[] highlights;
        private int generation;
        private float lastWidth;
        private int lastGridSize;
        private bool paused;

        private void Awake()
        {
            RectTransform self = transform as RectTransform;
            if (self == null) return;

            gridRect = AquaUI.Stretch("Pipe Grid", transform, 34f, 34f, 34f, 34f);
            grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            inputLabel = AquaUI.Text("IN Marker", transform, "IN", 23, AquaUI.Lime,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            outputLabel = AquaUI.Text("OUT Marker", transform, "OUT", 23, AquaUI.Cyan,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            AquaUI.AddOutline(inputLabel, new Color(0f, 0f, 0f, .55f), new Vector2(2f, -2f));
            AquaUI.AddOutline(outputLabel, new Color(0f, 0f, 0f, .55f), new Vector2(2f, -2f));
            inputLabel.rectTransform.SetAsLastSibling();
            outputLabel.rectTransform.SetAsLastSibling();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (gridRect != null && Puzzle != null)
                ConfigureGrid();
        }

        public void Load(int level)
        {
            generation++;
            StopAllCoroutines();
            Puzzle = LevelFactory.Create(Mathf.Clamp(level, 1, SaveStore.LevelCount));
            TutorialEnabled = level == 1 && SaveStore.BestStars(1) == 0;
            rotationInFlight = false;
            CompletionShown = false;
            InputLocked = false;
            paused = false;
            lastWidth = 0f;
            lastGridSize = 0;

            for (int i = gridRect.childCount - 1; i >= 0; i--)
                Destroy(gridRect.GetChild(i).gameObject);

            Views = new PipeView[Puzzle.Cells.Length];
            highlights = new GameObject[Puzzle.Cells.Length];
            grid.constraintCount = Puzzle.Size;
            ConfigureGrid();

            for (int i = 0; i < Puzzle.Cells.Length; i++)
                CreateTile(i);

            PositionEndpointLabel(inputLabel.rectTransform, Puzzle.Source);
            PositionEndpointLabel(outputLabel.rectTransform, Puzzle.Target);
            RefreshNetwork(false);
            UpdateTutorialHighlight();
            Changed?.Invoke();
        }

        public void Tap(int index)
        {
            if (InputLocked || CompletionShown || Puzzle == null || index < 0 || index >= Puzzle.Cells.Length)
                return;
            if (!Puzzle.Rotate(index))
                return;

            InputLocked = true;
            rotationInFlight = true;
            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlayTurn();
                AudioController.Instance.Haptic();
            }
            StartCoroutine(RotateAndRefresh(index, Puzzle.Cells[index].Rotation, generation));
        }

        public void Restart()
        {
            if (Puzzle == null) return;
            generation++;
            StopAllCoroutines();
            CompletionShown = false;
            paused = false;
            rotationInFlight = false;
            InputLocked = false;
            Puzzle.Reset();
            for (int i = 0; i < Views.Length; i++)
            {
                Views[i].SetOrientation(Puzzle.Cells[i].Rotation);
                Views[i].ResetFlow();
            }
            RefreshNetwork(false);
            UpdateTutorialHighlight();
            Changed?.Invoke();
        }

        public void Hint()
        {
            if (InputLocked || CompletionShown || Puzzle == null) return;
            int index = FindFirstIncorrectRouteTile();
            if (index >= 0) Tap(index);
        }

        public void SetPaused(bool value)
        {
            paused = value;
            InputLocked = value || CompletionShown || rotationInFlight;
        }

        private IEnumerator RotateAndRefresh(int index, int rotation, int token)
        {
            yield return Views[index].Turn(rotation);
            if (token != generation || Puzzle == null) yield break;
            rotationInFlight = false;
            RefreshNetwork(true);
            UpdateTutorialHighlight();
            Changed?.Invoke();
        }

        private void RefreshNetwork(bool allowCompletion)
        {
            if (Puzzle == null || Views == null) return;
            var network = Puzzle.Network();
            int farthest = 0;
            for (int i = 0; i < Views.Length; i++)
            {
                int distance = network.Distance[i];
                if (network.Connected[i]) farthest = Mathf.Max(farthest, distance);
                Views[i].Flow(network.Connected[i], network.Entry[i], network.Connected[i] ? Mathf.Max(0, distance) * .10f : 0f);
            }

            if (allowCompletion && network.ReachesTarget && Puzzle.Solved && !CompletionShown)
            {
                CompletionShown = true;
                InputLocked = true;
                int targetDistance = network.Distance[Puzzle.Target];
                StartCoroutine(FinishAfterWater(Puzzle.Target, Mathf.Max(0, targetDistance) * .10f + 5f, generation));
            }
            else if (!CompletionShown && !paused)
            {
                InputLocked = false;
            }
        }

        private IEnumerator FinishAfterWater(int targetIndex, float timeout, int token)
        {
            float elapsed = 0f;
            while (token == generation && Views != null && targetIndex >= 0 && targetIndex < Views.Length &&
                   Views[targetIndex].Fill < .98f && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (token != generation || Puzzle == null || !Puzzle.Solved) yield break;
            if (Views == null || targetIndex < 0 || targetIndex >= Views.Length || Views[targetIndex].Fill < .98f)
            {
                Debug.LogError("Aqua Path: target water fill timed out; victory was withheld.");
                yield break;
            }
            Completed?.Invoke();
        }

        private void CreateTile(int index)
        {
            Shape shape = Puzzle.Cells[index].Shape;
            string prefabName = shape == Shape.Tee ? "T" : shape == Shape.Source ? "Start" : shape == Shape.Target ? "End" : shape.ToString();
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Pipe_" + prefabName);
            RectTransform tile = prefab != null ? (RectTransform)Instantiate(prefab, gridRect, false).transform : AquaUI.Rect("Pipe_" + index, gridRect);
            tile.name = "Pipe_" + index;
            Image background = tile.gameObject.AddComponent<Image>();
            background.sprite = AquaUI.RoundedSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(.03f, .23f, .29f, .72f);

            Button button = tile.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.75f, 1f, 1f, 1f);
            colors.pressedColor = new Color(.5f, .88f, .9f, 1f);
            colors.disabledColor = Color.white;
            button.colors = colors;
            int captured = index;
            button.onClick.AddListener(() => Tap(captured));

            PipeView pipe = tile.GetComponent<PipeView>() ?? tile.gameObject.AddComponent<PipeView>();
            pipe.Initialize(Puzzle.Cells[index].Shape);
            pipe.SetOrientation(Puzzle.Cells[index].Rotation);
            Views[index] = pipe;

            Image glow = AquaUI.Image("Next Move Glow", tile, new Color(.78f, 1f, .35f, .13f), true);
            AquaUI.StretchRect(glow.rectTransform, -5f, -5f, -5f, -5f);
            glow.raycastTarget = false;
            Outline outline = glow.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.78f, 1f, .35f, .95f);
            outline.effectDistance = new Vector2(3f, -3f);
            BoardHighlight pulse = glow.gameObject.AddComponent<BoardHighlight>();
            pulse.Target = glow;
            highlights[index] = glow.gameObject;
            glow.gameObject.SetActive(false);

            if (Puzzle.Cells[index].Locked)
            {
                button.interactable = false;
                background.color = new Color(.08f, .35f, .4f, .8f);
            }
        }

        private void ConfigureGrid()
        {
            if (Puzzle == null || gridRect == null) return;
            float width = gridRect.rect.width;
            if (width < 20f) width = (transform as RectTransform).rect.width - 68f;
            if (width < 20f) width = 820f;
            if (Mathf.Abs(width - lastWidth) < .1f && lastGridSize == Puzzle.Size && grid.cellSize.x > 0f) return;
            lastWidth = width;
            lastGridSize = Puzzle.Size;
            float gap = Puzzle.Size <= 4 ? 14f : Puzzle.Size <= 5 ? 11f : 8f;
            float size = (width - gap * (Puzzle.Size - 1)) / Puzzle.Size;
            grid.spacing = Vector2.one * gap;
            grid.cellSize = Vector2.one * Mathf.Floor(size);
        }

        private void PositionEndpointLabel(RectTransform label, int index)
        {
            int row = index / Puzzle.Size;
            int column = index % Puzzle.Size;
            float normalizedX = (column + .5f) / Puzzle.Size;
            float normalizedY = 1f - (row + .5f) / Puzzle.Size;
            Vector2 anchor = new Vector2(normalizedX, normalizedY);
            Vector2 position = Vector2.zero;

            if (column == 0) { anchor.x = 0f; position.x = -4f; }
            else if (column == Puzzle.Size - 1) { anchor.x = 1f; position.x = 4f; }
            else if (row == 0) { anchor.y = 1f; position.y = 4f; }
            else { anchor.y = 0f; position.y = -4f; }

            label.anchorMin = label.anchorMax = anchor;
            label.pivot = new Vector2(.5f, .5f);
            label.anchoredPosition = position;
            label.sizeDelta = new Vector2(64f, 34f);
            label.SetAsLastSibling();
        }

        private int FindFirstIncorrectRouteTile()
        {
            int[] route = Puzzle.Route;
            for (int i = 0; i < route.Length; i++)
                if (IsIncorrectAndRotatable(route[i])) return route[i];
            for (int i = 0; i < Puzzle.Cells.Length; i++)
                if (IsIncorrectAndRotatable(i)) return i;
            return -1;
        }

        private bool IsIncorrectAndRotatable(int index)
        {
            if (index < 0 || index >= Puzzle.Cells.Length || Puzzle.Cells[index].Locked) return false;
            int current = Directions.Mask(Puzzle.Cells[index].Shape, Puzzle.Cells[index].Rotation);
            int solution = Directions.Mask(Puzzle.Cells[index].Shape, Puzzle.Cells[index].Solution);
            return current != solution;
        }

        private void UpdateTutorialHighlight()
        {
            if (highlights == null) return;
            for (int i = 0; i < highlights.Length; i++)
                if (highlights[i] != null) highlights[i].SetActive(false);
            if (Puzzle != null && Puzzle.Level == 1 && !Puzzle.Solved)
            {
                int index = FindFirstIncorrectRouteTile();
                if (index >= 0 && index < highlights.Length && highlights[index] != null)
                    highlights[index].SetActive(true);
            }
        }
    }

    public sealed class BoardHighlight : MonoBehaviour
    {
        public Graphic Target;
        private Color baseColor;
        private void Awake() { if (Target != null) baseColor = Target.color; }
        private void Update()
        {
            if (Target == null) return;
            Color color = baseColor;
            color.a = baseColor.a * (.68f + Mathf.Sin(Time.unscaledTime * 4f) * .32f);
            Target.color = color;
        }
    }
}

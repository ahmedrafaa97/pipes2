using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AquaPath
{
    public static class AquaUI
    {
        public static readonly Color Deep = Hex("061E27");
        public static readonly Color DeepBlue = Hex("082F3C");
        public static readonly Color PanelBlue = Hex("0B4050");
        public static readonly Color Cyan = Hex("22DFF3");
        public static readonly Color CyanSoft = Hex("9CF5F5");
        public static readonly Color Lime = Hex("C7FF5B");
        public static readonly Color White = Hex("F4FFFF");
        public static readonly Color Muted = Hex("89BBC3");
        public static readonly Color Coral = Hex("FF9475");

        private static Font cachedFont;
        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        public static Font Font
        {
            get
            {
                if (cachedFont == null)
                    cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return cachedFont;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            RectTransform rt = Rect(name, parent);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Image(string name, Transform parent, Color color, bool rounded = false)
        {
            RectTransform rt = Rect(name, parent);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            if (rounded)
            {
                image.sprite = RoundedSprite;
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }
            return image;
        }

        public static Image Circle(string name, Transform parent, Color color)
        {
            Image image = Image(name, parent, color);
            image.sprite = CircleSprite;
            image.preserveAspect = true;
            return image;
        }

        public static Text Text(string name, Transform parent, string value, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = Rect(name, parent);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static Button Button(string name, Transform parent, string label, UnityAction action,
            bool primary = false, int fontSize = 30)
        {
            Color baseColor = primary ? Cyan : new Color(1f, 1f, 1f, .08f);
            Color textColor = primary ? Deep : White;
            Image image = Image(name, parent, baseColor, true);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = primary ? new Color(.86f, 1f, 1f) : new Color(1f, 1f, 1f, .86f);
            colors.pressedColor = primary ? new Color(.6f, .93f, .96f) : new Color(.72f, .86f, .88f, .8f);
            colors.disabledColor = new Color(.45f, .55f, .58f, .28f);
            colors.fadeDuration = .1f;
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                if (AudioController.Instance != null) AudioController.Instance.PlayClick();
                if (action != null) action.Invoke();
            });

            Text text = Text("Label", button.transform, label, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            StretchRect(text.rectTransform, 20f, 8f, 20f, 8f);
            return button;
        }

        public static void StretchRect(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void Fixed(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        public static Color Hex(string rgb)
        {
            Color color;
            return ColorUtility.TryParseHtmlString("#" + rgb, out color) ? color : Color.white;
        }

        public static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite == null)
                    roundedSprite = MakeRoundedSprite(64, 15f, false);
                return roundedSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite == null)
                    circleSprite = MakeRoundedSprite(64, 31f, true);
                return circleSprite;
            }
        }

        public static void BuildBackdrop(Transform parent)
        {
            RectTransform gradientRect = Stretch("Aqua Gradient", parent);
            AquaGradient gradient = gradientRect.gameObject.AddComponent<AquaGradient>();
            gradient.Top = Hex("092A37");
            gradient.Bottom = Hex("03141C");
            gradient.raycastTarget = false;

            for (int i = 0; i < 5; i++)
            {
                Image ray = Image("Light Ray " + (i + 1), parent, new Color(.25f, .96f, 1f, .025f));
                RectTransform rt = ray.rectTransform;
                rt.anchorMin = new Vector2(.5f, 1f);
                rt.anchorMax = new Vector2(.5f, 1f);
                rt.pivot = new Vector2(.5f, 1f);
                rt.sizeDelta = new Vector2(130f + i * 32f, 1420f);
                rt.anchoredPosition = new Vector2((i - 2) * 210f, 30f);
                rt.localRotation = Quaternion.Euler(0f, 0f, (i - 2) * 8f);
                ray.raycastTarget = false;
            }

            for (int i = 0; i < 14; i++)
            {
                Image bubble = Circle("Bubble " + (i + 1), parent, new Color(.5f, 1f, 1f, .04f + (i % 3) * .018f));
                RectTransform rt = bubble.rectTransform;
                float size = 12f + (i * 17 % 42);
                rt.anchorMin = rt.anchorMax = new Vector2((i * .173f) % 1f, (i * .287f) % .92f);
                rt.sizeDelta = Vector2.one * size;
                rt.anchoredPosition = new Vector2((i % 2 == 0 ? 1 : -1) * 22f, 0f);
                bubble.raycastTarget = false;
                AquaBubble drift = bubble.gameObject.AddComponent<AquaBubble>();
                drift.Speed = 7f + i % 5 * 2f;
                drift.Phase = i * .73f;
            }
        }

        private static Sprite MakeRoundedSprite(int size, float radius, bool circle)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = circle ? "Aqua Circle" : "Aqua Rounded";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[size * size];
            Vector2 center = Vector2.one * (size - 1) * .5f;
            Vector2 half = Vector2.one * (size * .5f - radius);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 delta = new Vector2(Mathf.Abs(x - center.x), Mathf.Abs(y - center.y)) - half;
                float outside = new Vector2(Mathf.Max(delta.x, 0f), Mathf.Max(delta.y, 0f)).magnitude;
                float distance = outside + Mathf.Min(Mathf.Max(delta.x, delta.y), 0f) - radius;
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(.8f - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Vector4 border = circle ? Vector4.zero : Vector4.one * Mathf.RoundToInt(radius + 1f);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * .5f, 100f, 0, SpriteMeshType.FullRect, border);
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AquaGradient : MaskableGraphic
    {
        public Color Top = Color.white;
        public Color Bottom = Color.black;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector2(r.xMin, r.yMin); vertex.color = Bottom; vh.AddVert(vertex);
            vertex.position = new Vector2(r.xMin, r.yMax); vertex.color = Top; vh.AddVert(vertex);
            vertex.position = new Vector2(r.xMax, r.yMax); vertex.color = Top; vh.AddVert(vertex);
            vertex.position = new Vector2(r.xMax, r.yMin); vertex.color = Bottom; vh.AddVert(vertex);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }

    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastSafeArea;
        private Vector2Int lastScreen;

        private void Awake() { Apply(); }
        private void OnEnable() { Apply(); }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height)
                Apply();
        }

        private void Apply()
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            lastSafeArea = safe;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
        }
    }

    public sealed class AquaBubble : MonoBehaviour
    {
        public float Speed = 10f;
        public float Phase;
        private RectTransform rt;
        private Vector2 origin;

        private void Awake() { rt = transform as RectTransform; origin = rt.anchoredPosition; }
        private void Update()
        {
            float t = Time.unscaledTime + Phase;
            rt.anchoredPosition = origin + new Vector2(Mathf.Sin(t * .7f) * 8f, Mathf.Repeat(t * Speed, 90f));
        }
    }
}

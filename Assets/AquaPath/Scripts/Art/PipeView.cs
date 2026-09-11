using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AquaPath.Core;

namespace AquaPath
{
    [DisallowMultipleComponent]
    public sealed class PipeView : MonoBehaviour
    {
        public Core.Shape Shape;
        public bool IsAnimating { get; private set; }
        public float Fill { get; private set; }
        public RectTransform VisualRoot { get; private set; }
        Material waterMaterial;
        Image water;
        int orientation;
        int entry;
        float targetFill;
        float fillAt;
        float seed;

        void Start()
        {
            if (VisualRoot == null) Initialize(Shape);
        }

        public void Initialize(Shape shape)
        {
            Shape = shape;
            if (VisualRoot != null) Destroy(VisualRoot.gameObject);
            if (waterMaterial != null) Destroy(waterMaterial);
            var root = new GameObject("Glass and flowing water", typeof(RectTransform));
            VisualRoot = (RectTransform)root.transform;
            VisualRoot.SetParent(transform, false);
            Stretch(VisualRoot);
            GlassArt.Artwork art = GlassArt.Get(shape);
            water = Layer("Internal blue water", art.Water);
            Shader shader = Shader.Find("AquaPath/GlassWater");
            if (shader == null) shader = Resources.Load<Shader>("Shaders/GlassWater");
            if (shader != null)
            {
                waterMaterial = new Material(shader) { name = "Water instance" };
                waterMaterial.SetTexture("_DistanceTex", art.Distances);
                seed = Random.value * 20;
                waterMaterial.SetFloat("_Seed", seed);
                water.material = waterMaterial;
            }
            Layer("Transparent glass walls and elliptical rims", art.Glass);
            Fill = targetFill = 0;
            ApplyFill();
            SetOrientation(orientation);
        }

        Image Layer(string label, Sprite sprite)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(VisualRoot, false);
            Stretch((RectTransform)go.transform);
            Image im = go.GetComponent<Image>();
            im.sprite = sprite;
            im.raycastTarget = false;
            im.preserveAspect = true;
            return im;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(.5f, .5f);
        }

        public void SetOrientation(int rotation)
        {
            orientation = ((rotation % 4) + 4) % 4;
            IsAnimating = false;
            if (VisualRoot != null)
            {
                VisualRoot.localRotation = Quaternion.Euler(0, 0, -90 * orientation);
                VisualRoot.localScale = Vector3.one;
            }
        }

        public IEnumerator Turn(int clockwiseRotation, float duration = .18f)
        {
            if (VisualRoot == null) yield break;
            IsAnimating = true;
            float start = VisualRoot.localEulerAngles.z;
            int next = ((clockwiseRotation % 4) + 4) % 4;
            float end = start + Mathf.DeltaAngle(start, -90 * next);
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration <= 0 ? 1 : Mathf.Clamp01(elapsed / duration);
                float ease = 1 - Mathf.Pow(1 - t, 3);
                VisualRoot.localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(start, end, ease));
                yield return null;
            }
            SetOrientation(next);
            IsAnimating = false;
        }

        public void Flow(bool connected, int incomingGlobalDirection, float delay)
        {
            int global = incomingGlobalDirection >= 0 ? incomingGlobalDirection : orientation;
            entry = (global - orientation + 4) % 4;
            if (waterMaterial != null) waterMaterial.SetFloat("_Entry", entry);
            float next = connected ? 1 : 0;
            if (targetFill != next) fillAt = Time.time + Mathf.Max(0, delay);
            targetFill = next;
        }

        public void ResetFlow()
        {
            Fill = targetFill = 0;
            fillAt = Time.time;
            ApplyFill();
        }

        void Update()
        {
            if (Time.time >= fillAt) Fill = Mathf.MoveTowards(Fill, targetFill, Time.deltaTime * (targetFill > Fill ? 1.9f : 3.8f));
            ApplyFill();
        }

        void ApplyFill()
        {
            if (waterMaterial != null) waterMaterial.SetFloat("_Fill", Fill);
            else if (water != null) water.color = new Color(1, 1, 1, Fill);
        }

        void OnDestroy() { if (waterMaterial != null) Destroy(waterMaterial); }
    }
}

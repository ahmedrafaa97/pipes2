using System.Collections.Generic;
using UnityEngine;
using AquaPath.Core;

namespace AquaPath
{
    /// <summary>Shared, background-free glass and liquid artwork generated in linear tile space.</summary>
    public static class GlassArt
    {
        public sealed class Artwork
        {
            public Sprite Glass;
            public Sprite Water;
            public Texture2D Distances;
        }

        const int Size = 320;
        const float Radius = .202f;
        static readonly Dictionary<Shape, Artwork> Cache = new Dictionary<Shape, Artwork>();
        static readonly Color Ink = new Color(.035f, .105f, .145f, 1);
        static readonly Color Ice = new Color(.69f, .96f, 1, 1);
        static readonly Color White = new Color(.97f, 1, 1, 1);

        public static Artwork Get(Shape shape)
        {
            if (Cache.TryGetValue(shape, out Artwork art)) return art;
            var glass = new Color[Size * Size];
            var water = new Color[glass.Length];
            var inside = new bool[glass.Length];
            int mask = BaseMask(shape);
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int i = y * Size + x;
                Vector2 p = new Vector2((x + .5f) / Size - .5f, (y + .5f) / Size - .5f);
                Vector2 q = Nearest(p, shape);
                Vector2 delta = p - q;
                float d = delta.magnitude;
                float body = Edge(Radius - d, .0035f);
                // Open ends are cut before the projecting transparent collars.
                if (((mask & 1) != 0 && p.y > .421f) || ((mask & 2) != 0 && p.x > .421f) ||
                    ((mask & 4) != 0 && p.y < -.421f) || ((mask & 8) != 0 && p.x < -.421f)) body = 0;
                Color c = new Color(.65f, .89f, .95f, .042f * body);
                if (body > 0)
                {
                    float normalLight = delta.sqrMagnitude > .00001f ? Vector2.Dot(delta.normalized, new Vector2(-.74f, .67f)) : 0;
                    // Distinct outer dark refraction line, fine white edge, and an inner wall.
                    Over(ref c, Ink, Band(d, Radius - .001f, .005f) * .85f * body);
                    Over(ref c, White, Band(d, Radius - .009f, .0042f) * (.64f + .26f * normalLight) * body);
                    Over(ref c, Ice, Band(d, Radius - .023f, .009f) * .27f * body);
                    Over(ref c, Ink, Band(d, Radius - .038f, .0032f) * .37f * body);
                    Over(ref c, White, Band(d, Radius - .046f, .003f) * .34f * body);
                    // Long reflections sit over a clear core, not a solid tinted tube.
                    float reflection = Band(d, .126f, .016f) * Mathf.Pow(Mathf.Max(0, normalLight), 7);
                    Over(ref c, White, reflection * .61f * body);
                    Over(ref c, White, Band(d, .14f, .003f) * Mathf.Pow(Mathf.Max(0, normalLight), 3) * .7f * body);
                    Over(ref c, Ice, Band(d, .155f, .006f) * Mathf.Pow(Mathf.Max(0, -normalLight), 5) * .42f * body);
                }
                for (int dir = 0; dir < 4; dir++) if ((mask & (1 << dir)) != 0) Collar(ref c, p, dir);
                glass[i] = c;

                float liquidRadius = .153f;
                float coverage = Edge(liquidRadius - d, .003f);
                // A little clear air above horizontal runs makes the separate liquid unmistakable.
                float top = q.y + .103f + .006f * Mathf.Sin(p.x * 48 + .6f);
                coverage *= Edge(top - p.y, .004f);
                coverage *= Edge(.443f - Mathf.Abs(p.x), .003f) * Edge(.443f - Mathf.Abs(p.y), .003f);
                inside[i] = d < liquidRadius && Mathf.Abs(p.x) < .447f && Mathf.Abs(p.y) < .447f;
                float profile = Mathf.Clamp01(d / liquidRadius);
                Color wc = Color.Lerp(new Color(.025f, .42f, .73f), new Color(.04f, .75f, .97f), Mathf.Pow(profile, 3));
                wc = Color.Lerp(wc, new Color(.28f, .91f, 1), Band(delta.y, -.112f, .019f) * .48f);
                wc.a = coverage * .87f;
                float surface = Band(p.y, top - .003f, .0055f) * coverage;
                Over(ref wc, new Color(.58f, .96f, 1, 1), surface * .78f);
                water[i] = wc;
            }
            var distancePixels = new Color[glass.Length];
            for (int dir = 0; dir < 4; dir++)
            {
                float[] distances = FloodDistances(inside, dir, mask);
                for (int i = 0; i < distances.Length; i++)
                {
                    Color col = distancePixels[i];
                    col[dir] = distances[i];
                    distancePixels[i] = col;
                }
            }
            var gt = Texture(glass, shape + " Clear Glass", false);
            var wt = Texture(water, shape + " Blue Water", false);
            art = new Artwork {
                Glass = Sprite.Create(gt, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size),
                Water = Sprite.Create(wt, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size),
                Distances = Texture(distancePixels, shape + " Entry Distances", true)
            };
            art.Glass.name = shape + "_Glass";
            art.Water.name = shape + "_Water";
            Cache[shape] = art;
            return art;
        }

        static void Collar(ref Color c, Vector2 p, int dir)
        {
            float along = dir == 0 ? p.y : dir == 1 ? p.x : dir == 2 ? -p.y : -p.x;
            float across = dir % 2 == 0 ? p.x : p.y;
            float ax = Mathf.Abs(across);
            if (along < .318f || along > .499f || ax > .244f) return;
            // Short thick cylindrical connector with two displaced elliptical glass lips.
            float barrel = Edge(.235f - ax, .003f) * Edge(along - .377f, .004f) * Edge(.43f - along, .004f);
            Over(ref c, Ice, barrel * .095f);
            Over(ref c, Ink, Band(ax, .233f, .0035f) * barrel * .82f);
            Over(ref c, White, Band(ax, .225f, .006f) * barrel * .85f);
            Rim(ref c, across, along - .379f, .233f, .063f, .8f);
            Rim(ref c, across, along - .429f, .233f, .063f, 1);
            // Small broad flashes across the upper-left curved glass.
            float flash = Band(across, -.137f, .048f) * Band(along, .473f, .006f);
            Over(ref c, White, flash * .85f);
        }

        static void Rim(ref Color c, float x, float y, float rx, float ry, float strength)
        {
            float r = Mathf.Sqrt(x * x / (rx * rx) + y * y / (ry * ry));
            // Screen-space elliptical distance keeps the ring thickness consistent.
            float grad = Mathf.Sqrt(x * x / Mathf.Pow(rx, 4) + y * y / Mathf.Pow(ry, 4)) / Mathf.Max(r, .001f);
            float distance = (r - 1) / Mathf.Max(grad, 1);
            Over(ref c, Ink, Band(distance, 0, .006f) * .88f * strength);
            Over(ref c, White, Band(distance, -.006f, .004f) * .95f * strength);
            Over(ref c, Ice, Band(distance, -.013f, .006f) * .45f * strength);
            Over(ref c, Ink, Band(distance, -.020f, .0033f) * .55f * strength);
            Over(ref c, White, Band(distance, -.024f, .0027f) * .67f * strength);
        }

        static Vector2 Nearest(Vector2 p, Shape shape)
        {
            if (shape == Shape.Straight) return new Vector2(0, Mathf.Clamp(p.y, -.45f, .45f));
            if (shape == Shape.Elbow)
            {
                Vector2 center = new Vector2(.28f, .28f);
                Vector2 v = p - center;
                float angle = Mathf.Clamp(Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg, -180, -90);
                if (v.y > 0 && v.x < 0) angle = -180;
                Vector2 arc = center + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * .28f;
                Vector2 north = new Vector2(0, Mathf.Clamp(p.y, .28f, .45f));
                Vector2 east = new Vector2(Mathf.Clamp(p.x, .28f, .45f), 0);
                return Closest(p, arc, Closest(p, north, east));
            }
            if (shape == Shape.Source || shape == Shape.Target) return new Vector2(0, Mathf.Clamp(p.y, -.21f, .45f));
            Vector2 horizontal = new Vector2(Mathf.Clamp(p.x, -.45f, .45f), 0);
            Vector2 vertical = new Vector2(0, Mathf.Clamp(p.y, shape == Shape.Cross ? -.45f : 0, .45f));
            return Closest(p, horizontal, vertical);
        }

        static Vector2 Closest(Vector2 p, Vector2 a, Vector2 b) => (p - a).sqrMagnitude < (p - b).sqrMagnitude ? a : b;
        static int BaseMask(Shape s) => s == Shape.Straight ? 5 : s == Shape.Elbow ? 3 : s == Shape.Tee ? 11 : s == Shape.Cross ? 15 : 1;
        static float Edge(float v, float aa) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(v / aa + .5f));
        static float Band(float v, float center, float width) => Mathf.Exp(-Mathf.Pow((v - center) / width, 2));
        static void Over(ref Color bottom, Color top, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            float a = alpha + bottom.a * (1 - alpha);
            if (a < .00001f) return;
            bottom = new Color((top.r * alpha + bottom.r * bottom.a * (1 - alpha)) / a,
                (top.g * alpha + bottom.g * bottom.a * (1 - alpha)) / a,
                (top.b * alpha + bottom.b * bottom.a * (1 - alpha)) / a, a);
        }

        static float[] FloodDistances(bool[] inside, int dir, int mask)
        {
            if ((mask & (1 << dir)) == 0) for (int n = 0; n < 4; n++) if ((mask & (1 << n)) != 0) { dir = n; break; }
            var distances = new int[inside.Length];
            var queue = new int[inside.Length];
            for (int i = 0; i < distances.Length; i++) distances[i] = -1;
            int tail = 0, head = 0;
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                int i = y * Size + x;
                bool seed = dir == 0 ? y > Size * .93f : dir == 1 ? x > Size * .93f : dir == 2 ? y < Size * .07f : x < Size * .07f;
                if (inside[i] && seed) { distances[i] = 0; queue[tail++] = i; }
            }
            int maximum = 1;
            while (head < tail)
            {
                int i = queue[head++], x = i % Size, y = i / Size;
                Visit(i - 1, x > 0); Visit(i + 1, x + 1 < Size); Visit(i - Size, y > 0); Visit(i + Size, y + 1 < Size);
                void Visit(int j, bool valid)
                {
                    if (!valid || !inside[j] || distances[j] >= 0) return;
                    distances[j] = distances[i] + 1;
                    maximum = Mathf.Max(maximum, distances[j]);
                    queue[tail++] = j;
                }
            }
            var result = new float[inside.Length];
            for (int i = 0; i < result.Length; i++) result[i] = distances[i] < 0 ? 1 : .015f + .96f * distances[i] / maximum;
            return result;
        }

        static Texture2D Texture(Color[] colors, string name, bool linear)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, linear) {
                name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(colors);
            texture.Apply(false, false);
            return texture;
        }

        public static void ClearCache()
        {
            foreach (Artwork a in Cache.Values)
            {
                Object.Destroy(a.Glass.texture); Object.Destroy(a.Water.texture);
                Object.Destroy(a.Glass); Object.Destroy(a.Water); Object.Destroy(a.Distances);
            }
            Cache.Clear();
        }
    }
}

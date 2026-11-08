using UnityEngine;

public static class MidSealRasterizer
{
    private const int Size = 512;
    private static readonly Color Stone = new Color(0.10f, 0.087f, 0.098f);
    private static readonly Color Patina = new Color(0.36f, 0.28f, 0.19f);
    private static readonly Color PaleMetal = new Color(0.68f, 0.57f, 0.40f);
    private static readonly Color CoolMetal = new Color(0.34f, 0.55f, 0.50f);
    private static readonly Color Ember = new Color(0.31f, 0.66f, 0.73f);

    public static Texture2D Draw(out Texture2D emission)
    {
        var pixels = new Color[Size * Size];
        var light = new Color[Size * Size];
        Vector2 center = Vector2.one * 0.5f;
        const int segments = 120;

        for (int i = 0; i < segments; i++)
        {
            float a = i * 360f / segments;
            float b = (i + 1) * 360f / segments;
            FillTriangle(pixels, center * Size, Polar(a, 0.5f) * Size, Polar(b, 0.5f) * Size,
                Stone * 1.18f, Stone, Stone);

            Stroke(pixels, Polar(a, 0.473f), Polar(b, 0.473f), 0.0042f, Metal(a), Metal(b));
            Stroke(pixels, Polar(a, 0.391f), Polar(b, 0.391f), 0.0027f, Metal(a), Metal(b));
            if (i % 40 >= 3 && i % 40 < 35)
                Stroke(pixels, Polar(a, 0.337f), Polar(b, 0.337f), 0.0034f, Metal(a), Metal(b));
        }

        // Three sectors repeat the marks, while the diagonal paths leave open stone between them.
        for (int i = 0; i < 3; i++)
        {
            float a = 90f + i * 120f;
            Vector2 join = Polar(a, 0.297f);
            Stroke(pixels, Polar(a - 49f, 0.337f), join, 0.0050f, Patina, PaleMetal);
            Stroke(pixels, join, Polar(a + 49f, 0.337f), 0.0050f, PaleMetal, Patina);
            Stroke(pixels, join, Polar(a + 17f, 0.215f), 0.0030f, CoolMetal, Patina);
            Stroke(pixels, Polar(a + 17f, 0.215f), Polar(a + 38f, 0.264f), 0.0030f, Patina, CoolMetal);
            Stroke(light, Polar(a - 7f, 0.303f), join, 0.0040f, Color.black, Ember);
            Stroke(light, join, Polar(a + 7f, 0.303f), 0.0040f, Ember, Color.black);
            DrawGate(pixels, light, a);

            for (int mark = 1; mark <= 8; mark++)
            {
                float angle = a + mark * 12f;
                float outer = mark % 3 == 0 ? 0.457f : 0.446f;
                Stroke(pixels, Polar(angle, 0.415f), Polar(angle + 2f, outer), 0.0027f,
                    Patina, Metal(angle));
                if (mark % 2 == 0)
                    Stroke(pixels, Polar(angle - 2.7f, 0.429f), Polar(angle + 3f, 0.434f),
                        0.0023f, Patina, CoolMetal);
            }
        }

        emission = MakeTexture(light, "Mid Seal Junction Glow");
        return MakeTexture(pixels, "Mid Rasterized Seal");
    }

    private static void DrawGate(Color[] pixels, Color[] light, float angle)
    {
        Vector2 inner = Polar(angle, 0.387f);
        Vector2 outer = Polar(angle, 0.478f);
        Vector2 left = Polar(angle - 5f, 0.432f);
        Vector2 right = Polar(angle + 5f, 0.432f);
        Stroke(pixels, inner, left, 0.0038f, CoolMetal, Patina);
        Stroke(pixels, left, outer, 0.0038f, Patina, PaleMetal);
        Stroke(pixels, outer, right, 0.0038f, PaleMetal, Patina);
        Stroke(pixels, right, inner, 0.0038f, Patina, CoolMetal);
        Stroke(pixels, inner, outer, 0.0026f, CoolMetal, PaleMetal);
        Stroke(light, inner, Polar(angle, 0.441f), 0.0034f, Ember, Color.black);
    }

    private static Color Metal(float angle)
    {
        float shift = 0.5f + 0.5f * Mathf.Cos(angle * 3f * Mathf.Deg2Rad);
        return Color.Lerp(Patina, PaleMetal, shift * 0.72f);
    }

    private static Texture2D MakeTexture(Color[] pixels, string name)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true)
        {
            name = name,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Vector2 Polar(float degrees, float radius)
    {
        float angle = degrees * Mathf.Deg2Rad;
        return Vector2.one * 0.5f + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private static void Stroke(Color[] pixels, Vector2 start, Vector2 end, float halfWidth, Color from, Color to)
    {
        Vector2 delta = (end - start).normalized;
        Vector2 edge = new Vector2(-delta.y, delta.x) * halfWidth;
        Vector2 a = (start - edge) * Size;
        Vector2 b = (start + edge) * Size;
        Vector2 c = (end + edge) * Size;
        Vector2 d = (end - edge) * Size;
        FillTriangle(pixels, a, b, c, from, from, to);
        FillTriangle(pixels, a, c, d, from, to, to);
    }

    private static void FillTriangle(Color[] pixels, Vector2 a, Vector2 b, Vector2 c,
        Color colorA, Color colorB, Color colorC)
    {
        int left = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)));
        int right = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)));
        int bottom = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)));
        int top = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)));
        float area = Edge(a, b, c);

        for (int y = bottom; y <= top; y++)
        {
            for (int x = left; x <= right; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                float u = Edge(b, c, point) / area;
                float v = Edge(c, a, point) / area;
                float w = 1f - u - v;
                if (u >= 0f && v >= 0f && w >= 0f)
                    pixels[y * Size + x] = colorA * u + colorB * v + colorC * w;
            }
        }
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 p)
    {
        return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    }
}

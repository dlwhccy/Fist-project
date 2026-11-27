using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 统一配色 + 运行时生成贴图/精灵（不依赖任何美术素材）。
/// 好处：不用往 Assets 里塞图，也不会出现 PPU 对不上、拖拽漏掉的问题。
/// </summary>
public static class Theme
{
    // ---------- 配色（深色主题） ----------
    public static readonly Color BgTop = new Color32(0x1E, 0x2A, 0x4A, 0xFF);
    public static readonly Color BgBottom = new Color32(0x07, 0x0A, 0x12, 0xFF);
    public static readonly Color Wall = new Color32(0x2C, 0x3A, 0x58, 0xFF);
    public static readonly Color Ball = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    public static readonly Color BallGlow = new Color(0.45f, 0.85f, 1f, 0.22f);
    public static readonly Color PaddleMain = new Color32(0x54, 0xD6, 0xFF, 0xFF);
    public static readonly Color PaddleGlow = new Color(0.33f, 0.84f, 1f, 0.20f);

    // 砖块按行取色：上→下
    public static readonly Color[] BrickRows =
    {
        new Color32(0xFF, 0x4D, 0x6D, 0xFF), // 红
        new Color32(0xFF, 0x9F, 0x45, 0xFF), // 橙
        new Color32(0xFF, 0xD2, 0x4C, 0xFF), // 黄
        new Color32(0x6E, 0xE7, 0xB7, 0xFF), // 绿
        new Color32(0x4C, 0xC9, 0xF0, 0xFF), // 青
    };

    // ---------- 排序层（同一 Sorting Layer 内数值小的在后） ----------
    public const int OrderBackdrop = -40;
    public const int OrderStars = -30;
    public const int OrderWall = -10;
    public const int OrderBrick = 0;
    public const int OrderPaddle = 5;
    public const int OrderBall = 10;

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // ---------- 精灵工厂 ----------

    /// 白色圆形（1 像素软边抗锯齿），pixelsPerUnit = px，即精灵直径 1 个世界单位
    public static Sprite Circle(int px = 128)
    {
        string key = "circle" + px;
        Sprite s;
        if (cache.TryGetValue(key, out s)) return s;

        var tex = NewTexture(px, px);
        var cols = new Color32[px * px];
        float r = px * 0.5f - 1f;
        float cx = (px - 1) * 0.5f;
        for (int y = 0; y < px; y++)
            for (int x = 0; x < px; x++)
            {
                float dx = x - cx, dy = y - cx;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d + 0.5f);
                cols[y * px + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels32(cols);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), px);
        cache[key] = s;
        return s;
    }

    /// 圆角矩形，pixelsPerUnit = w，即精灵宽 1 个世界单位
    public static Sprite RoundedRect(int w = 128, int h = 72, int radius = 16)
    {
        string key = "rr" + w + "x" + h + "r" + radius;
        Sprite s;
        if (cache.TryGetValue(key, out s)) return s;

        var tex = NewTexture(w, h);
        var cols = new Color32[w * h];
        float hw = w * 0.5f - 1f, hh = h * 0.5f - 1f;
        float rad = Mathf.Min(radius, Mathf.Min(hw, hh));
        float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx) - (hw - rad);
                float dy = Mathf.Abs(y - cy) - (hh - rad);
                float ox = Mathf.Max(dx, 0f), oy = Mathf.Max(dy, 0f);
                float outside = Mathf.Sqrt(ox * ox + oy * oy);
                float d = outside + Mathf.Min(Mathf.Max(dx, dy), 0f) - rad; // 圆角矩形 SDF
                float a = Mathf.Clamp01(0.5f - d);
                cols[y * w + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels32(cols);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        cache[key] = s;
        return s;
    }

    /// 竖直渐变（上 top 下 bottom），pixelsPerUnit = w
    public static Sprite VerticalGradient(Color top, Color bottom, int w = 8, int h = 256)
    {
        string key = "grad" + w + "x" + h + (Color32)top + (Color32)bottom;
        Sprite s;
        if (cache.TryGetValue(key, out s)) return s;

        var tex = NewTexture(w, h);
        var cols = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bottom, top, (float)y / (h - 1));
            for (int x = 0; x < w; x++) cols[y * w + x] = c;
        }
        tex.SetPixels32(cols);
        tex.Apply();
        s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        cache[key] = s;
        return s;
    }

    static Texture2D NewTexture(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.DontSave;
        return tex;
    }

    /// 把精灵拉伸到指定世界尺寸（避免直接改 scale 把贴图比例压扁）
    public static void Fit(SpriteRenderer sr, float worldW, float worldH)
    {
        if (sr == null || sr.sprite == null) return;
        Vector2 b = sr.sprite.bounds.size;
        sr.transform.localScale = new Vector3(worldW / b.x, worldH / b.y, 1f);
    }

    /// 复用精灵自带材质，保证打包后 shader 一定在包里
    public static Material SpriteMaterial(SpriteRenderer sr)
    {
        if (sr != null && sr.sharedMaterial != null) return new Material(sr.sharedMaterial);
        var sh = Shader.Find("Sprites/Default");
        return sh != null ? new Material(sh) : null;
    }
}

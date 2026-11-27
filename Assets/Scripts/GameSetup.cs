using UnityEngine;

/// <summary>
/// 开局自动布景：相机自适应、深色背景 + 星空、砖块网格、球/挡板/墙的样式、音效初始化。
///
/// 用 RuntimeInitializeOnLoadMethod 不用在场景里挂脚本、也不用拖任何 Inspector 引用，
/// 所以你在场景里怎么摆都不会被破坏；原场景里手工摆的 5 块砖会被隐藏（保留，可恢复）。
/// </summary>
public static class GameSetup
{
    // 可供其它脚本读的状态
    public static float HalfWidth { get; private set; } = 7.5f;   // 场地半宽（墙内沿 x 坐标）
    public static int BricksLeft { get; set; }                     // 剩余砖块数
    public static float PaddleHalfWidth = 1.25f;                   // 挡板半宽（世界单位）

    public const float TopWallY = 5.5f;
    const float BaseOrtho = 6.4f;      // 16:9 时的正交尺寸
    const float MinHalfWidth = 4.2f;   // 竖屏时场地最窄
    const float MaxHalfWidth = 8.5f;   // 横屏时场地最宽（再宽画面就散了）
    const float CamY = -0.4f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // --- 音效 ---
        var audio = cam.GetComponent<AudioSource>();
        if (audio == null) audio = cam.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;
        Sfx.Init(audio);

        // --- 相机 + 场地 ---
        FitCamera(cam);
        BuildBackdrop();
        BuildStars();
        PositionWalls();

        // --- 角色样式 ---
        StyleBall(GameObject.Find("Ball"));
        StylePaddle(GameObject.Find("Paddle"));

        // --- 关卡 ---
        BuildLevel();

        // --- 屏幕尺寸变化（竖屏/横屏、浏览器窗口缩放）时重新贴合 ---
        if (cam.GetComponent<FieldFit>() == null) cam.gameObject.AddComponent<FieldFit>();
    }

    // ---------------- 相机适配 ----------------

    /// 竖屏时把相机拉远，保证场地宽度至少 2*MinHalfWidth；横屏时不放大，靠 MaxHalfWidth 收边
    public static void FitCamera(Camera cam)
    {
        if (cam == null) return;
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float ortho = Mathf.Max(BaseOrtho, MinHalfWidth / Mathf.Max(0.05f, aspect));
        cam.orthographicSize = ortho;
        cam.transform.position = new Vector3(0f, CamY, cam.transform.position.z);
        HalfWidth = Mathf.Clamp(ortho * aspect - 0.35f, MinHalfWidth, MaxHalfWidth);
    }

    /// 三面墙贴合场地边缘，挡板移动范围同步收窄
    public static void PositionWalls()
    {
        var l = GameObject.Find("Wall_Left");
        if (l != null) l.transform.position = new Vector3(-HalfWidth - 0.25f, 0f, 0f);

        var r = GameObject.Find("Wall_Right");
        if (r != null) r.transform.position = new Vector3(HalfWidth + 0.25f, 0f, 0f);

        var t = GameObject.Find("Wall_Top");
        if (t != null)
        {
            t.transform.position = new Vector3(0f, TopWallY, 0f);
            t.transform.localScale = new Vector3(2f * HalfWidth + 1f, 0.5f, 1f);
        }

        var pad = GameObject.Find("Paddle");
        if (pad != null)
        {
            var pm = pad.GetComponent<PaddleMove>();
            if (pm != null) pm.limit = Mathf.Max(0.5f, HalfWidth - PaddleHalfWidth);
        }
    }

    // ---------------- 背景 ----------------

    static void BuildBackdrop()
    {
        var go = new GameObject("Backdrop");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Theme.VerticalGradient(Theme.BgTop, Theme.BgBottom);
        sr.color = Color.white;
        sr.sortingOrder = Theme.OrderBackdrop;
        go.transform.position = new Vector3(0f, 0f, 0.5f);
        Theme.Fit(sr, 80f, 60f);   // 足够大，任何比例都铺满
    }

    static void BuildStars()
    {
        var root = new GameObject("Stars");
        var sprite = Theme.Circle(16);
        var rnd = new System.Random(20261118);   // 固定种子：每次开局星空一样，不闪

        for (int i = 0; i < 70; i++)
        {
            var go = new GameObject("Star");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(
                (float)(rnd.NextDouble() * 34.0 - 17.0),
                (float)(rnd.NextDouble() * 26.0 - 13.0),
                0.4f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 1f, 1f, (float)(0.05 + rnd.NextDouble() * 0.18));
            sr.sortingOrder = Theme.OrderStars;

            float size = (float)(0.05 + rnd.NextDouble() * 0.10);
            Theme.Fit(sr, size, size);
        }
    }

    // ---------------- 样式 ----------------

    static void StyleWalls()
    {
        string[] names = { "Wall_Left", "Wall_Right", "Wall_Top" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go == null) continue;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            sr.color = Theme.Wall;
            sr.sortingOrder = Theme.OrderWall;
        }
    }

    static void StyleBall(GameObject ball)
    {
        if (ball == null) return;
        var sr = ball.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.sprite = Theme.Circle(128);
        sr.color = Theme.Ball;
        sr.sortingOrder = Theme.OrderBall;
        Theme.Fit(sr, 0.62f, 0.62f);

        // 柔光（子物体，跟着球走）
        if (ball.transform.Find("BallGlow") == null)
        {
            var glow = new GameObject("BallGlow");
            glow.transform.SetParent(ball.transform, false);
            var gsr = glow.AddComponent<SpriteRenderer>();
            gsr.sprite = Theme.Circle(128);
            gsr.color = Theme.BallGlow;
            gsr.sortingOrder = Theme.OrderBall - 1;
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = new Vector3(2.1f, 2.1f, 1f);
        }

        // 拖尾
        var tr = ball.GetComponent<TrailRenderer>();
        if (tr == null) tr = ball.AddComponent<TrailRenderer>();
        tr.time = 0.30f;
        tr.startWidth = 0.32f;
        tr.endWidth = 0f;
        tr.numCapVertices = 4;
        tr.startColor = new Color(0.62f, 0.92f, 1f, 0.60f);
        tr.endColor = new Color(0.30f, 0.70f, 1f, 0f);
        tr.sortingOrder = Theme.OrderBall - 2;
        var mat = Theme.SpriteMaterial(sr);
        if (mat != null) tr.material = mat;
    }

    static void StylePaddle(GameObject paddle)
    {
        if (paddle == null) return;
        var sr = paddle.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.sprite = Theme.RoundedRect(256, 48, 20);
        sr.color = Theme.PaddleMain;
        sr.sortingOrder = Theme.OrderPaddle;
        Theme.Fit(sr, PaddleHalfWidth * 2f, 0.4f);

        if (paddle.transform.Find("PaddleGlow") == null)
        {
            var glow = new GameObject("PaddleGlow");
            glow.transform.SetParent(paddle.transform, false);
            var gsr = glow.AddComponent<SpriteRenderer>();
            gsr.sprite = Theme.RoundedRect(256, 48, 20);
            gsr.color = Theme.PaddleGlow;
            gsr.sortingOrder = Theme.OrderPaddle - 1;
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = new Vector3(1.12f, 1.9f, 1f);
        }
    }

    // ---------------- 关卡 ----------------

    static void BuildLevel()
    {
        // 1) 把手搓的 5 块砖藏起来（不是删掉，随时可以在 Inspector 里 SetActive 回来）
        var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in all)
            if (go.name.StartsWith("Brick")) go.SetActive(false);

        var oldRoot = GameObject.Find("Bricks_Generated");
        if (oldRoot != null) Object.Destroy(oldRoot);

        // 2) 按场地宽度算列数，生成网格
        var root = new GameObject("Bricks_Generated");
        var sprite = Theme.RoundedRect(128, 72, 16);

        float bw = 1f, bh = 0.56f, gapX = 0.13f, gapY = 0.19f;
        const int rows = 5;
        float usable = 2f * HalfWidth - 0.7f;
        int cols = Mathf.Clamp(Mathf.FloorToInt((usable + gapX) / (bw + gapX)), 4, 12);

        float totalW = cols * bw + (cols - 1) * gapX;
        float startX = -totalW * 0.5f + bw * 0.5f;
        float topY = 4.5f;

        BricksLeft = 0;

        for (int r = 0; r < rows; r++)
        {
            Color c = Theme.BrickRows[r % Theme.BrickRows.Length];
            for (int i = 0; i < cols; i++)
            {
                var go = new GameObject("Brick");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(startX + i * (bw + gapX), topY - r * (bh + gapY), 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = c;
                sr.sortingOrder = Theme.OrderBrick;
                Theme.Fit(sr, bw - 0.06f, bh);

                var bc = go.AddComponent<BoxCollider2D>();
                bc.size = sprite.bounds.size;   // 跟着精灵比例，保证碰撞体和画面一致

                BricksLeft++;
            }
        }

        StyleWalls();
    }
}

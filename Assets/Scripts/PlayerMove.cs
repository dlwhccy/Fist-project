using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerMove : MonoBehaviour
{
    public float speed = 8f;          // 球速（恒定，不随碰撞衰减）
    public TMP_Text scoreText;
    public int lives = 3;

    // 提示文字配色
    static readonly Color ColorReady = new Color32(0xFF, 0xEC, 0xB3, 0xFF);   // 暖金
    static readonly Color ColorOver  = new Color32(0xFF, 0x6B, 0x81, 0xFF);   // 珊瑚红
    static readonly Color ColorWin   = new Color32(0x8B, 0xF0, 0xC0, 0xFF);   // 薄荷绿

    private Rigidbody2D rb;
    private int score = 0;
    private bool gameOver = false;
    private bool started = false;     // 按 K / 点击才开始发球

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        transform.position = new Vector3(0f, -2f, 0f);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        UpdateUI();
        ShowReady();
    }

    // ---------------- UI ----------------

    void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "SCORE " + score + "    LIFE " + lives;
    }

    /// <summary>屏幕正中大字提示（没有提示层时退化成顶部文字）</summary>
    void SetMessage(string text, Color color)
    {
        var m = GameSetup.Message;
        if (m != null)
        {
            m.color = color;
            m.text = text;
            m.transform.SetAsLastSibling();   // 保证盖在分数之上
        }
        else if (scoreText != null)
        {
            scoreText.text = text;
        }
    }

    void ClearMessage()
    {
        var m = GameSetup.Message;
        if (m != null) m.text = "";
    }

    /// <summary>提示文字呼吸动画</summary>
    void PulseMessage()
    {
        var m = GameSetup.Message;
        if (m == null || string.IsNullOrEmpty(m.text)) return;
        Color c = m.color;
        c.a = 0.70f + 0.30f * (0.5f + 0.5f * Mathf.Sin(Time.time * 3f));
        m.color = c;
    }

    void ShowReady()
    {
        started = false;
        gameOver = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        transform.position = new Vector3(0f, -2f, 0f);
        SetMessage("按 K 或点击屏幕\n开始游戏", ColorReady);
    }

    // ---------------- 球 ----------------

    void ResetBall()
    {
        transform.position = new Vector3(0f, -2f, 0f);
        // 发球角度每次略有不同，别每次都一样
        float ang = Random.Range(35f, 55f) * Mathf.Deg2Rad;
        float sx = Random.value < 0.5f ? -1f : 1f;
        rb.linearVelocity = new Vector2(Mathf.Cos(ang) * sx, Mathf.Sin(ang)) * speed;
    }

    /// <summary>
    /// 就地重开：不复用 SceneManager.LoadScene，因为整个赛场是启动时用代码搭的，
    /// 重载场景会把布景销毁且不会重建（会白屏 / 碰一下直接通关）。
    /// </summary>
    void RestartGame()
    {
        score = 0;
        lives = 3;

        GameSetup.ResetGame();   // 重建砖块、挡板归位、清理碎屑
        UpdateUI();
        ShowReady();             // 回到「按 K 开始」，不自动发球
    }

    // 保持球速恒定，并防止球贴着水平线来回跑（那是卡死的常见原因）
    void FixedUpdate()
    {
        if (!started || gameOver || rb == null) return;
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.001f) return;

        v = v.normalized * speed;
        if (Mathf.Abs(v.y) < speed * 0.22f)
        {
            v.y = (v.y >= 0f ? 1f : -1f) * speed * 0.22f;
            v = v.normalized * speed;
        }
        rb.linearVelocity = v;
    }

    void Update()
    {
        PulseMessage();

        bool keyK = Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
        bool keyEsc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool tap = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;

        // Esc：退出（Windows 直接退出程序；网页里尝试关闭标签页）
        if (keyEsc)
        {
            QuitGame();
            return;
        }

        // 失败 / 通关后：K 或点击 → 重开
        if (gameOver)
        {
            if (keyK || tap) RestartGame();
            return;
        }

        // 开局待发球：K 或点击 → 开始
        if (!started)
        {
            if (keyK || tap)
            {
                started = true;
                ClearMessage();
                ResetBall();
                UpdateUI();
            }
            return;
        }

        if (transform.position.y < -6f ||
            Mathf.Abs(transform.position.x) > 30f ||
            Mathf.Abs(transform.position.y) > 30f)
        {
            lives = lives - 1;
            Sfx.Play("lose");

            if (lives <= 0)
            {
                gameOver = true;
                started = false;
                rb.linearVelocity = Vector2.zero;
                SetMessage("GAME OVER\n按 K 或点击重新开始", ColorOver);
                Sfx.Play("over");
            }
            else
            {
                UpdateUI();
                ResetBall();
            }
        }
    }

    // ---------------- 退出 ----------------

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        try { CloseTab(); } catch { }
        SetMessage("已请求关闭页面\n若没关闭请按 Ctrl+W", ColorReady);
#else
        Application.Quit();
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void CloseTab();
#endif

    // ---------------- 碰撞 ----------------

    void OnCollisionEnter2D(Collision2D collision)
    {
        string other = collision.gameObject.name;

        if (other.StartsWith("Brick"))
        {
            var sr = collision.gameObject.GetComponent<SpriteRenderer>();
            Fx.Burst(collision.transform.position, sr != null ? sr.color : Color.white);

            score = score + 1;
            if (GameSetup.BricksLeft > 0) GameSetup.BricksLeft--;
            UpdateUI();
            Sfx.Play("brick", Random.Range(1.0f, 1.18f));

            Destroy(collision.gameObject);

            if (GameSetup.BricksLeft <= 0)
            {
                gameOver = true;
                started = false;
                rb.linearVelocity = Vector2.zero;
                SetMessage("YOU WIN!\n按 K 或点击再来一局", ColorWin);
                Sfx.Play("win");
            }
            return;
        }

        if (other == "Paddle")
        {
            // 打在挡板哪个位置，决定反弹角度：越靠边越斜（经典打砖块手感）
            float halfW = Mathf.Max(0.1f, collision.transform.localScale.x * 0.5f);
            float offset = Mathf.Clamp((transform.position.x - collision.transform.position.x) / halfW, -1f, 1f);
            float rad = offset * 60f * Mathf.Deg2Rad;
            rb.linearVelocity = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * speed;
            Sfx.Play("paddle");
            return;
        }

        if (other.StartsWith("Wall"))
        {
            Sfx.Play("wall");
        }
    }
}

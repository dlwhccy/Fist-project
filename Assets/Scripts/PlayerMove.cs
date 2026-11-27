using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerMove : MonoBehaviour
{
    public float speed = 8f;          // 球速（恒定，不随碰撞衰减）
    public TMP_Text scoreText;
    public int lives = 3;

    private Rigidbody2D rb;
    private int score = 0;
    private bool gameOver = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ResetBall();
        UpdateUI();
    }

    void UpdateUI()
    {
        scoreText.text = "SCORE " + score + "    LIFE " + lives;
    }

    void ResetBall()
    {
        transform.position = new Vector3(0f, -2f, 0f);
        // 发球角度每次略有不同，别每次都一样
        float ang = Random.Range(35f, 55f) * Mathf.Deg2Rad;
        float sx = Random.value < 0.5f ? -1f : 1f;
        rb.linearVelocity = new Vector2(Mathf.Cos(ang) * sx, Mathf.Sin(ang)) * speed;
    }

    // 保持球速恒定，并防止球贴着水平线来回跑（那是卡死的常见原因）
    void FixedUpdate()
    {
        if (gameOver || rb == null) return;
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
        if (gameOver)
        {
            // 键盘 R 或 手指/鼠标点一下 都能重开（手机上不能按 R）
            bool key = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
            bool tap = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
            if (key || tap)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
                rb.linearVelocity = Vector2.zero;
                scoreText.text = "GAME OVER\nTAP TO RESTART";
                Sfx.Play("over");
            }
            else
            {
                UpdateUI();
                ResetBall();
            }
        }
    }

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
                rb.linearVelocity = Vector2.zero;
                scoreText.text = "YOU WIN!\nTAP TO RESTART";
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

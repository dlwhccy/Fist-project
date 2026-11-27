using UnityEngine;

/// <summary>
/// 砖块被击碎时飞出的小碎片（自己会淡出并销毁）。
/// 由 Fx.Burst 动态 AddComponent，不需要预制成 Prefab。
/// </summary>
public class Fragment : MonoBehaviour
{
    public Vector2 velocity;
    public float life = 0.55f;
    public float spin = 0f;

    SpriteRenderer sr;
    float t;
    float size;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        size = transform.localScale.x;
    }

    void Update()
    {
        t += Time.deltaTime;

        velocity += new Vector2(0f, -16f) * Time.deltaTime;      // 重力
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.Rotate(0f, 0f, spin * Time.deltaTime);

        float k = 1f - Mathf.Clamp01(t / life);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = k;
            sr.color = c;
        }
        transform.localScale = Vector3.one * size * Mathf.Lerp(0.55f, 1f, k);

        if (t >= life) Destroy(gameObject);
    }
}

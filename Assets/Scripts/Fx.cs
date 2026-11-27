using UnityEngine;

/// <summary>
/// 纯代码特效：砖块碎裂的粒子感碎片。
/// 用 SpriteRenderer 拼，不依赖 ParticleSystem 的材质/贴图设置。
/// </summary>
public static class Fx
{
    static Sprite square;

    public static void Burst(Vector3 pos, Color color, int count = 8)
    {
        if (square == null) square = Theme.Circle(12);

        var holder = new GameObject("FxBurst");
        holder.transform.position = pos;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Fragment");
            go.transform.SetParent(holder.transform, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * Random.Range(0.09f, 0.16f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color(color.r, color.g, color.b, 1f);
            sr.sortingOrder = Theme.OrderBall + 1;

            var f = go.AddComponent<Fragment>();
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float spd = Random.Range(2.5f, 7.5f);
            f.velocity = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            f.life = Random.Range(0.35f, 0.65f);
            f.spin = Random.Range(-720f, 720f);
        }

        Object.Destroy(holder, 1.2f);
    }
}

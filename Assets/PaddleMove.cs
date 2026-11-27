using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleMove : MonoBehaviour
{
    public float speed = 10f;
    public float limit = 7.5f;

    void Update()
    {
        Vector3 p = transform.position;

        // 手指或鼠标按住 → 挡板跟过去
        if (Pointer.current != null && Pointer.current.press.isPressed)
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Pointer.current.position.ReadValue());
            p.x = world.x;
        }
        else
        {
            // 键盘也行（PC 上保留）
            float h = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            }
            p.x += h * speed * Time.deltaTime;
        }

        p.x = Mathf.Clamp(p.x, -limit, limit);
        transform.position = p;
    }
}
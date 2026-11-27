using UnityEngine;

/// <summary>
/// 监听屏幕尺寸变化（浏览器窗口缩放、手机横竖屏切换），重新贴合相机与场地。
/// 由 GameSetup 自动挂到主相机上，不需要手拖。
/// </summary>
public class FieldFit : MonoBehaviour
{
    int lastW, lastH;

    void Start()
    {
        lastW = Screen.width;
        lastH = Screen.height;
    }

    void Update()
    {
        if (Screen.width == lastW && Screen.height == lastH) return;
        lastW = Screen.width;
        lastH = Screen.height;
        GameSetup.FitCamera(Camera.main);
        GameSetup.PositionWalls();
    }
}

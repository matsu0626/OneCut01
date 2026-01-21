#if _DEBUG
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// デバッグ用マウス入力ラッパー
/// </summary>
public sealed class DebugMouse : SingletonMonoBehaviour<DebugMouse>
{
    public bool HasMouse => Mouse.current != null;

    /// <summary>前フレームからの移動量（ピクセル）</summary>
    public Vector2 Delta { get; private set; }

    /// <summary>スクロール量（y方向）</summary>
    public float ScrollY { get; private set; }

    /// <summary>右ボタン押下中</summary>
    public bool RightPressed { get; private set; }

    /// <summary>中ボタン押下中</summary>
    public bool MiddlePressed { get; private set; }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            Delta = Vector2.zero;
            ScrollY = 0f;
            RightPressed = false;
            MiddlePressed = false;
            return;
        }

        Delta = mouse.delta.ReadValue();
        ScrollY = mouse.scroll.ReadValue().y;
        RightPressed = mouse.rightButton.isPressed;
        MiddlePressed = mouse.middleButton.isPressed;
    }
}
#endif

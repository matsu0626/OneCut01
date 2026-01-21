using UnityEngine;

[CreateAssetMenu(menuName = "UI Settings/ConfirmDialog Layout Settings")]
public class ConfirmDialogLayoutSettings : ScriptableObject
{
    [Header("TextBase")]
    public float textBaseMinHeight = 120f;
    public float textBaseAdjustHeight = 24f;    // テキスト上下の余白ぶん

    [Header("Base")]
    public float baseMinHeight = 240f;
    public float baseAdjustHeight = 56f;        // 上側の余白ぶん（ヘッダなど）

    // OKボタンのアンカーはAnchorPresetsはBottomCenter想定
    [Header("Spacing")]
    public float spacingTextToButton = 24f;     // TextBase と OKボタンの隙間
    public float buttonBottomPadding = 40f;     // Base 下端からボタン“下端”までの余白

}


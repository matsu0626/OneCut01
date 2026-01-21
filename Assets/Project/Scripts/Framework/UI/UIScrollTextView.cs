using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIScrollTextView : MonoBehaviour
{
    [Header("スクロールビュー本体")]
    [SerializeField] private ScrollRect m_scrollRect;

    [Header("表示用テキスト (Content 配下の TextMeshProUGUI)")]
    [SerializeField] private TextMeshProUGUI m_text;

    [Header("上下パディング")]
    [SerializeField] private float m_verticalPadding = 16f;

    [Header("左右パディング")]
    [SerializeField] private float m_horizontalPaddingLeft = 16f;
    [SerializeField] private float m_horizontalPaddingRight = 16f;

    [Header("OnEnable 時に先頭にスクロールするか")]
    [SerializeField] private bool m_scrollToTopOnEnable = false;

    public ScrollRect ScrollRect => m_scrollRect;
    public TextMeshProUGUI Text => m_text;

    private RectTransform ViewportRect
        => m_scrollRect ? m_scrollRect.viewport : null;

    private RectTransform ContentRect
        => m_scrollRect ? m_scrollRect.content : null;

    private RectTransform TextRect
        => m_text ? (RectTransform)m_text.transform : null;

    private void OnEnable()
    {
        if (m_scrollToTopOnEnable)
        {
            ScrollToTop();
        }
        RefreshLayout();
    }

    /// <summary>テキストをそのままセット。</summary>
    public void SetText(string text)
    {
        if (!m_text) return;

        m_text.text = text ?? string.Empty;
        RefreshLayout();
    }

    public void Clear()
    {
        if (!m_text) return;
        m_text.text = string.Empty;
        RefreshLayout();
    }

    /// <summary>
    /// レイアウトを再計算して、Text / Content のサイズを更新。
    /// </summary>
    public void RefreshLayout()
    {
        if (!m_scrollRect || !m_text)
            return;

        var vp = ViewportRect;
        var content = ContentRect;
        var textRt = TextRect;

        if (!vp || !content || !textRt)
            return;

        // 1. Viewport の幅から左右パディング分を差し引いた幅をテキスト幅にする
        float viewportWidth = vp.rect.width;
        if (viewportWidth <= 0f)
        {
            // まだレイアウト未確定のタイミングでは無理に計算しない
            return;
        }

        float totalHorizontalPadding = Mathf.Max(0f, m_horizontalPaddingLeft + m_horizontalPaddingRight);
        float textWidth = Mathf.Max(0f, viewportWidth - totalHorizontalPadding);

        // 左に余白を作りたいので、anchoredPosition.x を left padding にして、
        // width を「viewport 幅 − 左右パディング」にする
        textRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);

        var anchoredPos = textRt.anchoredPosition;
        anchoredPos.x = m_horizontalPaddingLeft;
        textRt.anchoredPosition = anchoredPos;

        // 2. TMP に計測させる
        m_text.ForceMeshUpdate();
        float preferredHeight = m_text.preferredHeight;

        // 3. Text の高さを反映
        float textHeight = preferredHeight;
        textRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

        // 4. Content の高さを Text + 縦パディング ×2 にする
        float contentHeight = textHeight + m_verticalPadding * 2f;
        var size = content.sizeDelta;
        size.y = contentHeight;
        content.sizeDelta = size;

        // Content の位置は上寄せ
        var contentPos = content.anchoredPosition;
        contentPos.x = 0f;
        contentPos.y = 0f;
        content.anchoredPosition = contentPos;
    }

    public void ScrollToTop()
    {
        if (!m_scrollRect) return;
        m_scrollRect.normalizedPosition = new Vector2(0f, 1f);
    }

    public void ScrollToBottom()
    {
        if (!m_scrollRect) return;
        m_scrollRect.normalizedPosition = new Vector2(0f, 0f);
    }

#if UNITY_EDITOR
    private void Reset()
    {
        if (!m_scrollRect)
        {
            m_scrollRect = GetComponentInChildren<ScrollRect>();
        }
        if (!m_text)
        {
            m_text = GetComponentInChildren<TextMeshProUGUI>();
        }
    }
#endif
}

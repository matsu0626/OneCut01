#if _DEBUG
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DebugPrint : SingletonMonoBehaviour<DebugPrint>
{
    [Header("Hierarchy")]
    [SerializeField] private RectTransform m_parent;

    [Header("Typography")]
    [SerializeField] private TMP_FontAsset m_font;
    [SerializeField] private int m_fontSize = 46;
    [SerializeField] private float m_lineSpacing = 60f;
    [SerializeField] private Color m_defaultColor = Color.white;

    private RectTransform m_topRoot;
    private RectTransform m_bottomRoot;

    private readonly List<Item> m_frameItems = new();
    private class Item { public bool Bottom; public float X; public int Line; public string Text; public Color Color; }

    private readonly List<TMP_Text> m_pool = new();
    private int m_poolCursor;

    protected override void Awake()
    {
        base.Awake();
        EnsureHierarchy();
    }


    private void EnsureHierarchy()
    {
        if (m_parent == null)
        {
            m_parent = transform as RectTransform;
            if (m_parent == null)
            {
                var go = new GameObject("DebugHUD", typeof(RectTransform));
                var rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                StretchFull(rt);
                m_parent = rt;
            }
        }

        if (m_topRoot == null) m_topRoot = CreateLayer("TopRoot", m_parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1));
        if (m_bottomRoot == null) m_bottomRoot = CreateLayer("BottomRoot", m_parent, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0));
    }

    private static RectTransform CreateLayer(string name, RectTransform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
        return rt;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
    }

    // ---------- API ----------
    [Conditional("_DEBUG")]
    public void PrintLine(float posX, int line, string text)
        => PrintLine(posX, line, text, m_defaultColor);

    [Conditional("_DEBUG")]
    public void PrintLine(float posX, int line, string text, Color color)
    {
        m_frameItems.Add(new Item { Bottom = false, X = posX, Line = line, Text = text, Color = color });
    }

    [Conditional("_DEBUG")]
    public void PrintLineBottom(float posX, int lineFromBottom, string text)
        => PrintLineBottom(posX, lineFromBottom, text, m_defaultColor);

    [Conditional("_DEBUG")]
    public void PrintLineBottom(float posX, int lineFromBottom, string text, Color color)
    {
        m_frameItems.Add(new Item { Bottom = true, X = posX, Line = lineFromBottom, Text = text, Color = color });
    }

    // ---------- 描画 ----------
    private void LateUpdate()
    {
        if (m_parent == null) { EnsureHierarchy(); if (m_parent == null) return; }

        m_poolCursor = 0;

        foreach (var it in m_frameItems)
        {
            var root = it.Bottom ? m_bottomRoot : m_topRoot;
            var label = Rent(root);

            var rt = (RectTransform)label.transform;
            if (it.Bottom)
            {
                // 下からの行番号 → +方向に積む
                rt.anchoredPosition = new Vector2(it.X, it.Line * m_lineSpacing);
            }
            else
            {
                // 上からの行番号 → -方向に積む
                rt.anchoredPosition = new Vector2(it.X, -it.Line * m_lineSpacing);
            }

            label.text = it.Text;
            label.color = it.Color;
        }

        for (int i = m_poolCursor; i < m_pool.Count; i++)
            m_pool[i].gameObject.SetActive(false);

        m_frameItems.Clear();
    }

    private TMP_Text Rent(RectTransform parent)
    {
        TMP_Text label;
        if (m_poolCursor < m_pool.Count)
        {
            label = m_pool[m_poolCursor];
        }
        else
        {
            label = CreateLabel(parent); // 初期親で仮設定
            m_pool.Add(label);
        }
        m_poolCursor++;

        var rt = (RectTransform)label.transform;

        // ★ ここが重要：今回使う側(Top/Bottom)に合わせて整形を必ず実行
        bool isTop = (parent == m_topRoot);
        if (rt.parent != parent)
        {
            rt.SetParent(parent, false);
            ConfigureLabelForSide(label, isTop);
        }
        else
        {
            // 同じ親でも、前フレームに Bottom→今フレーム Top などの入れ替わりがありうるので保険で毎回適用
            ConfigureLabelForSide(label, isTop);
        }

        label.gameObject.SetActive(true);
        return label;
    }

    private void ConfigureLabelForSide(TMP_Text tmp, bool isTop)
    {
        var rt = (RectTransform)tmp.transform;

        // 上左(0,1) or 下左(0,0) に統一
        var pivot = isTop ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
        rt.anchorMin = pivot;
        rt.anchorMax = pivot;
        rt.pivot = pivot;

        // ラベルの基準位置も一致させる
        tmp.alignment = isTop ? TextAlignmentOptions.TopLeft
                              : TextAlignmentOptions.BottomLeft;

        // 位置は毎フレーム LateUpdate 側で anchoredPosition を設定するのでここではゼロでOK
        rt.anchoredPosition = Vector2.zero;
    }

    private TMP_Text CreateLabel(RectTransform parent)
    {
        var go = new GameObject("DbgText", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        bool isTop = (parent == m_topRoot);
        rt.anchorMin = rt.anchorMax = rt.pivot = isTop ? new Vector2(0, 1) : new Vector2(0, 0);
        rt.anchoredPosition = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        if (m_font != null) tmp.font = m_font;
        tmp.fontSize = m_fontSize;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.margin = Vector4.zero;
        tmp.alignment = isTop ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.BottomLeft;
        tmp.text = "";

        return tmp;
    }

    public int GetFontSize() => m_fontSize;
    public float GetLineSpacing() => m_lineSpacing;
}
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIScrollView : MonoBehaviour
{
    [SerializeField] private ScrollRect m_scroll;       // 自動取得
    [SerializeField] private RectTransform m_content;   // 自動取得

    private readonly List<UIScrollCell> m_cells = new();
    private IScrollViewDelegate m_delegate;

    private void Awake()
    {
        if (!m_scroll) m_scroll = GetComponent<ScrollRect>();
        if (!m_content && m_scroll) m_content = m_scroll.content;
    }

    /// <summary>
    /// 初期化：横/縦、生成数、デリゲート。クリック配線は内部で自動。
    /// </summary>
    public void Initialize(GameObject cellPrefab, int itemCount, bool horizontal, IScrollViewDelegate del = null)
    {
        if (!m_scroll || !m_content)
        {
            Debug.LogWarning("[UIScrollView] ScrollRect/Content 未設定");
            return;
        }

        if (!cellPrefab)
        {
            Debug.LogWarning("[UIScrollView] cellPrefab が null");
            return;
        }

        m_delegate = del;

        // 向き設定（ここで一元管理）
        m_scroll.horizontal = horizontal;
        m_scroll.vertical = !horizontal;

        // 既存掃除
        for (int i = 0; i < m_cells.Count; i++)
        {
            if (!m_cells[i]) continue;
            m_cells[i].Unbind();
            Object.Destroy(m_cells[i].gameObject);
        }

        m_cells.Clear();

        // 生成→デリゲートで見た目更新→自動クリック配線
        for (int i = 0; i < itemCount; i++)
        {
            var go = Object.Instantiate(cellPrefab, m_content, false);
            go.name = $"{cellPrefab.name}_{i:D3}";

            var cell = go.GetComponent<UIScrollCell>();
            if (!cell) cell = go.AddComponent<UIScrollCell>();

            // 見た目更新＋デリゲート紐付け
            cell.Bind(i, m_delegate);

            // クリック自動登録（セル内の UIButton を一括スキャンして ID=GameObject名 で登録）
            cell.ClearClickables();
            var buttons = go.GetComponentsInChildren<UIButton>(true);
            foreach (var btn in buttons)
            {
                if (!btn) continue;
                cell.AddClickable(btn, btn.gameObject.name);
            }
            cell.ApplyClickWiring();

            m_cells.Add(cell);
        }

        // 先頭へ
        if (m_scroll.vertical)
        {
            m_scroll.normalizedPosition = new Vector2(m_scroll.normalizedPosition.x, 1f);
        }
        else
        {
            m_scroll.normalizedPosition = new Vector2(0f, m_scroll.normalizedPosition.y);
        }
    }

    /// <summary>データ差し替え等で再描画したいとき（Index は据え置き）。</summary>
    public void Refresh()
    {
        foreach (var cell in m_cells)
        {
            if (!cell) continue;

            // 見た目更新
            cell.Bind(cell.Index, m_delegate);

            // クリック配線を作り直す（構造が変わる可能性もあるため）
            cell.ClearClickables();
            var buttons = cell.gameObject.GetComponentsInChildren<UIButton>(true);
            foreach (var btn in buttons)
            {
                if (!btn) continue;
                cell.AddClickable(btn, btn.gameObject.name);
            }
            cell.ApplyClickWiring();
        }
    }
}

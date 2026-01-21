using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UIScrollCell : MonoBehaviour
{
    public int Index { get; private set; }

    private readonly List<(UIButton btn, string id)> m_clickables = new();
    private IScrollViewDelegate m_delegate;

    // 名前検索の結果をキャッシュ（1セル内）
    private readonly Dictionary<(System.Type, string), Component> m_cache = new();

    public void Bind(int index, IScrollViewDelegate del)
    {
        Unbind();
        Index = index;
        m_delegate = del;
        m_delegate?.SetupCell(this, Index);
    }

    public void Unbind()
    {
        foreach (var (btn, _) in m_clickables)
        {
            if (btn) btn.ClearOnClick();
        }

        m_clickables.Clear();
        m_cache.Clear();
        m_delegate = null;
    }

    public void AddClickable(UIButton button, string controlId)
    {
        if (!button) return;
        m_clickables.Add((button, controlId ?? string.Empty));
    }

    public void ClearClickables()
    {
        foreach (var (btn, _) in m_clickables)
        {
            if (btn) btn.ClearOnClick();
        }

        m_clickables.Clear();
    }

    public void ApplyClickWiring()
    {
        foreach (var (btn, id) in m_clickables)
        {
            if (!btn) continue;

            // キャプチャ用ローカル（ラムダの中で this / Index を安定して使うため）
            var capturedCell = this;
            var capturedIndex = Index;
            var capturedId = id;

            btn.ClearOnClick();
            btn.OnClickProc += () =>
            {
                // UIScrollCell 自体＋index＋controlId を渡す
                m_delegate?.OnCellClicked(capturedCell, capturedIndex, capturedId);
            };
        }
    }

    /// <summary>子孫から「名前で」Tを取得（キャッシュあり）。</summary>
    public T GetByName<T>(string name) where T : Component
    {
        var key = (typeof(T), name ?? "");
        if (m_cache.TryGetValue(key, out var c)) return (T)c;

        var found = CommonUtil.FindByName<T>(transform, name);
        if (found) m_cache[key] = found;
        return found;
    }
}

using System.Text;
using UnityEngine;

public sealed class DebugDialogAssetsPanel : MonoBehaviour
{
    [SerializeField] private UIScrollTextView m_scrollTextView;
    [SerializeField] private float m_updateInterval = 1.0f;
    [SerializeField] private bool m_updateOnlyWhenNearTop = true;
    [SerializeField, Range(0f, 1f)] private float m_topThreshold = 0.95f;

    private float m_timer;

    private void OnEnable()
    {
        m_timer = 0f;
        UpdateText();
        m_scrollTextView.ScrollToTop();
    }

    private void Update()
    {
        if (m_scrollTextView == null || m_scrollTextView.ScrollRect == null)
            return;

        var scrollRect = m_scrollTextView.ScrollRect;

        if (m_updateOnlyWhenNearTop)
        {
            // 先頭からだいぶ離れてるときは自動更新しない
            if (scrollRect.normalizedPosition.y < m_topThreshold)
            {
                return;
            }
        }

        m_timer += Time.unscaledDeltaTime;
        if (m_timer >= Mathf.Max(0.2f, m_updateInterval))
        {
            m_timer = 0f;
            var pos = scrollRect.normalizedPosition;
            UpdateText();
            m_scrollTextView.RefreshLayout();

            if (m_updateOnlyWhenNearTop)
            {
                // 「先頭追従モード」なら常に一番上に戻す
                m_scrollTextView.ScrollToTop();
            }
            else
            {
                // そうでないときは位置維持
                scrollRect.normalizedPosition = pos;
            }
        }
    }

    private void UpdateText()
    {
        var sb = new StringBuilder();

        // 1) 常駐（Common）
        var common = CommonAssetManager.Instance;
        if (common != null)
        {
            sb.AppendLine(common.BuildDebugText());
        }

        sb.AppendLine();
        sb.AppendLine();

        // 2) 動的ロード（AssetLoadHelper 経由）
        sb.AppendLine(AssetLoadTracker.BuildDebugText(currentSceneOnly: false));

        sb.AppendLine();
        sb.AppendLine();

        // 3) シーン直参照
        sb.AppendLine(SceneAssetScanner.BuildSceneAssetsDebugText());

        m_scrollTextView.SetText(sb.ToString());
    }
}

using TMPro;
using UnityEngine;

#if _DEBUG

/// <summary>
/// シーン遷移情報表示パネル。
/// SceneTransitionManager から現在シーン＋履歴を取得して表示する。
/// </summary>
public sealed class DebugDialogScenePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI m_sceneInfoText;

    [Header("更新間隔(sec)")]
    [SerializeField] private float m_updateInterval = 0.5f;

    private float m_timer;

    private void OnEnable()
    {
        m_timer = 0f;
        UpdateNow(force: true);
    }

    private void Update()
    {
        m_timer += Time.unscaledDeltaTime;
        if (m_timer >= Mathf.Max(0.1f, m_updateInterval))
        {
            m_timer = 0f;
            UpdateNow(force: false);
        }
    }

    private void UpdateNow(bool force)
    {
        if (m_sceneInfoText == null)
        {
            return;
        }

        SceneTransitionManager stm = SceneTransitionManager.Instance;
        if (stm == null)
        {
            if (force)
            {
                m_sceneInfoText.text = "SceneTransitionManager not found.";
            }
            return;
        }

        string info = stm.GetDebugSceneInfo();
        m_sceneInfoText.text = info;
    }
}

#else

public sealed class DebugMenuScenePanel : MonoBehaviour
{
}

#endif

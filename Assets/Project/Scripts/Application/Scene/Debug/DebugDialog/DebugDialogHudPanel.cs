using UnityEngine;
using UnityEngine.UI;

#if _DEBUG

/// <summary>
/// HUD / Log 系設定：DebugPrint HUD の ON/OFF。
/// </summary>
public sealed class DebugDialogHudPanel : MonoBehaviour
{
    [Header("DebugPrint HUD")]
    [SerializeField] private Toggle m_toggleDebugPrint;

    private bool m_updatingToggle;

    private void Awake()
    {
        if (m_toggleDebugPrint != null)
        {
            m_toggleDebugPrint.onValueChanged.AddListener(OnToggleDebugPrint);
        }
    }

    private void OnEnable()
    {
        DebugSystem sys = DebugSystem.Instance;
        bool enabled = (sys != null) && sys.ShowHud;

        m_updatingToggle = true;
        if (m_toggleDebugPrint != null)
        {
            m_toggleDebugPrint.isOn = enabled;
        }
        m_updatingToggle = false;
    }

    private void OnDestroy()
    {
        if (m_toggleDebugPrint != null)
        {
            m_toggleDebugPrint.onValueChanged.RemoveListener(OnToggleDebugPrint);
        }
    }

    private void OnToggleDebugPrint(bool isOn)
    {
        if (m_updatingToggle)
        {
            return;
        }

        DebugSystem sys = DebugSystem.Instance;
        if (sys != null)
        {
            sys.ShowHud = isOn;
        }
    }
}

#else

public sealed class DebugMenuHudPanel : MonoBehaviour
{
}

#endif

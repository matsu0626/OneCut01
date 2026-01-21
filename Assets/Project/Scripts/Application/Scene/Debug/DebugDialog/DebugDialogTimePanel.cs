using System;
using TMPro;
using UnityEngine;

#if _DEBUG

/// <summary>
/// Time.timeScale 制御パネル（1x / 2x / 4x / 8x）。
/// </summary>
public sealed class DebugDialogTimePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI m_currentValueText;

    [Header("Buttons")]
    [SerializeField] private UIButton m_btn1x;
    [SerializeField] private UIButton m_btn2x;
    [SerializeField] private UIButton m_btn4x;
    [SerializeField] private UIButton m_btn8x;

    private float m_defaultTimeScale;
    private float m_defaultFixedDeltaTime;

    // イベント解除用に Action を保持
    private Action m_handler1x;
    private Action m_handler2x;
    private Action m_handler4x;
    private Action m_handler8x;

    private void Awake()
    {
        m_defaultTimeScale = Time.timeScale;
        m_defaultFixedDeltaTime = Time.fixedDeltaTime;

        m_handler1x = OnClick1x;
        m_handler2x = OnClick2x;
        m_handler4x = OnClick4x;
        m_handler8x = OnClick8x;

        if (m_btn1x != null) m_btn1x.OnClickProc += m_handler1x;
        if (m_btn2x != null) m_btn2x.OnClickProc += m_handler2x;
        if (m_btn4x != null) m_btn4x.OnClickProc += m_handler4x;
        if (m_btn8x != null) m_btn8x.OnClickProc += m_handler8x;
    }

    private void OnEnable()
    {
        UpdateCurrentValueLabel();
    }

    private void OnDestroy()
    {
        if (m_btn1x != null) m_btn1x.OnClickProc -= m_handler1x;
        if (m_btn2x != null) m_btn2x.OnClickProc -= m_handler2x;
        if (m_btn4x != null) m_btn4x.OnClickProc -= m_handler4x;
        if (m_btn8x != null) m_btn8x.OnClickProc -= m_handler8x;
    }

    private void OnClick1x()
    {
        SetTimeScale(1f);
    }

    private void OnClick2x()
    {
        SetTimeScale(2f);
    }

    private void OnClick4x()
    {
        SetTimeScale(4f);
    }

    private void OnClick8x()
    {
        SetTimeScale(8f);
    }

    private void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = m_defaultFixedDeltaTime * scale;

        UpdateCurrentValueLabel();
    }

    private void UpdateCurrentValueLabel()
    {
        if (m_currentValueText == null)
        {
            return;
        }

        m_currentValueText.text = string.Format(
            "TimeScale : {0:F2}\nFixedDeltaTime : {1:F4}",
            Time.timeScale,
            Time.fixedDeltaTime);
    }
}

#else

public sealed class DebugMenuTimePanel : MonoBehaviour
{
}

#endif

using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public sealed class Debug3DLightUI : MonoBehaviour
{
    [Header("Directional Light")]
    [SerializeField] private Slider m_directionalSlider;
    [SerializeField] private TMP_Text m_directionalValueText;

    [Header("Lamp Light")]
    [SerializeField] private Slider m_lampSlider;
    [SerializeField] private TMP_Text m_lampValueText;

    [Header("Bloom")]
    [SerializeField] private Slider m_bloomSlider;
    [SerializeField] private TMP_Text m_bloomValueText;

    [Header("Buttons")]
    [SerializeField] private Button m_resetButton;

    // ================================
    // パネルの最小化
    // ================================
    [Header("UI - Panel")]
    [SerializeField] private GameObject m_panelContentRoot;      // 中身全部をぶら下げるルート
    [SerializeField] private Button m_panelToggleButton;         // 最小化トグルボタン
    [SerializeField] private TMP_Text m_panelToggleLabel;        // ボタンの表示（▲ / ▼）
    [SerializeField] private RectTransform m_panelRect;          // パネル本体（この Rect を動かす）

    [SerializeField] private float m_expandedHeight = 220f;      // 展開時の高さ（起動時に上書き）
    [SerializeField] private float m_collapsedHeight = 40f;      // 最小化したときの高さ
    [SerializeField] private float m_minimizedBottomMargin = 180f;// 画面下からのオフセット(px)

    private bool m_isMinimized;
    private Vector2 m_expandedAnchoredPos;                       // 展開時の anchoredPosition を記憶

    private CancellationToken m_token;

    // World からもらう参照
    private Light m_directionalLight;
    private Light m_lampLight;
    private Volume m_globalVolume;
    private Bloom m_bloom;

    // デフォルト値
    private float m_dirDefault;
    private float m_lampDefault;
    private float m_bloomDefault;

    // スライダーのレンジ
    private readonly Vector2 m_directionalRange = new Vector2(0f, 10f);
    private readonly Vector2 m_lampRange = new Vector2(0f, 20f);
    private readonly Vector2 m_bloomRange = new Vector2(0f, 20f);

    // カメラまわり
    private Camera m_previewCamera;
    private Vector3 m_defaultCameraPosition;
    private Quaternion m_defaultCameraRotation;

    public void Setup(Debug3DLightWorldRoot worldRoot, CancellationToken token)
    {
        m_token = token;

        // World からライトと Volume をもらう
        if (worldRoot != null)
        {
            m_directionalLight = worldRoot.DirectionalLight;
            m_lampLight = worldRoot.LampLight;
            m_globalVolume = worldRoot.GlobalVolume;
        }

        if (m_globalVolume != null)
        {
            // Bloom を取り出す
            if (!m_globalVolume.profile.TryGet(out m_bloom))
            {
                m_bloom = null;
            }
        }

        // デフォルト値を覚える
        if (m_directionalLight != null)
        {
            m_dirDefault = m_directionalLight.intensity;
        }

        if (m_lampLight != null)
        {
            m_lampDefault = m_lampLight.intensity;
        }

        if (m_bloom != null)
        {
            m_bloomDefault = m_bloom.intensity.value;
        }

        // スライダーの初期化
        SetupSlider(
            m_directionalSlider,
            m_directionalValueText,
            m_directionalRange,
            m_dirDefault,
            value =>
            {
                if (m_directionalLight != null)
                {
                    m_directionalLight.intensity = value;
                }
            });

        SetupSlider(
            m_lampSlider,
            m_lampValueText,
            m_lampRange,
            m_lampDefault,
            value =>
            {
                if (m_lampLight != null)
                {
                    m_lampLight.intensity = value;
                }
            });

        SetupSlider(
            m_bloomSlider,
            m_bloomValueText,
            m_bloomRange,
            m_bloomDefault,
            value =>
            {
                if (m_bloom != null)
                {
                    m_bloom.intensity.value = value;
                }
            });

        // Reset ボタン
        if (m_resetButton != null)
        {
            m_resetButton.onClick.RemoveListener(OnResetClicked);
            m_resetButton.onClick.AddListener(OnResetClicked);
        }

        // カメラセットアップ
        m_previewCamera = null;
        if (worldRoot != null)
        {
            m_previewCamera = worldRoot.PreviewCamera;
        }

        // World 側でカメラが設定されていなければ Camera.main を使う
        if (m_previewCamera == null)
        {
            m_previewCamera = Camera.main;
        }

        if (m_previewCamera != null)
        {
            m_defaultCameraPosition = m_previewCamera.transform.position;
            m_defaultCameraRotation = m_previewCamera.transform.rotation;
        }

        // パネル初期状態（展開状態でスタート）
        if (m_panelRect != null)
        {
            m_expandedAnchoredPos = m_panelRect.anchoredPosition;
            // インスペクタ値より Scene 上の実際の高さを優先
            m_expandedHeight = m_panelRect.sizeDelta.y;
        }

        if (m_panelToggleButton != null)
        {
            m_panelToggleButton.onClick.RemoveListener(OnClickTogglePanel);
            m_panelToggleButton.onClick.AddListener(OnClickTogglePanel);
        }

        m_isMinimized = false;
        ApplyPanelMinimizeState();

        // ラベルを初期値で更新
        UpdateValueLabel(m_directionalValueText, m_directionalSlider);
        UpdateValueLabel(m_lampValueText, m_lampSlider);
        UpdateValueLabel(m_bloomValueText, m_bloomSlider);
    }

    private void SetupSlider(
        Slider slider,
        TMP_Text valueText,
        Vector2 range,
        float defaultValue,
        Action<float> onValueChanged)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = range.x;
        slider.maxValue = range.y;

        var clamped = Mathf.Clamp(defaultValue, range.x, range.y);
        slider.SetValueWithoutNotify(clamped);

        if (valueText != null)
        {
            valueText.text = clamped.ToString("0.00");
        }

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(value =>
        {
            if (valueText != null)
            {
                valueText.text = value.ToString("0.00");
            }

            onValueChanged?.Invoke(value);
        });
    }

    private void OnResetClicked()
    {
        // ライトと Bloom を元の値に戻す
        if (m_directionalSlider != null)
        {
            m_directionalSlider.value = m_dirDefault;
        }

        if (m_lampSlider != null)
        {
            m_lampSlider.value = m_lampDefault;
        }

        if (m_bloomSlider != null)
        {
            m_bloomSlider.value = m_bloomDefault;
        }

        // カメラも初期位置に戻す
        if (m_previewCamera != null)
        {
            m_previewCamera.transform.position = m_defaultCameraPosition;
            m_previewCamera.transform.rotation = m_defaultCameraRotation;
        }
    }

    // ================================
    // パネル最小化まわり
    // ================================
    private void OnClickTogglePanel()
    {
        m_isMinimized = !m_isMinimized;
        ApplyPanelMinimizeState();
    }

    private void ApplyPanelMinimizeState()
    {
        if (m_panelContentRoot != null)
        {
            m_panelContentRoot.SetActive(!m_isMinimized);
        }

        if (m_panelToggleLabel != null)
        {
            m_panelToggleLabel.text = m_isMinimized ? "▲" : "▼";
        }

        if (m_panelRect == null)
        {
            return;
        }

        // 高さを切り替え
        var size = m_panelRect.sizeDelta;
        size.y = m_isMinimized ? m_collapsedHeight : m_expandedHeight;
        m_panelRect.sizeDelta = size;

        // 位置を計算（アンカーが中央の前提）
        var parentRect = m_panelRect.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        if (!m_isMinimized)
        {
            // 展開時は元の位置に戻す
            m_panelRect.anchoredPosition = m_expandedAnchoredPos;
        }
        else
        {
            float parentH = parentRect.rect.height;
            float selfH = m_panelRect.rect.height;
            float margin = m_minimizedBottomMargin;

            // 下から margin 分だけ上に、という y を計算（中央アンカー前提）
            float y = -parentH * 0.5f + selfH * 0.5f + margin;
            m_panelRect.anchoredPosition = new Vector2(m_expandedAnchoredPos.x, y);
        }
    }

    private static void UpdateValueLabel(TMP_Text label, Slider slider)
    {
        if (label == null || slider == null)
        {
            return;
        }

        label.text = slider.value.ToString("0.00");
    }
}

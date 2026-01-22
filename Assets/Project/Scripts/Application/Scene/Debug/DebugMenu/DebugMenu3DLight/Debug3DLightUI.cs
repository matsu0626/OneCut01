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


    private static void UpdateValueLabel(TMP_Text label, Slider slider)
    {
        if (label == null || slider == null)
        {
            return;
        }

        label.text = slider.value.ToString("0.00");
    }
}

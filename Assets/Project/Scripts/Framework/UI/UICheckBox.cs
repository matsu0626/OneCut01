using System;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/UICheckBox")]
public sealed class UICheckBox : Toggle
{
    [Header("サウンド再生設定")]
    [SerializeField]
    private string m_seId = "se_01";

    public Action<bool> OnValueChangedProc;

    protected override void Awake()
    {
        base.Awake();

        // プレイ中だけリスナーを登録
        if (Application.isPlaying)
        {
            onValueChanged.AddListener(OnValueChangedInternal);
        }
    }

    protected override void OnDestroy()
    {
        // プレイ中だけ解除
        if (Application.isPlaying)
        {
            onValueChanged.RemoveListener(OnValueChangedInternal);
        }

        base.OnDestroy();
    }

    private void OnValueChangedInternal(bool isOn)
    {
        PlaySe();
        OnValueChangedProc?.Invoke(isOn);
    }

    private void PlaySe()
    {
        // プレイ中チェック
        if (!Application.isPlaying)
        {
            return;
        }

        if (string.IsNullOrEmpty(m_seId))
        {
            return;
        }

        var sm = SoundManager.Instance;
        if (sm != null)
        {
            sm.PlaySE(m_seId);
        }
    }
}

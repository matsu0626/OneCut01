using UnityEngine;

public sealed class DebugDialogLauncher : SingletonMonoBehaviour<DebugDialogLauncher>
{
    [Header("開く対象の m_debugDialog")]
    [SerializeField] private DebugDialog m_debugDialog;

    [Header("右上タップ領域（うっすら色付きの UI ボタン）")]
    [SerializeField] private UIButton m_tapButton;

    [Header("ダイアログ背面のフルスクリーン BG")]
    [SerializeField] private GameObject m_background;

    [Header("起動時に自動で開くか（デバッグ用）")]
    [SerializeField] private bool m_openOnStart = false;

    protected override void Awake()
    {
        base.Awake();

#if !UNITY_EDITOR && !_DEBUG
        // 本番ビルドでは全部 OFF
        if (m_debugDialog != null)
        {
            m_debugDialog.Close();
        }

        if (m_tapButton != null)
        {
            m_tapButton.gameObject.SetActive(false);
        }

        if (m_background != null)
        {
            m_background.SetActive(false);
        }

        gameObject.SetActive(false);
        return;
#endif

        if (m_debugDialog == null)
        {
            m_debugDialog = GetComponentInChildren<DebugDialog>(true);
        }

        if (m_tapButton == null)
        {
            // 子階層にある Open 用ボタン（OpenBtn）を想定
            m_tapButton = GetComponentInChildren<UIButton>(true);
        }

        // 起動時はダイアログ閉じて BG も非表示
        if (m_debugDialog != null)
        {
            m_debugDialog.InitializeIfNeeded();
            m_debugDialog.Close();
        }

        if (m_background != null)
        {
            m_background.SetActive(false);
        }

        if (m_tapButton != null)
        {
            m_tapButton.OnClickProc += OnTapButtonClicked;
            m_tapButton.gameObject.SetActive(true);
        }

        if (m_openOnStart && m_debugDialog != null)
        {
            m_debugDialog.Open();
            if (m_background != null)
            {
                m_background.SetActive(true);
            }
        }
    }

    private void Update()
    {
#if UNITY_EDITOR || _DEBUG
        if (m_debugDialog == null || m_background == null)
        {
            return;
        }

        // ダイアログの開閉状態に BG の表示を合わせる
        bool isOpen = m_debugDialog.IsOpen;
        if (m_background.activeSelf != isOpen)
        {
            m_background.SetActive(isOpen);
        }
#endif
    }

    private void OnTapButtonClicked()
    {
        if (m_debugDialog == null)
        {
            return;
        }

        if (!m_debugDialog.IsOpen)
        {
            m_debugDialog.Open();
            // Update() 側でも同期されるけど、即座に反映しておく
            if (m_background != null)
            {
                m_background.SetActive(true);
            }
        }
        else
        {
            m_debugDialog.Close();
            if (m_background != null)
            {
                m_background.SetActive(false);
            }
        }
    }
}
